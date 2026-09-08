namespace SolarShield.Core.Entities;

public class Invoice
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string BillingPeriod { get; set; } = string.Empty;
    public double TotalMw { get; set; }
    public decimal BaseSubscription { get; set; }
    public decimal MwMonitoring { get; set; }
    public decimal AiModule { get; set; }
    public decimal ScadaIntegration { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Gst { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }

    public Organization Organization { get; set; } = null!;
}
