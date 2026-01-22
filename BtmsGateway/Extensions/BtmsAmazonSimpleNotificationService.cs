using System.Net;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using BtmsGateway.Config;

namespace BtmsGateway.Extensions;

public class BtmsAmazonSimpleNotificationService : AmazonSimpleNotificationServiceClient
{
    private readonly IConfiguration _configuration;

    public BtmsAmazonSimpleNotificationService(
        AmazonSimpleNotificationServiceConfig snsConfig,
        IConfiguration configuration
    )
        : base(snsConfig)
    {
        _configuration = configuration;
    }

    public BtmsAmazonSimpleNotificationService(
        AWSCredentials credentials,
        AmazonSimpleNotificationServiceConfig clientConfig,
        IConfiguration configuration
    )
        : base(credentials, clientConfig)
    {
        _configuration = configuration;
    }

    public override Task<ListTopicsResponse> ListTopicsAsync(
        ListTopicsRequest request,
        CancellationToken cancellationToken = new CancellationToken()
    )
    {
        var topics = _configuration
            .GetSection($"{AwsSqsOptions.SectionName}:{nameof(AwsSqsOptions.Topics)}")
            .Get<List<string>>();
        return Task.FromResult(
            new ListTopicsResponse()
            {
                HttpStatusCode = HttpStatusCode.OK,
                Topics = topics.Select(x => new Topic() { TopicArn = x }).ToList(),
            }
        );
    }
}
