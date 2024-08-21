public class AddressIncludeOptions : IncludeOptions<Address, AddressIncludeOptions>
{
    public AddressIncludeOptions WithCountry() => AddInclude(o => o.Country);
}
