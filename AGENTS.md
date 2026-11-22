# AGENTS.md: ZeroAlloc.Specification

Source-generated specification pattern for .NET 10 and .NET 11. A `[Specification]` `readonly partial struct`
gets `And`, `Or` and `Not` from a Roslyn generator, composing and `IsSatisfiedBy` allocate nothing, and
`ToExpression()` gives EF Core a combined expression tree. Two NuGet packages ship from one version:
`ZeroAlloc.Specification` (runtime library, with the generator bundled inside) and the standalone
`ZeroAlloc.Specification.Generator`.

## Layout

- `src/ZeroAlloc.Specification`: runtime library, targets `net10.0` and `net11.0`, `IsAotCompatible`.
  Public API is tracked by `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`.
- `src/ZeroAlloc.Specification.Generator`: the generator, `netstandard2.0`. Its diagnostics (ZA001 to
  ZA005) are tracked in `AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md`.
- The runtime csproj references the generator as an analyzer and also bundles the generator DLL into its
  own package through the `IncludeGeneratorInPackage` target. Both packages carry the same generator.
- `tests/ZeroAlloc.Specification.Tests`: xunit tests for runtime, generator and EF Core (Sqlite and
  InMemory). It sees internals of both `src` projects through `InternalsVisibleTo`.
- `samples/ZeroAlloc.Specification.AotSmoke`: the NativeAOT smoke app, targets `net11.0`. Not packed.
- `benchmarks/ZeroAlloc.Specification.Benchmarks`: BenchmarkDotNet, compares against Ardalis.Specification.
  Not packed.
- Deviation from the org rule on benchmark frameworks: the benchmark project targets only `net10.0`, and
  the AOT smoke only `net11.0`.

## Build and test

`global.json` pins SDK 11.0.100 with `rollForward: latestMinor`; CI also installs 10.0.x. The test
project targets `net10.0` and `net11.0`, so running it needs the .NET 10 and .NET 11 runtimes. The
`net10.0` run needs the 10 runtime even though the 11 SDK builds it.

CI job `build` (`.github/workflows/ci.yml`):

```bash
dotnet tool restore
VERSION=$(dotnet dotnet-gitversion /output json /showvariable SemVer)
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build --verbosity normal --logger "trx;LogFilePrefix=test-results" --collect "XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults
```

The `build` job then runs a coverage gate (`MarcelRoozekrans/ci-baseline/actions/coverage-gate@v1` over
`TestResults/**/coverage.cobertura.xml`) and uploads to Codecov. The command sequence above does not
reproduce the gate. `codecov.yml` keeps a project drop check of 1 percent.

CI job `aot-smoke` (needs clang and zlib1g-dev):

```bash
dotnet restore samples/ZeroAlloc.Specification.AotSmoke/ZeroAlloc.Specification.AotSmoke.csproj
dotnet publish samples/ZeroAlloc.Specification.AotSmoke/ZeroAlloc.Specification.AotSmoke.csproj -r linux-x64 -c Release -o ./aot-out
./aot-out/ZeroAlloc.Specification.AotSmoke
```

CI job `full` in `benchmarks.yml` (manual dispatch; the automatic `smoke` job uses the shared
`benchmark-smoke.yml`):

```bash
dotnet build benchmarks/ZeroAlloc.Specification.Benchmarks -c Release
dotnet run --project benchmarks/ZeroAlloc.Specification.Benchmarks -c Release --no-build -- --filter "${FILTER:-*}"
```

Other CI gates: `lint-commits` (commitlint, `.commitlintrc.yml`), `api-compat` and `release-tracking`
(shared workflows), and `baseline-checks.yml` (gitleaks, CodeQL, NuGetAudit).

Inner loop, verified locally, no CI job runs these:

```bash
dotnet test tests/ZeroAlloc.Specification.Tests -c Release
dotnet test tests/ZeroAlloc.Specification.Tests -c Release -f net10.0 --filter "FullyQualifiedName~CompositionAllocationTests"
```

Packing: `dotnet pack` fails with ZAVERSION001 or ZAVERSION002 unless the version is passed on the
command line, for example
`SHA=$(git rev-parse --short HEAD); dotnet pack -c Release -p:Version=9.9.9-local.$SHA`. When packing with
`--no-build`, pass the same `-p:Version` to the build step, or the assembly version differs.

