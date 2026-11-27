using FloristHub.DAL.Constants;
using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using FloristHub.DAL.Persistence;

namespace FloristHub.DAL.Seed;

/// <summary>Seed didático apenas em ambiente de desenvolvimento.</summary>
public static class DevelopmentDataSeeder
{
    public const string DemoTenantSlug = "flores-lina";
    public const string DemoSecondTenantSlug = "flores-do-campo";
    public const string DemoPassword = "DemoFlorist1!";
    public const string SuperAdminEmail = "admin@demo.floristhub.pt";

    public static async Task SeedAsync(IServiceProvider services, IHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentSeed");

        if (await db.Tenants.AnyAsync(t => t.Slug == DemoTenantSlug))
        {
            await PatchDemoDataAsync(db, users, roles, log);
            return;
        }

        var tenant = new Tenant
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
            Name = "Flores da Lina",
            Slug = DemoTenantSlug,
            ThemePrimaryColor = "#2d6a4f",
            ThemeAccentColor = "#95d5b2",
            ThemeFontFamily = "system",
            Description = "Florista familiar em Lisboa — arranjos sob medida, entregas na cidade e levantamento na loja.",
            ContactEmail = "ola@floresdalina.demo",
            ContactPhone = "+351 912 345 678",
            AddressLine = "Rua do Alecrim, 42",
            City = "Lisboa",
            PostalCode = "1200-018",
            LogoUrl = "/api/files/demo/flores-lina-logo.jpg",
            IsActive = true,
            OrdersAcceptingEnabled = true,
            MinimumOrderAdvanceMinutes = 180,
            PickupEnabled = true,
            HomeDeliveryEnabled = true,
            CemeteryDeliveryEnabled = true,
            IsPublished = true,
            OnboardingColorsAcknowledged = true,
            PlatformTermsAcceptedAt = DateTimeOffset.UtcNow,
        };
        EnsureDemoStoreCommerceReady(tenant);
        ApplyOpeningHours(tenant);
        ApplyDeliveryZones(tenant);
        db.Tenants.Add(tenant);

        var adminId = Guid.Parse("22222222-2222-2222-2222-222222222201");
        var floristId = Guid.Parse("22222222-2222-2222-2222-222222222202");
        var customerId = Guid.Parse("22222222-2222-2222-2222-222222222203");
        var campoFloristId = Guid.Parse("22222222-2222-2222-2222-222222222204");

        await EnsureSuperAdminAsync(users, roles, adminId, SuperAdminEmail, "Super Admin Demo");
        await EnsureUserAsync(users, roles, floristId, "florista@demo.floristhub.pt", "Lina Florista", tenant.Id, Roles.Florist);
        await EnsureUserAsync(users, roles, customerId, "cliente@demo.floristhub.pt", "João Cliente", tenant.Id, Roles.Customer);

