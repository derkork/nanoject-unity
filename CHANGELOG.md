# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] 2019-07-04
### Added
* Added a new function `IsDeclared` which allows extensions to check if a certain component is already declared in the context. This is intended for fail-fast behaviour in case a component is about to be declared twice which can result in hard-to-debug errors.

### Changed
* [Breaking Change] The `ScanForComponents` extension method on `DependencyContext` is now named `DeclareAnnotatedComponents`. This fits with the naming of the other methods which all start with  `Declare` and it also better reflects what the method is actually doing.

### Removed
* [Breaking Change] There is no longer a facility for declaring factories. The `DeclareFactory` method has been removed. This feature wasn't really thought out and it didn't fit with the main design goal of simplicity. 

## [1.0.0] 2019-06-23
* Initial release. 
