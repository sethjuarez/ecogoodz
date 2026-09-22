using System.Security.Claims;
using EcoGoodz.Data;
using EcoGoodz.Data.Identity;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Controllers;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Communication;
using EcoGoodz.Web.Models.Contact;
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

    private static EcoGoodzDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EcoGoodzDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EcoGoodzDbContext(options);
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
