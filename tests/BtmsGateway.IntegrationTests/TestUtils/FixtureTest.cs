namespace BtmsGateway.IntegrationTests.TestUtils;

public static class FixtureTest
{
    private static readonly string s_fixturesPath = Path.Combine("Fixtures");

    public static string UsingContent(string fixtureFile)
    {
        return File.ReadAllText(Path.Combine(s_fixturesPath, fixtureFile));
    }
}
