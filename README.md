# nanoject
## What it is and what it is not
Nanoject is a **minimal** solution for providing dependency injection for your Unity projects. Minimal really is the keyword here. I deliberately do not call it a framework because it really is just a single class to interact with and a few attributes to control the process.
 
 Nanoject allows you to use the dependency injection pattern without creating a ton of cognitive overhead and using arcane magic behind the scenes. It may not have all the features that other frameworks like Zenject have, but its workable for many scenarios and it is very easy to reason about what it is doing. 

## Installation

In order to install this package to your Unity project, open `Packages\manifest.json` and add the following dependency:

```json
"dependencies" : {
    "com.ancientlightstudios.nanoject": "https://github.com/derkork/nanoject-unity.git#3.0.0"
}
```

## Basic Usage

Create a dependency context, declare your dependencies and resolve the context. 

```c#
// new context
var context = new DependencyContext();

// declare the objects that have dependencies
// to each other, so the dependency context
// will know what classes exist and what dependencies
// they have on each other. Declaration order does
// not matter because dependencies are resolved
// later when all objects are declared

context.Declare<MyClass>();
context.Declare<MyOtherClass>();

// this will instantiate all declared dependencies
// and inject required dependencies into all objects
// using constructor injection 

context.Resolve();

```

## How can I ...

### Tell Nanoject what dependencies my class has?

You declare dependencies by putting them as constructor arguments. This allows you to see at a glance what dependencies an object has and also has the advantage that you cannot actually construct an object without supplying all of its dependencies. It also simplifies unit testing.

```c#
class MyClass {
    // MyClass needs an object of MyOtherClass to be constructed.
    public MyClass(MyOtherClass otherClass) {
    }
}
```

### Declare dependencies to objects where I have no control over the lifecycle?
Some objects like `MonoBehaviour`s may be created by the runtime and you have no control over their lifecycle. I this case letting Nanoject create the object will not work. Therefore you can declare an actual instance of an object instead of just its type.

```c#
// MyOtherClass is a MonoBehaviour so grab the instance from Unity
var myOtherClassInstance = (MyOtherClass) FindObjectOfType(typeof(MyOtherClass));

// manually declare the object that was constructed elsewhere
context.Declare(myOtherClassInstance); 
context.Declare<MyClass>();

// will inject myOtherClassInstance into a new instance of MyClass
context.Resolve();
```

If this object has dependencies you will need to create a late init method, because constructor injection will not work in this scenario because the object has already been created. A late init method works similar to a constructor, so you declare all dependencies as parameters of the late init method. Finally you add the `[LateInit]` attribute to let `DependencyContext` know that this object needs late initialization:

```c#
class MyOtherClass : MonoBehaviour {
    private PlayerService _playerService;
    
    // this method will be called when the context is resolved. You can name it
    // however you like, just be sure to add the [LateInit] attribute.
    [LateInit]
    public void MyLateInitMethod(PlayerService playerService) {
       _playerService = playerService;
    }
```

If you have a lot of `MonoBehaviours` in your scene that you want to quickly add to a dependency context, have a look at the [nanoject-unity-monobehaviours](https://github.com/derkork/nanoject-unity-monobehaviours/) extension, which can help with this.

### Avoid having to declare a bazillion objects?

There is a facility for scanning for objects. Simply put the `DependencyComponent` attribute on your class, to mark it as a component that should be declared automatically. Then call `DeclareAnnotatedComponents` which will scan the loaded assemblies for components with this attribute and declare them.

```c#
// annotate as component to be scanned
[DependencyComponent]
class MyClass {
    public MyClass(MyOtherClass otherClass) {
    }
}

// declare all annotated instead of calling Declare a thousand times
context.DeclareAnnotatedComponents();
// resolve the context
context.Resolve();
``` 

### Have multiple objects of the same class?

If you declare multiple objects of the same class Nanoject will not know which one to inject. You can use qualifiers to let Nanoject know what you want: 

```c#
// this is a house
class House {
    public House(string name) {
    }
}

var hut = new House("hut");
var palace = new House("palace");
context.DeclareQualified("hut", hut);
context.DeclareQualified("palace", palace);

// this is a peasant, he should live in the "hut" house
class Peasant {
    public Peasant([Qualifier("hut")] House house) {
    }
}
context.Declare<Peasant>();


// this is a king, he should live in the "palace" house.
class King {
    public King([Qualifier("palace")] House house) {
    }
}    

context.Declare<King>();
context.Resolve();

// now the king has the "palace" house
var king = context.Get<King>();
// assert king.house == palace;

// and the peasant has the "hut" house
var peasant = context.Get<Peasant>();
// assert peasant.house == hut;
```
Starting with version 3.0.0 you can now also inject all declared components of a certain type. To do this, simply inject an `IReadOnlyCollection<InterestingType>`. For example:

```c#
class Janitor {
    public Janitor(IReadOnlyCollection<House> allHouses) {
    }
}

context.Declare<Janitor>();
context.Resolve();

// now the Janitor instance has been given a list of all houses 
// known in DependencyContext. If no houses are known, then the list 
// will simply be empty.
```

It is also possible to combine `[Qualifier]` with injected lists:

```c#
class TaxCollector {
    public TaxCollector(
        [Qualifier("palace")] IReadOnlyCollection<House> palaces,
        [Qualifier("hut")] IReadOnlyCollection<House> huts) {
    }
}

context.Declare<TaxCollector>();
context.Resolve();

// now the TaxCollector instance will get all Houses that have
// been declared with a "palace" qualifier into the palaces collection
// and all that have been declared with a "hut" qualifier into the hut 
// collection.
```

### Get an object out of the dependency context?

You should avoid this if you can but especially in bootstrapping situations you sometimes need it.  

```c#
// make sure the context is resolved
context.Resolve();

var myClassInstance = context.Get<MyClass>();
```

In addition there is also a function that lets you get all objects of a certain type. 

```c#
// get all houses
var houses = context.GetAll<House>();
```

Don't use this facility to implement some kind of service locator pattern, this is going to bite you hard. 


### Resolve cyclic dependencies?
You don't. Cyclic dependencies just make things very very complicated so avoid having them.


