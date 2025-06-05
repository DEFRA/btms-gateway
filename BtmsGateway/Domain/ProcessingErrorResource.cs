using Defra.TradeImportsDataApi.Domain.Errors;

namespace BtmsGateway.Domain;

public class ProcessingErrorResource
{
    public required ProcessingError[] ProcessingErrors { get; set; }
}
