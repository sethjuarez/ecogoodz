using System.Linq.Expressions;
using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Controllers;
using EcoGoodz.Web.Controllers.Shared;
using EcoGoodz.Web.Extensions;
using EcoGoodz.Web.Models.Buyer;
using EcoGoodz.Web.Models.Location;
using EcoGoodz.Web.Models.PackageType;
using EcoGoodz.Web.Models.Product;
using EcoGoodz.Web.Models.Supplier;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Tests;

public class LegacyListSortTests
{
    [Theory]
    [InlineData("buyer", "NameSort")]
    [InlineData("supplier", "NameSort")]
    [InlineData("product", "NameSort")]
    [InlineData("package-type", "TypeSort")]
    [InlineData("location", "LocationSort")]
    [InlineData("location-city", "CitySort")]
    public void ExplicitLegacyListSortsUseComputedSortColumns(string controller, string expectedColumn)
    {
        using var context = CreateSqlServerContext();

        var sql = controller switch
        {
            "buyer" => new TestBuyerController(context).GetSortedSql("name"),
            "supplier" => new TestSupplierController(context).GetSortedSql("name"),
            "product" => new TestProductController(context).GetSortedSql("name"),
            "package-type" => new TestPackageTypeController(context).GetSortedSql("type"),
            "location" => new TestLocationController(context).GetSortedSql("name"),
            "location-city" => new TestLocationController(context).GetSortedSql("city"),
            _ => throw new ArgumentOutOfRangeException(nameof(controller), controller, null),
        };

        Assert.Contains($"ORDER BY", sql);
        Assert.Contains(expectedColumn, sql[sql.IndexOf("ORDER BY", StringComparison.Ordinal)..]);
    }

    private static EcoGoodzDbContext CreateSqlServerContext()
    {
        var options = new DbContextOptionsBuilder<EcoGoodzDbContext>()
            .UseSqlServer("Server=(local);Database=EcoGoodzQueryShapeOnly;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new EcoGoodzDbContext(options);
    }

    private sealed class TestBuyerController(EcoGoodzDbContext context)
        : BuyerController(context)
    {
        public string GetSortedSql(string sort)
        {
            var query = GetBaseQuery().AsNoTracking();
            var sorted = query.ApplySort(sort, false, SortColumns, DefaultSortColumn, out _);
            return sorted.Select(ProjectionExpression).ToQueryString();
        }
    }

    private sealed class TestSupplierController(EcoGoodzDbContext context)
        : SupplierController(context)
    {
        public string GetSortedSql(string sort)
        {
            var query = GetBaseQuery().AsNoTracking();
            var sorted = query.ApplySort(sort, false, SortColumns, DefaultSortColumn, out _);
            return sorted.Select(ProjectionExpression).ToQueryString();
        }
    }

    private sealed class TestLocationController(EcoGoodzDbContext context)
        : LocationController(context)
    {
        public string GetSortedSql(string sort)
        {
            var query = GetBaseQuery().AsNoTracking();
            var sorted = query.ApplySort(sort, false, SortColumns, DefaultSortColumn, out _);
            return sorted.Select(ProjectionExpression).ToQueryString();
        }
    }

    private sealed class TestProductController(EcoGoodzDbContext context)
        : ProductController(context)
    {
        public string GetSortedSql(string sort)
        {
            var query = GetBaseQuery().AsNoTracking();
            var sorted = query.ApplySort(sort, false, SortColumns, DefaultSortColumn, out _);
            return sorted.Select(ProjectionExpression).ToQueryString();
        }
    }

    private sealed class TestPackageTypeController(EcoGoodzDbContext context)
        : PackageTypeController(context)
    {
        public string GetSortedSql(string sort)
        {
            var query = GetBaseQuery().AsNoTracking();
            var sorted = query.ApplySort(sort, false, SortColumns, DefaultSortColumn, out _);
            return sorted.Select(ProjectionExpression).ToQueryString();
        }
    }
}
