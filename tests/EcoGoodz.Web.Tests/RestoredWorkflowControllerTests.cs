using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EcoGoodz.Data;
using EcoGoodz.Data.Identity;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Controllers;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.BuyerProduct;
using EcoGoodz.Web.Models.BuyerSupplier;
using EcoGoodz.Web.Models.Communication;
using EcoGoodz.Web.Models.Contact;
using EcoGoodz.Web.Models.Load;
using EcoGoodz.Web.Models.Note;
using EcoGoodz.Web.Models.StaffTask;
using EcoGoodz.Web.Models.StaffUser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EcoGoodz.Web.Tests;

public class RestoredWorkflowControllerTests
{
    [Fact]
    public async Task ContactCreate_AddsContactAndMovesPrimaryFlagForLocation()
    {
        await using var context = CreateContext();
        context.Buyers.Add(new Buyer { Id = 10, Name = "Buyer One", IsActive = true });
        context.Locations.Add(new Location { Id = 20, Location1 = "Main Dock", ClientId = 10, IsBuyer = true, IsActive = true });
        context.ContactInformations.Add(new ContactInformation { Id = 30, FirstName = "Existing", IsActive = true });
        context.Contacts.Add(new Contact
        {
            Id = 40,
            ContactId = 30,
            ClientId = 10,
            Location = 20,
            IsBuyer = true,
            IsActive = true,
            IsPrimaryContact = true,
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new ContactController(context));

        var result = await controller.Create(new ContactFormViewModel
        {
            LocationId = 20,
            FirstName = "New",
            LastName = "Primary",
            Email = "new@example.com",
            IsPrimaryContact = true,
            IsActive = true,
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(20, redirect.RouteValues!["locationId"]);

        var existing = await context.Contacts.FindAsync(40);
        var created = await context.Contacts.Include(c => c.ContactNavigation).SingleAsync(c => c.Id != 40);
        Assert.False(existing!.IsPrimaryContact);
        Assert.True(created.IsPrimaryContact);
        Assert.Equal(10, created.ClientId);
        Assert.True(created.IsBuyer);
        Assert.Equal(99, created.CreatedBy);
        Assert.Equal("New", created.ContactNavigation.FirstName);
    }

    [Fact]
    public async Task CommunicationCreate_LogsSupplierCommunicationWithOtherType()
    {
        await using var context = CreateContext();
        context.Suppliers.Add(new Supplier { Id = 7, Name = "Supplier One", IsActive = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new CommunicationController(context));

        var result = await controller.Create(new CommunicationFormViewModel
        {
            ClientType = "Supplier",
            SupplierId = 7,
            OtherType = "Check-in",
            Note = "Called supplier.",
            Date = new DateTime(2026, 9, 22),
        });

        Assert.IsType<RedirectToActionResult>(result);
        var communication = await context.Communications.SingleAsync();
        Assert.False(communication.IsBuyer);
        Assert.Equal(7, communication.ClientId);
        Assert.Null(communication.CommunicationType);
        Assert.Equal("Check-in", communication.OtherType);
        Assert.Equal("Called supplier.", communication.Note);
        Assert.True(communication.IsActive);
        Assert.Equal(99, communication.CreatedBy);
    }

    [Fact]
    public async Task NoteCreate_StoresProductScopedNote()
    {
        await using var context = CreateContext();
        context.Suppliers.Add(new Supplier { Id = 1, Name = "Supplier", IsActive = true });
        context.Products.Add(new Product { Id = 2, Name = "PET", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct { Id = 3, Supplier = 1, Product = 2, IsActive = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new NoteController(context));

        var result = await controller.Create(new NoteFormViewModel
        {
            Scope = "Product",
            Product = 3,
            Notes = "Supplier product note",
            IsActive = true,
        });

        Assert.IsType<RedirectToActionResult>(result);
        var note = await context.Notes.SingleAsync();
        Assert.True(note.IsProduct);
        Assert.Equal(3, note.Product);
        Assert.Null(note.BuyerLocation);
        Assert.Null(note.SupplierLocation);
        Assert.Equal("Supplier product note", note.Notes);
        Assert.Equal(99, note.CreatedBy);
    }

    [Fact]
    public async Task StaffTaskCreate_AssignsTaskAndCreatesAssignedHeadlineWhenMissing()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { Id = 5, FirstName = "Ava", LastName = "Trader", IsActive = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new StaffTaskController(context));

        var result = await controller.Create(new StaffTaskFormViewModel
        {
            Description = "Call buyer",
            DueDate = new DateTime(2026, 9, 30),
            AssignedTo = 5,
            IsActive = true,
        });

        Assert.IsType<RedirectToActionResult>(result);
        var task = await context.Tasks.Include(t => t.AssignTasks).SingleAsync();
        var assigned = task.AssignTasks.Single();
        var headline = await context.TaskHeadlines.SingleAsync();
        Assert.Equal("Call buyer", task.Description);
        Assert.Equal(99, task.CreatedBy);
        Assert.Equal(5, assigned.AssignedTo);
        Assert.Equal(headline.Id, assigned.TaskHeadline);
        Assert.Equal("Assigned", headline.Headline);
        Assert.False(assigned.IsRead);
    }

    [Fact]
    public async Task StaffUserCreate_AddsLegacyUserAndDefaultTaskHeadlines()
    {
        await using var context = CreateContext();
        context.Roles.Add(new Role { Id = 1, RoleName = "Admin" });
        await context.SaveChangesAsync();
        await using var identityContext = CreateIdentityContext();
        identityContext.Roles.Add(new IdentityRole<int> { Id = 1, Name = "Admin", NormalizedName = "ADMIN" });
        await identityContext.SaveChangesAsync();

        var controller = WithLegacyUser(new StaffUserController(context, CreateUserManager(identityContext)));

        var result = await controller.Create(new StaffUserFormViewModel
        {
            FirstName = "Sam",
            LastName = "Manager",
            UserName = "sam",
            Email = "sam@example.com",
            Role = 1,
            IsActive = true,
        });

        Assert.IsType<RedirectToActionResult>(result);
        var user = await context.Users.SingleAsync();
        Assert.Equal("sam", user.UserName);
        Assert.Equal("sam@example.com", user.Email);
        Assert.Equal(1, user.Role);
        Assert.Equal(99, user.CreatedBy);

        var headlines = await context.TaskHeadlines.OrderBy(h => h.Headline).Select(h => h.Headline).ToListAsync();
        Assert.Equal(["Assigned", "Assigned To"], headlines);

        var applicationUser = await identityContext.Users.SingleAsync();
        Assert.Equal(user.Id, applicationUser.LegacyUserId);
        Assert.True(applicationUser.MustChangePassword);
        Assert.Equal("sam@example.com", applicationUser.Email);
        var role = await identityContext.UserRoles.SingleAsync();
        Assert.Equal(applicationUser.Id, role.UserId);
        Assert.Equal(1, role.RoleId);
    }

    [Fact]
    public async Task BuyerProductEdit_PreservesAdditionalPackagingRows()
    {
        await using var context = CreateContext();
        context.Buyers.Add(new Buyer { Id = 1, Name = "Buyer", IsActive = true });
        context.Locations.Add(new Location { Id = 2, ClientId = 1, IsBuyer = true, Location1 = "Dock", IsActive = true });
        context.Products.Add(new Product { Id = 3, Name = "PET", IsActive = true });
        context.PackageTypes.AddRange(
            new PackageType { Id = 4, Type = "Bales", IsActive = true },
            new PackageType { Id = 5, Type = "Boxes", IsActive = true });
        context.BuyerProducts.Add(new BuyerProduct
        {
            Id = 6,
            Buyer = 1,
            Location = 2,
            Product = 3,
            IsActive = true,
            BuyerProductPackagings =
            [
                new BuyerProductPackaging { Id = 7, Packaging = 4 },
                new BuyerProductPackaging { Id = 8, Packaging = 5 },
            ],
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new BuyerProductController(context));

        var result = await controller.Edit(6, new BuyerProductFormViewModel
        {
            Id = 6,
            Buyer = 1,
            Location = 2,
            Product = 3,
            Packaging = 4,
            IsActive = false,
        });

        Assert.IsType<RedirectToActionResult>(result);
        var packagingIds = await context.BuyerProductPackagings
            .Where(p => p.BuyerProduct == 6)
            .OrderBy(p => p.Id)
            .Select(p => p.Packaging)
            .ToListAsync();
        Assert.Equal([4, 5], packagingIds);
    }

    [Fact]
    public async Task LoadCreate_PersistsLocationsAndProductLines()
    {
        await using var context = CreateContext();
        SeedLoadWorkflowData(context);
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new LoadController(context));

        var result = await controller.Create(new LoadFormViewModel
        {
            Buyer = 1,
            Supplier = 2,
            BuyerLocation = 3,
            SupplierLocation = 4,
            LoadStatus = 5,
            ShipmentDate = new DateTime(2026, 10, 1),
            Container = "PW-CONT",
            SupplierProductIds = [8],
            IsActive = true,
        });

        Assert.IsType<RedirectToActionResult>(result);
        var load = await context.Loads.Include(l => l.LoadProducts).SingleAsync();
        Assert.Equal(1, load.Buyer);
        Assert.Equal(2, load.Supplier);
        Assert.Equal(3, load.BuyerLocation);
        Assert.Equal(4, load.SupplierLocation);
        Assert.Equal(99, load.CreatedBy);
        Assert.Equal(8, load.LoadProducts.Single().Product);
    }

    [Fact]
    public async Task LoadDetails_ShowsLocationsAndProductLines()
    {
        await using var context = CreateContext();
        SeedLoadWorkflowData(context);
        context.Loads.Add(new Load
        {
            Id = 20,
            Buyer = 1,
            Supplier = 2,
            BuyerLocation = 3,
            SupplierLocation = 4,
            LoadStatus = 5,
            IsActive = true,
            LoadProducts = [new LoadProduct { Id = 21, Product = 8 }],
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new LoadController(context));

        var result = await controller.Details(20);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LoadDetailsViewModel>(view.Model);
        Assert.Equal("Buyer Dock", model.BuyerLocationName);
        Assert.Equal("Supplier Dock", model.SupplierLocationName);
        var product = Assert.Single(model.ProductLines);
        Assert.Equal("PET", product.ProductName);
        Assert.Equal("Bales", product.PackagingName);
        Assert.Equal(12.34m, product.CurrentPrice);
    }

    [Fact]
    public async Task LoadEdit_ReplacesProductLines()
    {
        await using var context = CreateContext();
        SeedLoadWorkflowData(context);
        context.Loads.Add(new Load
        {
            Id = 20,
            Buyer = 1,
            Supplier = 2,
            BuyerLocation = 3,
            SupplierLocation = 4,
            LoadStatus = 5,
            IsActive = true,
            LoadProducts = [new LoadProduct { Id = 21, Product = 8 }],
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new LoadController(context));

        var result = await controller.Edit(20, new LoadFormViewModel
        {
            Id = 20,
            Buyer = 1,
            Supplier = 2,
            BuyerLocation = 3,
            SupplierLocation = 4,
            LoadStatus = 5,
            ShipmentDate = new DateTime(2026, 11, 1),
            SupplierProductIds = [10],
            IsActive = true,
        });

        Assert.IsType<RedirectToActionResult>(result);
        var load = await context.Loads.Include(l => l.LoadProducts).SingleAsync(l => l.Id == 20);
        Assert.Equal(new DateTime(2026, 11, 1), load.ShipmentDate);
        Assert.Equal(10, load.LoadProducts.Single().Product);
    }

    [Fact]
    public async Task LoadEdit_KeepsExistingInactiveProductLine()
    {
        await using var context = CreateContext();
        SeedLoadWorkflowData(context);
        context.SupplierProducts.Add(new SupplierProduct
        {
            Id = 11,
            Supplier = 2,
            Location = 4,
            Product = 6,
            Packaging = 7,
            IsActive = false,
        });
        context.Loads.Add(new Load
        {
            Id = 20,
            Buyer = 1,
            Supplier = 2,
            BuyerLocation = 3,
            SupplierLocation = 4,
            LoadStatus = 5,
            IsActive = true,
            LoadProducts = [new LoadProduct { Id = 21, Product = 11 }],
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new LoadController(context));

        var result = await controller.Edit(20, new LoadFormViewModel
        {
            Id = 20,
            Buyer = 1,
            Supplier = 2,
            BuyerLocation = 3,
            SupplierLocation = 4,
            LoadStatus = 5,
            ShipmentDate = new DateTime(2026, 12, 1),
            SupplierProductIds = [11],
            IsActive = true,
        });

        Assert.IsType<RedirectToActionResult>(result);
        var load = await context.Loads.Include(l => l.LoadProducts).SingleAsync(l => l.Id == 20);
        Assert.Equal(new DateTime(2026, 12, 1), load.ShipmentDate);
        Assert.Equal(11, load.LoadProducts.Single().Product);
    }

    [Fact]
    public void BuyerSupplierForm_RequiresCompleteMatchKeys()
    {
        var model = new BuyerSupplierFormViewModel();
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(BuyerSupplierFormViewModel.Buyer)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(BuyerSupplierFormViewModel.Supplier)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(BuyerSupplierFormViewModel.BuyerLocation)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(BuyerSupplierFormViewModel.SupplierLocation)));
    }

    private static EcoGoodzDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EcoGoodzDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EcoGoodzDbContext(options);
    }

    private static void SeedLoadWorkflowData(EcoGoodzDbContext context)
    {
        context.Buyers.Add(new Buyer { Id = 1, Name = "Buyer", IsActive = true });
        context.Suppliers.Add(new Supplier { Id = 2, Name = "Supplier", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 3, ClientId = 1, IsBuyer = true, Location1 = "Buyer Dock", IsActive = true },
            new Location { Id = 4, ClientId = 2, IsBuyer = false, Location1 = "Supplier Dock", IsActive = true });
        context.LoadStatuses.Add(new LoadStatus { Id = 5, Status = "Booked" });
        context.Products.Add(new Product { Id = 6, Name = "PET", IsActive = true });
        context.PackageTypes.Add(new PackageType { Id = 7, Type = "Bales", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct
        {
            Id = 8,
            Supplier = 2,
            Location = 4,
            Product = 6,
            Packaging = 7,
            IsActive = true,
            SupplierProductRates =
            [
                new SupplierProductRate
                {
                    Id = 9,
                    Price = 12.34m,
                    EffectiveDate = new DateTime(2026, 9, 1),
                    IsActive = true,
                },
            ],
        });
        context.SupplierProducts.Add(new SupplierProduct
        {
            Id = 10,
            Supplier = 2,
            Location = 4,
            Product = 6,
            Packaging = 7,
            IsActive = true,
        });
    }

    private static EcoGoodzIdentityDbContext CreateIdentityContext()
    {
        var options = new DbContextOptionsBuilder<EcoGoodzIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EcoGoodzIdentityDbContext(options);
    }

    private static UserManager<ApplicationUser> CreateUserManager(EcoGoodzIdentityDbContext context)
    {
        var store = new UserStore<ApplicationUser, IdentityRole<int>, EcoGoodzIdentityDbContext, int>(context);
        return new UserManager<ApplicationUser>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    private static TController WithLegacyUser<TController>(TController controller)
        where TController : Controller
    {
        var identity = new ClaimsIdentity(
            [new Claim(ApplicationClaimsPrincipalFactory.LegacyUserIdClaimType, "99")],
            authenticationType: "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
        return controller;
    }
}
