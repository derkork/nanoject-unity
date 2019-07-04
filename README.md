# nanoject
## What it is and what it is not
Nanoject is a **minimal** solution for providing dependency injection for your Unity projects. Minimal really is the keyword here. I deliberately do not call it a framework because it really is just a single class with less than 300 lines of code and some attributes.
 
 Nanoject allows you to use the dependency injection pattern without creating a ton of cognitive overhead and using arcane magic behind the scenes. It may not have all the features that other frameworks like zenject have, but its workable for many scenarios and it is very easy to reason about what it is doing. 

## Installation

In order to install this package to your Unity project, open `Packages\manifest.json` and add the following dependency:

```json
"dependencies" : {
    ...
    "com.ancientlightstudios.nanoject": "https://github.com/derkork/nanoject-unity.git#2.0.0"
    ...
}
```

## Basic Usage

Create a dependency context, declare your dependencies and resolve the context. 

```csharp
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

```csharp
class MyClass {
    // MyClass needs an object of MyOtherClass to be constructed.
    public MyClass(MyOtherClass otherClass) {
    }
}
```

### Declare dependencies to objects where I have no control over the lifecycle?
Some objects like `MonoBehaviour`s may be created by the runtime and you have no control over their lifecycle. I this case letting Nanoject create the object will not work. Therefore you can declare an actual instance of an object instead of just its type.

```csharp
// MyOtherClass is a MonoBehaviour so grab the instance from Unity
var myOtherClassInstance = (MyOtherClass) FindObjectOfType(typeof(MyOtherClass));

// manually declare the object that was constructed elsewhere
context.Declare(myOtherClassInstance); 
context.Declare<MyClass>();

// will inject myOtherClassInstance into a new instance of MyClass
context.Resolve();
```

### Avoid having to declare a bazillion objects?

There is a facility for scanning for objects. Simply put the `DependencyComponent` attribute on your class, to mark it as a component that should be declared automatically. Then scan for components.

```csharp
// declare as component to be scanned
[DependencyComponent]
class MyClass {
    public MyClass(MyOtherClass otherClass) {
    }
}

// scan instead of calling Declare a thousand times
context.ScanForComponents();
// resolve the context
context.Resolve();
``` 

### Have multiple objects of the same class?

You will need to use a qualifier to let Nanoject know which object is required. 

```csharp
// this is a house
class House {
    public House(string name) {
    }
}

// this is a peasant, he should live in the "hut" house
class Peasant {
    public Peasant([Qualifier("hut")] House house) {
    }
}

// this is a king, he should live in the "palace" house.
class King {
    public King([Qualifier("palace")] House house) {
    }
}    

var palace = new House("Palace");
// declare the palace under the "palace" qualifier
context.Declare(palace, "palace"); 

var hut = new House("hut");
// declare the hut under the "hut" qualifier
context.Declare(hut, "hut");

// now declare the peasant and the king
context.Declare<Peasant>();
context.Declare<King>();

context.Resolve();

// now the king has the "palace" house
var king = context.Get<King>();
// and the peasant has the "hut" house
var hut = context.Get<Peasant>();
```

### Get an object out of the dependency context?

You should avoid this if you can but especially in bootstrapping situations you sometimes need it.  

```csharp
// make sure the context is resolved
context.Resolve();

var myClassInstance = context.Get<MyClass>();
```

In addition there is also a function that lets you get all objects of a certain type. 

```csharp
// get all houses
var houses = context.GetAll<House>();
```

Don't use this facility to implement some kind of service locator pattern, this is going to bite you hard. 


### Resolve cyclic dependencies?
You don't. Cyclic dependencies just make things very very complicated so avoid having them.


