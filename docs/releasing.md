# Releasing

This repo uses release-please in manifest mode with one component, `"."`, released as
`ZeroAlloc.Specification`. Both packages, `ZeroAlloc.Specification` and
`ZeroAlloc.Specification.Generator`, ship at its version, under tags such as
`ZeroAlloc.Specification-v1.3.2`.

## Why the root component matters

release-please attributes each commit to a package **by the path of the files it changed**. With
one component per `src/` package and nothing at the root, a commit touching no package path mapped
to nothing: every component reported `Considering: 0 commits`, no release PR opened, and the
workflow still went green. **A green merge was not a release.**

With central package management this was not hypothetical — **every dependency bump lives in root
`Directory.Packages.props`, which no `src/` path can see.** `ZeroAlloc.Saga` lost a release exactly
this way: its #131 raised a dependency floor, touched nothing under `src/`, released nothing, and
left the broken dependency live on NuGet while the issue it fixed sat closed with green CI.

A single root component closes that: every changed path counts toward the one release.

## Why one version for both packages

Independent versions left no single version at build time, so the build could not stamp the
assemblies, and the generator copy bundled in `ZeroAlloc.Specification` could differ from the
standalone `ZeroAlloc.Specification.Generator` package. One version makes both correct by
construction. See #64.

## Publishing

The `publish` job in `.github/workflows/release-please.yml` calls the shared
[`publish.yml`](https://github.com/ZeroAlloc-Net/.github/blob/main/.github/workflows/publish.yml).
It builds the release tag, packs every project under `src/` that MSBuild evaluates as packable,
checks the version inside each package, pushes to nuget.org and GitHub Packages, and then waits
until nuget.org serves every package at the release version.

If a release was tagged but not published, rescue it with
`gh workflow run publish-from-manifest.yml`, which runs the same workflow against the tag of the
manifest version. Add `-f dry-run=true` to build, pack and verify without pushing.

## Release tracking

`src/ZeroAlloc.Specification.Generator/AnalyzerReleases.Shipped.md` records the release each analyzer rule first shipped in, and any later change to its category or severity. A new rule goes into `AnalyzerReleases.Unshipped.md`. Changing a shipped rule's severity or category, or removing it, has to be declared there under `### Changed Rules` or `### Removed Rules`, or the build fails. The same move covers every `PublicAPI.Unshipped.txt`: new public API goes there, and removing shipped API is declared with a `*REMOVED*` line.

Nobody moves entries by hand. When release-please opens or updates the release PR, the `ship-release-tracking` job in `.github/workflows/release-please.yml` moves everything unshipped into the Shipped files on that branch, in a `chore: mark analyzer rules and public api shipped in <version>` commit. The `release-tracking` job in CI fails a release PR while anything is still unshipped. Both use the shared [`ship-release-tracking.py`](https://github.com/ZeroAlloc-Net/.github/blob/main/scripts/ship-release-tracking.py). **Before merging a release PR,** check that it has that commit. If it doesn't, run the script with the release version from the root of the release branch and push the result.
