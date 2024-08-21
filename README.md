
# FluentRepositories Demo Project

This project demonstrates an approach to building repositories using Entity Framework Core. The primary goal is to address common issues encountered with traditional repository methods, such as method overload, inconsistent naming, and excessive eager loading.

## Problem with Traditional Repository Methods


In traditional repository patterns, methods often grow in number and complexity as developers add more includes and filters to cover various use cases. This can lead to:

- ****Method Overload****: A huge number of different methods with confusing and often conflicting names.

- ****Excessive Eager Loading****: Developers frequently include all related entities because they're unsure of what will be needed or because they want to avoid missing data in certain contexts. This results in unnecessary data retrieval, impacting performance.

- ****Unintended Side Effects****: When developers modify existing methods to add more includes for their specific use cases, it can inadvertently cause other parts of the application to load more data than necessary.

- ****Duplicated business rules****: Each method can end up duplicating business rules for filtering data

 
### Example of Traditional Repository Method

```csharp
public List<Order> OrdersThatNeedShipping() => db.Orders
.Where(o => o.Status == OrderStatus.Submitted && o.PaymentConfirmedDate.HasValue)
.Include(o => o.DeliveryAddress)
.Include(o => o.Customer)
	.ThenInclude(c => c!.DeliveryAddress)
		.ThenInclude(da => da!.Country)
.Include(o => o.Customer)
	.ThenInclude(c => c!.BillingAddress)
		.ThenInclude(ba => ba!.Country);
```

### Example of the same query using a FluentRepository
```csharp
var ordersThatNeedShippingFluent = fluentOrderRepository
.Query()
	.ReadyToShip()
.Include()
	.DeliveryAddress()
	.CustomerWithAddresses();
```

These methods are defined in two classes, one for Filtering and one for Includes

```csharp
public class OrderFilterOptions : FilterOptions<Order, OrderIncludeOptions, OrderFilterOptions>
{
	// Example filter method that applies some business rules to the query
	public OrderFilterOptions ReadyToShip() => AddFilter(o => o.Status == OrderStatus.Submitted && o.PaymentConfirmedDate.HasValue);
}
```

```csharp
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
```

the Include methods can reference other Include option classes when including those types, such as this example for Customers.
```csharp
public class CustomerIncludeOptions : IncludeOptions<Customer, CustomerIncludeOptions>
{
    public CustomerIncludeOptions DeliveryAddress() => AddInclude(o => o.DeliveryAddress);
    public CustomerIncludeOptions BillingAddress() => AddInclude(o => o.BillingAddress);
    public CustomerIncludeOptions BothAddresses()
    {
        AddInclude(c => c.DeliveryAddress!, (AddressIncludeOptions a) => a.WithCountry());
        AddInclude(c => c.BillingAddress!, (AddressIncludeOptions a) => a.WithCountry());
        return this;
    }
}
```
