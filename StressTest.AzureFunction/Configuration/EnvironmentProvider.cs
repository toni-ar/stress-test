using System;
using Microsoft.Extensions.DependencyInjection;
using StressTest.AzureFunction.Exceptions;

namespace StressTest.AzureFunction.Configuration
{
    public class EnvironmentProvider
    {
        private const string AZURE_FUNCTIONS_ENVIRONMENT = "AZURE_FUNCTIONS_ENVIRONMENT";

        private readonly ApplicationEnvironment? _overrideEnvironment;

        private readonly string _variableEnvironment;

        [ActivatorUtilitiesConstructor]
        public EnvironmentProvider()
        {
            _overrideEnvironment = null;
            _variableEnvironment = System.Environment.GetEnvironmentVariable(AZURE_FUNCTIONS_ENVIRONMENT);
        }

        public EnvironmentProvider(ApplicationEnvironment? overrideEnvironment = null)
        {
            _overrideEnvironment = overrideEnvironment;
            _variableEnvironment = System.Environment.GetEnvironmentVariable(AZURE_FUNCTIONS_ENVIRONMENT);
        }

        public ApplicationEnvironment Environment()
        {
            if (_overrideEnvironment != null)
            {
                return _overrideEnvironment.Value;
            }

            if (!IsVariableDefined(_variableEnvironment))
            {
                throw new MissingEnvironmentVariableException(AZURE_FUNCTIONS_ENVIRONMENT);
            }

            if (_variableEnvironment == "Development")
            {
                return ApplicationEnvironment.Development;
            }

            if (_variableEnvironment == "MOCK")
            {
                return ApplicationEnvironment.MOCK;
            }

            if (_variableEnvironment == "TEST")
            {
                return ApplicationEnvironment.TEST;
            }

            if (_variableEnvironment == "UAT")
            {
                return ApplicationEnvironment.UAT;
            }

            if (_variableEnvironment == "Production")
            {
                return ApplicationEnvironment.Production;
            }

            throw new NotSupportedException();
        }

        private bool IsVariableDefined(string variableEnvironment)
        {
            return !String.IsNullOrEmpty(variableEnvironment);
        }
    }
}