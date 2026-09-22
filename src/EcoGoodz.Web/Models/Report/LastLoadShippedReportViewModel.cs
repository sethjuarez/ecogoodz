using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.Report;

public class LastLoadShippedReportViewModel
{
    public string ClientType { get; set; } = "Buyer";

    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public int? AccountManagerId { get; set; }

    [Range(1, 3650)]
    public int? StaleDays { get; set; }

    public List<AccountManagerReportOption> AccountManagers { get; set; } = [];

    public List<LastLoadShippedReportRow> Rows { get; set; } = [];

    public bool IsBuyerReport => !string.Equals(ClientType, "Supplier", StringComparison.OrdinalIgnoreCase);
}

public class AccountManagerReportOption
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class LastLoadShippedReportRow
{
    public int LoadId { get; set; }

    public int? ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public int? AccountManagerId { get; set; }

    public string? AccountManagerName { get; set; }

    public int? LocationId { get; set; }

    public string? LocationName { get; set; }

    public DateTime ShipmentDate { get; set; }

    public int DaysSinceShipment { get; set; }

    public string Products { get; set; } = string.Empty;

    public int? LastCommunicationId { get; set; }

    public DateTime? LastCommunicationDate { get; set; }
}
