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
