using FloristHub.Services;
using FloristHub.Services.Capacity;
using FloristHub.Services.DTOs.Capacity;
using FloristHub.Services.DTOs.Store;
using FloristHub.Services.Marketing;
using FloristHub.Services.Orders;
using FloristHub.Services.DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Route("api/store")]
public class StoreController : ControllerBase
{
    private readonly IStoreCheckoutInfoService _checkout;
    private readonly IPublicStoreThemeService _theme;
    private readonly IDeliveryCapacityService _capacity;
    private readonly IPublicStorefrontService _storefront;
    private readonly IReviewService _reviews;
    private readonly IOrderTrackingService _tracking;

    public StoreController(
        IStoreCheckoutInfoService checkout,
        IPublicStoreThemeService theme,
        IDeliveryCapacityService capacity,
        IPublicStorefrontService storefront,
        IReviewService reviews,
        IOrderTrackingService tracking)
    {
        _checkout = checkout;
        _theme = theme;
        _capacity = capacity;
        _storefront = storefront;
        _reviews = reviews;
        _tracking = tracking;
    }

    /// <summary>Definições de encomenda visíveis ao cliente (zonas ativas, horário, tipos de entrega).</summary>
    [AllowAnonymous]
    [HttpGet("checkout")]
    public async Task<ActionResult<PublicStoreCheckoutDto>> GetCheckout(CancellationToken ct) =>
        Ok(await _checkout.GetAsync(ct));

    /// <summary>Tema público da loja (cores e tipografia).</summary>
    /// <summary>Vagas de entrega visíveis ao cliente. Datas sem política ficam sem limite.</summary>
    [AllowAnonymous]
    [HttpGet("capacity")]
    public async Task<ActionResult<IReadOnlyList<PublicCapacityDayDto>>> Capacity(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct) =>
        Ok(await _capacity.GetPublicCalendarAsync(from, to, ct));

    [AllowAnonymous]
    [HttpGet("theme")]
    public async Task<ActionResult<PublicStoreThemeDto>> GetTheme(CancellationToken ct) =>
        Ok(await _theme.GetAsync(ct));

    [AllowAnonymous]
    [HttpGet("home")]
    public async Task<ActionResult<PublicStorefrontDto>> Home(CancellationToken ct) =>
        Ok(await _storefront.GetAsync(ct));

    [AllowAnonymous]
    [HttpGet("seasonal")]
    public async Task<ActionResult<IReadOnlyList<PublicSeasonalFlowerDto>>> Seasonal([FromQuery] int? month, CancellationToken ct) =>
        Ok(await _storefront.SeasonalAsync(month, ct));

    [AllowAnonymous]
    [HttpGet("campaigns/{slug}")]
    public async Task<ActionResult<PublicCampaignDto>> Campaign(string slug, CancellationToken ct)
    {
        var campaign = await _storefront.CampaignBySlugAsync(slug, ct);
        return campaign is null ? NotFound() : Ok(campaign);
    }

    [AllowAnonymous]
    [HttpGet("reviews")]
    public async Task<ActionResult<IReadOnlyList<PublicReviewDto>>> Reviews(CancellationToken ct) =>
        Ok(await _reviews.ListPublicAsync(ct));

  /// <summary>Acompanhamento público de encomenda por token seguro (requer tenant da loja).</summary>
    [AllowAnonymous]
    [HttpGet("order-tracking")]
    public async Task<ActionResult<PublicOrderTrackingDto>> TrackOrder([FromQuery] string token, CancellationToken ct)
    {
        var row = await _tracking.GetByTokenAsync(token, ct);
        return row is null ? NotFound() : Ok(row);
    }
}
