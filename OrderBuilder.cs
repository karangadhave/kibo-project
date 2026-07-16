using System;
using System.Collections.Generic;
using System.Linq;

namespace Kibo.TestingFramework
{
    public class OrderBuilder
    {
        private string _customerEmail = $"test+{Guid.NewGuid()}@kibo.com";
        private string _tenantId = "tenant-abc-123";
        private List<LineItem> _lineItems = new();

        public OrderBuilder WithCustomerEmail(string email)
        {
            _customerEmail = email;
            return this;
        }

        public OrderBuilder ForTenant(string tenantId)
        {
            _tenantId = tenantId;
            return this;
        }

        public OrderBuilder WithItems(int count)
        {
            _lineItems = Enumerable.Range(1, count).Select(_ => LineItemBuilder.Random().Build()).ToList();
            return this;
        }

        public OrderBuilder WithLineItems(params LineItem[] items)
        {
            _lineItems = items.ToList();
            return this;
        }

        public Order Build()
        {
            return new Order
            {
                CustomerEmail = _customerEmail,
                TenantId = _tenantId,
                LineItems = _lineItems.Count > 0 ? _lineItems : new List<LineItem> { LineItemBuilder.Random().Build() }
            };
        }
    }

    public class LineItemBuilder
    {
        private string _productCode = $"SKU-{Guid.NewGuid().ToString()[..8]}";
        private int _quantity = 1;
        private decimal _unitPrice = 10.0m;

        public static LineItemBuilder Random()
        {
            var rnd = new Random();
            return new LineItemBuilder()
                .WithProductCode($"SKU-{rnd.Next(100,999)}")
                .WithQuantity(rnd.Next(1,5))
                .WithUnitPrice((decimal)(rnd.NextDouble() * 100 + 1));
        }

        public LineItemBuilder WithProductCode(string code)
        {
            _productCode = code;
            return this;
        }

        public LineItemBuilder WithQuantity(int qty)
        {
            _quantity = qty;
            return this;
        }

        public LineItemBuilder WithUnitPrice(decimal price)
        {
            _unitPrice = price;
            return this;
        }

        public LineItem Build()
        {
            return new LineItem
            {
                ProductCode = _productCode,
                Quantity = _quantity,
                UnitPrice = _unitPrice
            };
        }
    }

    // DTOs for test serialization
    public class Order
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public List<LineItem> LineItems { get; set; } = new();
    }

    public class LineItem
    {
        public string ProductCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
