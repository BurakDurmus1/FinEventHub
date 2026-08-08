using FinEventHub.Contracts.Requests;
using FinEventHub.Contracts.Responses;
using FinEventHub.Ingestion.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinEventHub.Ingestion.Api.Controllers
{
    [Route("api/v1/events")]
    [ApiController]
    public sealed class EventsController : ControllerBase
    {
        private readonly IIngestionService _service;

        public EventsController(IIngestionService service)
        {
            _service = service;
        }

        [HttpPost("batch")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<ActionResult<BatchAcceptedResponse>> Post(
            BatchEventRequest request,
            CancellationToken cancellationToken)
        {
            var accepted = await _service.ProcessBatchAsync(
                request,
                cancellationToken);

            return Accepted(new BatchAcceptedResponse
            {
                Accepted = accepted,
                TraceId = HttpContext.TraceIdentifier
            });
        }
    }
}
