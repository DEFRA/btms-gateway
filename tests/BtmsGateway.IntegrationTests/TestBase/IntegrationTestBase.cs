using System.Net.Http.Headers;
using BtmsGateway.IntegrationTests.Data.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace BtmsGateway.IntegrationTests.TestBase;

[Trait("Category", "IntegrationTest")]
[Collection("Integration Tests")]
public abstract class IntegrationTestBase
{
    protected IntegrationTestBase()
    {
        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
        };

        ConventionRegistry.Register(nameof(conventionPack), conventionPack, _ => true);
    }

    protected static HttpClient CreateHttpClient(bool withAuthentication = true)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:3091") };

        if (withAuthentication)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                // See compose.yml for username, password and scope configuration
                Convert.ToBase64String("IntegrationTests:integration-tests-pwd"u8.ToArray())
            );
        }

        return httpClient;
    }

    private static IMongoDatabase GetMongoDatabase()
    {
        var settings = MongoClientSettings.FromConnectionString("mongodb://127.0.0.1:27017/?directConnection=true");
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        settings.ConnectTimeout = TimeSpan.FromSeconds(5);
        settings.SocketTimeout = TimeSpan.FromSeconds(5);

        return new MongoClient(settings).GetDatabase("trade-imports-cds-simulator-api");
    }

    protected static IMongoCollection<Notification> GetDecisionNotificationsCollection()
    {
        var db = GetMongoDatabase();

        return db.GetCollection<Notification>("DecisionNotifications");
    }

    protected static IMongoCollection<Notification> GetErrorNotificationsCollection()
    {
        var db = GetMongoDatabase();

        return db.GetCollection<Notification>("ErrorNotifications");
    }
}
