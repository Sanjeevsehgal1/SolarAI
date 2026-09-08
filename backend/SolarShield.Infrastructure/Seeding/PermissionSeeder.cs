using Microsoft.EntityFrameworkCore;
using SolarShield.Core.Constants;
using SolarShield.Core.Entities;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.Infrastructure.Seeding;

public static class PermissionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.Permissions.AnyAsync()) return;

        var permissions = new[]
        {
            new Permission { Code = PermissionCodes.OrganizationView, Name = "View Organizations", Category = "Organization" },
            new Permission { Code = PermissionCodes.OrganizationCreate, Name = "Create Organizations", Category = "Organization" },
            new Permission { Code = PermissionCodes.OrganizationEdit, Name = "Edit Organizations", Category = "Organization" },
            new Permission { Code = PermissionCodes.PlantCreate, Name = "Create Plants", Category = "Plant" },
            new Permission { Code = PermissionCodes.PlantEdit, Name = "Edit Plants", Category = "Plant" },
            new Permission { Code = PermissionCodes.PlantView, Name = "View Plants", Category = "Plant" },
            new Permission { Code = PermissionCodes.UserCreate, Name = "Create Users", Category = "User" },
            new Permission { Code = PermissionCodes.UserEdit, Name = "Edit Users", Category = "User" },
            new Permission { Code = PermissionCodes.UserView, Name = "View Users", Category = "User" },
            new Permission { Code = PermissionCodes.AlertView, Name = "View Alerts", Category = "Operations" },
            new Permission { Code = PermissionCodes.AlertAcknowledge, Name = "Acknowledge Alerts", Category = "Operations" },
            new Permission { Code = PermissionCodes.MaintenanceCreate, Name = "Create Maintenance", Category = "Operations" },
            new Permission { Code = PermissionCodes.MaintenanceAssign, Name = "Assign Maintenance", Category = "Operations" },
            new Permission { Code = PermissionCodes.MaintenanceComplete, Name = "Complete Maintenance", Category = "Operations" },
            new Permission { Code = PermissionCodes.ReportView, Name = "View Reports", Category = "Reports" },
            new Permission { Code = PermissionCodes.ReportExport, Name = "Export Reports", Category = "Reports" },
            new Permission { Code = PermissionCodes.ScadaView, Name = "View SCADA", Category = "SCADA" },
            new Permission { Code = PermissionCodes.AiPredictionView, Name = "View AI Predictions", Category = "AI" },
            new Permission { Code = PermissionCodes.PlatformAdmin, Name = "Platform Administration", Category = "Platform" }
        };

        db.Permissions.AddRange(permissions);
        await db.SaveChangesAsync();
    }
}
