using BtmsGateway.Config;
using Microsoft.AspNetCore.Authentication;

namespace BtmsGateway.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationAuthorization(this IServiceCollection services)
    {
        services.AddOptions<AclOptions>().BindConfiguration("Acl").ValidateOptions();

        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                BasicAuthenticationHandler.SchemeName,
                _ => { }
            );

        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                PolicyNames.Read,
                builder => builder.RequireAuthenticatedUser().RequireClaim(Claims.Scope, Scopes.Read)
            )
            .AddPolicy(
                PolicyNames.Write,
                builder => builder.RequireAuthenticatedUser().RequireClaim(Claims.Scope, Scopes.Write)
            )
            .AddPolicy(
                PolicyNames.Execute,
                builder => builder.RequireAuthenticatedUser().RequireClaim(Claims.Scope, Scopes.Execute)
            );

        return services;
    }
}
