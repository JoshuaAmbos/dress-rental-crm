using CRM.api.DTOs;
using CRM.api.Services;
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
    public async Task<ActionResult<LoyaltyOverviewDto>> GetOverview([FromQuery] int companyId = 1, [FromQuery] string search = "")
    {
        var overview = await _loyaltyService.GetLoyaltyOverviewAsync(companyId, search);
        return Ok(overview);
    }

    [HttpGet("discount")]
    public async Task<ActionResult<LoyaltyDiscountCalculationDto>> CalculateDiscount([FromQuery] int companyId, [FromQuery] int customerId, [FromQuery] decimal rentalFee)
    {
        var result = await _loyaltyService.CalculateCustomerDiscountAsync(companyId, customerId, rentalFee);
        return Ok(result);
    }

    [HttpPost("tier")]
    public async Task<IActionResult> SaveTier([FromBody] LoyaltyTierDto tierDto)
    {
        await _loyaltyService.SaveTierRuleAsync(tierDto);
        return Ok(new { message = "Loyalty tier updated successfully." });
    }
}