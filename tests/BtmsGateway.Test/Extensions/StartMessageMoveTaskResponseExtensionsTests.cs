using System.Net;
using Amazon.Runtime;
using Amazon.SQS.Model;
using BtmsGateway.Extensions;
using FluentAssertions;

namespace BtmsGateway.Test.Extensions;

public class StartMessageMoveTaskResponseExtensionsTests
{
    [Fact]
    public void When_response_converted_to_string_Then_object_converted()
    {
        var taskResponse = new StartMessageMoveTaskResponse
        {
            HttpStatusCode = HttpStatusCode.InternalServerError,
            TaskHandle = "test-task-handle",
            ContentLength = 1,
            ResponseMetadata = new ResponseMetadata { Metadata = { ["Foo"] = "Bar", ["ABC"] = "123" } },
        };

        var result = taskResponse.ToStringExtended();

        result
            .Should()
            .Be(
                "Http Status Code: InternalServerError, TaskHandle: test-task-handle, Content Length: 1\nFoo: Bar\nABC: 123"
            );
    }
}
