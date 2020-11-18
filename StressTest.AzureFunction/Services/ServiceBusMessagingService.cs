using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using StressTest.AzureFunction.Interfaces;

namespace StressTest.AzureFunction.Services
{
    public class ServiceBusMessagingService : IMessagingService
    {
        private readonly ServiceBusConnection _serviceBusConnection;
        private readonly RetryPolicy _retryPolicy;
        
        private static class MessageContentType
        {
            public const string Plain = "text/plain";

            public const string Xml = "application/xml";
        
            public const string Json = "application/json";
        }

        public ServiceBusMessagingService(ServiceBusConnection serviceBusConnection)
        {
            _serviceBusConnection = serviceBusConnection;
            _retryPolicy = new RetryExponential(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMinutes(1),
                10);
        }

        public Task SendJsonMessage(string messageQueue, object message)
        {
            var binaryMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            return SendMessage(messageQueue, binaryMessage, MessageContentType.Json);
        }

        public async Task SendMessage(string messageQueue, byte[] messageBody, string contentType = "")
        {
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = MessageContentType.Json;
            }

            var queueClient = new QueueClient(_serviceBusConnection, messageQueue.ToString(), ReceiveMode.PeekLock, _retryPolicy);
            var message = new Message(messageBody) {ContentType = contentType};
            await queueClient.SendAsync(message);
        }
    }
}