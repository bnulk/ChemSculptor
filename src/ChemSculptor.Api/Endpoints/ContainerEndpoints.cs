using ChemSculptor.Core;
using ChemSculptor.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

/// <summary>
/// 技能容器相关端点。
/// </summary>
public static class ContainerEndpoints
{
    /// <summary>登记容器列表与注册端点。</summary>
    public static IEndpointRouteBuilder MapContainerEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder containers = EndpointRouteBuilderExtensions.MapGroup(app, "/containers");

        EndpointRouteBuilderExtensions.MapGet(containers, "/", ListContainers);
        EndpointRouteBuilderExtensions.MapPost(containers, "/register", RegisterContainerAsync);

        return app;
    }

    /// <summary>列出已注册容器。</summary>
    private static IResult ListContainers(IContainerRegistry registry)
    {
        return Results.Ok(registry.List());
    }

    /// <summary>注册内置演示容器。</summary>
    private static async Task<IResult> RegisterContainerAsync(
        RegisterContainerRequest request,
        IContainerRegistry registry,
        EchoSkillContainer echo,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Id, echo.Name, StringComparison.OrdinalIgnoreCase))
        {
            ApiError error = new ApiError();
            error.Error = "Container '" + request.Id +
                "' has no built-in implementation; register an ISkillContainer in DI first.";
            return Results.BadRequest(error);
        }

        await registry.RegisterAsync(echo, cancellationToken);

        ContainerDescriptor descriptor = new ContainerDescriptor();
        descriptor.Id = echo.Name;
        descriptor.Version = echo.Version;
        descriptor.Capabilities = new List<string>(echo.Capabilities);

        string location = "/containers/" + echo.Name;
        return Results.Created(location, descriptor);
    }
}
