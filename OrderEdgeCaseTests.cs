using System.Threading.Tasks;
using Xunit;
using Kibo.TestingFramework;

namespace Kibo.LegacyTests
{
    public class OrderEdgeCaseTests
    {
        [Fact]
        public async Task CreateOrder_WithSqlInjectionInTenantHeader_ShouldReturn401Or400()
        {
            var client = new KiboApiClient(tenantHeader: "tenant-abc-123'; DROP TABLE Orders; --");
            var order = new OrderBuilder().WithItems(1).Build();
            var response = await client.PostAsync<Order>("/v1/orders", order);
            // Acceptable: 401 Unauthorized or 400 Bad Request, but not 500
            Assert.True(response.StatusCode == 401 || response.StatusCode == 400, $"Expected 401/400, got {response.StatusCode}");
        }

        [Fact]
        public async Task CreateOrder_WithNegativeUnitPrice_ShouldReturn400()
        {
            var client = new KiboApiClient();
            var item = new LineItemBuilder().WithUnitPrice(-10m).Build();
            var order = new OrderBuilder().WithLineItems(item).Build();
            var response = await client.PostAsync<Order>("/v1/orders", order);
            // Should reject negative price
            Assert.Equal(400, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_WithLongCustomerEmail_ShouldReturn400()
        {
            var client = new KiboApiClient();
            var longEmail = new string('a', 300) + "@example.com";
            var order = new OrderBuilder().WithCustomerEmail(longEmail).WithItems(1).Build();
            var response = await client.PostAsync<Order>("/v1/orders", order);
            // Should reject overly long email
            Assert.Equal(400, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_WithEmptyLineItems_ShouldReturn400()
        {
            var client = new KiboApiClient();
            var order = new OrderBuilder().WithLineItems().Build();
            var response = await client.PostAsync<Order>("/v1/orders", order);
            // Should reject empty line items
            Assert.Equal(400, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_WithUnicodeCharacters_ShouldReturn201()
        {
            var client = new KiboApiClient();
            var order = new OrderBuilder()
                .WithCustomerEmail("测试@例子.公司")
                .WithItems(1)
                .Build();
            var response = await client.PostAsync<Order>("/v1/orders", order);
            // Should accept unicode emails if valid
            Assert.Equal(201, response.StatusCode);
        }
    }
}
