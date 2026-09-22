using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.Report;

public class CommunicationReportViewModel
{
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public List<CommunicationReportUserColumn> Users { get; set; } = [];

    public List<CommunicationReportRow> Rows { get; set; } = [];
}

public class CommunicationReportUserColumn
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class CommunicationReportRow
{
    public DateTime Date { get; set; }

    public List<CommunicationReportUserCell> UserCells { get; set; } = [];
}

public class CommunicationReportUserCell
{
    public int UserId { get; set; }

    public List<CommunicationReportCount> Counts { get; set; } = [];
}

public class CommunicationReportCount
{
    public string Label { get; set; } = string.Empty;

    public bool IsProductMetric { get; set; }

    public int BuyerCount { get; set; }

    public int SupplierCount { get; set; }

    public int TotalCount => BuyerCount + SupplierCount;
}
