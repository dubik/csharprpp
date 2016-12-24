[![Build status](https://ci.appveyor.com/api/projects/status/fvxuhtuk6bmuyp6p/branch/master?svg=true)](https://ci.appveyor.com/project/dubik/csharprpp/branch/master)


## CSharpRpp

Compiler which implements subset of Scala grammar and uses CLR as a backend.

### Features

Supported (usually with some limitations):
  * Generic classes
  * Variance annotations
  * Upper bounds
  * Inner classes and abstract types
  * Implicit and explicit tuples
  * Type inference (rather limited but works pretty well for my use cases)
  * Pattern matching

### Build
  Open `CSharpRpp.sln` in Visual Studio 2015 and compile for `Debug` or `Release`.

  `BufferCompiler` - Can be used to invoke and debug compiler within Visual Studio
  `CSharpRpp` - Compiler executable
  `CSharpRppTest` - Unit/Functional tests
  `RppRuntime` - Bridge between .NET functions and Rpp
  `RppStdlib` - Rpp standard library


### Test
  Use Visual Studio test discovery window to launch tests.
