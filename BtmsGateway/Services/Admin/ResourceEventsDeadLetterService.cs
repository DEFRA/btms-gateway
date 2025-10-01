using Amazon.SQS;
using Amazon.SQS.Model;
using BtmsGateway.Config;
using BtmsGateway.Extensions;
using Microsoft.Extensions.Options;

namespace BtmsGateway.Services.Admin;

public interface IResourceEventsDeadLetterService
{
    Task<bool> Redrive(CancellationToken cancellationToken);
}

public class ResourceEventsDeadLetterService(
    IAmazonSQS amazonSqs,
    IOptions<AwsSqsOptions> awsSqsOptions,
    ILogger<ResourceEventsDeadLetterService> logger
) : IResourceEventsDeadLetterService
{
    public async Task<bool> Redrive(CancellationToken cancellationToken)
    {
        try
        {
            var request = new StartMessageMoveTaskRequest
            {
                SourceArn = awsSqsOptions.Value.ResourceEventsDeadLetterQueueArn,
            };

            var response = await amazonSqs.StartMessageMoveTaskAsync(request, cancellationToken);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                logger.LogInformation(
                    "Redrive message move task started with TaskHandle: {TaskHandle}",
                    response.TaskHandle
                );
                return true;
            }

            logger.LogError("Redrive failed with response: {TaskResponse}", response.ToStringExtended());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initiate start message move task during redrive request.");
        }

        return false;
    }
}
