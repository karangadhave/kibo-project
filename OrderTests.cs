using System.Net;
using System.Threading.Tasks;
using Kibo.TestingFramework;

namespace Kibo.LegacyTests;

/// <summary>
/// ⚠️ WARNING — This test class is INTENTIONALLY written with poor practices.
/// It represents a "legacy" test suite that a Lead SDET candidate must refactor.
///
/// Known anti-patterns embedded here:
///   • HttpClient created directly in every test method (no reuse / disposal)
///   • Hardcoded base URL (http://localhost:5000) copy-pasted everywhere
///   • x-kibo-tenant header logic duplicated in every method
///   • Raw JSON strings built inline instead of using a builder/model
///   • Thread.Sleep(6000) used to wait for async status changes (brittle & slow)
/// </summary>
public class OrderTests
{
    [Fact]
    public async Task CreateOrder_ReturnsSuccess()
    {
        var client = new KiboApiClient();
        var order = new OrderBuilder()
            .WithCustomerEmail("john.doe@example.com")
            .WithItems(1)
            .Build();
        var response = await client.PostAsync<Kibo.TestingFramework.Order>("/v1/orders", order);
        Assert.Equal(201, response.StatusCode);
        Assert.NotNull(response.Data);
        Assert.Equal("Pending", response.Data.Status);
    }

    [Fact]
    public async Task CreateOrder_WithoutTenantHeader_Returns401()
    {
        var client = new KiboApiClient();
        var order = new OrderBuilder()
            .WithCustomerEmail("no-tenant@example.com")
            .WithItems(1)
            .Build();
        var response = await client.PostAsync<Kibo.TestingFramework.Order>("/v1/orders", order, includeTenant: false);
        Assert.Equal(401, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_AfterCreation_StatusBecomesReadyForFulfillment()
    {
        var client = new KiboApiClient();
        var order = new OrderBuilder()
            .WithCustomerEmail("status-check@example.com")
            .WithItems(1)
            .Build();
        var createResponse = await client.PostAsync<Kibo.TestingFramework.Order>("/v1/orders", order);
        Assert.Equal(201, createResponse.StatusCode);
        Assert.NotNull(createResponse.Data);
        var orderId = createResponse.Data.Id;
        var readyOrder = await Poller.WaitUntilAsync(
            action: () => client.GetAsync<Kibo.TestingFramework.Order>($"/v1/orders/{orderId}"),
            condition: r => r.Data != null && r.Data.Status == "ReadyForFulfillment"
        );
        Assert.NotNull(readyOrder.Data);
        Assert.Equal("ReadyForFulfillment", readyOrder.Data.Status);
    }

    [Fact]
    public async Task GetOrder_WithInvalidId_Returns404()
    {
        var client = new KiboApiClient();
        var invalidId = Guid.NewGuid();
        var response = await client.GetAsync<Kibo.TestingFramework.Order>($"/v1/orders/{invalidId}");
        Assert.Equal(404, response.StatusCode);
    }
}
