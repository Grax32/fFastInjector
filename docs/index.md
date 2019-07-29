# Project Description

fFastInjector is a high-performing dependency injector, service locator, and/or IOC (inversion of control) container.

It is designed to be very efficient in both lines of code and in actual operations.

# Quick Start For MVC

Install via NuGet.  This command will install both the fFastInjector and the fFastInjector-MVC packages

`install-package fFastInjector-MVC`

Add the following line to your Application_Start method

`fFastInjector.fFastInjectorControllerFactory.RegisterControllerFactory();`

follow the instructions below to set up resolution for your interfaces.

# Quick Start Instructions

Set resolver for interface

`fFastInjector.Injector.SetResolver<MyInterface, MyConcreteClass>();`

Resolve interface.  If no resolver is set for an interface, an exception is thrown.

`var result = Injector.Resolve<MyInterface>();`

Resolve concrete class.  If no resolver is set for a concrete class, this class will be resolved by looking for the constructor with the fewest parameters (preference given to the parameterless constructor).  If there are parameters, fFastInjector will attempt to resolve them as well.  If they cause an infinite loop with their dependencies, fFastInjector will throw an exception.

`var result = Injector.Resolve<MyConcreteClass>();`

Resolve class or interface and additionally set a property.  This first method will simply use fFastInjector to resolve the value for MyProperty based on the type of MyProperty.

`
fFastInjector.Injector
     .SetResolver<MyInterface, MyTestClass>()
     .AddPropertyInjector(v => v.MyProperty);
`

Resolve class or interface and additionally set a property.  This second method allows you to specify and expression that will be evaluated to set MyProperty.

`
fFastInjector.Injector
      .SetResolver<MyInterface, MyTestClass>()       .AddPropertyInjector(v => v.MyOtherProperty, () => new MyPropertyClass());
`
 
