# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.0] - 2019-12-17
### Added
* Support for injecting collections of components. For example if you need to have all known instances of `House` injected into some component of yours you previously had to do it manually. Now you can define it as a `IReadOnlyCollection<House>` parameter to your constructor or `[LateInit]` annotated initialization method. 

  ```c#
  public class HouseKeeper {
      public HouseKeeper(IReadOnlyCollection<House> houses) {
          // do something with the houses here.
      }
  }
  ```

  Note that the collection is under control of the `DependencyContext`, so if you call `Resolve` again, the contents of the collection may change. This is also why it is a read-only collection. Also note that list dependencies are always treated as optional. E.g. if you have no `House` declared in the dependency context, you will receive an empty collection. Collections will never be injected as `null` values. Finally, if you have manually declared an `IReadOnlyCollection<House>` this will take precedence over any automatically created collection.

### Changed
* When a context cannot be resolved you will now get better debugging output, showing exactly which parameter of which declared object cannot be resolved.
* It is now possible to declare multiple components of the same type with no or the same qualifier. However when a single component is required that is declared multiple times the qualifier for this component must be unique:

    ```c#
    // declare a few huts and a palace
    context.Declare<House>("hut");
    context.Declare<House>("hut");
    context.Declare<House>("palace")
    
    ...
    class HouseKeeper {
        // this will not work as there is no house without a qualifier
        public HouseKeeper(House house) {
        }
    
        // this will not work as it is not clear which hut should be injected
        public HouseKeeper([Qualifier("hut")] House house) {
        }
    
        // this will work as the palace is unique
        public HouseKeeper([Qualifier("palace")] House house) {
        }
    
        // this will work and you will get all huts
        public HouseKeeper([Qualifier("hut")] IReadonlyCollection<House> allHuts) {
        }
    }   
    ```
* [Breaking Change] Dependency parameters now take type hierarchy into account.  For example:
    ```c#
    class HouseKeeper {
        // this requires a house
        public HouseKeeper(House house) {
        }
    }
    
    abstract class House {
    }
    
    // a palace is a house
    class Palace : House {
    }
    
    // a hut is a house
    class Hut : House {
    }
    
    ...
    context.Declare<Palace>();
    context.Declare<HouseKeeper>();
    // This will work, the HouseKeeper will receive the palace as dependency.
    context.Resolve();
    
    ...
    
    context.Declare<Hut>();
    context.Declare<HouseKeeper>();
    // This will work, the HouseKeeper will receive the Hut as dependency.
    context.Resolve();
    
    ...
    
    context.Declare<Palace>();
    context.Declare<Hut>();
    context.Declare<HouseKeeper>();
    // This will not work, there are now two possible options for a matching house
    // so resolution will fail.
    context.Resolve();
    
    ```
* [Breaking Change] The `DependencyComponent` attribute can now only be used on class declarations. Before it could be used everywhere (even though it would only be evaluated when put on a class declaration).
* [Breaking Change] The `LateInit` attribute can now only be used on method declarations. Before it could be used everywhere (even though it would only be evaluated when put on a method declaration).
* [Breaking Change] Any component that is late-initialized may now only have one method annotated with `LateInit`. This is to avoid ambiguity over which method will actually be called.
* [Breaking Change] Any component that is created through constructor injection may now only have a single constructor. If it needs multiple constructors the constructor that should be used for dependency injection must be marked with the new `Constructor` attribute.

    ```c#
    public class MyClassWithMultipleConstructors {
        // Constructor for testing purposes    
        public MyClassWithMultipleConstructors() {
        }
     
        // Constructor to be used must be annotated with the Constructor attribute.
        [Constructor]      
        public MyClassWithMultipleConstructors(MyDependency dependency) {
        }
    }
    ```
* Improved the documentation where it was less than clear.

### Removed
* [Breaking Change] Removed `IsDeclared` method in `DependencyContext`. Because you can now declare multiple components with the same or no qualifier, it is no longer needed to check if a component with that qualifier has already been declared.

## [2.0.0] 2019-07-04
### Added
* Added a new function `IsDeclared` which allows extensions to check if a certain component is already declared in the context. This is intended for fail-fast behaviour in case a component is about to be declared twice which can result in hard-to-debug errors.

### Changed
* [Breaking Change] The `ScanForComponents` extension method on `DependencyContext` is now named `DeclareAnnotatedComponents`. This fits with the naming of the other methods which all start with  `Declare` and it also better reflects what the method is actually doing.

### Removed
* [Breaking Change] There is no longer a facility for declaring factories. The `DeclareFactory` method has been removed. This feature wasn't really thought out and it didn't fit with the main design goal of simplicity. 

## [1.0.0] 2019-06-23
* Initial release. 
