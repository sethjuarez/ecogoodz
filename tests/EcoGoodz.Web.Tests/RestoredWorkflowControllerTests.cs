using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using EcoGoodz.Data;
using EcoGoodz.Data.Identity;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Controllers;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.Buyer;
using EcoGoodz.Web.Models.BuyerProduct;
using EcoGoodz.Web.Models.BuyerSupplier;
using EcoGoodz.Web.Models.Communication;
using EcoGoodz.Web.Models.Contact;
using EcoGoodz.Web.Models.Load;
using EcoGoodz.Web.Models.Location;
using EcoGoodz.Web.Models.Note;
using EcoGoodz.Web.Models.Report;
using EcoGoodz.Web.Models.Shared;
using EcoGoodz.Web.Models.StaffTask;
using EcoGoodz.Web.Models.StaffUser;
using EcoGoodz.Web.Models.Supplier;
using EcoGoodz.Web.Models.SupplierProduct;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
    public async Task ContactCopy_DuplicatesContactAsNonPrimary()
    {
        await using var context = CreateContext();
        context.Buyers.Add(new Buyer { Id = 10, Name = "Buyer One", IsActive = true });
        context.Locations.Add(new Location { Id = 20, Location1 = "Main Dock", ClientId = 10, IsBuyer = true, IsActive = true });
        context.Contacts.Add(new Contact
        {
            Id = 30,
            ClientId = 10,
            Location = 20,
            IsBuyer = true,
            IsPrimaryContact = true,
            IsDockContact = true,
            IsActive = true,
            ContactNavigation = new ContactInformation
            {
                Id = 31,
                FirstName = "Existing",
                LastName = "Primary",
                Email = "existing@example.com",
                OfficePhone = "555-0100",
                IsActive = true,
            },
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new ContactController(context));

        var result = await controller.Copy(30);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(20, redirect.RouteValues!["locationId"]);

        var copy = await context.Contacts
            .Include(contact => contact.ContactNavigation)
            .SingleAsync(contact => contact.Id != 30);
        Assert.Equal(10, copy.ClientId);
        Assert.Equal(20, copy.Location);
        Assert.True(copy.IsBuyer);
        Assert.False(copy.IsPrimaryContact);
        Assert.True(copy.IsDockContact);
        Assert.Equal(99, copy.CreatedBy);
        Assert.Equal("Existing", copy.ContactNavigation.FirstName);
        Assert.Equal("existing@example.com", copy.ContactNavigation.Email);
        Assert.Equal("555-0100", copy.ContactNavigation.OfficePhone);
    }

    [Fact]
    public async Task LocationCreate_WithCopySource_ClonesContactsAndBuyerProducts()
    {
        await using var context = CreateContext();
        context.Buyers.Add(new Buyer { Id = 1, Name = "Buyer", IsActive = true });
        context.PackageTypes.Add(new PackageType { Id = 2, Type = "Bales", IsActive = true });
        context.Products.Add(new Product { Id = 3, Name = "PET", IsActive = true });
        context.Locations.Add(new Location
        {
            Id = 4,
            ClientId = 1,
            IsBuyer = true,
            Location1 = "Source Dock",
            DockHours = "8-5",
            Drayage1 = 12.50m,
            NearestPort1 = "Tacoma",
            IsActive = true,
            Contacts =
            [
                new Contact
                {
                    Id = 5,
                    ClientId = 1,
                    IsBuyer = true,
                    IsPrimaryContact = true,
                    IsDockContact = false,
                    IsActive = true,
                    ContactNavigation = new ContactInformation
                    {
                        Id = 6,
                        FirstName = "Primary",
                        LastName = "Contact",
                        Email = "primary@example.com",
                        IsActive = true,
                    },
                },
                new Contact
                {
                    Id = 7,
                    ClientId = 1,
                    IsBuyer = true,
                    IsDockContact = true,
                    IsActive = true,
                    ContactNavigation = new ContactInformation
                    {
                        Id = 8,
                        FirstName = "Dock",
                        IsActive = true,
                    },
                },
            ],
            BuyerProducts =
            [
                new BuyerProduct
                {
                    Id = 9,
                    Buyer = 1,
                    Product = 3,
                    IsActive = true,
                    BuyerProductPackagings =
                    [
                        new BuyerProductPackaging
                        {
                            Id = 10,
                            Packaging = 2,
                        },
                    ],
                },
            ],
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new LocationController(context));

        var result = await controller.Create(new LocationFormViewModel
        {
            Name = "Copied Dock",
            CopyLocationId = 4,
            ClientType = "Buyer",
            BuyerClientId = 1,
            DockHours = "7-3",
            IsActive = true,
        });

        Assert.IsType<RedirectToActionResult>(result);
        var copied = await context.Locations
            .Include(location => location.Contacts)
                .ThenInclude(contact => contact.ContactNavigation)
            .Include(location => location.BuyerProducts)
                .ThenInclude(product => product.BuyerProductPackagings)
            .SingleAsync(location => location.Id != 4);
        Assert.Equal("Copied Dock", copied.Location1);
        Assert.Equal("7-3", copied.DockHours);
        Assert.Equal(12.50m, copied.Drayage1);
        Assert.Equal("Tacoma", copied.NearestPort1);

        var contact = Assert.Single(copied.Contacts);
        Assert.Equal("Primary", contact.ContactNavigation.FirstName);
        Assert.Equal("primary@example.com", contact.ContactNavigation.Email);
        Assert.True(contact.IsPrimaryContact);
        Assert.False(contact.IsDockContact);

        var buyerProduct = Assert.Single(copied.BuyerProducts);
        Assert.Equal(3, buyerProduct.Product);
        Assert.Equal(99, buyerProduct.CreatedBy);
        Assert.Equal(2, Assert.Single(buyerProduct.BuyerProductPackagings).Packaging);
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
    public async Task StaffTaskCreate_AssignsOneTaskToMultipleUsers()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User { Id = 5, FirstName = "Ava", LastName = "Trader", IsActive = true },
            new User { Id = 6, FirstName = "Ben", LastName = "Broker", IsActive = true });
        context.TaskHeadlines.Add(new TaskHeadline { Id = 7, UserId = 6, Headline = "Assigned", IsActive = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new StaffTaskController(context));

        var result = await controller.Create(new StaffTaskFormViewModel
        {
            Description = "Follow up with both managers",
            DueDate = new DateTime(2026, 10, 5),
            AssignedToIds = [5, 6],
            IsActive = true,
        });

        Assert.IsType<RedirectToActionResult>(result);
        var task = await context.Tasks.Include(t => t.AssignTasks).SingleAsync();
        Assert.Equal("Follow up with both managers", task.Description);
        Assert.Equal(2, task.AssignTasks.Count);
        Assert.Equal([5, 6], task.AssignTasks.OrderBy(a => a.AssignedTo).Select(a => a.AssignedTo).ToList());
        Assert.All(task.AssignTasks, assignment => Assert.Equal("Assigned", context.TaskHeadlines.Single(h => h.Id == assignment.TaskHeadline).Headline));
        Assert.Equal(2, await context.TaskHeadlines.CountAsync(h => h.Headline == "Assigned"));
    }

    [Fact]
    public async Task StaffTaskHeadlineCreate_AddsHeadlineForCurrentUser()
    {
        await using var context = CreateContext();
        var controller = WithLegacyUser(new StaffTaskController(context));

        var result = await controller.CreateHeadline(new StaffTaskHeadlineFormViewModel
        {
            Headline = "Follow-up",
        });

        Assert.IsType<RedirectToActionResult>(result);
        var headline = await context.TaskHeadlines.SingleAsync();
        Assert.Equal("Follow-up", headline.Headline);
        Assert.Equal(99, headline.UserId);
        Assert.Equal(99, headline.CreatedBy);
        Assert.True(headline.IsActive);
    }

    [Fact]
    public async Task StaffTaskHeadlineCreate_RejectsReservedAssignedHeadline()
    {
        await using var context = CreateContext();
        var controller = WithLegacyUser(new StaffTaskController(context));

        var result = await controller.CreateHeadline(new StaffTaskHeadlineFormViewModel
        {
            Headline = " assigned ",
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(context.TaskHeadlines);
    }

    [Fact]
    public async Task StaffTaskHeadlines_ListsCurrentUserHeadlinesExcludingAssigned()
    {
        await using var context = CreateContext();
        context.TaskHeadlines.AddRange(
            new TaskHeadline { Id = 1, UserId = 99, Headline = "Assigned", IsActive = true },
            new TaskHeadline { Id = 2, UserId = 99, Headline = "Calls", IsActive = true },
            new TaskHeadline { Id = 3, UserId = 100, Headline = "Other user's calls", IsActive = true });
        context.Tasks.AddRange(
            new TaskItem
            {
                Id = 4,
                Description = "Call buyer",
                IsActive = true,
                AssignTasks =
                [
                    new AssignTask { Id = 5, AssignedTo = 99, TaskHeadline = 2, IsActive = true, IsDone = false },
                ],
            },
            new TaskItem
            {
                Id = 6,
                Description = "Inactive parent task",
                IsActive = false,
                AssignTasks =
                [
                    new AssignTask { Id = 7, AssignedTo = 99, TaskHeadline = 2, IsActive = true, IsDone = false },
                ],
            });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new StaffTaskController(context));

        var result = Assert.IsType<ViewResult>(await controller.Headlines());
        var model = Assert.IsAssignableFrom<IReadOnlyList<StaffTaskHeadlineListItemViewModel>>(result.Model);

        var headline = Assert.Single(model);
        Assert.Equal("Calls", headline.Headline);
        Assert.Equal(1, headline.OpenTaskCount);
    }

    [Fact]
    public async Task StaffTaskHeadlineDeactivate_BlocksWhenOpenTasksExist()
    {
        await using var context = CreateContext();
        context.TaskHeadlines.Add(new TaskHeadline { Id = 1, UserId = 99, Headline = "Calls", IsActive = true });
        context.Tasks.Add(new TaskItem
        {
            Id = 2,
            Description = "Call buyer",
            IsActive = true,
            AssignTasks =
            [
                new AssignTask
                {
                    Id = 3,
                    AssignedTo = 99,
                    TaskHeadline = 1,
                    IsActive = true,
                    IsDone = false,
                },
            ],
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new StaffTaskController(context));
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider());

        var result = await controller.DeactivateHeadline(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True((await context.TaskHeadlines.FindAsync(1))!.IsActive);
        Assert.Equal("Complete or move open tasks before deleting this headline.", controller.TempData["Error"]);
    }

    [Fact]
    public async Task StaffTaskHeadlineDeactivate_IgnoresInactiveParentTasks()
    {
        await using var context = CreateContext();
        context.TaskHeadlines.Add(new TaskHeadline { Id = 1, UserId = 99, Headline = "Calls", IsActive = true });
        context.Tasks.Add(new TaskItem
        {
            Id = 2,
            Description = "Deleted task",
            IsActive = false,
            AssignTasks =
            [
                new AssignTask
                {
                    Id = 3,
                    AssignedTo = 99,
                    TaskHeadline = 1,
                    IsActive = true,
                    IsDone = false,
                },
            ],
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new StaffTaskController(context));

        var result = await controller.DeactivateHeadline(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.False((await context.TaskHeadlines.FindAsync(1))!.IsActive);
    }

    [Fact]
    public async Task StaffTaskHeadlineEdit_DoesNotAllowEditingAnotherUsersHeadline()
    {
        await using var context = CreateContext();
        context.TaskHeadlines.Add(new TaskHeadline { Id = 1, UserId = 100, Headline = "Other user", IsActive = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new StaffTaskController(context));

        Assert.IsType<NotFoundResult>(await controller.EditHeadline(1));
    }

    [Fact]
    public async Task SupplierProductAssignToBuyers_AddsProductAndRateToExistingMatch()
    {
        await using var context = CreateContext();
        context.Suppliers.Add(new Supplier { Id = 1, Name = "Legacy Supplier", IsActive = true });
        context.Buyers.Add(new Buyer { Id = 2, Name = "Legacy Buyer", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 3, ClientId = 1, IsBuyer = false, IsActive = true, Location1 = "Supplier Dock" },
            new Location { Id = 4, ClientId = 2, IsBuyer = true, IsActive = true, BuyerStatus = 2, Location1 = "Buyer Dock" });
        context.Products.Add(new Product { Id = 5, Name = "OCC", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct { Id = 6, Supplier = 1, Location = 3, Product = 5, IsActive = true });
        context.BuyerSuppliers.Add(new BuyerSupplier
        {
            Id = 7,
            Buyer = 2,
            Supplier = 1,
            BuyerLocation = 4,
            SupplierLocation = 3,
            IsActive = true,
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierProductController(context));

        var result = await controller.AssignToBuyers(new SupplierProductAssignToBuyersViewModel
        {
            ProductId = 6,
            TiedBuyers =
            [
                new SupplierProductAssignBuyerRowViewModel
                {
                    BuyerId = 2,
                    IsSelected = true,
                    SelectedLocationIds = [4],
                    Rate = 111.25m,
                    EffectiveDate = new DateTime(2026, 9, 22),
                },
            ],
        });

        Assert.IsType<RedirectToActionResult>(result);
        var buyerSupplierProduct = await context.BuyerSupplierProducts.Include(product => product.BuyerProductRates).SingleAsync();
        Assert.Equal(7, buyerSupplierProduct.BuyerSupplierId);
        Assert.Equal(6, buyerSupplierProduct.SupplierProduct);
        Assert.Equal(99, buyerSupplierProduct.CreatedBy);
        var rate = Assert.Single(buyerSupplierProduct.BuyerProductRates);
        Assert.Equal(111.25m, rate.Price);
        Assert.Equal(new DateTime(2026, 9, 22), rate.EffectiveDate);
        Assert.True(rate.IsActive);
    }

    [Fact]
    public async Task SupplierProductAssignToBuyers_CreatesProposedMatchForActiveBuyerLocation()
    {
        await using var context = CreateContext();
        context.Suppliers.Add(new Supplier { Id = 1, Name = "Legacy Supplier", IsActive = true });
        context.Buyers.Add(new Buyer { Id = 2, Name = "Legacy Buyer", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 3, ClientId = 1, IsBuyer = false, IsActive = true, Location1 = "Supplier Dock" },
            new Location { Id = 4, ClientId = 2, IsBuyer = true, IsActive = true, BuyerStatus = 3, Location1 = "Buyer Dock" });
        context.Products.Add(new Product { Id = 5, Name = "OCC", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct { Id = 6, Supplier = 1, Location = 3, Product = 5, IsActive = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierProductController(context));

        var result = await controller.AssignToBuyers(new SupplierProductAssignToBuyersViewModel
        {
            ProductId = 6,
            ActiveBuyers =
            [
                new SupplierProductAssignBuyerRowViewModel
                {
                    BuyerId = 2,
                    IsSelected = true,
                    SelectedLocationIds = [4],
                    Rate = 125m,
                    EffectiveDate = new DateTime(2026, 9, 23),
                },
            ],
        });

        Assert.IsType<RedirectToActionResult>(result);
        var match = await context.BuyerSuppliers.Include(match => match.BuyerSupplierProducts).SingleAsync();
        Assert.Equal(3, match.Status);
        Assert.Equal(2, match.Buyer);
        Assert.Equal(1, match.Supplier);
        Assert.Equal(4, match.BuyerLocation);
        Assert.Equal(3, match.SupplierLocation);
        Assert.True(match.IsActive);
        var assignedProduct = Assert.Single(match.BuyerSupplierProducts);
        Assert.Equal(6, assignedProduct.SupplierProduct);
        Assert.Equal(125m, await context.BuyerProductRates.Where(rate => rate.BuyerSupplierProductId == assignedProduct.Id).Select(rate => rate.Price).SingleAsync());
    }

    [Fact]
    public async Task SupplierProductAssignToBuyers_DoesNotListAlreadyAssignedLocationAsUntied()
    {
        await using var context = CreateContext();
        context.Suppliers.Add(new Supplier { Id = 1, Name = "Legacy Supplier", IsActive = true });
        context.Buyers.Add(new Buyer { Id = 2, Name = "Legacy Buyer", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 3, ClientId = 1, IsBuyer = false, IsActive = true, Location1 = "Supplier Dock" },
            new Location { Id = 4, ClientId = 2, IsBuyer = true, IsActive = true, BuyerStatus = 2, Location1 = "Buyer Dock" });
        context.Products.Add(new Product { Id = 5, Name = "OCC", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct { Id = 6, Supplier = 1, Location = 3, Product = 5, IsActive = true });
        context.BuyerSuppliers.Add(new BuyerSupplier
        {
            Id = 7,
            Buyer = 2,
            Supplier = 1,
            BuyerLocation = 4,
            SupplierLocation = 3,
            IsActive = true,
            BuyerSupplierProducts = [new BuyerSupplierProduct { Id = 8, SupplierProduct = 6 }],
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierProductController(context));

        var result = Assert.IsType<ViewResult>(await controller.AssignToBuyers(6));
        var model = Assert.IsType<SupplierProductAssignToBuyersViewModel>(result.Model);

        Assert.Empty(model.TiedBuyers);
        Assert.DoesNotContain(model.ActiveBuyers.SelectMany(buyer => buyer.Locations), location => location.Id == 4);
    }

    [Fact]
    public async Task SupplierProductAssignToBuyers_ReusesExistingMatchForStaleActiveBuyerPost()
    {
        await using var context = CreateContext();
        context.Suppliers.Add(new Supplier { Id = 1, Name = "Legacy Supplier", IsActive = true });
        context.Buyers.Add(new Buyer { Id = 2, Name = "Legacy Buyer", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 3, ClientId = 1, IsBuyer = false, IsActive = true, Location1 = "Supplier Dock" },
            new Location { Id = 4, ClientId = 2, IsBuyer = true, IsActive = true, BuyerStatus = 2, Location1 = "Buyer Dock" });
        context.Products.Add(new Product { Id = 5, Name = "OCC", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct { Id = 6, Supplier = 1, Location = 3, Product = 5, IsActive = true });
        context.BuyerSuppliers.Add(new BuyerSupplier
        {
            Id = 7,
            Buyer = 2,
            Supplier = 1,
            BuyerLocation = 4,
            SupplierLocation = 3,
            IsActive = true,
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierProductController(context));

        var result = await controller.AssignToBuyers(new SupplierProductAssignToBuyersViewModel
        {
            ProductId = 6,
            ActiveBuyers =
            [
                new SupplierProductAssignBuyerRowViewModel
                {
                    BuyerId = 2,
                    IsSelected = true,
                    SelectedLocationIds = [4],
                    Rate = 125m,
                    EffectiveDate = new DateTime(2026, 9, 23),
                },
            ],
        });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(1, await context.BuyerSuppliers.CountAsync());
        var assignedProduct = await context.BuyerSupplierProducts.SingleAsync();
        Assert.Equal(7, assignedProduct.BuyerSupplierId);
        Assert.Equal(6, assignedProduct.SupplierProduct);
    }

    [Fact]
    public async Task BuyerDetails_IncludesFiveMostRecentActiveLoads()
    {
        await using var context = CreateContext();
        context.Buyers.Add(new Buyer { Id = 1, Name = "Recent Buyer", IsActive = true });
        context.Suppliers.Add(new Supplier { Id = 2, Name = "Recent Supplier", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 3, ClientId = 1, IsBuyer = true, IsActive = true, Location1 = "Buyer Dock" },
            new Location { Id = 4, ClientId = 2, IsBuyer = false, IsActive = true, Location1 = "Supplier Dock" });
        context.LoadStatuses.Add(new LoadStatus { Id = 5, Status = "Shipped" });
        context.Products.Add(new Product { Id = 6, Name = "OCC", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct { Id = 7, Supplier = 2, Location = 4, Product = 6, IsActive = true });
        for (var i = 0; i < 6; i++)
        {
            context.Loads.Add(new Load
            {
                Id = 10 + i,
                Buyer = 1,
                Supplier = 2,
                BuyerLocation = 3,
                SupplierLocation = 4,
                LoadStatus = 5,
                ShipmentDate = new DateTime(2026, 9, 1).AddDays(i),
                IsActive = true,
                LoadProducts = [new LoadProduct { Id = 100 + i, Product = 7 }],
            });
        }

        context.Loads.Add(new Load
        {
            Id = 99,
            Buyer = 1,
            Supplier = 2,
            BuyerLocation = 3,
            SupplierLocation = 4,
            LoadStatus = 5,
            ShipmentDate = new DateTime(2026, 10, 1),
            IsActive = false,
        });
        context.Loads.Add(new Load
        {
            Id = 16,
            Buyer = 1,
            Supplier = 2,
            BuyerLocation = 3,
            SupplierLocation = 4,
            LoadStatus = 5,
            ShipmentDate = new DateTime(2026, 9, 7),
            IsActive = true,
            LoadProducts = [new LoadProduct { Id = 198, Product = 999 }],
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new BuyerController(context));

        var result = Assert.IsType<ViewResult>(await controller.Details(1));
        var model = Assert.IsType<BuyerDetailsViewModel>(result.Model);

        Assert.Equal([16, 15, 14, 13, 12], model.RecentLoads.Select(load => load.Id).ToList());
        Assert.All(model.RecentLoads, load =>
        {
            Assert.Equal("Recent Supplier", load.SupplierName);
            Assert.Equal("Shipped", load.StatusName);
        });
        Assert.Equal(string.Empty, model.RecentLoads[0].Products);
        Assert.Equal("OCC", model.RecentLoads[1].Products);
    }

    [Fact]
    public async Task SupplierDetails_IncludesFiveMostRecentActiveLoads()
    {
        await using var context = CreateContext();
        context.Buyers.Add(new Buyer { Id = 1, Name = "Recent Buyer", IsActive = true });
        context.Suppliers.Add(new Supplier { Id = 2, Name = "Recent Supplier", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 3, ClientId = 1, IsBuyer = true, IsActive = true, Location1 = "Buyer Dock" },
            new Location { Id = 4, ClientId = 2, IsBuyer = false, IsActive = true, Location1 = "Supplier Dock" });
        context.LoadStatuses.Add(new LoadStatus { Id = 5, Status = "Pending" });
        context.Products.Add(new Product { Id = 6, Name = "Mixed Rags", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct { Id = 7, Supplier = 2, Location = 4, Product = 6, IsActive = true });
        for (var i = 0; i < 6; i++)
        {
            context.Loads.Add(new Load
            {
                Id = 20 + i,
                Buyer = 1,
                Supplier = 2,
                BuyerLocation = 3,
                SupplierLocation = 4,
                LoadStatus = 5,
                ShipmentDate = new DateTime(2026, 8, 1).AddDays(i),
                IsActive = true,
                LoadProducts = [new LoadProduct { Id = 200 + i, Product = 7 }],
            });
        }

        context.Loads.Add(new Load
        {
            Id = 99,
            Buyer = 1,
            Supplier = 2,
            BuyerLocation = 3,
            SupplierLocation = 4,
            LoadStatus = 5,
            ShipmentDate = new DateTime(2026, 10, 1),
            IsActive = false,
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierController(context));

        var result = Assert.IsType<ViewResult>(await controller.Details(2));
        var model = Assert.IsType<SupplierDetailsViewModel>(result.Model);

        Assert.Equal([25, 24, 23, 22, 21], model.RecentLoads.Select(load => load.Id).ToList());
        Assert.All(model.RecentLoads, load =>
        {
            Assert.Equal("Recent Buyer", load.BuyerName);
            Assert.Equal("Mixed Rags", load.Products);
            Assert.Equal("Pending", load.StatusName);
        });
    }

    [Fact]
    public async Task LocationToggleFavorite_AddsAndRemovesBuyerFavoriteForCurrentUser()
    {
        await using var context = CreateContext();
        context.Locations.Add(new Location { Id = 1, IsBuyer = true, IsActive = true, Location1 = "Buyer Dock" });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new LocationController(context));

        Assert.IsType<RedirectToActionResult>(await controller.ToggleFavorite(1));

        var favorite = await context.Favorites.SingleAsync();
        Assert.Equal(99, favorite.UserId);
        Assert.Equal(1, favorite.Location);
        Assert.True(favorite.IsBuyer);

        var details = Assert.IsType<ViewResult>(await controller.Details(1));
        Assert.True(Assert.IsType<LocationDetailsViewModel>(details.Model).IsFavorite);

        Assert.IsType<RedirectToActionResult>(await controller.ToggleFavorite(1));
        Assert.Empty(context.Favorites);
    }

    [Fact]
    public async Task LocationToggleFavorite_UsesSupplierFavoriteSide()
    {
        await using var context = CreateContext();
        context.Locations.Add(new Location { Id = 1, IsBuyer = false, IsActive = true, Location1 = "Supplier Dock" });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new LocationController(context));

        Assert.IsType<RedirectToActionResult>(await controller.ToggleFavorite(1));

        var favorite = await context.Favorites.SingleAsync();
        Assert.Equal(99, favorite.UserId);
        Assert.Equal(1, favorite.Location);
        Assert.False(favorite.IsBuyer);
    }

    [Fact]
    public async Task LocationDetails_FavoriteStateIsScopedToCurrentUserAndSide()
    {
        await using var context = CreateContext();
        context.Locations.Add(new Location { Id = 1, IsBuyer = true, IsActive = true, Location1 = "Buyer Dock" });
        context.Favorites.AddRange(
            new Favorite { Id = 1, UserId = 100, Location = 1, IsBuyer = true },
            new Favorite { Id = 2, UserId = 99, Location = 1, IsBuyer = false });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new LocationController(context));

        var details = Assert.IsType<ViewResult>(await controller.Details(1));

        Assert.False(Assert.IsType<LocationDetailsViewModel>(details.Model).IsFavorite);
    }

    [Fact]
    public async Task LocationToggleFavorite_RemovesDuplicateFavoritesTogether()
    {
        await using var context = CreateContext();
        context.Locations.Add(new Location { Id = 1, IsBuyer = true, IsActive = true, Location1 = "Buyer Dock" });
        context.Favorites.AddRange(
            new Favorite { Id = 1, UserId = 99, Location = 1, IsBuyer = true },
            new Favorite { Id = 2, UserId = 99, Location = 1, IsBuyer = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new LocationController(context));

        Assert.IsType<RedirectToActionResult>(await controller.ToggleFavorite(1));

        Assert.Empty(context.Favorites);
    }

    [Fact]
    public async Task LocationToggleFavorite_RejectsLocationWithoutBuyerSupplierSide()
    {
        await using var context = CreateContext();
        context.Locations.Add(new Location { Id = 1, IsBuyer = null, IsActive = true, Location1 = "Unassigned Dock" });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new LocationController(context));

        Assert.IsType<BadRequestResult>(await controller.ToggleFavorite(1));
        Assert.Empty(context.Favorites);
    }

    [Fact]
    public async Task BuyerIndex_FavoritesOnlyFiltersCurrentUserBuyerLocations()
    {
        await using var context = CreateContext();
        context.Buyers.AddRange(
            new Buyer { Id = 1, Name = "Favorite Buyer", IsActive = true },
            new Buyer { Id = 2, Name = "Other Buyer", IsActive = true },
            new Buyer { Id = 3, Name = "Wrong Side Buyer", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 10, ClientId = 1, IsBuyer = true, Location1 = "Favorite Buyer Dock", IsActive = true },
            new Location { Id = 11, ClientId = 2, IsBuyer = true, Location1 = "Other Buyer Dock", IsActive = true },
            new Location { Id = 12, ClientId = 3, IsBuyer = false, Location1 = "Wrong Side Dock", IsActive = true });
        context.Favorites.AddRange(
            new Favorite { Id = 20, UserId = 99, Location = 10, IsBuyer = true },
            new Favorite { Id = 21, UserId = 100, Location = 11, IsBuyer = true },
            new Favorite { Id = 22, UserId = 99, Location = 12, IsBuyer = false });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new BuyerController(context));
        controller.ControllerContext.HttpContext.Request.Query = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["favoritesOnly"] = "true",
            });

        var result = await controller.Index(search: null, sort: "name", desc: false);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<BuyerListItemViewModel>>(view.Model);
        var item = Assert.Single(model.Items);
        Assert.Equal("Favorite Buyer", item.Name);
        Assert.Equal("true", model.Page.AdditionalQueryParameters["favoritesOnly"]);
    }

    [Fact]
    public async Task SupplierIndex_FavoritesOnlyFiltersCurrentUserSupplierLocations()
    {
        await using var context = CreateContext();
        context.Suppliers.AddRange(
            new Supplier { Id = 1, Name = "Favorite Supplier", IsActive = true },
            new Supplier { Id = 2, Name = "Other Supplier", IsActive = true },
            new Supplier { Id = 3, Name = "Wrong Side Supplier", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 10, ClientId = 1, IsBuyer = false, Location1 = "Favorite Supplier Dock", IsActive = true },
            new Location { Id = 11, ClientId = 2, IsBuyer = false, Location1 = "Other Supplier Dock", IsActive = true },
            new Location { Id = 12, ClientId = 3, IsBuyer = true, Location1 = "Wrong Side Dock", IsActive = true });
        context.Favorites.AddRange(
            new Favorite { Id = 20, UserId = 99, Location = 10, IsBuyer = false },
            new Favorite { Id = 21, UserId = 100, Location = 11, IsBuyer = false },
            new Favorite { Id = 22, UserId = 99, Location = 12, IsBuyer = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierController(context));
        controller.ControllerContext.HttpContext.Request.Query = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["favoritesOnly"] = "true",
            });

        var result = await controller.Index(search: null, sort: "name", desc: false);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<SupplierListItemViewModel>>(view.Model);
        var item = Assert.Single(model.Items);
        Assert.Equal("Favorite Supplier", item.Name);
        Assert.Equal("true", model.Page.AdditionalQueryParameters["favoritesOnly"]);
    }

    [Fact]
    public async Task BuyerGetSubStatus_ReturnsChildStatuses()
    {
        await using var context = CreateContext();
        context.BuyerStatuses.AddRange(
            new BuyerStatus { Id = 1, Status = "Parent" },
            new BuyerStatus { Id = 2, Status = "Beta", ParentStatusId = 1 },
            new BuyerStatus { Id = 3, Status = "Alpha", ParentStatusId = 1 },
            new BuyerStatus { Id = 4, Status = "Other", ParentStatusId = 99 });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new BuyerController(context));

        var result = Assert.IsType<JsonResult>(await controller.GetSubStatus(1));
        var values = Assert.IsAssignableFrom<IEnumerable<object>>(result.Value).ToList();

        Assert.Equal(["Alpha", "Beta"], values.Select(GetTextProperty));
    }

    [Fact]
    public async Task BuyerChangeStatus_UpdatesBuyerLocationStatusAndOtherStatus()
    {
        await using var context = CreateContext();
        context.BuyerStatuses.Add(new BuyerStatus { Id = 2, Status = "Interested" });
        context.Locations.Add(new Location { Id = 10, IsBuyer = true, BuyerStatus = 1, OtherStatus = "Old", IsActive = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new BuyerController(context));

        var result = Assert.IsType<JsonResult>(await controller.ChangeStatus(10, 2, "Needs follow-up"));

        Assert.True((bool)result.Value!.GetType().GetProperty("success")!.GetValue(result.Value)!);
        var location = await context.Locations.FindAsync(10);
        Assert.Equal(2, location!.BuyerStatus);
        Assert.Equal("Needs follow-up", location.OtherStatus);
        Assert.Equal(99, location.UpdatedBy);
    }

    [Fact]
    public async Task SupplierChangeStatus_UpdatesSupplierLocationStatusAndClearsBlankOtherStatus()
    {
        await using var context = CreateContext();
        context.SupplierStatuses.Add(new SupplierStatus { Id = 2, Status = "Dormant" });
        context.Locations.Add(new Location { Id = 10, IsBuyer = false, SupplierStatus = 1, OtherStatus = "Old", IsActive = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierController(context));

        var result = Assert.IsType<JsonResult>(await controller.ChangeStatus(10, 2, " "));

        Assert.True((bool)result.Value!.GetType().GetProperty("success")!.GetValue(result.Value)!);
        var location = await context.Locations.FindAsync(10);
        Assert.Equal(2, location!.SupplierStatus);
        Assert.Null(location.OtherStatus);
        Assert.Equal(99, location.UpdatedBy);
    }

    [Fact]
    public async Task SupplierChangeStatus_RejectsBuyerLocation()
    {
        await using var context = CreateContext();
        context.SupplierStatuses.Add(new SupplierStatus { Id = 2, Status = "Dormant" });
        context.Locations.Add(new Location { Id = 10, IsBuyer = true, BuyerStatus = 1, IsActive = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierController(context));

        Assert.IsType<BadRequestObjectResult>(await controller.ChangeStatus(10, 2, null));
    }

    [Fact]
    public async Task SupplierProductRateChanges_RecordAndDisplayHistory()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { Id = 99, FirstName = "Ava", LastName = "Auditor", IsActive = true });
        context.Suppliers.Add(new Supplier { Id = 1, Name = "Supplier", IsActive = true });
        context.Locations.Add(new Location { Id = 2, ClientId = 1, IsBuyer = false, Location1 = "Supplier Dock", IsActive = true });
        context.Products.Add(new Product { Id = 3, Name = "OCC", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct { Id = 4, Supplier = 1, Location = 2, Product = 3, IsActive = true });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierProductController(context));

        Assert.IsType<RedirectToActionResult>(await controller.AddRate(new SupplierProductRateFormViewModel
        {
            SupplierProductId = 4,
            Price = 10.25m,
            EffectiveDate = new DateTime(2026, 9, 22),
        }));

        var rate = await context.SupplierProductRates.SingleAsync();
        Assert.IsType<RedirectToActionResult>(await controller.EditRate(rate.Id, new SupplierProductRateFormViewModel
        {
            Id = rate.Id,
            SupplierProductId = 4,
            Price = 12.50m,
            EffectiveDate = new DateTime(2026, 10, 1),
        }));
        Assert.IsType<RedirectToActionResult>(await controller.DeactivateRate(rate.Id, 4));

        var history = await context.SupplierProductHistories.OrderBy(h => h.Id).ToListAsync();
        Assert.Equal(["Add", "Update", "Delete"], history.Select(h => h.Action).ToList());
        Assert.Null(history[0].OldPrice);
        Assert.Equal(10.25m, history[0].NewPrice);
        Assert.Equal(10.25m, history[1].OldPrice);
        Assert.Equal(12.50m, history[1].NewPrice);
        Assert.Equal(12.50m, history[2].OldPrice);
        Assert.Null(history[2].NewPrice);
        Assert.Equal(new DateTime(2026, 10, 1), history[2].OldEffectiveDate);
        Assert.Null(history[2].NewEffectiveDate);

        var details = Assert.IsType<ViewResult>(await controller.Details(4));
        var model = Assert.IsType<SupplierProductDetailsViewModel>(details.Model);
        Assert.Equal(["Delete", "Update", "Add"], model.RateHistory.Select(h => h.Action).ToList());
        Assert.All(model.RateHistory, h => Assert.Equal("Ava Auditor", h.UserName));
    }

    [Fact]
    public async Task BuyerProductRateChanges_RecordAndDisplayHistory()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { Id = 99, FirstName = "Ava", LastName = "Auditor", IsActive = true });
        context.Buyers.Add(new Buyer { Id = 1, Name = "Buyer", IsActive = true });
        context.Suppliers.Add(new Supplier { Id = 2, Name = "Supplier", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 3, ClientId = 1, IsBuyer = true, Location1 = "Buyer Dock", IsActive = true },
            new Location { Id = 4, ClientId = 2, IsBuyer = false, Location1 = "Supplier Dock", IsActive = true });
        context.Products.Add(new Product { Id = 5, Name = "OCC", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct { Id = 6, Supplier = 2, Location = 4, Product = 5, IsActive = true });
        context.BuyerSuppliers.Add(new BuyerSupplier
        {
            Id = 7,
            Buyer = 1,
            Supplier = 2,
            BuyerLocation = 3,
            SupplierLocation = 4,
            IsActive = true,
        });
        context.BuyerSupplierProducts.Add(new BuyerSupplierProduct { Id = 8, BuyerSupplierId = 7, SupplierProduct = 6 });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new BuyerSupplierController(context));

        Assert.IsType<RedirectToActionResult>(await controller.AddProductRate(new BuyerProductRateFormViewModel
        {
            BuyerSupplierId = 7,
            BuyerSupplierProductId = 8,
            Price = 20.25m,
            EffectiveDate = new DateTime(2026, 9, 22),
        }));

        var rate = await context.BuyerProductRates.SingleAsync();
        Assert.IsType<RedirectToActionResult>(await controller.EditProductRate(rate.Id, new BuyerProductRateFormViewModel
        {
            Id = rate.Id,
            BuyerSupplierId = 7,
            BuyerSupplierProductId = 8,
            Price = 21.75m,
            EffectiveDate = new DateTime(2026, 10, 1),
        }));
        Assert.IsType<RedirectToActionResult>(await controller.DeactivateProductRate(rate.Id, 8));

        var history = await context.BuyerProductHistories.OrderBy(h => h.Id).ToListAsync();
        Assert.Equal(["Add", "Update", "Delete"], history.Select(h => h.Action).ToList());
        Assert.Null(history[0].OldPrice);
        Assert.Equal(20.25m, history[0].NewPrice);
        Assert.Equal(20.25m, history[1].OldPrice);
        Assert.Equal(21.75m, history[1].NewPrice);
        Assert.Equal(21.75m, history[2].OldPrice);
        Assert.Null(history[2].NewPrice);
        Assert.Equal(new DateTime(2026, 10, 1), history[2].OldEffectiveDate);
        Assert.Null(history[2].NewEffectiveDate);

        var details = Assert.IsType<ViewResult>(await controller.ProductDetails(8));
        var model = Assert.IsType<BuyerSupplierProductDetailsViewModel>(details.Model);
        Assert.Equal(["Delete", "Update", "Add"], model.RateHistory.Select(h => h.Action).ToList());
        Assert.All(model.RateHistory, h => Assert.Equal("Ava Auditor", h.UserName));
    }

    [Fact]
    public async Task SupplierProductPropagateBuyerRates_ListsActiveTiedBuyerProducts()
    {
        await using var context = CreateContext();
        SeedSupplierProductPropagationData(context);
        context.BuyerSuppliers.Add(new BuyerSupplier
        {
            Id = 20,
            Buyer = 2,
            Supplier = 1,
            BuyerLocation = 4,
            SupplierLocation = 3,
            IsActive = false,
            BuyerSupplierProducts = [new BuyerSupplierProduct { Id = 21, SupplierProduct = 6 }],
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierProductController(context));

        var result = Assert.IsType<ViewResult>(await controller.PropagateBuyerRates(6, null, null));
        var model = Assert.IsType<SupplierProductRatePropagationViewModel>(result.Model);

        var row = Assert.Single(model.BuyerProducts);
        Assert.Equal(8, row.BuyerSupplierProductId);
        Assert.Equal("Legacy Buyer", row.BuyerName);
        Assert.Equal("Buyer Dock", row.BuyerLocationName);
        Assert.Equal(90m, row.CurrentRate);
        Assert.Equal(new DateTime(2026, 9, 1), row.CurrentEffectiveDate);
        Assert.Equal(120m, row.UpdatedRate);
        Assert.Equal(new DateTime(2026, 9, 22), row.UpdatedEffectiveDate);
    }

    [Fact]
    public async Task SupplierProductPropagateBuyerRates_PrefillsLatestSupplierRate()
    {
        await using var context = CreateContext();
        SeedSupplierProductPropagationData(context);
        context.SupplierProductRates.Add(new SupplierProductRate
        {
            Id = 31,
            SupplierProductId = 6,
            Price = 135m,
            EffectiveDate = new DateTime(2026, 10, 1),
            CreatedDate = new DateTime(2026, 9, 25),
            IsActive = true,
        });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierProductController(context));

        var result = Assert.IsType<ViewResult>(await controller.PropagateBuyerRates(6, null, null));
        var model = Assert.IsType<SupplierProductRatePropagationViewModel>(result.Model);
        var row = Assert.Single(model.BuyerProducts);

        Assert.Equal(135m, row.UpdatedRate);
        Assert.Equal(new DateTime(2026, 10, 1), row.UpdatedEffectiveDate);
    }

    [Fact]
    public async Task SupplierProductPropagateBuyerRates_ShowsRowValidationErrors()
    {
        await using var context = CreateContext();
        SeedSupplierProductPropagationData(context);
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierProductController(context));

        var result = await controller.PropagateBuyerRates(new SupplierProductRatePropagationViewModel
        {
            SupplierProductId = 6,
            BuyerProducts =
            [
                new SupplierProductRatePropagationRowViewModel
                {
                    BuyerSupplierProductId = 8,
                    BuyerName = "Legacy Buyer",
                    BuyerLocationName = "Buyer Dock",
                    IsSelected = true,
                },
            ],
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("BuyerProducts[0].UpdatedRate"));
        Assert.True(controller.ModelState.ContainsKey("BuyerProducts[0].UpdatedEffectiveDate"));
    }

    [Fact]
    public async Task SupplierProductPropagateBuyerRates_AddsRatesAndHistoryForSelectedRows()
    {
        await using var context = CreateContext();
        SeedSupplierProductPropagationData(context);
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new SupplierProductController(context));
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider());

        var result = await controller.PropagateBuyerRates(new SupplierProductRatePropagationViewModel
        {
            SupplierProductId = 6,
            SuggestedRate = 130m,
            SuggestedEffectiveDate = new DateTime(2026, 10, 1),
            BuyerProducts =
            [
                new SupplierProductRatePropagationRowViewModel
                {
                    BuyerSupplierProductId = 8,
                    BuyerName = "Legacy Buyer",
                    BuyerLocationName = "Buyer Dock",
                    CurrentRate = 1m,
                    CurrentEffectiveDate = new DateTime(2020, 1, 1),
                    IsSelected = true,
                    UpdatedRate = 130m,
                    UpdatedEffectiveDate = new DateTime(2026, 10, 1),
                },
            ],
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(6, redirect.RouteValues!["id"]);

        var rates = await context.BuyerProductRates.Where(rate => rate.BuyerSupplierProductId == 8).OrderBy(rate => rate.Id).ToListAsync();
        Assert.Equal(2, rates.Count);
        Assert.Equal(130m, rates[1].Price);
        Assert.Equal(new DateTime(2026, 10, 1), rates[1].EffectiveDate);
        Assert.True(rates[1].IsActive);

        var history = await context.BuyerProductHistories.SingleAsync();
        Assert.Equal(8, history.BuyerSupplierProductId);
        Assert.Equal(90m, history.OldPrice);
        Assert.Equal(130m, history.NewPrice);
        Assert.Equal(new DateTime(2026, 9, 1), history.OldEffectiveDate);
        Assert.Equal(new DateTime(2026, 10, 1), history.NewEffectiveDate);
        Assert.Equal("Add", history.Action);
        Assert.Equal(99, history.UserId);
    }

    [Fact]
    public async Task LoadExport_ReturnsFilteredCsvWithOperationalColumns()
    {
        await using var context = CreateContext();
        context.Buyers.AddRange(
            new Buyer { Id = 1, Name = "Needle, Buyer", IsActive = true },
            new Buyer { Id = 2, Name = "Other Buyer", IsActive = true });
        context.Suppliers.Add(new Supplier { Id = 3, Name = "Export Supplier", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 4, ClientId = 1, IsBuyer = true, IsActive = true, Location1 = "Buyer Dock" },
            new Location { Id = 5, ClientId = 3, IsBuyer = false, IsActive = true, Location1 = "Supplier Dock" });
        context.LoadStatuses.Add(new LoadStatus { Id = 6, Status = "Shipped" });
        context.Products.Add(new Product { Id = 7, Name = "Mixed Rags", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct { Id = 8, Supplier = 3, Location = 5, Product = 7, IsActive = true });
        context.Loads.AddRange(
            new Load
            {
                Id = 9,
                Buyer = 1,
                Supplier = 3,
                BuyerLocation = 4,
                SupplierLocation = 5,
                LoadStatus = 6,
                ShipmentDate = new DateTime(2026, 9, 22),
                BookingDate = new DateTime(2026, 9, 20),
                BuyerRef = "BR-1",
                SupplierRef = "SR-1",
                Container = "CONT-1",
                BuyerInvoiceAmount = 123.45m,
                SupplierInvoiceAmount = 67.89m,
                IsActive = true,
                LoadProducts = [new LoadProduct { Id = 10, Product = 8 }],
            },
            new Load
            {
                Id = 11,
                Buyer = 2,
                Supplier = 3,
                BuyerLocation = 4,
                SupplierLocation = 5,
                LoadStatus = 6,
                ShipmentDate = new DateTime(2026, 9, 21),
                IsActive = true,
            });
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new LoadController(context));

        var result = Assert.IsType<FileContentResult>(await controller.Export(search: "Needle", sort: "shipmentDate", desc: true));
        var csv = Encoding.UTF8.GetString(result.FileContents);

        Assert.Equal("text/csv", result.ContentType);
        Assert.Equal("loads.csv", result.FileDownloadName);
        Assert.Contains("Id,Status,Buyer,Buyer Location,Supplier,Supplier Location,Products,Shipment Date", csv);
        Assert.Contains("9,Shipped,\"Needle, Buyer\",Buyer Dock,Export Supplier,Supplier Dock,Mixed Rags,2026-09-22,2026-09-20,BR-1,SR-1,CONT-1", csv);
        Assert.DoesNotContain("Other Buyer", csv);
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

    [Fact]
    public async Task LastLoadShippedReport_GroupsByBuyerAndUsesMostRecentLoad()
    {
        await using var context = CreateContext();
        SeedLastLoadReportData(context);
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new ReportController(context));

        var result = await controller.LastLoadShipped(new LastLoadShippedReportViewModel
        {
            ClientType = "Buyer",
            StartDate = DateTime.Today.AddDays(-90),
            EndDate = DateTime.Today,
        });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LastLoadShippedReportViewModel>(view.Model);
        Assert.Equal(2, model.Rows.Count);

        var buyerOne = model.Rows.Single(row => row.ClientName == "Buyer One");
        Assert.Equal(DateTime.Today.AddDays(-5), buyerOne.ShipmentDate);
        Assert.Equal(5, buyerOne.DaysSinceShipment);
        Assert.Equal("HDPE", buyerOne.Products);
        Assert.Equal("Ava Manager", buyerOne.AccountManagerName);
        Assert.Equal(DateTime.Today.AddDays(-2), buyerOne.LastCommunicationDate);
    }

    [Fact]
    public async Task LastLoadShippedReport_StaleDaysFiltersAfterMostRecentLoad()
    {
        await using var context = CreateContext();
        SeedLastLoadReportData(context);
        await context.SaveChangesAsync();

        var controller = WithLegacyUser(new ReportController(context));

        var result = await controller.LastLoadShipped(new LastLoadShippedReportViewModel
        {
            ClientType = "Buyer",
            StaleDays = 30,
        });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LastLoadShippedReportViewModel>(view.Model);
        var row = Assert.Single(model.Rows);
        Assert.Equal("Buyer Two", row.ClientName);
        Assert.Equal(60, row.DaysSinceShipment);
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

    private static void SeedSupplierProductPropagationData(EcoGoodzDbContext context)
    {
        context.Users.Add(new User { Id = 99, FirstName = "Ava", LastName = "Auditor", IsActive = true });
        context.Suppliers.Add(new Supplier { Id = 1, Name = "Legacy Supplier", IsActive = true });
        context.Buyers.Add(new Buyer { Id = 2, Name = "Legacy Buyer", IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 3, ClientId = 1, IsBuyer = false, IsActive = true, Location1 = "Supplier Dock" },
            new Location { Id = 4, ClientId = 2, IsBuyer = true, IsActive = true, BuyerStatus = 2, Location1 = "Buyer Dock" });
        context.Products.Add(new Product { Id = 5, Name = "OCC", IsActive = true });
        context.SupplierProducts.Add(new SupplierProduct
        {
            Id = 6,
            Supplier = 1,
            Location = 3,
            Product = 5,
            IsActive = true,
            SupplierProductRates =
            [
                new SupplierProductRate
                {
                    Id = 30,
                    Price = 120m,
                    EffectiveDate = new DateTime(2026, 9, 22),
                    CreatedDate = new DateTime(2026, 9, 20),
                    IsActive = true,
                },
            ],
        });
        context.BuyerSuppliers.Add(new BuyerSupplier
        {
            Id = 7,
            Buyer = 2,
            Supplier = 1,
            BuyerLocation = 4,
            SupplierLocation = 3,
            IsActive = true,
            BuyerSupplierProducts =
            [
                new BuyerSupplierProduct
                {
                    Id = 8,
                    SupplierProduct = 6,
                    BuyerProductRates =
                    [
                        new BuyerProductRate
                        {
                            Id = 9,
                            Price = 90m,
                            EffectiveDate = new DateTime(2026, 9, 1),
                            CreatedDate = new DateTime(2026, 8, 31),
                            IsActive = true,
                        },
                    ],
                },
            ],
        });
    }

    private static void SeedLastLoadReportData(EcoGoodzDbContext context)
    {
        context.Users.Add(new User { Id = 10, FirstName = "Ava", LastName = "Manager", IsActive = true });
        context.Buyers.AddRange(
            new Buyer { Id = 1, Name = "Buyer One", AccountManager = 10, IsActive = true },
            new Buyer { Id = 2, Name = "Buyer Two", AccountManager = 10, IsActive = true });
        context.Suppliers.Add(new Supplier { Id = 3, Name = "Supplier", AccountManager = 10, IsActive = true });
        context.Locations.AddRange(
            new Location { Id = 4, ClientId = 1, IsBuyer = true, Location1 = "Buyer One Dock", IsActive = true },
            new Location { Id = 5, ClientId = 2, IsBuyer = true, Location1 = "Buyer Two Dock", IsActive = true },
            new Location { Id = 6, ClientId = 3, IsBuyer = false, Location1 = "Supplier Dock", IsActive = true });
        context.LoadStatuses.AddRange(
            new LoadStatus { Id = 2, Status = "Shipped" },
            new LoadStatus { Id = 4, Status = "Completed" });
        context.Products.AddRange(
            new Product { Id = 7, Name = "PET", IsActive = true },
            new Product { Id = 8, Name = "HDPE", IsActive = true });
        context.SupplierProducts.AddRange(
            new SupplierProduct { Id = 9, Supplier = 3, Location = 6, Product = 7, IsActive = true },
            new SupplierProduct { Id = 10, Supplier = 3, Location = 6, Product = 8, IsActive = true });
        context.Loads.AddRange(
            new Load
            {
                Id = 20,
                Buyer = 1,
                Supplier = 3,
                BuyerLocation = 4,
                SupplierLocation = 6,
                BuyerAccountMgr = 10,
                LoadStatus = 2,
                ShipmentDate = DateTime.Today.AddDays(-40),
                IsActive = true,
                LoadProducts = [new LoadProduct { Id = 21, Product = 9 }],
            },
            new Load
            {
                Id = 22,
                Buyer = 1,
                Supplier = 3,
                BuyerLocation = 4,
                SupplierLocation = 6,
                BuyerAccountMgr = 10,
                LoadStatus = 4,
                ShipmentDate = DateTime.Today.AddDays(-5),
                IsActive = true,
                LoadProducts = [new LoadProduct { Id = 23, Product = 10 }],
            },
            new Load
            {
                Id = 24,
                Buyer = 2,
                Supplier = 3,
                BuyerLocation = 5,
                SupplierLocation = 6,
                BuyerAccountMgr = 10,
                LoadStatus = 2,
                ShipmentDate = DateTime.Today.AddDays(-60),
                IsActive = true,
                LoadProducts = [new LoadProduct { Id = 25, Product = 9 }],
            });
        context.Communications.Add(new Communication
        {
            Id = 30,
            ClientId = 1,
            IsBuyer = true,
            Date = DateTime.Today.AddDays(-2),
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

    private static string? GetTextProperty(object value) =>
        value.GetType().GetProperty("text")?.GetValue(value)?.ToString();

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = [];

        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _data.Clear();
            foreach (var value in values)
            {
                _data[value.Key] = value.Value;
            }
        }
    }
}
