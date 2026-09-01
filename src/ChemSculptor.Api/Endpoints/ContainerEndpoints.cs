using ChemSculptor.Core;
using ChemSculptor.Domain;

namespace ChemSculptor.Api;

public static class ContainerEndpoints
{
    public static IEndpointRouteBuilder MapContainerEndpoints(this IEndpointRouteBuilder app)
    {
        var containers = app.MapGroup("/containers");

        containers.MapGet("/", (IContainerRegistry registry) => Results.Ok(registry.List()));

        containers.MapPost("/register",
            async (
                RegisterContainerRequest request,
                IContainerRegistry registry,
                EchoSkillContainer echo,
                CancellationToken ct) =>
            {
                if (!string.Equals(request.Id, echo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new
                    {
                        error = $"Container '{request.Id}' has no built-in implementation; register an ISkillContainer in DI first."
                    });
                }

                await registry.RegisterAsync(echo, ct);
                return Results.Created($"/containers/{echo.Name}", new ContainerDescriptor
                {
                    Id = echo.Name,
                    Version = echo.Version,
                    Capabilities = echo.Capabilities
                });
            });

        return app;
    }
}
