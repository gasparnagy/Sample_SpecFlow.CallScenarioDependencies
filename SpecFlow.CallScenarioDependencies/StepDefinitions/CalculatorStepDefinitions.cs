using System;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace SpecFlow.CallScenarioDependencies.StepDefinitions
{
    [Binding]
    public sealed class CalculatorStepDefinitions
    {
        private int _firstNumber;
        private int _secondNumber;
        private int _result;

        [BeforeScenario]
        public void SampleBeforeScenario()
        {
            Console.WriteLine("SampleBeforeScenario");
        }

        [AfterScenario]
        public void SampleAfterScenario()
        {
            Console.WriteLine("SampleAfterScenario");
        }

        [Given("the first number is (.*)")]
        public void GivenTheFirstNumberIs(int number)
        {
            _firstNumber = number;
        }

        [Given("the second number is (.*)")]
        public void GivenTheSecondNumberIs(int number)
        {
            _secondNumber = number;
        }

        [When("the two numbers are added")]
        public void WhenTheTwoNumbersAreAdded()
        {
            _result = _firstNumber + _secondNumber; 
        }

        [Then("the result should be (.*)")]
        public void ThenTheResultShouldBe(int expectedResult)
        {
            Assert.AreEqual(expectedResult, _result);
        }
    }
}