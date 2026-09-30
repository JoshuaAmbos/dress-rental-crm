using CRM.api.DTOs;
using CRM.api.Services;
using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.AspNetCore.Mvc;

namespace CRM.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoyaltyAwardController : ControllerBase
{
    private readonly LoyaltyAwardService _loyaltyService;

    public LoyaltyAwardController(LoyaltyAwardService loyaltyService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
    }

    public LoyaltyAwardController(Func<TenantCrmDbContext> contextFactory)
    {
        _loyaltyService = new LoyaltyAwardService(contextFactory);
    }

    [HttpGet("overview")]
    public async Task<ActionResult<LoyaltyOverviewDto>> GetOverview(
        [FromQuery] int companyId = 1,
        [FromQuery] int? branchId = null,
        [FromQuery] string search = "",
        [FromQuery] bool includeArchived = false)
    {
        var overview = await _loyaltyService.GetLoyaltyOverviewAsync(companyId, branchId, search, includeArchived);
        return Ok(overview);
    }

    [HttpGet("tiers")]
    public async Task<ActionResult<List<LoyaltyAward>>> GetTiers([FromQuery] bool includeArchived = false)
    {
        var tiers = await _loyaltyService.GetAllTiersAsync(includeArchived);
        return Ok(tiers);
    }

    [HttpGet("discount")]
    public async Task<ActionResult<LoyaltyDiscountCalculationDto>> CalculateDiscount(
        [FromQuery] int companyId,
        [FromQuery] int customerId,
        [FromQuery] decimal rentalFee,
        [FromQuery] int? branchId = null)
    {
        var result = await _loyaltyService.CalculateCustomerDiscountAsync(companyId, customerId, rentalFee, branchId);
        return Ok(result);
    }

    [HttpPost("tier")]
    public async Task<IActionResult> SaveTier([FromBody] LoyaltyTierDto tierDto)
    {
        await _loyaltyService.SaveTierRuleAsync(tierDto);
        return Ok(new { message = "Loyalty tier saved successfully." });
    }

    [HttpPost]
    public async Task<IActionResult> CreateTier([FromBody] LoyaltyAward tier)
    {
        try
        {
            await _loyaltyService.CreateTierAsync(tier);
            return CreatedAtAction(nameof(GetTiers), new { id = tier.LoyaltyAwardId }, tier);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTier(int id, [FromBody] LoyaltyAward tier)
    {
        if (id != tier.LoyaltyAwardId) return BadRequest(new { message = "ID mismatch." });

        try
        {
            await _loyaltyService.UpdateTierAsync(tier);
            return Ok(new { message = "Tier updated successfully." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Tier not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}/toggle")]
    public async Task<IActionResult> ToggleTierStatus(int id)
    {
        try
        {
            await _loyaltyService.ToggleTierStatusAsync(id);
            return Ok(new { message = "Tier status toggled successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}