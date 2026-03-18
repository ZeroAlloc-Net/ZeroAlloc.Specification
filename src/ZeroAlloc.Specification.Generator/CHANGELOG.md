# Changelog

## [0.2.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/compare/ZeroAlloc.Specification.Generator-v0.1.0...ZeroAlloc.Specification.Generator-v0.2.0) (2026-03-18)


### Features

* add incremental source generator skeleton with And/Or/Not method emission ([ab1a974](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/ab1a974775c856b4a81214d62b8e3c0b2b5b565f))
* add ZA001-ZA004 compile-time diagnostics to generator ([ad377e2](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/ad377e27e8f1a30a6c8e871fa0d55602f9d4e0d6))
* generator emits implicit Expression conversion operator ([68127e8](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/68127e879b0a655bf9a20bc1cb32cc325014f381))
* implicit Expression&lt;Func&lt;T,bool&gt;&gt; conversion on all spec types ([d8f576a](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/d8f576a9419a504cd19576d780935471ee11d0fe))


### Bug Fixes

* add Location exclusion comment and analyzer release tracking files ([d3a520e](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/d3a520e69e323aff0f93b1bb2b38ce066350c231))
* store Location in SpecificationInfo (excluded from equality) and emit ZA001 for non-struct types ([47f7925](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/47f7925a40c9e0351f3ee98125e8024f07f6f700))
* strengthen Or assertion and scope integration test fixtures to internal ([9121a4f](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/9121a4fde8d625946beb4f7088293ff93671ab2b))
* tighten ISpecification namespace check and remove Location from SpecificationInfo for incremental caching ([cc470cc](https://github.com/ZeroAlloc-Net/ZeroAlloc.Specification/commit/cc470cc89d605b43b4ece53de8a9001b4a314bca))