CI job `sdk10-consumer` needs a 10.0.1xx SDK; `tests/ZeroAlloc.Specification.Sdk10Smoke/global.json` pins
10.0.100 with `rollForward: latestPatch`, so a newer band cannot stand in for it. It builds and runs a
consumer of the packed packages with Roslyn 5.0:

```bash
dotnet pack src/ZeroAlloc.Specification -c Release -o artifacts -p:Version=9.9.9-local.sha$(git rev-parse --short HEAD)
dotnet pack src/ZeroAlloc.Specification.Generator -c Release -o artifacts -p:Version=9.9.9-local.sha$(git rev-parse --short HEAD)
cd tests/ZeroAlloc.Specification.Sdk10Smoke/Consumer
dotnet run -c Release -p:ZeroAllocSpecificationVersion=9.9.9-local.sha$(git rev-parse --short HEAD)
```

CI job `roslyn-floor` calls the shared `roslyn-floor.yml` from ZeroAlloc-Net/.github: it packs the
solution and fails when a packed DLL references Roslyn above 5.0.0.

## Conventions and gotchas

- The generator references `Microsoft.CodeAnalysis.CSharp` 5.0.0, the compiler of SDK band 10.0.1xx, the
  oldest supported SDK. A compiler older than the Roslyn the DLL references skips the generator with
  `CS9057`, and nothing is generated. The shared Renovate preset holds it below 5.1. If a newer Roslyn API
  is needed, change the code; raise the floor only together with the minimum SDK. The test project may use
  a newer Roslyn.
- `Directory.Build.props` sets `LangVersion` 12, `TreatWarningsAsErrors` true, and `RS0016` and `RS0017`
  as errors. A new or changed public API member in `src/ZeroAlloc.Specification` needs a line in
  `PublicAPI.Unshipped.txt`, or the build fails. Do not edit `PublicAPI.Shipped.txt` by hand: the
  release workflow moves entries.
- A new generator diagnostic needs an entry in `AnalyzerReleases.Unshipped.md` and a documented ID in
  `docs/diagnostics.md`.
- C# 12 means no C# 13 features such as `ref struct` locals in `async` methods. The generator targets
  `netstandard2.0`, so it cannot use newer BCL APIs, and it builds with `EnforceExtendedAnalyzerRules`.
- Emitted code must stay free of LINQ and reflection, since consumers publish with NativeAOT.
- Generator tests compile strings with Roslyn and assert on the emitted source (`GeneratorRunner.cs`),
  not on stored snapshot files. Change the assertions together with the generator.
- Hot path here is `And`, `Or`, `Not` and `IsSatisfiedBy` on struct specs, budget exactly 0 B, gated by
  `CompositionAllocationTests` through `AllocationGate.AssertBudget`. `ToExpression()` builds an
  expression tree and allocates by design; it is outside the 0 B claim. Say so in any doc that cites 0 B.
- A spec that captures state is copied into the composed struct. Its `ToExpression()` copies captured
  fields into locals first, as the README quick start does, so the tree does not close over `this`.

<!-- org-core:start sha256=8faf417fa86f11bd8ad9bc98dfa4683fba44e440291e84c6cb9b0ec3ca0c0e7e (generated from docker-nuc scripts/agents-md/org-core-zeroalloc.md; do not edit here) -->
## Org rules

These rules hold in every ZeroAlloc-Net repo. Each library makes two promises: it works under
NativeAOT and trimming, and its hot path allocates nothing. The 2026-10-11 sweep found 16 of 28 repos
allocating on the hot path, most with docs claiming 0 B.

### Hot path

The operation a consumer runs repeatedly: `Send`, a cache hit, `Validate`, `Map`, `Serialize`, a
resilience call, a state transition, an append, a worker poll, including the idle poll. Startup and
registration are not hot path. For a source generator, the hot path is the code it emits: judge the
snapshots and the generated output.

### Allocation traps the sweep found

