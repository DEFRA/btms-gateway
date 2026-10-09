namespace BtmsGateway.Config;

public class CdsOptions
{
    public const string SectionName = nameof(CdsOptions);

    public required string Username { get; init; } = "default-username";

    public required string Password { get; init; } = "default-password";
}
