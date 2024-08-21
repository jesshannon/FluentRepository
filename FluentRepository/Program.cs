using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

using var db = new AppDbContext();
db.Database.OpenConnection();
db.Database.EnsureCreated();

var fluentOrderRepository = new OrderRepository(db);

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
var ordersThatNeedShippingFluent = fluentOrderRepository
    .Query()
        .ReadyToShip()
    .Include()
        .DeliveryAddress()
        .CustomerWithAddresses();


// generated SQL for comparison
var queryStandard = ordersThatNeedShipping.ToQueryString();
var queryFluent = ordersThatNeedShippingFluent.Query!.ToQueryString();

Console.WriteLine(queryStandard == queryFluent ? " ==== Queries are the same" : " !!!! Queries are NOT the same");

Console.WriteLine("\n\nStandard query:");
Console.WriteLine(queryStandard);

Console.WriteLine("\n\nFluent Repository:");
Console.WriteLine(queryFluent);

Console.ReadKey();

