using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace StressTest.AzureFunction
{
    public class DataProcessingStressTest
    {
        private readonly ILogger<DataProcessingStressTest> _logger;

        public DataProcessingStressTest(
            ILogger<DataProcessingStressTest> logger)
        {
            _logger = logger;
        }

        [FunctionName("DataProcessingStressTest")]
        public async Task Run(
            [ServiceBusTrigger("stress-test-dp", Connection = "ServiceBusConnectionString")]
            string message,
            MessageReceiver messageReceiver,
            string lockToken)
        {
            // We mark ServiceBusTrigger messages as completed so it won't double trigger or get moved to dead letter queue
            await messageReceiver.CompleteAsync(lockToken);

            try
            {
                var stressTestDto = JsonConvert.DeserializeObject<DataProcessingStressTestDto>(message);

                _logger.BeginScope(new Dictionary<string, object>
                {
                    ["DataProcessingStressTest"] = true,
                    ["DataProcessingStressTestId"] = stressTestDto.Id,
                    ["DataProcessingStressTestExecutionGuid"] = stressTestDto.StressTestExecutionGuid.ToString()
                });

                _logger.LogInformation(
                    $"[DataProcessingStressTest] {nameof(DataProcessingStressTest)} function starting...");


                _logger.LogInformation($"[DataProcessingStressTest] Started | Id = {stressTestDto.Id} ");

                DoSomething(stressTestDto);

                _logger.LogInformation($"[DataProcessingStressTest] Finished | Id = {stressTestDto.Id} ");

                _logger.LogInformation($"{nameof(DataProcessingStressTest)} function complete.");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{nameof(DataProcessingStressTest)} function failed: {ex.Message}.");
            }
        }

        private void DoSomething(DataProcessingStressTestDto stressTestDto)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _logger.LogInformation($"Process should end after ~{stressTestDto.DurationSeconds} seconds");
            while (timer.Elapsed.TotalSeconds < stressTestDto.DurationSeconds)
            {
                var tempFileName = GetTempFileName();
                WriteRandomData(stressTestDto.FileSizeInMb, tempFileName);
                _logger.LogInformation($"Finding {stressTestDto.PrimeNumberCount}. prime number...");
                long nthPrime = FindPrimeNumber(stressTestDto.PrimeNumberCount);
                _logger.LogInformation($"{stressTestDto.PrimeNumberCount}. prime number is: {nthPrime}");
                _logger.LogInformation($"Deleting `{tempFileName}`");
                File.Delete(tempFileName);
                _logger.LogInformation($"File deleted: '{tempFileName}'");
            }

            timer.Stop();
        }

        private void WriteRandomData(int sizeInMb, string fileName)
        {
            byte[] data = new byte[sizeInMb * 1024 * 1024];
            Random rng = new Random();
            rng.NextBytes(data);
            _logger.LogInformation($"Writing random {sizeInMb}MB data...");
            File.WriteAllBytes(fileName, data);
            _logger.LogInformation($"Data successfully written to the disk.");
        }

        private string GetTempFileName()
        {
            var guid = Guid.NewGuid().ToString();
            var fileName = Path.Combine(Path.GetTempPath(), $"{guid}.tmp");
            _logger.LogInformation($"Creating temp at {fileName}");
            return fileName;
        }

        private long FindPrimeNumber(int n)
        {
            int count = 0;
            long a = 2;
            while (count < n)
            {
                long b = 2;
                int prime = 1;
                while (b * b <= a)
                {
                    if (a % b == 0)
                    {
                        prime = 0;
                        break;
                    }

                    b++;
                }

                if (prime > 0)
                {
                    count++;
                }

                a++;
            }

            return (--a);
        }
    }
}