| Pattern | Instead |
|---|---|
| `async ValueTask` that only awaits and returns | `return new ValueTask<T>(inner.XAsync(ct));` when the method only forwards a `Task<T>` (return a forwarded `ValueTask<T>` directly). The wrapper stores a synchronous throw in the returned task; the direct return lets it, and the cancellation status of the callee, reach the caller. Keep the `async` wrapper when callers depend on either |
| `Task.WhenAll` over tasks that already completed | skip the await only when every task has `IsCompletedSuccessfully` (`Status == TaskStatus.RanToCompletion` on `netstandard2.0`); otherwise `await Task.WhenAll`, which also reports faults of completed tasks |
| `TagList` or log `params object[]` built before `Enabled`/`IsEnabled` | check first, or `LoggerMessage.Define` |
| event args allocated before checking for subscribers | copy the handler to a local, check, then allocate |
| `new Serializer()` / `new Utf8JsonWriter()` per call | cache stateless instances, reuse writers with `Reset` |
| `GetString()` / `ToString()` round-trips in JSON converters | parse and format through spans |
| `ToArray()` / `ToList()` copies between layers | pass `ReadOnlyMemory<T>` or spans, copy once at most |
| LINQ in emitted code | emit `for` loops |
| `yield` iterator behind a public `GetEnumerator` | public struct enumerator, from the first release |
| `Enum.IsDefined(typeof(E), v)` with a boxed value, `decimal.GetBits(v)` to read the scale | `Enum.IsDefined<E>(v)` when `v` has type `E` (.NET 5 or later, so not on .NET Framework, `netstandard2.0`, `netstandard2.1` or .NET Core 3.1); `v.Scale` for a scale read only (.NET 7 or later, never for the coefficient or sign). Keep the old call on older targets |
| configuration parsed or read per call | parse once and cache |
| an `AsyncLocal` write per call | pass state explicitly |
| a capturing lambda or method-group delegate per call | cache the delegate in a field, or a static lambda |
| a per-key object that is never removed | remove on release; otherwise it is a leak |

Inherent allocations are fine when documented: the consumer's own result, an exception on a failure
path, a one-time cache fill.

### Tests and benchmarks

- Every hot path has an allocation gate test (`AllocationGate` from ZeroAlloc.TestHelpers) that measures
  the whole public call, including any state setup.
- Budgets are exact. If a path allocates, assert that number and comment what it is.
- Test the generated code, not a hand-written stand-in, and cover every shape: each attribute, nested
  collections, and the asynchronous case with a fake that returns pending tasks.
- Benchmarks run in CI through the shared `benchmark-smoke.yml` and target the library's LTS
  frameworks. The benchmark body allocates nothing besides the library itself. Run locally with
  `--inProcess`.
- Publish exact allocations and same-run ratios against a `Baseline`, never absolute timings. Run with
  `--launchCount 3`, pin CPUs with `taskset`, give the spread, and label every table.

### NativeAOT and trimming

- Packable runtime projects, and projects consumers publish, set `IsAotCompatible=true` on modern
  target frameworks. Analyzers and source generators target `netstandard2.0`.
- No reflection or runtime code generation on a supported path. An opt-in reflection fallback carries
  honest `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`. Every suppression must be true;
  remove dead ones.
- The AOT smoke sample publishes with `TreatWarningsAsErrors=true`. If a third-party warning really
  blocks it, use a narrow, justified suppression for that warning. `WarningsNotAsErrors` selects codes,
  not assemblies, so it would also downgrade the same warning from generated code.
- The smoke runs the hot path of every package in the repo. A package without a smoke has unknown AOT
  status. Adapters run against real services in CI service containers; publish-only is a last resort.
  App and template smokes make authenticated requests.

### Docs and shipping

- Docs match measurements. After a hot-path change, re-measure and update `docs/performance.md` and the
  README. Never write "0 B" without a test that asserts it.
- New public API is `feat:`, even for a performance change. Public API changes are additive: never add
  an optional parameter to a shipped signature. A breaking end state goes in a `next-major` issue.
- A behaviour change gets a `## Behaviour change` section in the PR body.
- Conventional Commits with release-please. Subjects start lowercase, body lines stay within 100
  characters, and a body never contains nested parentheses: release-please drops the whole commit.
- Local packs use `9.9.9-local.<sha>`. After a release, verify the package on NuGet.
- Fix review findings in the PR; file an issue only for what cannot go in it.

Full rules, template wiring lessons and the reasons behind each rule: https://github.com/ZeroAlloc-Net/.github/blob/main/AGENTS.md
<!-- org-core:end -->
