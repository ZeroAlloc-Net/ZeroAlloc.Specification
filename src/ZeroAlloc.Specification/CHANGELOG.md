# Changelog

## [1.1.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/compare/ZeroAlloc.Specification-v1.0.0...ZeroAlloc.Specification-v1.1.0) (2026-05-01)


### Features

* bundle source generator into ZeroAlloc.Specification package ([61a1ad9](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/61a1ad9e6d6366eea4c685ebb7e3ccd4bcc528f7))
* bundle source generator into ZeroAlloc.Specification package ([1f58e5d](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/1f58e5db6951e6d32c1dfcfbe8da515bdc1fad0d))
* lock public API surface (PublicApiAnalyzers + api-compat gate) ([#26](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/issues/26)) ([b42c78a](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/b42c78ae3610f16eb344a36fe0eaea7a9e73a6cf))

## [1.0.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/compare/ZeroAlloc.Specification-v0.3.0...ZeroAlloc.Specification-v1.0.0) (2026-04-28)


### Miscellaneous Chores

* **release:** promote to 1.0.0 stability milestone ([7ca3836](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/7ca38369d9317c85deac605334adbf68243f00ec))

## 1.0.0

Stability milestone — public API of `ZeroAlloc.Specification` is now considered stable. No code changes from 0.3.0; this release marks the transition out of pre-1.0 SemVer.

## [0.3.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/compare/ZeroAlloc.Specification-v0.2.0...ZeroAlloc.Specification-v0.3.0) (2026-03-18)


### Features

* add AndSpecification&lt;TLeft, TRight, T&gt; combinator struct ([2e3887c](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/2e3887c88c271389ebe37ef6d24a1e8b1f808482))
* add implicit Expression conversion to AndSpecification ([cd2f53e](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/cd2f53e2e99cb21d794ccc99273ce76bbee3c9a7))
* add implicit Expression conversion to OrSpecification and NotSpecification ([c0cc095](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/c0cc09587695f866744d52f29d576bdcd83198ff))
* add ISpecification&lt;T&gt; interface and SpecificationAttribute ([1581584](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/158158408ab59a34400b023d0953e6cd93060460))
* add OrSpecification and NotSpecification combinator structs ([212c7e5](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/212c7e5ff0b3f79205b28235b7cc8ba38701d14e))
* add ParameterRebinder for expression composition ([9b3fdc3](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/9b3fdc316537ebb73aa0e8546341167b05beca51))
* add static Spec builder for And/Or/Not ([b1b1a74](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/b1b1a749b8927b2ec16f3cf6ab2d6e8ceee0bc03))
* implicit Expression&lt;Func&lt;T,bool&gt;&gt; conversion on all spec types ([d8f576a](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/d8f576a9419a504cd19576d780935471ee11d0fe))

## [0.2.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/compare/ZeroAlloc.Specification-v0.1.0...ZeroAlloc.Specification-v0.2.0) (2026-03-18)


### Features

* add AndSpecification&lt;TLeft, TRight, T&gt; combinator struct ([2e3887c](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/2e3887c88c271389ebe37ef6d24a1e8b1f808482))
* add implicit Expression conversion to AndSpecification ([cd2f53e](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/cd2f53e2e99cb21d794ccc99273ce76bbee3c9a7))
* add implicit Expression conversion to OrSpecification and NotSpecification ([c0cc095](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/c0cc09587695f866744d52f29d576bdcd83198ff))
* add ISpecification&lt;T&gt; interface and SpecificationAttribute ([1581584](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/158158408ab59a34400b023d0953e6cd93060460))
* add OrSpecification and NotSpecification combinator structs ([212c7e5](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/212c7e5ff0b3f79205b28235b7cc8ba38701d14e))
* add ParameterRebinder for expression composition ([9b3fdc3](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/9b3fdc316537ebb73aa0e8546341167b05beca51))
* add static Spec builder for And/Or/Not ([b1b1a74](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/b1b1a749b8927b2ec16f3cf6ab2d6e8ceee0bc03))
* implicit Expression&lt;Func&lt;T,bool&gt;&gt; conversion on all spec types ([d8f576a](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/d8f576a9419a504cd19576d780935471ee11d0fe))
