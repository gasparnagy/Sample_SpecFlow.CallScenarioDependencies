@featureTag
Feature: Calculator

#The scenarios that are used as dependency has to be marked with @dependency
@dependency
Scenario: 01 Add two numbers - dependency
	Given the first number is 50
	And the second number is 70
	When the two numbers are added
	Then the result should be 120

#To specify the dependency, you need to use the test name (that is displayed in the Test Explorer)
@dependsOn:_01AddTwoNumbers_Dependency
Scenario: 02 Add other two numbers - dependent
	Given the first number is 5
	And the second number is 7
	When the two numbers are added
	Then the result should be 12

#A scenario can be a dependency and dependent on another at the same time
@dependsOn:_01AddTwoNumbers_Dependency
@dependency
Scenario: 03 Add other two numbers - dependent and dependency
	Given the first number is 3
	And the second number is 4
	When the two numbers are added
	Then the result should be 7

@dependsOn:_03AddOtherTwoNumbers_DependentAndDependency
Scenario: 04 Add other two numbers - dependent on a dependent
	Given the first number is 1
	And the second number is 2
	When the two numbers are added
	Then the result should be 3
