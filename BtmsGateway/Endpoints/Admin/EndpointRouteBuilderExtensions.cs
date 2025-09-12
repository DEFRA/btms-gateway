using BtmsGateway.Authentication;
using BtmsGateway.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace BtmsGateway.Endpoints.Admin;

public static class EndpointRouteBuilderExtensions
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("admin/redrive", PostRedrive)
            .WithName("PostRedrive")
            .WithTags("Admin")
            .WithSummary("Initiates redrive of messages in the dead letter queue")
            .WithDescription("Redrives all messages in the resource events dead leter queue")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status405MethodNotAllowed)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(PolicyNames.Execute);
    }

    [HttpPost]
    private static async Task<IResult> PostRedrive(
        [FromServices] ISqsService sqsService,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (!await sqsService.Redrive(cancellationToken))
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
