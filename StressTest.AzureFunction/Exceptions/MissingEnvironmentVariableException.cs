using System;

namespace StressTest.AzureFunction.Exceptions
{
    public class MissingEnvironmentVariableException : Exception
    {
        public MissingEnvironmentVariableException(string variable) : base($"'{variable}' variable is not defined in the system") { }
    }
}