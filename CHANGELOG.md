# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/).

## [1.1.3] - Unreleased

### Fixed
- Values at array index 0, and every value nested under it (for example `Urls:0` or
  `Endpoints:0:Url`), were never transformed. All array elements are now resolved.

### Added
- Nested placeholders such as `{Urls:{Environment}}`: the inner placeholder resolves first.
- `ConfigTransformer.MaxPasses` constant documenting the pass limit.
- XML documentation, SourceLink and a symbols package (`.snupkg`).
- Unit tests and CI.

### Changed
- `Transform()` throws `ArgumentNullException` for a null builder.
- The README now documents the reload and escaping limitations accurately.

## [1.1.2]

- Last release published before the source was made public.
