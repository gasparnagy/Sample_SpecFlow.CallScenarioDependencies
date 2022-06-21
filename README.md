# SpecFlow.CallScenarioDependencies - Sample application to demonstrate how to invoke other scenarios as a dependency

*Warning:* Making scenarios dependent on each-other increases coupling and makes maintanance harder. This sample application was created to workaround legacy dependency issues.

The sample supports NUnit. 

For usage check the [Calculator.feature](SpecFlow.CallScenarioDependencies/Features/Calculator.feature) file.

The dependency management logic is implemented in [DependencyInvoker.cs](SpecFlow.CallScenarioDependencies/Support/DependencyInvoker.cs).

## License

The sample application is licensed under the [MIT license](LICENSE).

Copyright (c) 2022 Spec Solutions and Gaspar Nagy, https://www.specsolutions.eu
