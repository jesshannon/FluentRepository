using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

using var db = new AppDbContext();
db.Database.OpenConnection();
db.Database.EnsureCreated();

var orderRepository = new OrderRepository(db);

// Example of regular Query
var ordersThatNeedShipping = db.Orders
    .Where(o => o.Status == OrderStatus.Submitted && o.PaymentConfirmedDate.HasValue)
    .Include(o => o.DeliveryAddress)
    .Include(o => o.Customer)
        .ThenInclude(c => c!.DeliveryAddress)
            .ThenInclude(da => da!.Country)
    .Include(o => o.Customer)
        .ThenInclude(c => c!.BillingAddress)
            .ThenInclude(ba => ba!.Country);

// Same Query using Fluent Repo
var ordersThatNeedShippingFluent = orderRepository
    .Query()
        .ReadyToShip()
    .Include()
        .DeliveryAddress()
        .CustomerWithAddresses();

// show generated SQL for comparison

var queryStandard = ordersThatNeedShipping.ToQueryString();
var queryFluent = ordersThatNeedShippingFluent.Query!.ToQueryString();

Console.WriteLine(queryStandard == queryFluent ? " ==== Queries are the same" : " !!!! Queries are NOT the same");

Console.WriteLine("\n\nStandard query:");
Console.WriteLine(queryStandard);

Console.WriteLine("\n\nFluent Repository:");
Console.WriteLine(queryFluent);

Console.ReadKey();

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

public class AddressIncludeOptions : IncludeOptions<Address, AddressIncludeOptions>
{
    public AddressIncludeOptions WithCountry() => AddInclude(o => o.Country);
}

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

public abstract class FluentRepository<Entity, FilterOptions, IncludeOptions>
    where FilterOptions : FilterOptions<Entity, IncludeOptions, FilterOptions>, new()
    where IncludeOptions : IncludeOptions<Entity, IncludeOptions>, new()
    where Entity : class
{
    private readonly DbContext dbContext;

    protected FluentRepository(DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public FilterOptions Query() => new FilterOptions
    {
        Query = dbContext.Set<Entity>()
    };
}

public abstract class FilterOptions<Entity, IncludeOptions, Self>
    where Self : FilterOptions<Entity, IncludeOptions, Self>
    where IncludeOptions : IncludeOptions<Entity, IncludeOptions>, new()
    where Entity : class
{
    internal IQueryable<Entity> Query { get; set; }
    protected Self AddFilter(Expression<Func<Entity, bool>> predicate)
    {
        Query = Query.Where(predicate);
        return (Self)this;
    }

    public IncludeOptions Include() => new IncludeOptions
    {
        Query = Query,
    };
}
public abstract class IncludeOptions<Entity, Self> : IParentIncludeOptions, IIncludeEntityType<Entity>
    where Self : IncludeOptions<Entity, Self>
    where Entity : class
{
    IQueryable? _query;
    internal IQueryable? Query
    {
        get
        {
            if (ParentOptions != null)
            {
                var resetInclude = ParentOptions.ParentOptions == null;

                return AppendInclude(ParentOptions.Query, ParentIncludePredicate, resetInclude, true);
            }
            else
            {
                return _query;
            }
        }
        set
        {
            if (ParentOptions != null)
            {
                ParentOptions.Query = value;
            }
            else
                _query = value;
        }
    }

    private IQueryable AppendInclude(IQueryable? query, LambdaExpression predicate, bool resetInclude = false, bool isCurrent = false)
    {
        MethodInfo includeMethod;
        if (query != null && predicate != null)
        {
            if (!resetInclude && query.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIncludableQueryable<,>)))
            {
                // it's already an includable so we use a ThenInclude
                includeMethod = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods()
                    .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude)
                        && m.GetParameters().Length == 2
                        && m.GetParameters()[0].ParameterType.GenericTypeArguments[1].Name != typeof(IEnumerable<>).Name
                        && m.GetParameters()[1].ParameterType.IsAssignableTo(typeof(LambdaExpression)));
            }
            else
            {
                resetInclude = true;
                // should just be a regular IQueryable so we'll use an Include
                includeMethod = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods()
                    .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.Include) && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.IsAssignableTo(typeof(LambdaExpression)));
            }

            if (resetInclude)
                includeMethod = includeMethod.MakeGenericMethod(TopType, predicate.ReturnType);
            else
                includeMethod = includeMethod.MakeGenericMethod(TopType, predicate.Parameters.First().Type, predicate.ReturnType);

            var qry = (IQueryable)includeMethod.Invoke(null, new object[] { query, predicate })!;
            return qry;
        }
        throw new InvalidOperationException("Parent query or predicate is null.");
    }

    internal Type TopType => ParentOptions?.TopType ?? typeof(Entity);
    internal IParentIncludeOptions ParentOptions { get; set; }
    internal LambdaExpression ParentIncludePredicate { get; set; }

    IParentIncludeOptions IParentIncludeOptions.ParentOptions => ParentOptions;
    Type IParentIncludeOptions.TopType => TopType;
    IQueryable? IParentIncludeOptions.Query { get => Query; set => Query = value; }

    protected Self AddInclude<TProperty>(Expression<Func<Entity, TProperty>> predicate)
    {
        Query = AppendInclude(Query, predicate);
        return (Self)this;
    }
    protected Self AddInclude<OtherOptions, TProperty>(Expression<Func<Entity, TProperty>> predicate, Action<OtherOptions>? otherIncludes)
        where OtherOptions : IncludeOptions<TProperty, OtherOptions>, new()
        where TProperty : class
    {
        if (otherIncludes != null)
        {
            var other = new OtherOptions
            {
                ParentOptions = this,
                ParentIncludePredicate = predicate,
            };
            otherIncludes.Invoke(other);
        }
        else
            AddInclude(predicate);

        return (Self)this;
    }
}

internal interface IParentIncludeOptions
{
    IParentIncludeOptions ParentOptions { get; }
    Type TopType { get; }
    IQueryable? Query { get; set; }
}
internal interface IIncludeEntityType<E>
{
}