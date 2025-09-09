using Amazon.SQS;
using Amazon.SQS.Model;
using BtmsGateway.Config;
using Microsoft.Extensions.Options;

namespace BtmsGateway.Services.Admin;

public interface ISqsService
{
    Task<bool> Redrive(CancellationToken cancellationToken);
}

public class SqsService(IAmazonSQS amazonSqs, IOptions<AwsSqsOptions> awsSqsOptions, ILogger<SqsService> logger)
    : ISqsService
{
    public async Task<bool> Redrive(CancellationToken cancellationToken)
    {
        try
        {
            var startMessageMoveTaskRequest = new StartMessageMoveTaskRequest
            {
                SourceArn = awsSqsOptions.Value.OutboundClearanceDecisionsDeadLetterQueueArn,
            };

            var startMessageMoveTaskResponse = await amazonSqs.StartMessageMoveTaskAsync(
                startMessageMoveTaskRequest,
                cancellationToken
            );

            if (startMessageMoveTaskResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                logger.LogInformation(
                    "Redrive message move task started with TaskHandle: {TaskHandle}",
                    startMessageMoveTaskResponse.TaskHandle
                );
                return true;
            }

            logger.LogError("Redrive failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initiate start message move task during redrive request.");
        }

        return false;
    }
}
