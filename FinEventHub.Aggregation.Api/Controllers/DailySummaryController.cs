using FinEventHub.Aggregation.Api.Data;
using FinEventHub.Aggregation.Api.Interfaces;
using FinEventHub.Aggregation.Api.Models;
using FinEventHub.Aggregation.Api.Validators;
using Microsoft.AspNetCore.Mvc;

namespace Aggregation.Api.Controllers;

[ApiController]
[Route("api/v1/customers/{customerId}/daily-summary")]
public class DailySummaryController : ControllerBase
{
    private readonly IDailySummaryService _dailySummaryService;

    public DailySummaryController(
     IDailySummaryService dailySummaryService)
    {
        _dailySummaryService = dailySummaryService;
    }
    [HttpGet]
    public async Task<IActionResult> GetDailySummary([FromRoute] string customerId, [FromQuery] DailySummaryQuery query, CancellationToken cancellationToken)
    {
        var summary = await _dailySummaryService.GetAsync(customerId,query.Date,query.Currency,cancellationToken);

        if (summary is null)
            return NotFound();

        return Ok(summary);
    }
}