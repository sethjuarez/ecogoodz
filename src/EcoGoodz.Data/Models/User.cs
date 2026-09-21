using System;
using System.Collections.Generic;

namespace EcoGoodz.Data.Models;

public partial class User
{
    public int Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Initials { get; set; }

    public string? Email { get; set; }

    public string? OfficePhone { get; set; }

    public string? CellPhone { get; set; }

    public string? MiddleName { get; set; }

    public DateTime? BirthDate { get; set; }

    public string? Password { get; set; }

    public int? Role { get; set; }

    public DateTime? CreateOn { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public int? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? LastLoginTime { get; set; }

    public string? UserName { get; set; }

    public bool IsSendCompanyReport { get; set; }

    public bool IsSendManagerReport { get; set; }

    public bool IsShowCommunicationReport { get; set; }

    public virtual ICollection<AccountManagerGoal> AccountManagerGoalUpdatedByNavigations { get; set; } = new List<AccountManagerGoal>();

    public virtual ICollection<AccountManagerGoal> AccountManagerGoalUserNavigations { get; set; } = new List<AccountManagerGoal>();

    public virtual ICollection<AssignTask> AssignTasks { get; set; } = new List<AssignTask>();

    public virtual ICollection<Buyer> BuyerAccountManagerNavigations { get; set; } = new List<Buyer>();

    public virtual ICollection<Buyer> BuyerCreatedByNavigations { get; set; } = new List<Buyer>();

    public virtual ICollection<BuyerProductHistory> BuyerProductHistories { get; set; } = new List<BuyerProductHistory>();

    public virtual ICollection<BuyerProductRate> BuyerProductRates { get; set; } = new List<BuyerProductRate>();

    public virtual ICollection<BuyerSupplierHistory> BuyerSupplierHistories { get; set; } = new List<BuyerSupplierHistory>();

    public virtual ICollection<BuyerTrackingProductSupplier> BuyerTrackingProductSuppliers { get; set; } = new List<BuyerTrackingProductSupplier>();

    public virtual ICollection<Communication> Communications { get; set; } = new List<Communication>();

    public virtual ICollection<CompanyGoal> CompanyGoals { get; set; } = new List<CompanyGoal>();

    public virtual ICollection<CompanyNews> CompanyNews { get; set; } = new List<CompanyNews>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<GoalParameter> GoalParameters { get; set; } = new List<GoalParameter>();

    public virtual ICollection<Load> LoadBuyerAccountMgrNavigations { get; set; } = new List<Load>();

    public virtual ICollection<Load> LoadCreatedByNavigations { get; set; } = new List<Load>();

    public virtual ICollection<Load> LoadSupplierAccountMgrNavigations { get; set; } = new List<Load>();

    public virtual ICollection<Note> NoteCreatedByNavigations { get; set; } = new List<Note>();

    public virtual ICollection<Note> NoteUpdatedByNavigations { get; set; } = new List<Note>();

    public virtual Role? RoleNavigation { get; set; }

    public virtual ICollection<Supplier> SupplierAccountManagerNavigations { get; set; } = new List<Supplier>();

    public virtual ICollection<Supplier> SupplierCreatedByNavigations { get; set; } = new List<Supplier>();

    public virtual ICollection<SupplierProductHistory> SupplierProductHistories { get; set; } = new List<SupplierProductHistory>();

    public virtual ICollection<SupplierProductRate> SupplierProductRates { get; set; } = new List<SupplierProductRate>();

    public virtual ICollection<TaskHeadline> TaskHeadlines { get; set; } = new List<TaskHeadline>();

    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    public virtual ICollection<UserBuyer> UserBuyers { get; set; } = new List<UserBuyer>();

    public virtual ICollection<UserCustomField> UserCustomFields { get; set; } = new List<UserCustomField>();

    public virtual ICollection<UserInRole> UserInRoles { get; set; } = new List<UserInRole>();

    public virtual ICollection<UserProduct> UserProducts { get; set; } = new List<UserProduct>();

    public virtual ICollection<UserSupplier> UserSuppliers { get; set; } = new List<UserSupplier>();

    public virtual ICollection<CompanyNews> News { get; set; } = new List<CompanyNews>();
}
