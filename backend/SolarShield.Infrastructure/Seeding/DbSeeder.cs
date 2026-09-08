using Microsoft.EntityFrameworkCore;
using SolarShield.Core.Entities;
using SolarShield.Core.Enums;
using SolarShield.Infrastructure.Data;

namespace SolarShield.Infrastructure.Seeding;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await PermissionSeeder.SeedAsync(db);

        if (!await db.Users.AnyAsync())
            await SeedInitialDataAsync(db);
        else
            await MigrateMultiTenantAsync(db);

        await EnsureDemoPlantsAsync(db);
        await EnsureAssetsAndScadaAsync(db);
        await EnsureInvoicesAsync(db);
    }

    public static async Task EnsureDemoDataAsync(ApplicationDbContext db)
    {
        var siteCount = await db.SolarSites.CountAsync();
        if (siteCount == 0) return;

        var sitesWithFaults = await db.Faults.Select(f => f.SiteId).Distinct().CountAsync();
        var sitesWithGen = await db.GenerationRecords.Select(g => g.SiteId).Distinct().CountAsync();
        if (sitesWithFaults >= siteCount && sitesWithGen >= siteCount && await db.Notifications.AnyAsync())
            return;

        var technician = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Technician);
        var supervisor = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Supervisor || u.Role == UserRole.PlantAdmin);
        if (technician == null || supervisor == null) return;

        var sites = await db.SolarSites.OrderBy(s => s.Id).ToListAsync();
        for (var i = 0; i < sites.Count; i++)
        {
            var site = sites[i];
            await EnsureZonesAsync(db, site);

            var zones = await db.Zones.Where(z => z.SiteId == site.Id).OrderBy(z => z.Id).ToArrayAsync();
            if (zones.Length == 0) continue;

            await EnsureAnalyticsDataAsync(db, site, zones, i);
            await EnsureCleaningSchedulesAsync(db, site, zones, i);
            await EnsureOperationalDataAsync(db, site, zones, technician.Id, supervisor.Id, i);
        }

        await EnsureNotificationsAsync(db);
        await EnsureFaultCommentsAsync(db);
        await EnsureAssetsAndScadaAsync(db);
        await EnsureInvoicesAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task SeedInitialDataAsync(ApplicationDbContext db)
    {
        var superAdmin = CreateUser("superadmin@solarshield.ai", "+919000000001", "Super@123", "Platform Super Admin", UserRole.SuperAdmin, null);
        var legacyAdmin = CreateUser("admin@solarshield.ai", "+919876543210", "Admin@123", "Shubham Sehgal", UserRole.CompanyAdmin, null);

        db.Users.AddRange(superAdmin, legacyAdmin);
        await db.SaveChangesAsync();

        var abcSolar = new Organization
        {
            Name = "ABC Solar Pvt Ltd",
            Code = "ABC",
            ContactEmail = "admin@abc-solar.com",
            SubscriptionPlan = "Enterprise",
            MaxPlants = 10
        };
        var greenEnergy = new Organization
        {
            Name = "Green Energy Corp",
            Code = "GEC",
            ContactEmail = "admin@greenenergy.com",
            SubscriptionPlan = "Standard",
            MaxPlants = 5
        };
        db.Organizations.AddRange(abcSolar, greenEnergy);
        await db.SaveChangesAsync();

        legacyAdmin.OrganizationId = abcSolar.Id;

        var abcAdmin = CreateUser("admin@abc-solar.com", "+919876543211", "Admin@123", "Rahul Sharma", UserRole.CompanyAdmin, abcSolar.Id);
        var abcTechAdmin = CreateUser("tech.admin@abc-solar.com", "+919876543212", "Tech@123", "Priya Mehta", UserRole.CompanyTechnicalAdmin, abcSolar.Id);
        var plantAdminRj = CreateUser("plant.admin.rj@abc-solar.com", "+919876543213", "Plant@123", "Amit Verma", UserRole.PlantAdmin, abcSolar.Id);
        var supervisor = CreateUser("supervisor@solarshield.ai", "+919876543214", "Super@123", "Rajesh Kumar", UserRole.Supervisor, abcSolar.Id);
        var technician = CreateUser("tech@solarshield.ai", "+919876543215", "Tech@123", "Amit Sharma", UserRole.Technician, abcSolar.Id);
        var viewer = CreateUser("viewer@abc-solar.com", "+919876543216", "View@123", "Neha Singh", UserRole.Viewer, abcSolar.Id);
        var gecAdmin = CreateUser("admin@greenenergy.com", "+919876543217", "Admin@123", "Vikram Patel", UserRole.CompanyAdmin, greenEnergy.Id);

        db.Users.AddRange(abcAdmin, abcTechAdmin, plantAdminRj, supervisor, technician, viewer, gecAdmin);
        await db.SaveChangesAsync();

        var plantRj = CreatePlant(abcSolar.Id, "RJ-001", "Rajasthan Solar Farm - Phase 1", "Jodhpur, Rajasthan", 3.5, 26.2389, 73.0243, 82);
        var plantGj = CreatePlant(abcSolar.Id, "GJ-001", "Gujarat Solar Park", "Kutch, Gujarat", 5.0, 23.7337, 69.8597, 84);
        var plantHr = CreatePlant(abcSolar.Id, "HR-001", "Haryana Grid Plant", "Hisar, Haryana", 2.8, 29.1492, 75.7217, 81);
        var plantMh = CreatePlant(greenEnergy.Id, "MH-001", "Maharashtra Solar Unit", "Pune, Maharashtra", 1.5, 18.5204, 73.8567, 80);

        db.SolarSites.AddRange(plantRj, plantGj, plantHr, plantMh);
        await db.SaveChangesAsync();

        db.UserSiteAssignments.AddRange(
            new UserSiteAssignment { UserId = plantAdminRj.Id, SiteId = plantRj.Id },
            new UserSiteAssignment { UserId = supervisor.Id, SiteId = plantRj.Id },
            new UserSiteAssignment { UserId = supervisor.Id, SiteId = plantGj.Id },
            new UserSiteAssignment { UserId = technician.Id, SiteId = plantRj.Id },
            new UserSiteAssignment { UserId = technician.Id, SiteId = plantGj.Id },
            new UserSiteAssignment { UserId = technician.Id, SiteId = plantHr.Id },
            new UserSiteAssignment { UserId = viewer.Id, SiteId = plantGj.Id },
            new UserSiteAssignment { UserId = viewer.Id, SiteId = plantMh.Id }
        );

        await db.SaveChangesAsync();
    }

    public static async Task MigrateMultiTenantAsync(ApplicationDbContext db)
    {
        if (await db.Organizations.AnyAsync()) return;

        var defaultOrg = new Organization
        {
            Name = "ABC Solar Pvt Ltd",
            Code = "ABC",
            ContactEmail = "admin@solarshield.ai",
            SubscriptionPlan = "Enterprise",
            MaxPlants = 10
        };
        db.Organizations.Add(defaultOrg);
        await db.SaveChangesAsync();

        var sites = await db.SolarSites.ToListAsync();
        var codes = new[] { "RJ-001", "GJ-001", "HR-001", "MH-001" };
        for (int i = 0; i < sites.Count; i++)
        {
            sites[i].OrganizationId = defaultOrg.Id;
            if (string.IsNullOrEmpty(sites[i].PlantCode))
                sites[i].PlantCode = i < codes.Length ? codes[i] : $"PL-{sites[i].Id:D3}";
        }

        var users = await db.Users.Where(u => u.Role != UserRole.SuperAdmin).ToListAsync();
        foreach (var user in users)
        {
            if (!user.OrganizationId.HasValue)
                user.OrganizationId = defaultOrg.Id;
            if (user.Role == UserRole.Admin)
                user.Role = UserRole.CompanyAdmin;
        }

        if (!await db.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
        {
            db.Users.Add(CreateUser("superadmin@solarshield.ai", "+919000000001", "Super@123", "Platform Super Admin", UserRole.SuperAdmin, null));
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureDemoPlantsAsync(ApplicationDbContext db)
    {
        var abc = await db.Organizations.FirstOrDefaultAsync(o => o.Code == "ABC")
            ?? await db.Organizations.OrderBy(o => o.Id).FirstOrDefaultAsync();
        if (abc == null) return;

        var gec = await db.Organizations.FirstOrDefaultAsync(o => o.Code == "GEC");
        if (gec == null)
        {
            gec = new Organization
            {
                Name = "Green Energy Corp",
                Code = "GEC",
                ContactEmail = "admin@greenenergy.com",
                SubscriptionPlan = "Standard",
                MaxPlants = 5
            };
            db.Organizations.Add(gec);
            await db.SaveChangesAsync();
        }

        var demoPlants = new[]
        {
            CreatePlant(abc.Id, "RJ-001", "Rajasthan Solar Farm - Phase 1", "Jodhpur, Rajasthan", 3.5, 26.2389, 73.0243, 82),
            CreatePlant(abc.Id, "GJ-001", "Gujarat Solar Park", "Kutch, Gujarat", 5.0, 23.7337, 69.8597, 84),
            CreatePlant(abc.Id, "HR-001", "Haryana Grid Plant", "Hisar, Haryana", 2.8, 29.1492, 75.7217, 81),
            CreatePlant(gec.Id, "MH-001", "Maharashtra Solar Unit", "Pune, Maharashtra", 1.5, 18.5204, 73.8567, 80)
        };

        foreach (var plant in demoPlants)
        {
            if (await db.SolarSites.AnyAsync(s => s.PlantCode == plant.PlantCode)) continue;
            db.SolarSites.Add(plant);
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureZonesAsync(ApplicationDbContext db, SolarSite site)
    {
        if (await db.Zones.AnyAsync(z => z.SiteId == site.Id)) return;

        if (site.PlantCode == "RJ-001")
        {
            db.Zones.AddRange(
                new Zone { SiteId = site.Id, Name = "Zone A - North", StringId = "STR-A1", Latitude = 26.2395, Longitude = 73.0238, PanelCount = 1200 },
                new Zone { SiteId = site.Id, Name = "Zone B - East", StringId = "STR-B1", Latitude = 26.2385, Longitude = 73.0250, PanelCount = 1100 },
                new Zone { SiteId = site.Id, Name = "Zone C - South", StringId = "STR-C1", Latitude = 26.2378, Longitude = 73.0240, PanelCount = 1150 },
                new Zone { SiteId = site.Id, Name = "Zone D - West", StringId = "STR-D1", Latitude = 26.2390, Longitude = 73.0225, PanelCount = 1050 }
            );
        }
        else
        {
            db.Zones.AddRange(
                new Zone { SiteId = site.Id, Name = "Zone A", StringId = "STR-A1", Latitude = site.Latitude + 0.001, Longitude = site.Longitude, PanelCount = 800 },
                new Zone { SiteId = site.Id, Name = "Zone B", StringId = "STR-B1", Latitude = site.Latitude, Longitude = site.Longitude + 0.001, PanelCount = 750 },
                new Zone { SiteId = site.Id, Name = "Zone C", StringId = "STR-C1", Latitude = site.Latitude - 0.001, Longitude = site.Longitude, PanelCount = 720 }
            );
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureAnalyticsDataAsync(ApplicationDbContext db, SolarSite site, Zone[] zones, int siteIndex)
    {
        var today = DateTime.UtcNow.Date;
        var random = new Random(42 + siteIndex);

        var existingDates = await db.GenerationRecords
            .Where(g => g.SiteId == site.Id)
            .Select(g => g.RecordDate)
            .ToListAsync();

        for (var i = 29; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            if (existingDates.Contains(date)) continue;

            var irradiance = 4.2 + random.NextDouble() * 2.2;
            var theoretical = site.CapacityMw * 1000 * irradiance * 0.85;
            var pr = 74 + random.NextDouble() * 12 - (i == 2 ? 8 : 0);
            db.GenerationRecords.Add(new GenerationRecord
            {
                SiteId = site.Id,
                RecordDate = date,
                TheoreticalKwh = Math.Round(theoretical, 2),
                ActualKwh = Math.Round(theoretical * pr / 100, 2),
                PerformanceRatio = Math.Round(pr, 2),
                Irradiance = Math.Round(irradiance, 2),
                Temperature = Math.Round(28 + random.NextDouble() * 12, 1)
            });
        }

        var periodStart = today.AddDays(-13);
        var existingSoiling = await db.SoilingRecords
            .Where(s => s.SiteId == site.Id && s.RecordDate >= periodStart)
            .Select(s => new { s.ZoneId, s.RecordDate })
            .ToListAsync();
        var existingSoilingSet = existingSoiling
            .Select(s => (s.ZoneId, s.RecordDate))
            .ToHashSet();

        var baseSoiling = new[] { 12.5, 28.3, 8.1, 45.2, 22.0, 35.6 };
        for (var z = 0; z < zones.Length; z++)
        {
            for (var i = 13; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                if (existingSoilingSet.Contains((zones[z].Id, date)))
                    continue;

                var baseIndex = baseSoiling[(z + siteIndex) % baseSoiling.Length];
                var soiling = Math.Round(Math.Min(55, baseIndex - i * 0.3 + random.NextDouble() * 2), 1);
                db.SoilingRecords.Add(new SoilingRecord
                {
                    SiteId = site.Id,
                    ZoneId = zones[z].Id,
                    RecordDate = date,
                    SoilingIndex = soiling,
                    EstimatedLossPercent = Math.Round(soiling * 0.35, 2)
                });
            }
        }
    }

    private static async Task EnsureCleaningSchedulesAsync(ApplicationDbContext db, SolarSite site, Zone[] zones, int siteIndex)
    {
        if (await db.CleaningSchedules.AnyAsync(c => c.SiteId == site.Id)) return;

        var today = DateTime.UtcNow.Date;
        var baseSoiling = new[] { 12.5, 28.3, 45.2, 18.0, 33.0, 40.0 };

        for (var z = 0; z < zones.Length; z++)
        {
            var soiling = baseSoiling[(z + siteIndex) % baseSoiling.Length];
            var priority = soiling > 40 ? 1 : soiling > 25 ? 2 : 3;
            db.CleaningSchedules.Add(new CleaningSchedule
            {
                SiteId = site.Id,
                ZoneId = zones[z].Id,
                ScheduledDate = today.AddDays(priority == 1 ? 1 : priority == 2 ? 3 : 7),
                SoilingIndexAtSchedule = soiling,
                Priority = priority,
                Notes = priority == 1 ? "Urgent cleaning required" : null
            });
        }
    }

    private static async Task EnsureOperationalDataAsync(
        ApplicationDbContext db, SolarSite site, Zone[] zones, int technicianId, int supervisorId, int siteIndex)
    {
        if (await db.Faults.AnyAsync(f => f.SiteId == site.Id)) return;

        var zoneA = zones[0];
        var zoneB = zones.Length > 1 ? zones[1] : zones[0];
        var zoneC = zones.Length > 2 ? zones[2] : zones[0];

        db.Faults.AddRange(
            new Fault
            {
                SiteId = site.Id,
                ZoneId = zoneB.Id,
                Type = FaultType.StringMismatch,
                Severity = siteIndex == 0 ? FaultSeverity.Critical : FaultSeverity.Medium,
                Status = FaultStatus.Open,
                Title = $"{site.PlantCode} - String voltage mismatch",
                Description = $"Zone {zoneB.Name} showing lower voltage than adjacent strings.",
                IsPredicted = true,
                ConfidenceScore = 85 + siteIndex,
                DetectedAt = DateTime.UtcNow.AddHours(-4 - siteIndex)
            },
            new Fault
            {
                SiteId = site.Id,
                ZoneId = zoneC.Id,
                Type = FaultType.SoilingLoss,
                Severity = FaultSeverity.Medium,
                Status = siteIndex % 2 == 0 ? FaultStatus.Assigned : FaultStatus.Open,
                Title = $"High soiling - {zoneC.Name}",
                Description = "Soiling index above threshold. Cleaning recommended within 48 hours.",
                IsPredicted = true,
                ConfidenceScore = 90.0,
                AssignedToUserId = siteIndex % 2 == 0 ? technicianId : null,
                DetectedAt = DateTime.UtcNow.AddHours(-10 - siteIndex)
            },
            new Fault
            {
                SiteId = site.Id,
                Type = FaultType.InverterError,
                Severity = FaultSeverity.Low,
                Status = FaultStatus.Resolved,
                Title = $"Inverter comm check - {site.PlantCode}",
                Description = "Communication restored after cable reseating.",
                IsPredicted = false,
                FixNotes = "RJ45 connector replaced on inverter bus.",
                DetectedAt = DateTime.UtcNow.AddDays(-2),
                ResolvedAt = DateTime.UtcNow.AddDays(-1)
            }
        );

        db.WorkTasks.AddRange(
            new WorkTask
            {
                SiteId = site.Id,
                ZoneId = zoneB.Id,
                Title = $"Inspect inverter at {site.PlantCode}",
                Description = "Check inverter communication cable and terminations.",
                Status = WorkTaskStatus.Open,
                AssignedToUserId = technicianId,
                CreatedByUserId = supervisorId,
                IsAutoGenerated = true,
                ScheduledAt = DateTime.UtcNow.AddHours(2 + siteIndex),
                LocationLabel = "Inverter Room"
            },
            new WorkTask
            {
                SiteId = site.Id,
                ZoneId = zoneC.Id,
                Title = $"Clean {zoneC.Name} panels",
                Description = "Scheduled cleaning based on soiling alert.",
                Status = siteIndex == 0 ? WorkTaskStatus.InProgress : WorkTaskStatus.Open,
                AssignedToUserId = technicianId,
                CreatedByUserId = supervisorId,
                IsAutoGenerated = true,
                ScheduledAt = DateTime.UtcNow,
                StartedAt = siteIndex == 0 ? DateTime.UtcNow.AddHours(-1) : null,
                LocationLabel = zoneC.Name
            },
            new WorkTask
            {
                SiteId = site.Id,
                ZoneId = zoneA.Id,
                Title = $"IV curve test - {zoneA.Name}",
                Description = "Routine IV curve tracing for performance baseline.",
                Status = WorkTaskStatus.Done,
                AssignedToUserId = technicianId,
                CreatedByUserId = supervisorId,
                ScheduledAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow.AddHours(-6),
                LocationLabel = zoneA.Name
            }
        );
    }

    private static async Task EnsureNotificationsAsync(ApplicationDbContext db)
    {
        if (await db.Notifications.AnyAsync()) return;

        var supervisor = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Supervisor);
        var technician = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Technician);
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.CompanyAdmin);
        if (supervisor == null || technician == null) return;

        var criticalFault = await db.Faults
            .Where(f => f.Severity == FaultSeverity.Critical && f.Status != FaultStatus.Resolved)
            .OrderByDescending(f => f.DetectedAt)
            .FirstOrDefaultAsync();

        var now = DateTime.UtcNow;
        var notifications = new List<Notification>
        {
            new()
            {
                UserId = supervisor.Id,
                Title = "Critical fault detected",
                Message = "String voltage mismatch reported at Rajasthan Solar Farm.",
                IsRead = false,
                LinkType = "fault",
                LinkId = criticalFault?.Id,
                CreatedAt = now.AddMinutes(-30)
            },
            new()
            {
                UserId = technician.Id,
                Title = "New task assigned",
                Message = "Clean Zone D panels - high priority cleaning scheduled for today.",
                IsRead = false,
                LinkType = "task",
                CreatedAt = now.AddHours(-1)
            },
            new()
            {
                UserId = technician.Id,
                Title = "Cleaning reminder",
                Message = "Zone B soiling index crossed 25%. Schedule cleaning this week.",
                IsRead = true,
                LinkType = "cleaning",
                CreatedAt = now.AddHours(-5)
            }
        };

        if (admin != null)
        {
            notifications.Add(new Notification
            {
                UserId = admin.Id,
                Title = "Weekly performance report ready",
                Message = "Plant performance summary for all ABC Solar sites is available.",
                IsRead = false,
                LinkType = "analytics",
                CreatedAt = now.AddHours(-3)
            });
        }

        db.Notifications.AddRange(notifications);
    }

    private static async Task EnsureFaultCommentsAsync(ApplicationDbContext db)
    {
        if (await db.FaultComments.AnyAsync()) return;

        var fault = await db.Faults
            .Where(f => f.Status == FaultStatus.Assigned || f.Status == FaultStatus.Open)
            .OrderByDescending(f => f.DetectedAt)
            .FirstOrDefaultAsync();
        if (fault == null) return;

        var supervisor = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Supervisor || u.Role == UserRole.PlantAdmin);
        var technician = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Technician);
        if (supervisor == null || technician == null) return;

        db.FaultComments.AddRange(
            new FaultComment
            {
                FaultId = fault.Id,
                UserId = supervisor.Id,
                Comment = "Please inspect during morning shift before peak generation hours.",
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new FaultComment
            {
                FaultId = fault.Id,
                UserId = technician.Id,
                Comment = "On site now. Initial check shows dust accumulation on string B1.",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        );
    }

    private static async Task EnsureAssetsAndScadaAsync(ApplicationDbContext db)
    {
        if (await db.Assets.AnyAsync()) return;

        var sites = await db.SolarSites.OrderBy(s => s.Id).ToListAsync();
        var random = new Random(99);

        foreach (var site in sites)
        {
            var zones = await db.Zones.Where(z => z.SiteId == site.Id).OrderBy(z => z.Id).ToListAsync();
            var zoneA = zones.FirstOrDefault();

            var inverters = new List<Asset>();
            for (var i = 1; i <= 3; i++)
            {
                var health = 55 + random.NextDouble() * 40;
                var status = health < 65 ? AssetStatus.Warning : health < 50 ? AssetStatus.Critical : AssetStatus.Online;
                inverters.Add(new Asset
                {
                    SiteId = site.Id,
                    ZoneId = zones.ElementAtOrDefault(i - 1)?.Id,
                    AssetCode = $"INV-{site.PlantCode}-{i:D3}",
                    Type = AssetType.Inverter,
                    SerialNumber = $"SN-INV-{site.Id}{i:D4}",
                    Manufacturer = "SMA Solar",
                    Model = "SUN2000-60KTL",
                    InstallationDate = DateTime.UtcNow.AddYears(-2).AddDays(i * 30),
                    CapacityKw = site.CapacityMw * 333,
                    Location = zones.ElementAtOrDefault(i - 1)?.Name ?? "Inverter Room",
                    Status = status,
                    HealthScore = Math.Round(health, 1),
                    LastMaintenanceAt = DateTime.UtcNow.AddDays(-30 - i * 10)
                });
            }

            inverters.Add(new Asset
            {
                SiteId = site.Id,
                AssetCode = $"TRF-{site.PlantCode}-001",
                Type = AssetType.Transformer,
                SerialNumber = $"SN-TRF-{site.Id}001",
                Manufacturer = "ABB",
                Model = "ONAN-2500",
                InstallationDate = DateTime.UtcNow.AddYears(-3),
                CapacityKw = site.CapacityMw * 1000,
                Location = "Main Substation",
                Status = AssetStatus.Online,
                HealthScore = 92,
                LastMaintenanceAt = DateTime.UtcNow.AddDays(-45)
            });

            inverters.Add(new Asset
            {
                SiteId = site.Id,
                ZoneId = zoneA?.Id,
                AssetCode = $"WS-{site.PlantCode}-001",
                Type = AssetType.WeatherStation,
                SerialNumber = $"SN-WS-{site.Id}001",
                Manufacturer = "Kipp & Zonen",
                Model = "CMP11",
                InstallationDate = DateTime.UtcNow.AddYears(-1),
                CapacityKw = 0,
                Location = "Site Meteorological Tower",
                Status = AssetStatus.Online,
                HealthScore = 98
            });

            db.Assets.AddRange(inverters);
            await db.SaveChangesAsync();

            var now = DateTime.UtcNow;
            db.ScadaReadings.Add(new ScadaReading
            {
                SiteId = site.Id,
                Voltage = 380 + random.NextDouble() * 20,
                Current = 120 + random.NextDouble() * 40,
                PowerKw = site.CapacityMw * 800 * (0.7 + random.NextDouble() * 0.25),
                Temperature = 32 + random.NextDouble() * 8,
                Frequency = 49.9 + random.NextDouble() * 0.2,
                InverterStatus = "Running",
                CommunicationStatus = "Connected",
                EnergyGenerationKwh = site.CapacityMw * 600 * (0.6 + random.NextDouble() * 0.3),
                RecordedAt = now
            });

            foreach (var inv in inverters.Where(a => a.Type == AssetType.Inverter))
            {
                db.ScadaReadings.Add(new ScadaReading
                {
                    SiteId = site.Id,
                    AssetId = inv.Id,
                    Voltage = 380 + random.NextDouble() * 15,
                    Current = 40 + random.NextDouble() * 20,
                    PowerKw = inv.CapacityKw * (0.65 + random.NextDouble() * 0.3),
                    Temperature = 35 + random.NextDouble() * 15,
                    Frequency = 50,
                    InverterStatus = inv.Status == AssetStatus.Online ? "Running" : "Warning",
                    CommunicationStatus = "Connected",
                    EnergyGenerationKwh = inv.CapacityKw * 4,
                    RecordedAt = now
                });
            }
        }
    }

    private static async Task EnsureInvoicesAsync(ApplicationDbContext db)
    {
        if (await db.Invoices.AnyAsync()) return;

        var orgs = await db.Organizations.ToListAsync();
        foreach (var org in orgs)
        {
            var capacity = await db.SolarSites.Where(s => s.OrganizationId == org.Id).SumAsync(s => s.CapacityMw);
            var baseRate = org.SubscriptionPlan == "Enterprise" ? 50000m : 25000m;
            var mwRate = org.SubscriptionPlan == "Enterprise" ? 1500m : 800m;
            var aiRate = org.SubscriptionPlan == "Enterprise" ? 500m : 200m;
            var scada = 10000m;
            var monitoring = (decimal)capacity * mwRate;
            var ai = (decimal)capacity * aiRate;
            var subtotal = baseRate + monitoring + ai + scada;
            var gst = subtotal * 0.18m;

            db.Invoices.Add(new Invoice
            {
                OrganizationId = org.Id,
                InvoiceNumber = $"INV-{org.Code}-{DateTime.UtcNow:yyyyMM}",
                BillingPeriod = DateTime.UtcNow.ToString("MMMM yyyy"),
                TotalMw = capacity,
                BaseSubscription = baseRate,
                MwMonitoring = monitoring,
                AiModule = ai,
                ScadaIntegration = scada,
                Subtotal = subtotal,
                Gst = gst,
                Total = subtotal + gst,
                Status = "Paid",
                IssueDate = DateTime.UtcNow.AddDays(-5),
                DueDate = DateTime.UtcNow.AddDays(25),
                PaidDate = DateTime.UtcNow.AddDays(-2)
            });

            db.Invoices.Add(new Invoice
            {
                OrganizationId = org.Id,
                InvoiceNumber = $"INV-{org.Code}-{DateTime.UtcNow.AddMonths(-1):yyyyMM}",
                BillingPeriod = DateTime.UtcNow.AddMonths(-1).ToString("MMMM yyyy"),
                TotalMw = capacity,
                BaseSubscription = baseRate,
                MwMonitoring = monitoring,
                AiModule = ai,
                ScadaIntegration = scada,
                Subtotal = subtotal,
                Gst = gst,
                Total = subtotal + gst,
                Status = "Paid",
                IssueDate = DateTime.UtcNow.AddMonths(-1).AddDays(-5),
                DueDate = DateTime.UtcNow.AddMonths(-1).AddDays(25),
                PaidDate = DateTime.UtcNow.AddMonths(-1)
            });
        }
    }

    private static User CreateUser(string email, string phone, string password, string name, UserRole role, int? orgId) => new()
    {
        Email = email,
        Phone = phone,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        FullName = name,
        Role = role,
        OrganizationId = orgId,
        PreferredLanguage = "en"
    };

    private static SolarSite CreatePlant(int orgId, string code, string name, string location, double mw, double lat, double lng, double targetPr) => new()
    {
        OrganizationId = orgId,
        PlantCode = code,
        Name = name,
        Location = location,
        CapacityMw = mw,
        Latitude = lat,
        Longitude = lng,
        TargetPr = targetPr
    };
}
