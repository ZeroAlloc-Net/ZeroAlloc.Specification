# Releasing

This repo uses release-please in manifest mode with two components:

```
"."                                    -> ZeroAlloc.Specification   (catch-all)
"src/ZeroAlloc.Specification.Generator" -> ZeroAlloc.Specification.Generator
```

The two packages version independently, and the root `"."` component absorbs any changed path not
matched by the more specific one.

## Why the root component matters

release-please attributes each commit to a package **by the path of the files it changed**. With
one component per `src/` package and nothing at the root, a commit touching no package path mapped
to nothing: every component reported `Considering: 0 commits`, no release PR opened, and the
workflow still went green. **A green merge was not a release.**

With central package management this was not hypothetical — **every dependency bump lives in root
`Directory.Packages.props`, which no `src/` path can see.** `ZeroAlloc.Saga` lost a release exactly
this way: its #131 raised a dependency floor, touched nothing under `src/`, released nothing, and
left the broken dependency live on NuGet while the issue it fixed sat closed with green CI.

Making `"."` the component for `ZeroAlloc.Specification` closes that: orphan paths now fall to the
core package, which is where a repo-wide change belongs anyway.

## Why not a single component

`ZeroAlloc.Saga` and `ZeroAlloc.EventSourcing` collapsed to one component instead, because their
sibling packages carry `ProjectReference`s to the core package. Packing with a global
`-p:PackageVersion` stamped each sibling's own version onto that dependency, so they published
dependency ranges naming versions they were never built against — in one case a version that did
not exist. A single version makes that correct by construction.

That does not apply here. `ZeroAlloc.Specification.Generator` is standalone and declares no
dependency on `ZeroAlloc.Specification`, so there is no ProjectReference to mis-stamp and no reason
to force the two packages into lockstep.

## Verify against NuGet, not against a green tick

Producing no packages is now a hard error, but the final check still belongs to you:

```bash
curl -s https://api.nuget.org/v3-flatcontainer/zeroalloc.specification/index.json
```

## Release tracking

`src/ZeroAlloc.Specification.Generator/AnalyzerReleases.Shipped.md` records the release each analyzer rule first shipped in, and any later change to its category or severity. A new rule goes into `AnalyzerReleases.Unshipped.md`. Changing a shipped rule's severity or category, or removing it, has to be declared there under `### Changed Rules` or `### Removed Rules`, or the build fails. The same move covers every `PublicAPI.Unshipped.txt`: new public API goes there, and removing shipped API is declared with a `*REMOVED*` line.

Nobody moves entries by hand. When release-please opens or updates the release PR, the `ship-release-tracking` job in `.github/workflows/release-please.yml` moves everything unshipped into the Shipped files on that branch, in a `chore: mark analyzer rules and public api shipped in <version>` commit. The `release-tracking` job in CI fails a release PR while anything is still unshipped. Both use the shared [`ship-release-tracking.py`](https://github.com/ZeroAlloc-Net/.github/blob/main/scripts/ship-release-tracking.py). **Before merging a release PR,** check that it has that commit. If it doesn't, run the script with the release version from the root of the release branch and push the result.
