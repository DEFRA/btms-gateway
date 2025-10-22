namespace BtmsGateway.IntegrationTests.Data.Entities;

public class Notification
{
    public required string Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Mrn { get; init; }
    public required string Xml { get; init; }
}
