using BtmsGateway.Authentication;
using BtmsGateway.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace BtmsGateway.Endpoints.Admin;

public static class EndpointRouteBuilderExtensions
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("admin/redrive", PostRedrive)
            .WithName(nameof(PostRedrive))
            .WithTags("Admin")
            .WithSummary("Initiates redrive of messages from the dead letter queue")
            .WithDescription("Redrives all messages on the resource events dead letter queue")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Execute);
    }

    [HttpPost]
    private static async Task<IResult> PostRedrive(
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
}
