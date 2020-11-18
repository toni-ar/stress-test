using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StressTest.AzureFunction.Interfaces;

namespace StressTest.AzureFunction
{
    public class DataProcessingStressTestTrigger
    {
        private readonly ILogger<DataProcessingStressTestTrigger> _logger;
        private readonly IMessagingService _messagingService;

        public DataProcessingStressTestTrigger(
            ILogger<DataProcessingStressTestTrigger> logger,
            IMessagingService messagingService)
        {
            _logger = logger;
            _messagingService = messagingService;
        }

        private class StressTestParamsDto
        {
            public int ProcessCount { get; set; } = 1;
            public int DelaySeconds { get; set; } = 5;
            public int DurationSeconds { get; set; } = 5;
            public int FileSizeInMb { get; set; } = 20;
            public int PrimeNumberCount { get; set; } = 1000000;
        }

        [FunctionName("DataProcessingStressTestTrigger")]
        public async Task Run(
            [ServiceBusTrigger("stress-test-dp-trigger", Connection = "ServiceBusConnectionString")]
            string message,
            MessageReceiver messageReceiver,
            string lockToken)
        {
            var executionGuid = Guid.NewGuid();
            _logger.BeginScope(new Dictionary<string, object>
            {
                ["DataProcessingStressTest"] = true,
                ["DataProcessingStressTestExecutionGuid"] = executionGuid.ToString()
            });
            
            // We mark ServiceBusTrigger messages as completed so it won't double trigger or get moved to dead letter queue
            await messageReceiver.CompleteAsync(lockToken);

            try
            {
                _logger.LogInformation(
                    $"[DataProcessingStressTestTrigger] {nameof(DataProcessingStressTestTrigger)} function starting...");
                
                _logger.LogInformation($"[DataProcessingStressTestTrigger] DataProcessingStressTestExecutionGuid = {executionGuid.ToString()}");

                var stressTestParams = JsonConvert.DeserializeObject<StressTestParamsDto>(message);

                for (int i = 0; i < stressTestParams.ProcessCount; i++)
                {
                    _logger.LogInformation($"[DataProcessingStressTestTrigger] Sending {i + 1}. message...");
                    await _messagingService.SendJsonMessage(
                        "stress-test-dp",
                        new DataProcessingStressTestDto()
                        {
                            Id = i + 1,
                            StressTestExecutionGuid = executionGuid,
                            DurationSeconds = stressTestParams.DurationSeconds,
                            FileSizeInMb = stressTestParams.FileSizeInMb,
                            PrimeNumberCount = stressTestParams.PrimeNumberCount
                        });
                    await Task.Delay(stressTestParams.DelaySeconds * 1000);
                }

                _logger.LogInformation($"{nameof(DataProcessingStressTestTrigger)} function complete.");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{nameof(DataProcessingStressTestTrigger)} function failed: {ex.Message}.");
            }
        }
    }
}