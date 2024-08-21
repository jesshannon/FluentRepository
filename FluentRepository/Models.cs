
public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public OrderStatus Status { get; set; }
    public Address? BillingAddress { get; set; }
    public Address? DeliveryAddress { get; set; }
    public Customer? Customer { get; set; }
    public List<LineItem> LineItems { get; set; } = new();
    public DateTime? PaymentConfirmedDate { get; set; }
}

public class LineItem
{
    public int Id { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public Order Order { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public Address? BillingAddress { get; set; }
    public Address? DeliveryAddress { get; set; }
}

public class Address
{
    public int Id { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PostalCode { get; set; }
    public Country? Country { get; set; }
}

public class Country
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Enum
public enum OrderStatus
{
    Draft,
    Submitted,
    Shipped,
    Delivered,
    Returned,
    Canceled
}