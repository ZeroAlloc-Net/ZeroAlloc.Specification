# ZeroAlloc.Specification — Docs, README, and GitHub Infrastructure Design

**Date:** 2026-03-18

## Problem

The library implementation is complete but has no documentation, no polished README, and no CI/CD pipeline. The repository needs to be publication-ready for the ZeroAlloc-Net GitHub organization.

## Goal

Add plain-markdown Docusaurus-ready documentation, a README matching the ZeroAlloc.Mediator style, and a complete `.github/` folder with CI, release automation, issue templates, and PR template.

## Approach

Option A (chosen): Plain markdown with Docusaurus frontmatter — portable, no build tooling required in this repo, Docusaurus site can be managed separately.

---

## Documentation (`docs/`)

Nine documentation pages + three cookbook recipes under `docs/`:

```
docs/
  intro.md                     ← what it is, why it exists
  getting-started.md           ← install + first spec in 5 minutes
  core-concepts.md             ← ISpecification<T>, struct constraints, zero alloc
  fluent-api.md                ← .And() .Or() .Not() generated methods
  static-builder.md            ← Spec.And() Spec.Or() Spec.Not()
  expression-composition.md    ← ToExpression(), EF Core, ParameterRebinder
  diagnostics.md               ← ZA001–ZA004 table + remediation
  generator-internals.md       ← how the Roslyn generator works
  performance.md               ← benchmarks, allocation comparison
  cookbook/
    ef-core-repository.md      ← spec in a repository pattern with EF Core
    combining-specs.md         ← complex multi-spec chains
    stateless-caching.md       ← static cached expression convention
```

Each file has Docusaurus frontmatter (`id`, `title`, `sidebar_label`, `sidebar_position`).

---

## README.md

Structure matching ZeroAlloc.Mediator:

1. **Header** — title + one-line description
2. **Badges** — NuGet (core + generator), build status, license
3. **Install** — two `dotnet add package` commands (tabbed: .NET CLI / Package Manager)
4. **Quick Example** — `ActiveUserSpec` + `PremiumUserSpec` with fluent + static builder composition and EF Core usage
5. **Features** — bullet list
6. **Documentation** — table linking to docs pages
7. **License** — MIT

---

## `.github/` Infrastructure

```
.github/
  workflows/
    ci.yml                 ← commitlint + build + test (ubuntu-latest, .NET 8)
    release.yml            ← release-please + NuGet publish (both packages)
  ISSUE_TEMPLATE/
    bug_report.yml         ← structured bug form
    feature_request.yml    ← structured feature form
  PULL_REQUEST_TEMPLATE.md ← checklist: tests, docs, conventional commit
  commitlint.config.js     ← conventional commits config
```

Root-level release config:

```
release-please-config.json          ← manifest strategy, both packages
.release-please-manifest.json       ← initial versions (0.1.0)
```

### CI (`ci.yml`)

Triggers: push to `main`, all PRs.

Steps:
1. Checkout
2. Setup .NET 8
3. `dotnet build --configuration Release`
4. `dotnet test --configuration Release --no-build`
5. commitlint (on PR only, via `wagoid/commitlint-github-action`)

### Release (`release.yml`)

Triggers: push to `main`.

Jobs:
1. `release-please-action@v4` → outputs `release_created`, `tag_name`
2. On release: build, pack both projects, push to NuGet with `NUGET_API_KEY` secret

### Conventional Commits

`commitlintrc.yml` with `@commitlint/config-conventional` extending the default ruleset.

---

## Centralized Build Props

`Directory.Build.props` at solution root — centralizes `Nullable`, `LangVersion`, `ImplicitUsings` for all projects:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <LangVersion>12</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```
