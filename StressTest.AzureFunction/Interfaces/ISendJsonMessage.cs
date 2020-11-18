using System.Threading.Tasks;

namespace StressTest.AzureFunction.Interfaces
{
    public interface IMessagingService
    {
        Task SendJsonMessage(string messageQueue, object message);
        Task SendMessage(string messageQueue, byte[] messageBody, string contentType = "");
    }
}