        var tenantCampo = new Tenant
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
            Name = "Flores do Campo",
            Slug = DemoSecondTenantSlug,
            Description = "Arranjos rústicos e silvestres no Porto.",
            ThemePrimaryColor = "#7b2cbf",
            ThemeAccentColor = "#e0aaff",
            ThemeFontFamily = "serif",
            ContactEmail = "ola@floresdocampo.demo",
            LogoUrl = "/api/files/demo/flores-do-campo-logo.jpg",
            IsActive = true,
            IsPublished = true,
            OnboardingColorsAcknowledged = true,
            PlatformTermsAcceptedAt = DateTimeOffset.UtcNow,
        };
        EnsureDemoStoreCommerceReady(tenantCampo);
        ApplyOpeningHours(tenantCampo);
        ApplyDeliveryZones(tenantCampo, seedIdOffset: 10);
        db.Tenants.Add(tenantCampo);
        await EnsureUserAsync(
            users, roles, campoFloristId, "florista.campo@demo.floristhub.pt", "Marta Campo", tenantCampo.Id, Roles.Florist);
        db.Flowers.AddRange(CreateCampoDemoFlowers(tenantCampo.Id));

        var rose = new Flower
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333301"),
            TenantId = tenant.Id,
            Name = "Rosa vermelha",
            Color = "Vermelho",
            UnitPrice = 2.5m,
            Unit = "ramo",
            StockQuantity = 120,
            SeasonMonths = SeasonMask(10, 11, 12),
            IsApproximatelyAvailable = true,
            IsCatalogActive = true,
            ImageUrl = "/api/files/demo/flower-rosa-vermelha.jpg"
        };
        var tulip = new Flower
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333302"),
            TenantId = tenant.Id,
            Name = "Túlipa amarela",
            Color = "Amarelo",
            UnitPrice = 1.8m,
            Unit = "unidade",
            SeasonMonths = SeasonMask(3, 4, 5),
            IsApproximatelyAvailable = true,
            IsCatalogActive = true,
            ImageUrl = "/api/files/demo/flower-tulipa-amarela.jpg"
        };
        var greenery = new Flower
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333303"),
            TenantId = tenant.Id,
            Name = "Eucalipto",
            Color = "Verde",
            UnitPrice = 0.9m,
            Unit = "ramo",
            SeasonMonths = SeasonMask(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12),
            IsApproximatelyAvailable = true,
            IsCatalogActive = true,
            Notes = "Material de folhagem",
            ImageUrl = "/api/files/demo/flower-eucalipto.jpg"
        };
        db.Flowers.AddRange(rose, tulip, greenery);

        var portfolio = new PortfolioArrangement
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444401"),
            TenantId = tenant.Id,
            Title = "Buquê primaveril",
            Slug = "buque-primaveril",
            Description = "Combinação leve de rosas e túlipas.",
            Price = 35m,
            Tags = "casamento,primavera",
            Status = PortfolioArrangementStatus.Active,
            SortOrder = 0
        };
        portfolio.Photos.Add(new PortfolioArrangementPhoto
        {
            TenantId = tenant.Id,
            Url = "/api/files/demo/bouquet-primaveril.jpg",
            SortOrder = 0
        });
        portfolio.Items.Add(new PortfolioArrangementItem
        {
            TenantId = tenant.Id,
            Name = rose.Name,
            FlowerId = rose.Id,
            Quantity = 8,
            IsMaterial = false
        });
        portfolio.Items.Add(new PortfolioArrangementItem
        {
            TenantId = tenant.Id,
            Name = "Espuma floral",
            Quantity = 1,
            IsMaterial = true
        });
        db.PortfolioArrangements.Add(portfolio);

        var pendingOrder = new Order
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555501"),
            TenantId = tenant.Id,
            CustomerId = customerId,
            Status = OrderStatus.PendingConfirmation,
            BaseType = ArrangementBaseType.Sponge,
            ArrangementShape = ArrangementShape.Sponge,
            ArrangementVariationSeed = 48291,
            DeliveryType = DeliveryType.StorePickup,
            CustomerNotes = "Para oferecer no sábado.",
            EstimatedTotal = 28m,
            DeliveryFee = 0m
        };
        pendingOrder.Items.Add(new OrderItem
        {
            TenantId = tenant.Id,
            FlowerId = rose.Id,
            Name = rose.Name,
            Quantity = 10,
            UnitPrice = rose.UnitPrice
        });
        pendingOrder.StatusHistory.Add(new OrderStatusHistory
        {
            TenantId = tenant.Id,
            FromStatus = OrderStatus.PendingConfirmation,
            ToStatus = OrderStatus.PendingConfirmation,
            ChangedByUserId = customerId,
            Notes = "Encomenda criada pelo cliente (seed)."
        });

        var proposalOrder = new Order
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555502"),
            TenantId = tenant.Id,
            CustomerId = customerId,
            Status = OrderStatus.PendingConfirmation,
            BaseType = ArrangementBaseType.Jar,
            ArrangementShape = ArrangementShape.Long,
            ArrangementVariationSeed = 90210,
            DeliveryType = DeliveryType.HomeDelivery,
            DeliveryAddress = "Rua das Flores, 12, Lisboa",
            EstimatedTotal = 40.5m,
            DeliveryFee = 4.5m
        };
        proposalOrder.Items.Add(new OrderItem
        {
            TenantId = tenant.Id,
            FlowerId = tulip.Id,
            Name = tulip.Name,
            Quantity = 20,
            UnitPrice = tulip.UnitPrice
        });
        proposalOrder.StatusHistory.Add(new OrderStatusHistory
        {
            TenantId = tenant.Id,
            FromStatus = OrderStatus.PendingConfirmation,
            ToStatus = OrderStatus.PendingConfirmation,
            ChangedByUserId = customerId,
            Notes = "Encomenda criada pelo cliente (seed)."
        });

        var itemChangesJson =
            """[{"flowerId":"33333333-3333-3333-3333-333333333301","name":"Rosa vermelha","quantity":12,"unitPrice":2.5}]""";

        proposalOrder.ChangeProposals.Add(new OrderChangeProposal
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666601"),
            TenantId = tenant.Id,
            ProposedByUserId = floristId,
            Message = "Túlipas indisponíveis; sugerimos rosas vermelhas.",
            ProposedItemChangesJson = itemChangesJson,
            RevisedTotal = 34.5m,
            Status = OrderChangeProposalStatus.Pending
        });
        proposalOrder.StatusHistory.Add(new OrderStatusHistory
        {
            TenantId = tenant.Id,
            FromStatus = OrderStatus.PendingConfirmation,
            ToStatus = OrderStatus.PendingConfirmation,
            ChangedByUserId = floristId,
            Notes = "Proposta de alteração: Túlipas indisponíveis; sugerimos rosas vermelhas."
        });

        db.Orders.AddRange(pendingOrder, proposalOrder);
        await EnsureDemoMetricsOrdersAsync(db, tenant.Id, customerId, rose.Id, tulip.Id);
        await BackfillFloristMembershipsAsync(db);
        await db.SaveChangesAsync();

        log.LogInformation(
            "Seed de demonstração criado. Tenant={Slug} Florista={FloristEmail} Cliente={CustomerEmail}",
            DemoTenantSlug,
            "florista@demo.floristhub.pt",
            "cliente@demo.floristhub.pt");
    }

    private static async Task PatchDemoDataAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger log)
    {
        await MigrateLegacyAdminRoleAsync(users, roleManager);
        await EnsureSuperAdminAsync(users, roleManager, Guid.Parse("22222222-2222-2222-2222-222222222201"), SuperAdminEmail, "Super Admin Demo");

        var tenant = await db.Tenants
            .Include(t => t.OpeningHours)
            .Include(t => t.DeliveryZones)
            .FirstOrDefaultAsync(t => t.Slug == DemoTenantSlug);
        if (tenant is not null)
        {
            tenant.Description ??= "Florista familiar em Lisboa — arranjos sob medida.";
            tenant.ThemePrimaryColor = string.IsNullOrWhiteSpace(tenant.ThemePrimaryColor) ? "#2d6a4f" : tenant.ThemePrimaryColor;
            tenant.ThemeAccentColor = string.IsNullOrWhiteSpace(tenant.ThemeAccentColor) ? "#95d5b2" : tenant.ThemeAccentColor;
            tenant.ThemeFontFamily ??= "system";
            tenant.ContactEmail ??= "ola@floresdalina.demo";
            tenant.ContactPhone ??= "+351 912 345 678";
            tenant.AddressLine ??= "Rua do Alecrim, 42";
            tenant.City ??= "Lisboa";
            tenant.PostalCode ??= "1200-018";
            tenant.MinimumOrderAdvanceMinutes = tenant.MinimumOrderAdvanceMinutes == 0 ? 180 : tenant.MinimumOrderAdvanceMinutes;
            if (tenant.OpeningHours.Count == 0)
                ApplyOpeningHours(tenant);
            if (tenant.DeliveryZones.Count == 0)
                ApplyDeliveryZones(tenant);

            EnsureDemoStoreCommerceReady(tenant);
            PatchDemoLogo(tenant, "/api/files/demo/flores-lina-logo.jpg");
            await PatchLinaPortfolioPhotoAsync(db, tenant.Id, "/api/files/demo/bouquet-primaveril.jpg");
            await PatchDemoFlowerImagesAsync(db, tenant.Id);
            var demoCustomerId = Guid.Parse("22222222-2222-2222-2222-222222222203");
            var flowers = await db.Flowers.Where(f => f.TenantId == tenant.Id).ToListAsync();
            var rose = flowers.FirstOrDefault(f => f.Name == "Rosa vermelha");
            var tulip = flowers.FirstOrDefault(f => f.Name == "Túlipa amarela");
            if (rose is not null && tulip is not null)
                await EnsureDemoMetricsOrdersAsync(db, tenant.Id, demoCustomerId, rose.Id, tulip.Id);
        }

        if (!await db.Tenants.AnyAsync(t => t.Slug == DemoSecondTenantSlug))
        {
            var tenantCampo = new Tenant
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                Name = "Flores do Campo",
                Slug = DemoSecondTenantSlug,
                Description = "Arranjos rústicos e silvestres no Porto.",
                ThemePrimaryColor = "#7b2cbf",
                ThemeAccentColor = "#e0aaff",
                ThemeFontFamily = "serif",
                ContactEmail = "ola@floresdocampo.demo",
                LogoUrl = "/api/files/demo/flores-do-campo-logo.jpg",
                IsActive = true,
                IsPublished = true,
                OnboardingColorsAcknowledged = true,
                PlatformTermsAcceptedAt = DateTimeOffset.UtcNow,
            };
            EnsureDemoStoreCommerceReady(tenantCampo);
            ApplyOpeningHours(tenantCampo);
            ApplyDeliveryZones(tenantCampo, seedIdOffset: 10);
            db.Tenants.Add(tenantCampo);
            await EnsureUserAsync(
                users, roleManager, Guid.Parse("22222222-2222-2222-2222-222222222204"),
                "florista.campo@demo.floristhub.pt", "Marta Campo", tenantCampo.Id, Roles.Florist);
            db.Flowers.AddRange(CreateCampoDemoFlowers(tenantCampo.Id));
        }
        else
        {
            var tenantCampo = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == DemoSecondTenantSlug);
            if (tenantCampo is not null)
            {
                EnsureDemoStoreCommerceReady(tenantCampo);
                PatchDemoLogo(tenantCampo, "/api/files/demo/flores-do-campo-logo.jpg");
                await EnsureCampoDemoFlowersAsync(db, tenantCampo.Id);
                await PatchDemoFlowerImagesAsync(db, tenantCampo.Id);
            }
        }

        await PatchDemoArrangementSeedsAsync(db);
        await BackfillFloristMembershipsAsync(db);
        await db.SaveChangesAsync();
        log.LogInformation("Seed de demonstração atualizado.");
    }

    private static async Task PatchDemoArrangementSeedsAsync(ApplicationDbContext db)
    {
        var pending = await db.Orders.FirstOrDefaultAsync(o => o.Id == Guid.Parse("55555555-5555-5555-5555-555555555501"));
        if (pending is not null && pending.ArrangementVariationSeed == 0)
        {
            pending.ArrangementShape = ArrangementShape.Sponge;
            pending.ArrangementVariationSeed = 48291;
            pending.BaseType = ArrangementBaseType.Sponge;
        }

        var proposal = await db.Orders.FirstOrDefaultAsync(o => o.Id == Guid.Parse("55555555-5555-5555-5555-555555555502"));
        if (proposal is not null && proposal.ArrangementVariationSeed == 0)
        {
            proposal.ArrangementShape = ArrangementShape.Long;
            proposal.ArrangementVariationSeed = 90210;
            proposal.BaseType = ArrangementBaseType.Jar;
        }
    }

    private static async Task MigrateLegacyAdminRoleAsync(UserManager<ApplicationUser> users, RoleManager<IdentityRole<Guid>> roleManager)
    {
        if (!await roleManager.RoleExistsAsync(Roles.SuperAdmin) && await roleManager.RoleExistsAsync("AdminPlataforma"))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = Roles.SuperAdmin });
        }

        var legacyUsers = await users.GetUsersInRoleAsync("AdminPlataforma");
        foreach (var user in legacyUsers)
        {
            await users.RemoveFromRoleAsync(user, "AdminPlataforma");
            if (!await users.IsInRoleAsync(user, Roles.SuperAdmin))
                await users.AddToRoleAsync(user, Roles.SuperAdmin);
            user.TenantId = null;
            await users.UpdateAsync(user);
        }
    }

    private static async Task EnsureSuperAdminAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        Guid id,
        string email,
        string fullName)
    {
        if (!await roleManager.RoleExistsAsync(Roles.SuperAdmin))
            await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = Roles.SuperAdmin });

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            existing.TenantId = null;
            existing.FullName = fullName;
            await userManager.UpdateAsync(existing);
            if (!await userManager.IsInRoleAsync(existing, Roles.SuperAdmin))
                await userManager.AddToRoleAsync(existing, Roles.SuperAdmin);
            return;
        }

        await EnsureUserAsync(userManager, roleManager, id, email, fullName, null, Roles.SuperAdmin);
    }

    private static void EnsureDemoStoreCommerceReady(Tenant tenant)
    {
        tenant.IsPublished = true;
        tenant.OnboardingColorsAcknowledged = true;
        tenant.PlatformTermsAcceptedAt ??= DateTimeOffset.UtcNow;
        tenant.PaymentsDisabledByChoice = false;
        var accountId = DemoStripeAccountId(tenant.Slug);
        if (string.IsNullOrWhiteSpace(tenant.StripeAccountId) || tenant.StripeAccountId == "acct_dev_fake")
            tenant.StripeAccountId = accountId;
        tenant.StripeChargesEnabled = true;
        tenant.StripePayoutsEnabled = true;
        tenant.StripeDetailsSubmitted = true;
        tenant.StripeCardPayments = true;
        tenant.StripeMbWay = true;
        tenant.StripeMultibanco = true;
    }

    private static string DemoStripeAccountId(string slug) => "acct_dev_" + slug;

    private static void ApplyOpeningHours(Tenant tenant)
    {
        foreach (var day in Enumerable.Range(0, 7))
        {
            var closed = day is 0;
            tenant.OpeningHours.Add(new TenantOpeningHour
            {
                TenantId = tenant.Id,
                DayOfWeek = (DayOfWeek)day,
                IsClosed = closed,
                OpenTime = closed ? null : new TimeOnly(9, 0),
                CloseTime = closed ? null : new TimeOnly(19, 0)
            });
        }
    }

    private static void ApplyDeliveryZones(Tenant tenant, int seedIdOffset = 0)
    {
        var suffix1 = (1 + seedIdOffset).ToString("D2");
        var suffix2 = (2 + seedIdOffset).ToString("D2");
        tenant.DeliveryZones.Add(new DeliveryZone
        {
            Id = Guid.Parse($"77777777-7777-7777-7777-7777777777{suffix1}"),
            TenantId = tenant.Id,
            Name = "Lisboa centro",
            Fee = 4.5m,
            IsActive = true,
            SortOrder = 0
        });
        tenant.DeliveryZones.Add(new DeliveryZone
        {
            Id = Guid.Parse($"77777777-7777-7777-7777-7777777777{suffix2}"),
            TenantId = tenant.Id,
            Name = "Margem sul",
            Fee = 7m,
            IsActive = true,
            SortOrder = 1
        });
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        Guid id,
        string email,
        string fullName,
        Guid? tenantId,
        string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = role });

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
                await userManager.AddToRoleAsync(existing, role);
            return;
        }

        var user = new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            FullName = fullName,
            TenantId = tenantId,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Falha ao criar {email}: " + string.Join("; ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, role);
    }

    private static void PatchDemoLogo(Tenant tenant, string demoLogoUrl)
    {
        // Lojas de demonstração: força sempre o LogoUrl versionado (evita URLs antigas ou ficheiros em cache).
        if (tenant.Slug is DemoTenantSlug or DemoSecondTenantSlug)
        {
            tenant.LogoUrl = demoLogoUrl;
            return;
        }

        if (tenant.LogoUrl is null or { Length: 0 }
            || tenant.LogoUrl.Contains("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            tenant.LogoUrl = demoLogoUrl;
        }
    }

    private static readonly Dictionary<string, string> DemoFlowerImages = new(StringComparer.Ordinal)
    {
        ["Rosa vermelha"] = "/api/files/demo/flower-rosa-vermelha.jpg",
        ["Túlipa amarela"] = "/api/files/demo/flower-tulipa-amarela.jpg",
        ["Eucalipto"] = "/api/files/demo/flower-eucalipto.jpg",
        ["Lavanda selvagem"] = "/api/files/demo/flower-lavanda-selvagem.jpg",
        ["Girassol"] = "/api/files/demo/flower-girassol.jpg",
        ["Margarida silvestre"] = "/api/files/demo/flower-margarida-silvestre.jpg",
    };

    private static readonly Dictionary<string, int[]> DemoSeasonMonths = new()
    {
        ["Rosa vermelha"] = [10, 11, 12],
        ["Túlipa amarela"] = [3, 4, 5],
        ["Eucalipto"] = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
        ["Lavanda selvagem"] = [6, 7, 8],
        ["Girassol"] = [7, 8, 9],
        ["Margarida silvestre"] = [4, 5, 6],
    };

    private static IEnumerable<Flower> CreateCampoDemoFlowers(Guid tenantId) =>
    [
        new Flower
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333401"),
            TenantId = tenantId,
            Name = "Lavanda selvagem",
            Color = "Lilás",
            UnitPrice = 1.6m,
            Unit = "ramo",
            SeasonMonths = SeasonMask(6, 7, 8),
            IsApproximatelyAvailable = true,
            IsCatalogActive = true,
            ImageUrl = "/api/files/demo/flower-lavanda-selvagem.jpg",
        },
        new Flower
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333402"),
            TenantId = tenantId,
            Name = "Girassol",
            Color = "Amarelo",
            UnitPrice = 2.2m,
            Unit = "unidade",
            SeasonMonths = SeasonMask(7, 8, 9),
            IsApproximatelyAvailable = true,
            IsCatalogActive = true,
            ImageUrl = "/api/files/demo/flower-girassol.jpg",
        },
        new Flower
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333403"),
            TenantId = tenantId,
            Name = "Margarida silvestre",
            Color = "Branco",
            UnitPrice = 1.4m,
            Unit = "unidade",
            SeasonMonths = SeasonMask(4, 5, 6),
            IsApproximatelyAvailable = true,
            IsCatalogActive = true,
            ImageUrl = "/api/files/demo/flower-margarida-silvestre.jpg",
        },
    ];

    private static async Task EnsureCampoDemoFlowersAsync(ApplicationDbContext db, Guid tenantId)
    {
        if (await db.Flowers.AnyAsync(f => f.TenantId == tenantId))
            return;
        db.Flowers.AddRange(CreateCampoDemoFlowers(tenantId));
    }

    private static async Task PatchDemoFlowerImagesAsync(ApplicationDbContext db, Guid tenantId)
    {
        var flowers = await db.Flowers.Where(f => f.TenantId == tenantId).ToListAsync();
        foreach (var flower in flowers)
        {
            if (!DemoFlowerImages.TryGetValue(flower.Name, out var url))
                continue;
            if (string.IsNullOrWhiteSpace(flower.ImageUrl)
                || flower.ImageUrl.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
                || !flower.ImageUrl.Contains("flower-", StringComparison.OrdinalIgnoreCase))
            {
                flower.ImageUrl = url;
            }

            if (flower.SeasonMonths == 0 && DemoSeasonMonths.TryGetValue(flower.Name, out var months))
                flower.SeasonMonths = SeasonMask(months);
        }
    }

    private static int SeasonMask(params int[] months)
    {
        var mask = 0;
        foreach (var month in months)
            mask |= 1 << (month - 1);
        return mask;
    }

    private static async Task EnsureDemoMetricsOrdersAsync(
        ApplicationDbContext db,
        Guid tenantId,
        Guid customerId,
        Guid roseId,
        Guid tulipId)
    {
        var markerId = Guid.Parse("66666666-6666-6666-6666-666666666601");
        if (await db.Orders.AnyAsync(o => o.Id == markerId))
            return;

        var utcToday = DateTimeOffset.UtcNow.Date;
        var templates = new[]
        {
            (DaysAgo: 1, Total: 32m, Status: OrderStatus.Delivered, Captured: true, RoseQty: 6, TulipQty: 0),
            (DaysAgo: 3, Total: 18m, Status: OrderStatus.Delivered, Captured: true, RoseQty: 0, TulipQty: 8),
            (DaysAgo: 5, Total: 45m, Status: OrderStatus.Confirmed, Captured: false, RoseQty: 10, TulipQty: 4),
            (DaysAgo: 8, Total: 27m, Status: OrderStatus.Delivered, Captured: true, RoseQty: 5, TulipQty: 5),
            (DaysAgo: 14, Total: 21m, Status: OrderStatus.Delivered, Captured: true, RoseQty: 4, TulipQty: 0),
            (DaysAgo: 21, Total: 36m, Status: OrderStatus.Delivered, Captured: true, RoseQty: 8, TulipQty: 6),
        };

        var orders = new List<Order>(templates.Length);
        var index = 0;
        foreach (var t in templates)
        {
            var id = index == 0 ? markerId : Guid.Parse($"66666666-6666-6666-6666-6666666666{index + 1:D2}");
            var created = new DateTimeOffset(utcToday.AddDays(-t.DaysAgo).AddHours(10), TimeSpan.Zero);
            var order = new Order
            {
                Id = id,
                TenantId = tenantId,
                CustomerId = customerId,
                Status = t.Status,
                BaseType = ArrangementBaseType.Sponge,
                DeliveryType = DeliveryType.StorePickup,
                EstimatedTotal = t.Total,
                PaymentCaptured = t.Captured,
                CreatedAt = created,
            };
            if (t.RoseQty > 0)
            {
                order.Items.Add(new OrderItem
                {
                    TenantId = tenantId,
                    FlowerId = roseId,
                    Name = "Rosa vermelha",
                    Quantity = t.RoseQty,
                    UnitPrice = 2.5m,
                });
            }

            if (t.TulipQty > 0)
            {
                order.Items.Add(new OrderItem
                {
                    TenantId = tenantId,
                    FlowerId = tulipId,
                    Name = "Túlipa amarela",
                    Quantity = t.TulipQty,
                    UnitPrice = 1.8m,
                });
            }

            orders.Add(order);
            index++;
        }

        db.Orders.AddRange(orders);
    }

    private static async Task PatchLinaPortfolioPhotoAsync(ApplicationDbContext db, Guid tenantId, string demoPhotoUrl)
    {
        var portfolio = await db.PortfolioArrangements
            .Include(p => p.Photos)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Title == "Buquê primaveril");
        if (portfolio is null)
            return;
        if (string.IsNullOrWhiteSpace(portfolio.Slug))
            portfolio.Slug = "buque-primaveril";

        foreach (var photo in portfolio.Photos.Where(p =>
                     p.Url.Contains("placeholder", StringComparison.OrdinalIgnoreCase)))
        {
            photo.Url = demoPhotoUrl;
        }
    }

    private static async Task BackfillFloristMembershipsAsync(ApplicationDbContext db)
    {
        var pairs = await (
            from user in db.Users
            join userRole in db.UserRoles on user.Id equals userRole.UserId
            join role in db.Roles on userRole.RoleId equals role.Id
            where role.Name == Roles.Florist && user.TenantId != null
            select new { user.Id, TenantId = user.TenantId!.Value }).ToListAsync();

        foreach (var pair in pairs)
            await EnsureFloristMembershipAsync(db, pair.Id, pair.TenantId);
    }

    private static async Task EnsureFloristMembershipAsync(ApplicationDbContext db, Guid userId, Guid tenantId)
    {
        var exists = await db.FloristTenantMemberships.AnyAsync(m => m.UserId == userId && m.TenantId == tenantId);
        if (exists)
            return;

        db.FloristTenantMemberships.Add(new FloristTenantMembership
        {
            UserId = userId,
            TenantId = tenantId,
        });
    }
}
