using Microsoft.EntityFrameworkCore;

/// <summary>
/// This class doesn't neccessarily need to exist, it could be registered with DI with just the generic definition
/// </summary>
public class OrderRepository : FluentRepository<Order, OrderFilterOptions, OrderIncludeOptions>
{
    public OrderRepository(DbContext dbContext) : base(dbContext)
    {
    }

}
public class OrderFilterOptions : FilterOptions<Order, OrderIncludeOptions, OrderFilterOptions>
{
    // Example filter method that applies some business rules to the query
    public OrderFilterOptions ReadyToShip() => AddFilter(o => o.Status == OrderStatus.Submitted && o.PaymentConfirmedDate.HasValue);
}

public class OrderIncludeOptions : IncludeOptions<Order, OrderIncludeOptions>
{
    // Example of using an optional Action parameter to request additional includes from the Customer object using another IncludeOptions instance
    public OrderIncludeOptions Customer(Action<CustomerIncludeOptions>? extra = null)
        => AddInclude(o => o.Customer!, extra);

    // Example of using a separate method to always include some specific additional properties on Customer
    public OrderIncludeOptions CustomerWithAddresses()
        => AddInclude(o => o.Customer!, (CustomerIncludeOptions c) => c.BothAddresses());

    // A standard include method for a single navigation property
    public OrderIncludeOptions DeliveryAddress()
        => AddInclude(o => o.DeliveryAddress);

    // Example of including multiple properties at once
    public OrderIncludeOptions BothAddresses()
    {
        AddInclude(o => o.DeliveryAddress);
        AddInclude(o => o.BillingAddress);
        return this;
    }
}