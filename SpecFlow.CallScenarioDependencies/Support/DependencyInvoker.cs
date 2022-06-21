using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Internal;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Infrastructure;

namespace SpecFlow.CallScenarioDependencies.Support
{
    [Binding]
    public class DependencyInvoker
    {
        private const string DependsOnTagPrefix = "dependsOn:";
        private const string DependencyTag = "dependency";

        private static readonly ConcurrentDictionary<string, Exception> _dependencyExecutions = new();

        private readonly ScenarioContext _scenarioContext;
        private readonly ITestRunner _testRunner;
        private readonly IContextManager _contextManager;

        public class DependencyInvokerException : Exception
        {
            public DependencyInvokerException(string message) : base(message)
            {
            }

            public DependencyInvokerException(string message, Exception inner) : base(message, inner)
            {
            }
        }

        public DependencyInvoker(ITestRunner testRunner, ScenarioContext scenarioContext, IContextManager contextManager)
        {
            _testRunner = testRunner;
            _scenarioContext = scenarioContext;
            _contextManager = contextManager;
        }

        [BeforeScenario(Order = -1)]
        public void CheckDependency()
        {
            var dependsOn = _scenarioContext.ScenarioInfo.ScenarioAndFeatureTags.FirstOrDefault(t => t.StartsWith(DependsOnTagPrefix));
            if (dependsOn != null)
                EnsureDependencyInvoked(dependsOn);
        }

        [BeforeScenario(DependencyTag, Order = -2)]
        public void AvoidDependencyDoubleExecution()
        {
            // this scenario is used as a dependency, we need to make sure that it is not run twice
            if (_scenarioContext.ScenarioInfo.Arguments.Count > 0)
                throw new DependencyInvokerException("Scenario Outlines cannot be used as dependency!");

            string id = GetDependencyId();
            if (!_dependencyExecutions.TryAdd(id, null))
            {
                // was already executed, we re-throw the original error when failed
                var result = _dependencyExecutions[id];
                ReplayResult(result);
            }
        }

        [AfterScenario(DependencyTag, Order = -2)]
        public void RegisterDependencyResult()
        {
            // we remember the test result of the dependency so that we can "replay" it in case of double run
            string id = GetDependencyId();
            if (_scenarioContext.TestError is not IgnoreException)
                _dependencyExecutions.TryUpdate(id, _scenarioContext.TestError, null);
        }

        private string GetDependencyId()
        {
            return $"{_contextManager.FeatureContext.FeatureInfo.Title}.{_scenarioContext.ScenarioInfo.Title}";
        }

        private void EnsureDependencyInvoked(string dependsOn)
        {
            var featureType = DetectFeatureType();
            var featureInstance = CreateFeatureInstance(featureType);

            var testMethod = FindDependencyTestMethod(dependsOn, featureType);

            // stop current scenario execution
            var scenarioInfo = _scenarioContext.ScenarioInfo;
            _contextManager.CleanupScenarioContext();

            var dependencyResult = InvokeDependency(testMethod, featureInstance);

            // restart current scenario execution
            _testRunner.OnScenarioInitialize(scenarioInfo);
            if (dependencyResult != null)
                throw new DependencyInvokerException("The dependency failed", dependencyResult);
        }

        private Exception InvokeDependency(MethodInfo testMethod, object featureInstance)
        {
            var scenarioName = GetScenarioName(testMethod);
            var featureName = GetFeatureName(testMethod);
            var id = $"{featureName}.{scenarioName}";
            Console.WriteLine($"Invoking dependency: {testMethod.Name} ({id})");

            if (_dependencyExecutions.TryGetValue(id, out var dependencyResult))
            {
                ReplayResult(dependencyResult, false);
                return dependencyResult;
            }

            // invoke dependency
            Exception dependencyError = null;
            try
            {
                testMethod.Invoke(featureInstance, Array.Empty<object>());
            }
            catch (TargetInvocationException invocationException)
            {
                dependencyError = invocationException.InnerException;
            }
            catch (Exception ex)
            {
                dependencyError = ex;
            }
            finally
            {
                _testRunner.OnScenarioEnd();
            }
            Console.WriteLine("Invoking dependency done");
            if (dependencyError != null)
                Console.WriteLine("Dependency failed with an error");
            return dependencyError;
        }

        private static void ReplayResult(Exception result, bool throwException = true)
        {
            if (result == null)
            {
                Console.WriteLine("The dependency was already executed and succeeded");
                if (throwException)
                    Assert.Ignore("The dependency was already executed and succeeded");
            }
            else
            {
                Console.WriteLine("The dependency was already executed and failed");
                if (throwException)
                    throw new DependencyInvokerException("The dependency was already executed and failed", result);
            }
        }

        private string GetScenarioName(MethodInfo testMethod)
        {
            var attr = testMethod.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Properties.Get(PropertyNames.Description)?.ToString() ?? testMethod.Name;
        }

        private string GetFeatureName(MethodInfo testMethod)
        {
            var featureType = testMethod.DeclaringType!;
            var attr = featureType.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Properties.Get(PropertyNames.Description)?.ToString() ?? featureType.Name;
        }

        private static MethodInfo FindDependencyTestMethod(string dependencyTag, Type featureType)
        {
            var dependencyName = dependencyTag.Substring(DependsOnTagPrefix.Length);
            var testMethod = featureType.GetMethod(dependencyName);
            if (testMethod == null)
                throw new DependencyInvokerException($"Dependency test method '{dependencyName}' cannot be found");

            if (!testMethod.GetCustomAttributes<CategoryAttribute>().Any(ca => ca.Name.Equals(DependencyTag)))
                throw new DependencyInvokerException($"Dependency '{dependencyName}' does not have @{DependencyTag} tag.");

            return testMethod;
        }

        private object CreateFeatureInstance(Type featureType)
        {
            var featureInstance = Activator.CreateInstance(featureType);
            featureType.GetField("testRunner", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(featureInstance, _testRunner);
            return featureInstance;
        }

        private static Type DetectFeatureType()
        {
            var stackTrace = new StackTrace(false);
            var featureType = stackTrace.GetFrames()
                .Select(sf => sf.GetMethod()?.DeclaringType)
                .FirstOrDefault(t => t?.Name.EndsWith("Feature") ?? false);
            if (featureType == null)
                throw new DependencyInvokerException("Feature type cannot be detected");
            return featureType;
        }
    }
}
