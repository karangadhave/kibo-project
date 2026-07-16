using System.Threading.Tasks;
using Xunit;
using Kibo.TestingFramework;

namespace Kibo.LegacyTests
{
    public class OrderPerformanceTests
    {
        [Fact]
        public async Task CreateOrder_ResponseTime_ShouldBeUnder500ms()
        {
            var client = new KiboApiClient(enableLogging: true);
            var order = new OrderBuilder().WithItems(1).Build();
            var response = await client.PostAsync<Order>("/v1/orders", order);
            Assert.Equal(201, response.StatusCode);
            Assert.True(response.ElapsedMs < 500, $"Expected <500ms, got {response.ElapsedMs}ms");
            // Diagnostics
            if (response.RequestLog != null && response.ResponseLog != null)
            {
                System.Console.WriteLine(response.RequestLog);
                System.Console.WriteLine(response.ResponseLog);
            }
        }
    }
}
