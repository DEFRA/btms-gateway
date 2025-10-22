namespace BtmsGateway.IntegrationTests.TestUtils;

public static class FixtureTest
{
    private static readonly string s_fixturesPath = Path.Combine("Fixtures");

    public static string UsingContent(string fixtureFile)
    {
        return File.ReadAllText(Path.Combine(s_fixturesPath, fixtureFile));
    }

    public static string WithRandomCorrelationId(this string contentTemplate)
    {
        return contentTemplate.Replace("{CORRELATION_ID}", new Random().Next(1000000, 9999999).ToString());
    }
}
