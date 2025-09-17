using Amazon.SQS.Model;

namespace BtmsGateway.Extensions;

public static class StartMessageMoveTaskResponseExtensions
{
    public static string ToStringExtended(this StartMessageMoveTaskResponse startMessageMoveTaskResponse)
    {
        var stringResponse =
            $"Http Status Code: {startMessageMoveTaskResponse.HttpStatusCode}, TaskHandle: {startMessageMoveTaskResponse.TaskHandle}, Content Length: {startMessageMoveTaskResponse.ContentLength}";

        return startMessageMoveTaskResponse.ResponseMetadata.Metadata.Aggregate(
            stringResponse,
            (current, keyValuePair) => current + $"\n{keyValuePair.Key}: {keyValuePair.Value}"
        );
    }
}
