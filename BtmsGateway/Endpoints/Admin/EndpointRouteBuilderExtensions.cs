using BtmsGateway.Authentication;
using BtmsGateway.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace BtmsGateway.Endpoints.Admin;

public static class EndpointRouteBuilderExtensions
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("admin/dlq/redrive", Redrive)
            .WithName(nameof(Redrive))
            .WithTags("Admin")
            .WithSummary("Initiates redrive of messages from the dead letter queue")
            .WithDescription("Redrives all messages on the resource events dead letter queue")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Execute);

        app.MapPost("admin/dlq/remove-message", RemoveMessage)
            .WithName(nameof(RemoveMessage))
            .WithTags("Admin")
            .WithSummary("Initiates removal of message from the dead letter queue")
            .WithDescription(
                "Attempts to find and remove a message on the resource events dead letter queue by message ID"
            )
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Execute);

        app.MapPost("admin/dlq/drain", Drain)
            .WithName(nameof(Drain))
            .WithTags("Admin")
            .WithSummary("Initiates drain of all messages from the dead letter queue")
            .WithDescription("Drains all messages on the resource events dead letter queue")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Execute);
    }

    [HttpPost]
    private static async Task<IResult> Redrive(
        [FromServices] IResourceEventsDeadLetterService resourceEventsDeadLetterService,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (!await resourceEventsDeadLetterService.Redrive(cancellationToken))
            {
                return Results.InternalServerError();
            }
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }

        return Results.Accepted();
    }

    [HttpPost]
    private static async Task<IResult> RemoveMessage(
        string messageId,
        [FromServices] IResourceEventsDeadLetterService resourceEventsDeadLetterService,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await resourceEventsDeadLetterService.Remove(messageId, cancellationToken);

            return Results.Content(result, "text/plain; charset=utf-8");
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }
    }

    [HttpPost]
    private static async Task<IResult> Drain(
        [FromServices] IResourceEventsDeadLetterService resourceEventsDeadLetterService,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (!await resourceEventsDeadLetterService.Drain(cancellationToken))
            {
                return Results.InternalServerError();
            }
        }
        catch (Exception)
        {
            return Results.InternalServerError();
        }

        return Results.Ok();
    }
}
