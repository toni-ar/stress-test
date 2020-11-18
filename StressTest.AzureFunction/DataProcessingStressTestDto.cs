using System;

namespace StressTest.AzureFunction
{
    public class DataProcessingStressTestDto
    {
        public int Id { get; set; }
        public Guid StressTestExecutionGuid { get; set; } = Guid.Empty;
        public int DurationSeconds { get; set; } = 1;
        public int FileSizeInMb { get; set; } = 20;
        public int PrimeNumberCount { get; set; } = 1000000;
    }
}