namespace FinEventHub.Contracts.Responses
{
    public class BatchAcceptedResponse
    {
        public int Accepted { get; init; }
        public string TraceId { get; init; } = default!;
    }
}
