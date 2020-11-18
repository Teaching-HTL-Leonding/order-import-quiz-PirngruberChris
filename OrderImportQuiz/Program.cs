using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Xml.Serialization;

var factory = new OrderContextFactory();
using var context = factory.CreateDbContext(args);


if (args[0] == "clean" || args[0] == "full")
{
    //Delete every row
    context.Customers.RemoveRange(context.Customers);
    context.Orders.RemoveRange(context.Orders);

    //Set Id to 0
    await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT('Orders', RESEED, 0)");
    await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT('Customers', RESEED, 0)");
}

if (args[0] == "import" || args[0] == "full")
{
    IEnumerable<string> fileLinesCustomers = await File.ReadAllLinesAsync(args[1]);
    fileLinesCustomers = fileLinesCustomers.Skip(1);
    var splittedLinesCustomers = fileLinesCustomers.
        Select(l => l.Split('\t'))
        .ToList();

    IEnumerable<string> fileLinesOrders = await File.ReadAllLinesAsync(args[2]);
    fileLinesOrders = fileLinesOrders.Skip(1);
    var splittedLinesOrders = fileLinesOrders.
        Select(l => l.Split('\t'))
        .ToList();

    foreach (var item in splittedLinesOrders)
    {
        var order = new Order { OrderDate = Convert.ToDateTime(item[1]), OrderValue = Convert.ToDecimal(item[2]) };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }

    foreach (var item in splittedLinesCustomers)
    {
        var customers = splittedLinesCustomers
        .Select(x => new Customer
        {
            Name = x[0],
            CreditLimit = Convert.ToDecimal(x[1]),
            Orders = splittedLinesOrders
        .Where(y => y[0] == x[0])
        .Select(y => new Order { OrderDate = Convert.ToDateTime(y[1]), OrderValue = Convert.ToDecimal(y[2]) })
        .ToList()
        });
        context.Customers.Add((Customer)customers);
        await context.SaveChangesAsync();
    }
}
if (args[0] == "check" || args[0] == "full")
{
    
}


Console.WriteLine();

class Customer
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [Column(TypeName = "decimal(8, 2)")]
    public decimal CreditLimit { get; set; }

    public List<Order> Orders { get; set; } = new();
}

class Order
{
    public int Id { get; set; }

    public DateTime OrderDate { get; set; }

    public Customer? Customer { get; set; }

    public int CustomerId { get; set; }

    [Column(TypeName = "decimal(8, 2)")]
    public decimal OrderValue { get; set; }
}

class OrderContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }

    public DbSet<Order> Orders { get; set; }

    public OrderContext(DbContextOptions<OrderContext> options)
        :base(options)
    { }
}

class OrderContextFactory : IDesignTimeDbContextFactory<OrderContext>
{
    public OrderContext CreateDbContext(string[]? args = null)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("AppSettings.json").Build();

        var optionsBuilder = new DbContextOptionsBuilder<OrderContext>();
        optionsBuilder
            // Uncomment the following line if you want to print generated
            // SQL statements on the console.
            .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

        return new OrderContext(optionsBuilder.Options);
    }
}
