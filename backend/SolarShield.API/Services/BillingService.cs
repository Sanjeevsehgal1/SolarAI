using Microsoft.EntityFrameworkCore;
using SolarShield.Core.DTOs;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.API.Services;

public class BillingService
{
    private readonly ApplicationDbContext _db;
    private readonly AccessControlService _access;

    public BillingService(ApplicationDbContext db, AccessControlService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<BillingSummaryDto?> GetBillingSummaryAsync(int userId, UserRole role, int? organizationId)
    {
        if (!_access.IsCompanyAdmin(role) && !_access.IsSuperAdmin(role))
            return null;

        var orgId = organizationId;
        if (_access.IsSuperAdmin(role) && !orgId.HasValue)
        {
            var firstOrg = await _db.Organizations.FirstOrDefaultAsync(o => o.IsActive);
            orgId = firstOrg?.Id;
        }

        if (!orgId.HasValue) return null;

        var org = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orgId.Value);
        if (org == null) return null;

        var capacity = await _db.SolarSites
            .Where(s => s.OrganizationId == orgId.Value && s.IsActive)
            .SumAsync(s => s.CapacityMw);

        var invoices = await _db.Invoices.AsNoTracking()
            .Where(i => i.OrganizationId == orgId.Value)
            .OrderByDescending(i => i.IssueDate)
            .Take(12)
            .Select(i => new InvoiceDto(
                i.Id, i.InvoiceNumber, i.BillingPeriod, i.TotalMw,
                i.Subtotal, i.Gst, i.Total, i.Status, i.IssueDate, i.DueDate, i.PaidDate))
            .ToListAsync();

        var current = invoices.FirstOrDefault();
        var nextBilling = DateTime.UtcNow.AddMonths(1);
        nextBilling = new DateTime(nextBilling.Year, nextBilling.Month, 1);

        return new BillingSummaryDto(
            org.SubscriptionPlan,
            nextBilling,
            capacity,
            current?.Total ?? 0,
            current?.Status ?? "None",
            invoices
        );
    }
}
