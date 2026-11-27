using FloristHub.Api.Extensions;
using FloristHub.DAL.Constants;
using FloristHub.Services;
using FloristHub.Services.DTOs.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FloristHub.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Florist)]
[Route("api/florist/store")]
public class FloristStoreController : ControllerBase
{
    private readonly IFloristStoreSettingsService _store;

    public FloristStoreController(IFloristStoreSettingsService store) => _store = store;

    [HttpGet]
    public async Task<ActionResult<StoreSettingsDto>> Get(CancellationToken ct) =>
        Ok(await _store.GetAsync(ct));

    [HttpPut]
    public async Task<ActionResult<StoreSettingsDto>> Update([FromBody] UpdateStoreSettingsRequest request, CancellationToken ct) =>
        Ok(await _store.UpdateAsync(request, User.GetUserId(), ct));
}
