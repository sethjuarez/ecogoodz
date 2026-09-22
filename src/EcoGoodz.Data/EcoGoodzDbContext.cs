using System;
using System.Collections.Generic;
using EcoGoodz.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Data;

public partial class EcoGoodzDbContext : DbContext
{
    public EcoGoodzDbContext(DbContextOptions<EcoGoodzDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountManagerGoal> AccountManagerGoals { get; set; }

    public virtual DbSet<AccountManagerGoalMonth> AccountManagerGoalMonths { get; set; }

    public virtual DbSet<AssignTask> AssignTasks { get; set; }

    public virtual DbSet<Buyer> Buyers { get; set; }

    public virtual DbSet<BuyerPaymentType> BuyerPaymentTypes { get; set; }

    public virtual DbSet<BuyerProduct> BuyerProducts { get; set; }

    public virtual DbSet<BuyerProductHistory> BuyerProductHistories { get; set; }

    public virtual DbSet<BuyerProductPackaging> BuyerProductPackagings { get; set; }

    public virtual DbSet<BuyerProductRate> BuyerProductRates { get; set; }

    public virtual DbSet<BuyerStatus> BuyerStatuses { get; set; }

    public virtual DbSet<BuyerSupplier> BuyerSuppliers { get; set; }

    public virtual DbSet<BuyerSupplierHistory> BuyerSupplierHistories { get; set; }

    public virtual DbSet<BuyerSupplierProduct> BuyerSupplierProducts { get; set; }

    public virtual DbSet<BuyerTrackingProduct> BuyerTrackingProducts { get; set; }

    public virtual DbSet<BuyerTrackingProductSupplier> BuyerTrackingProductSuppliers { get; set; }

    public virtual DbSet<Communication> Communications { get; set; }

    public virtual DbSet<CommunicationType> CommunicationTypes { get; set; }

    public virtual DbSet<CompanyGoal> CompanyGoals { get; set; }

    public virtual DbSet<CompanyGoalMonth> CompanyGoalMonths { get; set; }

    public virtual DbSet<CompanyNews> CompanyNews { get; set; }

    public virtual DbSet<Contact> Contacts { get; set; }

    public virtual DbSet<ContactInformation> ContactInformations { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<CustomField> CustomFields { get; set; }

    public virtual DbSet<CustomFieldType> CustomFieldTypes { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<FrequencyPackageType> FrequencyPackageTypes { get; set; }

    public virtual DbSet<GoalParameter> GoalParameters { get; set; }

    public virtual DbSet<GrossProfitProjection> GrossProfitProjections { get; set; }

    public virtual DbSet<GrossProfitProjectionLoad> GrossProfitProjectionLoads { get; set; }

    public virtual DbSet<Load> Loads { get; set; }

    public virtual DbSet<LoadProduct> LoadProducts { get; set; }

    public virtual DbSet<LoadStatus> LoadStatuses { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<MarkUpColor> MarkUpColors { get; set; }

    public virtual DbSet<MigrationHistory> MigrationHistories { get; set; }

    public virtual DbSet<Note> Notes { get; set; }

    public virtual DbSet<PackageType> PackageTypes { get; set; }

    public virtual DbSet<PaymentTerm> PaymentTerms { get; set; }

    public virtual DbSet<PaymentType> PaymentTypes { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductMarkUpColor> ProductMarkUpColors { get; set; }

    public virtual DbSet<ProductV2> ProductV2s { get; set; }

    public virtual DbSet<RegenerateReportLog> RegenerateReportLogs { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<State> States { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SupplierBuyerStatus> SupplierBuyerStatuses { get; set; }

    public virtual DbSet<SupplierPaymentType> SupplierPaymentTypes { get; set; }

    public virtual DbSet<SupplierProduct> SupplierProducts { get; set; }

    public virtual DbSet<SupplierProductHistory> SupplierProductHistories { get; set; }

    public virtual DbSet<SupplierProductRate> SupplierProductRates { get; set; }

    public virtual DbSet<SupplierStatus> SupplierStatuses { get; set; }

    public virtual DbSet<TaskItem> Tasks { get; set; }

    public virtual DbSet<TaskHeadline> TaskHeadlines { get; set; }

    public virtual DbSet<Threshold> Thresholds { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserBuyer> UserBuyers { get; set; }

    public virtual DbSet<UserCustomField> UserCustomFields { get; set; }

    public virtual DbSet<UserInRole> UserInRoles { get; set; }

    public virtual DbSet<UserProduct> UserProducts { get; set; }

    public virtual DbSet<UserSupplier> UserSuppliers { get; set; }

    public virtual DbSet<UserSupplierBuyerLocation> UserSupplierBuyerLocations { get; set; }

    public virtual DbSet<VwBuyerTrackingLoadCount> VwBuyerTrackingLoadCounts { get; set; }

    public virtual DbSet<VwContactInformation> VwContactInformations { get; set; }

    public virtual DbSet<VwSupplier> VwSuppliers { get; set; }

    public virtual DbSet<VwSupplierRate> VwSupplierRates { get; set; }

    public virtual DbSet<VwSupplierStatus> VwSupplierStatuses { get; set; }

    public virtual DbSet<VwSupplierTrackingLoadCount> VwSupplierTrackingLoadCounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountManagerGoal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.AccountManagerGoal");

            entity.ToTable("AccountManagerGoal");

            entity.Property(e => e.CommissionPer).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.M1).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M10).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M11).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M12).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M2).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M3).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M4).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M5).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M6).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M7).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M8).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M9).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.AccountManagerGoalUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_dbo.AccountManagerGoal_dbo.User_UpdatedBy");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.AccountManagerGoalUserNavigations).HasForeignKey(d => d.User);
        });

        modelBuilder.Entity<AccountManagerGoalMonth>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AccountM__3214EC07005F52AF");

            entity.ToTable("AccountManagerGoalMonth");

            entity.HasIndex(e => new { e.GoalId, e.Month }, "UQ_AccountManagerGoalMonth").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.Goal).WithMany(p => p.AccountManagerGoalMonths)
                .HasForeignKey(d => d.GoalId)
                .HasConstraintName("FK_AccountManagerGoalMonth_Goal");
        });

        modelBuilder.Entity<AssignTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.AssignTask");

            entity.ToTable("AssignTask");

            entity.Property(e => e.DoneDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.AssignedToNavigation).WithMany(p => p.AssignTasks)
                .HasForeignKey(d => d.AssignedTo)
                .HasConstraintName("FK_dbo.AssignTask_dbo.User_AssignedTo");

            entity.HasOne(d => d.TaskHeadlineNavigation).WithMany(p => p.AssignTasks)
                .HasForeignKey(d => d.TaskHeadline)
                .HasConstraintName("FK_dbo.AssignTask_dbo.TaskHeadlines_TaskHeadline");

            entity.HasOne(d => d.Task).WithMany(p => p.AssignTasks)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("FK_AssignTask_TaskId_Task");
        });

        modelBuilder.Entity<Buyer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Buyer");

            entity.ToTable("Buyer");

            entity.Property<string>("NameSort")
                .HasComputedColumnSql("CONVERT(nvarchar(450), [Name])", stored: true);
            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.AccountManagerNavigation).WithMany(p => p.BuyerAccountManagerNavigations)
                .HasForeignKey(d => d.AccountManager)
                .HasConstraintName("FK_dbo.Buyer_dbo.User_AccountManager");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BuyerCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_dbo.Buyer_dbo.User_CreatedBy");
        });

        modelBuilder.Entity<BuyerPaymentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.BuyerPaymentType");

            entity.ToTable("BuyerPaymentType");

            entity.Property(e => e.BuyerId).HasColumnName("Buyer_Id");

            entity.HasOne(d => d.Buyer).WithMany(p => p.BuyerPaymentTypes)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("FK_dbo.BuyerPaymentType_dbo.Buyer_Buyer");

            entity.HasOne(d => d.LocationNavigation).WithMany(p => p.BuyerPaymentTypes).HasForeignKey(d => d.Location);

            entity.HasOne(d => d.PaymentTypeNavigation).WithMany(p => p.BuyerPaymentTypes).HasForeignKey(d => d.PaymentType);
        });

        modelBuilder.Entity<BuyerProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.BuyerProduct");

            entity.ToTable("BuyerProduct");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.BuyerNavigation).WithMany(p => p.BuyerProducts).HasForeignKey(d => d.Buyer);

            entity.HasOne(d => d.LocationNavigation).WithMany(p => p.BuyerProducts).HasForeignKey(d => d.Location);

            entity.HasOne(d => d.ProductNavigation).WithMany(p => p.BuyerProducts).HasForeignKey(d => d.Product);
        });

        modelBuilder.Entity<BuyerProductHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.BuyerProductHistory");

            entity.ToTable("BuyerProductHistory");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.NewEffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.NewPrice).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.OldEffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.OldPrice).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.BuyerSupplierProduct).WithMany(p => p.BuyerProductHistories)
                .HasForeignKey(d => d.BuyerSupplierProductId)
                .HasConstraintName("FK_BuyerProductHistory_BuyerSupplierProductId_BuyerSupplierProduct");

            entity.HasOne(d => d.User).WithMany(p => p.BuyerProductHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_BuyerProductHistory_UserId_User");
        });

        modelBuilder.Entity<BuyerProductPackaging>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.BuyerProductPackaging");

            entity.ToTable("BuyerProductPackaging");

            entity.HasOne(d => d.BuyerProductNavigation).WithMany(p => p.BuyerProductPackagings)
                .HasForeignKey(d => d.BuyerProduct)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.PackagingNavigation).WithMany(p => p.BuyerProductPackagings)
                .HasForeignKey(d => d.Packaging)
                .HasConstraintName("FK_dbo.BuyerProductPackaging_dbo.PackageType_Packaging");
        });

        modelBuilder.Entity<BuyerProductRate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.BuyerProductRates");

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("BuyerProductRate_Trigger_Delete");
                    tb.HasTrigger("BuyerProductRates_Trigger_Insert");
                    tb.HasTrigger("BuyerProductRates_Trigger_Update");
                });

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.EffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.BuyerSupplierProduct).WithMany(p => p.BuyerProductRates)
                .HasForeignKey(d => d.BuyerSupplierProductId)
                .HasConstraintName("FK_BuyerProductRates_BuyerSupplierProductId_BuyerSupplierProduct");

            entity.HasOne(d => d.User).WithMany(p => p.BuyerProductRates)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_BuyerProductRates_UserId_User");
        });

        modelBuilder.Entity<BuyerStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.BuyerStatus");

            entity.ToTable("BuyerStatus");

            entity.HasIndex(e => e.ParentStatusId, "IX_ParentStatusId");

            entity.HasOne(d => d.ParentStatus).WithMany(p => p.InverseParentStatus)
                .HasForeignKey(d => d.ParentStatusId)
                .HasConstraintName("FK_dbo.BuyerStatus_dbo.BuyerStatus_ParentStatusId");
        });

        modelBuilder.Entity<BuyerSupplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.BuyerSuppliers");

            entity.ToTable(tb =>
                {
                    tb.HasTrigger("BuyerSuppliers_Trigger_Insert");
                    tb.HasTrigger("BuyerSuppliers_Trigger_Update");
                });

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.BuyerNavigation).WithMany(p => p.BuyerSuppliers).HasForeignKey(d => d.Buyer);

            entity.HasOne(d => d.BuyerLocationNavigation).WithMany(p => p.BuyerSupplierBuyerLocationNavigations)
                .HasForeignKey(d => d.BuyerLocation)
                .HasConstraintName("FK_dbo.BuyerSuppliers_dbo.Location_BuyerLocation");

            entity.HasOne(d => d.StatusNavigation).WithMany(p => p.BuyerSuppliers)
                .HasForeignKey(d => d.Status)
                .HasConstraintName("FK_dbo.BuyerSuppliers_dbo.SupplierBuyerStatus_Status");

            entity.HasOne(d => d.SupplierNavigation).WithMany(p => p.BuyerSuppliers).HasForeignKey(d => d.Supplier);

            entity.HasOne(d => d.SupplierLocationNavigation).WithMany(p => p.BuyerSupplierSupplierLocationNavigations)
                .HasForeignKey(d => d.SupplierLocation)
                .HasConstraintName("FK_dbo.BuyerSuppliers_dbo.Location_SupplierLocation");
        });

        modelBuilder.Entity<BuyerSupplierHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.BuyerSupplierHistory");

            entity.ToTable("BuyerSupplierHistory");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.BuyerSupplierHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_BuyerSupplierHistory_UserId_User");
        });

        modelBuilder.Entity<BuyerSupplierProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.BuyerSupplierProduct");

            entity.ToTable("BuyerSupplierProduct");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.BuyerSupplier).WithMany(p => p.BuyerSupplierProducts)
                .HasForeignKey(d => d.BuyerSupplierId)
                .HasConstraintName("FK_dbo.BuyerSupplierProduct_dbo.BuyerSuppliers_BuyerSupplierId");

            entity.HasOne(d => d.SupplierProductNavigation).WithMany(p => p.BuyerSupplierProducts).HasForeignKey(d => d.SupplierProduct);
        });

        modelBuilder.Entity<BuyerTrackingProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.BuyerTrackingProduct");

            entity.ToTable("BuyerTrackingProduct");

            entity.HasIndex(e => e.ProductId, "IX_ProductId");

            entity.HasIndex(e => e.UserBuyerId, "IX_UserBuyerId");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.BuyerTrackingProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BuyerTrackingProduct_ProductId_Product");

            entity.HasOne(d => d.UserBuyer).WithMany(p => p.BuyerTrackingProducts)
                .HasForeignKey(d => d.UserBuyerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BuyerTrackingProduct_UserBuyerId_UserBuyer");
        });

        modelBuilder.Entity<BuyerTrackingProductSupplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.BuyerTrackingProductSupplier");

            entity.ToTable("BuyerTrackingProductSupplier");

            entity.HasIndex(e => e.BuyerProductId, "IX_BuyerProductId");

            entity.HasIndex(e => e.LocationId, "IX_LocationId");

            entity.HasIndex(e => e.NoteUpdatedBy, "IX_NoteUpdatedBy");

            entity.HasIndex(e => e.SupplierId, "IX_SupplierId");

            entity.HasIndex(e => e.UserBuyerId, "IX_UserBuyerId");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.BuyerProduct).WithMany(p => p.BuyerTrackingProductSuppliers)
                .HasForeignKey(d => d.BuyerProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo.BuyerTrackingProductSupplier_dbo.BuyerTrackingProduct_BuyerProductId");

            entity.HasOne(d => d.Location).WithMany(p => p.BuyerTrackingProductSuppliers)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BuyerTrackingProductSupplier_LocationId_Location");

            entity.HasOne(d => d.NoteUpdatedByNavigation).WithMany(p => p.BuyerTrackingProductSuppliers)
                .HasForeignKey(d => d.NoteUpdatedBy)
                .HasConstraintName("FK_dbo.BuyerTrackingProductSupplier_dbo.User_NoteUpdatedBy");

            entity.HasOne(d => d.Supplier).WithMany(p => p.BuyerTrackingProductSuppliers)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BuyerTrackingProductSupplier_SupplierId_Supplier");

            entity.HasOne(d => d.UserBuyer).WithMany(p => p.BuyerTrackingProductSuppliers)
                .HasForeignKey(d => d.UserBuyerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BuyerTrackingProductSupplier_UserBuyerId_UserBuyer");
        });

        modelBuilder.Entity<Communication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Communication");

            entity.ToTable("Communication");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.CommunicationTypeNavigation).WithMany(p => p.Communications).HasForeignKey(d => d.CommunicationType);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Communications)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_dbo.Communication_dbo.User_CreatedBy");
        });

        modelBuilder.Entity<CommunicationType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.CommunicationType");

            entity.ToTable("CommunicationType");
        });

        modelBuilder.Entity<CompanyGoal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.CompanyGoals");

            entity.Property(e => e.M1).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M10).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M11).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M12).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M2).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M3).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M4).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M5).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M6).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M7).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M8).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.M9).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.P1).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.P10).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.P11).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.P12).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.P2).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.P3).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.P4).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.P5).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.P6).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.P7).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.P8).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.P9).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CompanyGoals)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_dbo.CompanyGoals_dbo.User_UpdatedBy");
        });

        modelBuilder.Entity<CompanyGoalMonth>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CompanyG__3214EC0733D0E719");

            entity.ToTable("CompanyGoalMonth");

            entity.HasIndex(e => new { e.CompanyGoalId, e.Month }, "UQ_CompanyGoalMonth").IsUnique();

            entity.Property(e => e.GoalAmount).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.PriorYearActual).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.CompanyGoal).WithMany(p => p.CompanyGoalMonths)
                .HasForeignKey(d => d.CompanyGoalId)
                .HasConstraintName("FK_CompanyGoalMonth_Goal");
        });

        modelBuilder.Entity<CompanyNews>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.CompanyNews");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.News).HasMaxLength(200);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CompanyNews)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_dbo.CompanyNews_dbo.User_CreatedBy");

            entity.HasOne(d => d.NewsNavigation).WithMany(p => p.InverseNewsNavigation)
                .HasForeignKey(d => d.NewsId)
                .HasConstraintName("FK_dbo.CompanyNews_dbo.CompanyNews_NewsId");

            entity.HasMany(d => d.Users).WithMany(p => p.News)
                .UsingEntity<Dictionary<string, object>>(
                    "RemovedCompanyNews",
                    r => r.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK_dbo.RemovedCompanyNews_dbo.User_UserId"),
                    l => l.HasOne<CompanyNews>().WithMany()
                        .HasForeignKey("NewsId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_dbo.RemovedCompanyNews_dbo.CompanyNews_NewsId"),
                    j =>
                    {
                        j.HasKey("NewsId", "UserId").HasName("PK_dbo.RemovedCompanyNews");
                    });
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Contact");

            entity.ToTable("Contact");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.ContactNavigation).WithMany(p => p.Contacts)
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo.Contact_dbo.ContactInformation_ContactId");

            entity.HasOne(d => d.LocationNavigation).WithMany(p => p.Contacts).HasForeignKey(d => d.Location);
        });

        modelBuilder.Entity<ContactInformation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.ContactInformation");

            entity.ToTable("ContactInformation");

            entity.HasOne(d => d.CountryNavigation).WithMany(p => p.ContactInformations).HasForeignKey(d => d.Country);

            entity.HasOne(d => d.StateNavigation).WithMany(p => p.ContactInformations)
                .HasForeignKey(d => d.State)
                .HasConstraintName("FK_dbo.ContactInformation_dbo.States_State");
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Country");

            entity.ToTable("Country");
        });

        modelBuilder.Entity<CustomField>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.CustomField");

            entity.ToTable("CustomField");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<CustomFieldType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.CustomFieldType");

            entity.ToTable("CustomFieldType");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Favorites");

            entity.HasOne(d => d.LocationNavigation).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.Location)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Favorites_UserId_User");
        });

        modelBuilder.Entity<FrequencyPackageType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.FrequencyPackageType");

            entity.ToTable("FrequencyPackageType");
        });

        modelBuilder.Entity<GoalParameter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.GoalParameters");

            entity.Property(e => e.AnnualMinimum).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Excess).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Maximum).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.AccountManagerGoalNavigation).WithMany(p => p.GoalParameters)
                .HasForeignKey(d => d.AccountManagerGoal)
                .HasConstraintName("FK_dbo.GoalParameters_dbo.AccountManagerGoal_AccountManagerGoal");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.GoalParameters)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_dbo.GoalParameters_dbo.User_UpdatedBy");
        });

        modelBuilder.Entity<GrossProfitProjection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.GrossProfitProjection");

            entity.ToTable("GrossProfitProjection");

            entity.Property(e => e.Averagelbs).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BuyerRate).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.CreatedTime).HasColumnType("datetime");
            entity.Property(e => e.Estimate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SupplierRate).HasColumnType("decimal(18, 3)");
        });

        modelBuilder.Entity<GrossProfitProjectionLoad>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GrossPro__3214EC0701FCF6E5");

            entity.ToTable("GrossProfitProjectionLoad");

            entity.HasIndex(e => new { e.GrossProfitProjectionId, e.LoadId }, "UQ_GrossProfitProjectionLoad").IsUnique();

            entity.HasOne(d => d.GrossProfitProjection).WithMany(p => p.GrossProfitProjectionLoads)
                .HasForeignKey(d => d.GrossProfitProjectionId)
                .HasConstraintName("FK_GPPLoad_Projection");

            entity.HasOne(d => d.Load).WithMany(p => p.GrossProfitProjectionLoads)
                .HasForeignKey(d => d.LoadId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GPPLoad_Load");
        });

        modelBuilder.Entity<Load>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Loads");

            entity.Property(e => e.BookingDate).HasColumnType("datetime");
            entity.Property(e => e.BuyerInvoiceAmount).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.BuyerInvoiceDate).HasColumnType("datetime");
            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.FreightAmountBilled).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.FreightAmountQuoted).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ShipmentDate).HasColumnType("datetime");
            entity.Property(e => e.SupplierInvoiceAmount).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.SupplierInvoiceDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.BuyerNavigation).WithMany(p => p.Loads)
                .HasForeignKey(d => d.Buyer)
                .HasConstraintName("FK_dbo.Loads_dbo.Buyer_Buyer");

            entity.HasOne(d => d.BuyerAccountMgrNavigation).WithMany(p => p.LoadBuyerAccountMgrNavigations)
                .HasForeignKey(d => d.BuyerAccountMgr)
                .HasConstraintName("FK_dbo.Loads_dbo.User_BuyerAccountMgr");

            entity.HasOne(d => d.BuyerLocationNavigation).WithMany(p => p.LoadBuyerLocationNavigations)
                .HasForeignKey(d => d.BuyerLocation)
                .HasConstraintName("FK_dbo.Loads_dbo.Location_BuyerLocation");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.LoadCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_dbo.Loads_dbo.User_CreatedBy");

            entity.HasOne(d => d.LoadStatusNavigation).WithMany(p => p.Loads)
                .HasForeignKey(d => d.LoadStatus)
                .HasConstraintName("FK_dbo.Loads_dbo.LoadStatus_LoadStatus");

            entity.HasOne(d => d.SupplierNavigation).WithMany(p => p.Loads)
                .HasForeignKey(d => d.Supplier)
                .HasConstraintName("FK_dbo.Loads_dbo.Supplier_Supplier");

            entity.HasOne(d => d.SupplierAccountMgrNavigation).WithMany(p => p.LoadSupplierAccountMgrNavigations)
                .HasForeignKey(d => d.SupplierAccountMgr)
                .HasConstraintName("FK_dbo.Loads_dbo.User_SupplierAccountMgr");

            entity.HasOne(d => d.SupplierLocationNavigation).WithMany(p => p.LoadSupplierLocationNavigations)
                .HasForeignKey(d => d.SupplierLocation)
                .HasConstraintName("FK_dbo.Loads_dbo.Location_SupplierLocation");
        });

        modelBuilder.Entity<LoadProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.LoadProduct");

            entity.ToTable("LoadProduct");

            entity.HasIndex(e => new { e.Load, e.Product }, "IX_Load_Product");

            entity.HasOne(d => d.LoadNavigation).WithMany(p => p.LoadProducts)
                .HasForeignKey(d => d.Load)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo.LoadProduct_dbo.Loads_Load");

            entity.HasOne(d => d.ProductNavigation).WithMany(p => p.LoadProducts)
                .HasForeignKey(d => d.Product)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo.LoadProduct_dbo.SupplierProduct_Product");
        });

        modelBuilder.Entity<LoadStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.LoadStatus");

            entity.ToTable("LoadStatus");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Location");

            entity.ToTable("Location");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.Drayage1).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Drayage2).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Drayage3).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.Location1).HasColumnName("Location");
            entity.Property(e => e.NpaymentTerms).HasColumnName("NPaymentTerms");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.BuyerStatusNavigation).WithMany(p => p.Locations)
                .HasForeignKey(d => d.BuyerStatus)
                .HasConstraintName("FK_dbo.Location_dbo.BuyerStatus_BuyerStatus");

            entity.HasOne(d => d.CountryNavigation).WithMany(p => p.Locations)
                .HasForeignKey(d => d.Country)
                .HasConstraintName("FK_dbo.Location_dbo.Country_Country");

            entity.HasOne(d => d.PaymentTermsNavigation).WithMany(p => p.Locations)
                .HasForeignKey(d => d.PaymentTerms)
                .HasConstraintName("FK_dbo.Location_dbo.PaymentTerms_PaymentTerms");

            entity.HasOne(d => d.StateNavigation).WithMany(p => p.Locations)
                .HasForeignKey(d => d.State)
                .HasConstraintName("FK_dbo.Location_dbo.States_State");

            entity.HasOne(d => d.SupplierStatusNavigation).WithMany(p => p.Locations)
                .HasForeignKey(d => d.SupplierStatus)
                .HasConstraintName("FK_dbo.Location_dbo.SupplierStatus_Status");
        });

        modelBuilder.Entity<MarkUpColor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.MarkUpColors");
        });

        modelBuilder.Entity<MigrationHistory>(entity =>
        {
            entity.HasKey(e => new { e.MigrationId, e.ContextKey }).HasName("PK_dbo.__MigrationHistory2");

            entity.ToTable("__MigrationHistory");

            entity.Property(e => e.MigrationId).HasMaxLength(150);
            entity.Property(e => e.ContextKey).HasMaxLength(300);
            entity.Property(e => e.ProductVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Notes");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.BuyerLocationNavigation).WithMany(p => p.NoteBuyerLocationNavigations)
                .HasForeignKey(d => d.BuyerLocation)
                .HasConstraintName("FK_dbo.Notes_dbo.Location_Location");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.NoteCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_dbo.Notes_dbo.User_CreatedBy");

            entity.HasOne(d => d.ProductNavigation).WithMany(p => p.Notes)
                .HasForeignKey(d => d.Product)
                .HasConstraintName("FK_Notes_Product_SupplierProduct");

            entity.HasOne(d => d.SupplierLocationNavigation).WithMany(p => p.NoteSupplierLocationNavigations)
                .HasForeignKey(d => d.SupplierLocation)
                .HasConstraintName("FK_dbo.Notes_dbo.Location_SupplierLocation");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.NoteUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_dbo.Notes_dbo.User_UpdatedBy");
        });

        modelBuilder.Entity<PackageType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.PackageType");

            entity.ToTable("PackageType");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.Type).HasMaxLength(100);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<PaymentTerm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.PaymentTerms");
        });

        modelBuilder.Entity<PaymentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.PaymentType");

            entity.ToTable("PaymentType");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Product");

            entity.ToTable("Product");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<ProductMarkUpColor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.ProductMarkUpColor");

            entity.ToTable("ProductMarkUpColor");

            entity.Property(e => e.MaxRate).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.MinRate).HasColumnType("decimal(18, 6)");

            entity.HasOne(d => d.MarkUpColorNavigation).WithMany(p => p.ProductMarkUpColors)
                .HasForeignKey(d => d.MarkUpColor)
                .HasConstraintName("FK_dbo.ProductMarkUpColor_dbo.MarkUpColors_MarkUpColor");

            entity.HasOne(d => d.ProductNavigation).WithMany(p => p.ProductMarkUpColors)
                .HasForeignKey(d => d.Product)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo.ProductMarkUpColor_dbo.Product_Product");
        });

        modelBuilder.Entity<ProductV2>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.ProductV2");

            entity.ToTable("ProductV2");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<RegenerateReportLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.RegenerateReportLogs");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Role");

            entity.ToTable("Role");
        });

        modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.States");

            entity.HasOne(d => d.Country).WithMany(p => p.States)
                .HasForeignKey(d => d.CountryId)
                .HasConstraintName("FK_dbo.States_dbo.Country_CountryId");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Supplier");

            entity.ToTable("Supplier");

            entity.Property<string>("NameSort")
                .HasComputedColumnSql("CONVERT(nvarchar(450), [Name])", stored: true);
            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.AccountManagerNavigation).WithMany(p => p.SupplierAccountManagerNavigations)
                .HasForeignKey(d => d.AccountManager)
                .HasConstraintName("FK_dbo.Supplier_dbo.User_AccountManager");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.SupplierCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_dbo.Supplier_dbo.User_CreatedBy");
        });

        modelBuilder.Entity<SupplierBuyerStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.SupplierBuyerStatus");

            entity.ToTable("SupplierBuyerStatus");
        });

        modelBuilder.Entity<SupplierPaymentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.SupplierPaymentType");

            entity.ToTable("SupplierPaymentType");

            entity.Property(e => e.SupplierId).HasColumnName("Supplier_Id");

            entity.HasOne(d => d.LocationNavigation).WithMany(p => p.SupplierPaymentTypes).HasForeignKey(d => d.Location);

            entity.HasOne(d => d.PaymentTypeNavigation).WithMany(p => p.SupplierPaymentTypes)
                .HasForeignKey(d => d.PaymentType)
                .HasConstraintName("FK_dbo.SupplierPaymentType_dbo.PaymentType_PaymentType");

            entity.HasOne(d => d.Supplier).WithMany(p => p.SupplierPaymentTypes)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_dbo.SupplierPaymentType_dbo.Supplier_Supplier");
        });

        modelBuilder.Entity<SupplierProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.SupplierProduct");

            entity.ToTable("SupplierProduct");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.PackagingPrice).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.FrequencyPackageTypeNavigation).WithMany(p => p.SupplierProducts)
                .HasForeignKey(d => d.FrequencyPackageType)
                .HasConstraintName("FK_dbo.SupplierProduct_dbo.FrequencyPackageType_FrequencyPackageType");

            entity.HasOne(d => d.LocationNavigation).WithMany(p => p.SupplierProducts)
                .HasForeignKey(d => d.Location)
                .HasConstraintName("FK_dbo.SupplierProduct_dbo.Location_Location");

            entity.HasOne(d => d.PackagingNavigation).WithMany(p => p.SupplierProducts)
                .HasForeignKey(d => d.Packaging)
                .HasConstraintName("FK_dbo.SupplierProduct_dbo.PackageType_Packaging");

            entity.HasOne(d => d.ProductNavigation).WithMany(p => p.SupplierProducts)
                .HasForeignKey(d => d.Product)
                .HasConstraintName("FK_dbo.SupplierProduct_dbo.Product_Product");

            entity.HasOne(d => d.SupplierNavigation).WithMany(p => p.SupplierProducts)
                .HasForeignKey(d => d.Supplier)
                .HasConstraintName("FK_dbo.SupplierProduct_dbo.Supplier_Supplier");
        });

        modelBuilder.Entity<SupplierProductHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.SupplierProductHistory");

            entity.ToTable("SupplierProductHistory");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.NewEffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.NewPrice).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.OldEffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.OldPrice).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.SupplierProduct).WithMany(p => p.SupplierProductHistories)
                .HasForeignKey(d => d.SupplierProductId)
                .HasConstraintName("FK_dbo.SupplierProductHistory_dbo.SupplierProduct_SupplierProductId");

            entity.HasOne(d => d.User).WithMany(p => p.SupplierProductHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_dbo.SupplierProductHistory_dbo.User_UserId");
        });

        modelBuilder.Entity<SupplierProductRate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.SupplierProductRate");

            entity.ToTable("SupplierProductRate", tb =>
                {
                    tb.HasTrigger("SupplierProductRate_Trigger_Delete");
                    tb.HasTrigger("SupplierProductRate_Trigger_Insert");
                    tb.HasTrigger("SupplierProductRate_Trigger_Update");
                });

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.EffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.SupplierProduct).WithMany(p => p.SupplierProductRates)
                .HasForeignKey(d => d.SupplierProductId)
                .HasConstraintName("FK_dbo.SupplierProductRate_dbo.SupplierProduct_SupplierProductId");

            entity.HasOne(d => d.User).WithMany(p => p.SupplierProductRates)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_dbo.SupplierProductRate_dbo.User_UserId");
        });

        modelBuilder.Entity<SupplierStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.SupplierStatus");

            entity.ToTable("SupplierStatus");

            entity.HasOne(d => d.ParentStatus).WithMany(p => p.InverseParentStatus)
                .HasForeignKey(d => d.ParentStatusId)
                .HasConstraintName("FK_dbo.SupplierStatus_dbo.SupplierStatus_ParentStatusId");
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Task");

            entity.ToTable("Task");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.Duedate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_dbo.Task_dbo.User_CreatedBy");
        });

        modelBuilder.Entity<TaskHeadline>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.TaskHeadlines");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.TaskHeadlines)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_dbo.TaskHeadlines_dbo.User_UserId");
        });

        modelBuilder.Entity<Threshold>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.Threshold");

            entity.ToTable("Threshold");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.Percentage).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.User");

            entity.ToTable("User");

            entity.Property(e => e.BirthDate).HasColumnType("datetime");
            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.LastLoginTime).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.RoleNavigation).WithMany(p => p.Users)
                .HasForeignKey(d => d.Role)
                .HasConstraintName("FK_dbo.User_dbo.Role_Role");
        });

        modelBuilder.Entity<UserBuyer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.UserBuyer");

            entity.ToTable("UserBuyer");

            entity.HasIndex(e => e.BuyerId, "IX_BuyerId");

            entity.HasIndex(e => e.LocationId, "IX_LocationId");

            entity.HasIndex(e => e.UserId, "IX_UserId");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.Buyer).WithMany(p => p.UserBuyers)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("FK_dbo.UserBuyer_dbo.Buyer_BuyerId");

            entity.HasOne(d => d.Location).WithMany(p => p.UserBuyers)
                .HasForeignKey(d => d.LocationId)
                .HasConstraintName("FK_dbo.UserBuyer_dbo.Location_LocationId");

            entity.HasOne(d => d.User).WithMany(p => p.UserBuyers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_dbo.UserBuyer_dbo.User_UserId");
        });

        modelBuilder.Entity<UserCustomField>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.UserCustomField");

            entity.ToTable("UserCustomField");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.CustomFieldNavigation).WithMany(p => p.UserCustomFields)
                .HasForeignKey(d => d.CustomField)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo.UserCustomField_dbo.CustomField_CustomField");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.UserCustomFields)
                .HasForeignKey(d => d.User)
                .HasConstraintName("FK_dbo.UserCustomField_dbo.User_User");
        });

        modelBuilder.Entity<UserInRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.UserInRoles");

            entity.HasOne(d => d.RoleNavigation).WithMany(p => p.UserInRoles)
                .HasForeignKey(d => d.Role)
                .HasConstraintName("FK_dbo.UserInRoles_dbo.Role_Role");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.UserInRoles)
                .HasForeignKey(d => d.User)
                .HasConstraintName("FK_dbo.UserInRoles_dbo.User_User");
        });

        modelBuilder.Entity<UserProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.UserProduct");

            entity.ToTable("UserProduct");

            entity.HasIndex(e => e.Product, "IX_Product");

            entity.HasIndex(e => e.UserId, "IX_UserId");

            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.ProductNavigation).WithMany(p => p.UserProducts)
                .HasForeignKey(d => d.Product)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo.UserProduct_dbo.Product_Product");

            entity.HasOne(d => d.User).WithMany(p => p.UserProducts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo.UserProduct_dbo.User_UserId");
        });

        modelBuilder.Entity<UserSupplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_dbo.UserSupplier");

            entity.ToTable("UserSupplier");

            entity.HasIndex(e => e.LocationId, "IX_LocationId");

            entity.HasIndex(e => e.NoteUpdatedBy, "IX_NoteUpdatedBy");

            entity.HasIndex(e => e.SupplierId, "IX_SupplierId");

            entity.HasIndex(e => e.UserProductId, "IX_UserProductId");

            entity.Property(e => e.BuyerLocationIds).HasMaxLength(300);
            entity.Property(e => e.CreateOn).HasColumnType("datetime");
            entity.Property(e => e.SupplierNote).HasMaxLength(500);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

            entity.HasOne(d => d.Location).WithMany(p => p.UserSuppliers)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo.UserSupplier_dbo.Location_LocationId");

            entity.HasOne(d => d.NoteUpdatedByNavigation).WithMany(p => p.UserSuppliers)
                .HasForeignKey(d => d.NoteUpdatedBy)
                .HasConstraintName("FK_dbo.UserSupplier_dbo.User_NoteUpdatedBy");

            entity.HasOne(d => d.Supplier).WithMany(p => p.UserSuppliers)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_dbo.UserSupplier_dbo.Supplier_SupplierId");

            entity.HasOne(d => d.UserProduct).WithMany(p => p.UserSuppliers)
                .HasForeignKey(d => d.UserProductId)
                .HasConstraintName("FK_dbo.UserSupplier_dbo.UserProduct_UserProductId");
        });

        modelBuilder.Entity<UserSupplierBuyerLocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserSupp__3214EC07CA3A74F7");

            entity.ToTable("UserSupplierBuyerLocation");

            entity.HasIndex(e => new { e.UserSupplierId, e.LocationId }, "UQ_UserSupplierBuyerLocation").IsUnique();

            entity.HasOne(d => d.Location).WithMany(p => p.UserSupplierBuyerLocations)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_USBL_Location");

            entity.HasOne(d => d.UserSupplier).WithMany(p => p.UserSupplierBuyerLocations)
                .HasForeignKey(d => d.UserSupplierId)
                .HasConstraintName("FK_USBL_UserSupplier");
        });

        modelBuilder.Entity<VwBuyerTrackingLoadCount>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwBuyerTrackingLoadCount");

            entity.Property(e => e.BuyerProductId).HasColumnName("BuyerProductID");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Pendingload).HasColumnName("pendingload");
            entity.Property(e => e.Shipmentdate).HasColumnName("shipmentdate");
            entity.Property(e => e.Shippedload).HasColumnName("shippedload");
            entity.Property(e => e.UserBuyerId).HasColumnName("UserBuyerID");
        });

        modelBuilder.Entity<VwContactInformation>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwContactInformation");
        });

        modelBuilder.Entity<VwSupplier>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwSupplier");

            entity.Property(e => e.SubStatus)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwSupplierRate>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwSupplierRate");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Rate).HasColumnType("decimal(18, 4)");
        });

        modelBuilder.Entity<VwSupplierStatus>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwSupplierStatus");

            entity.Property(e => e.Status).HasColumnName("STATUS");
            entity.Property(e => e.SubStatus)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwSupplierTrackingLoadCount>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VwSupplierTrackingLoadCount");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
