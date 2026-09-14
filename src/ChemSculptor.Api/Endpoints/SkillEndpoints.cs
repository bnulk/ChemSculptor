using ChemSculptor.Core;
using ChemSculptor.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

/// <summary>
/// 技能相关端点。
/// </summary>
public static class SkillEndpoints
{
    /// <summary>登记技能列表与注册端点。</summary>
    public static IEndpointRouteBuilder MapSkillEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder skills = EndpointRouteBuilderExtensions.MapGroup(app, "/skills");

        EndpointRouteBuilderExtensions.MapGet(skills, "/", ListSkills);
        EndpointRouteBuilderExtensions.MapPost(skills, "/register", RegisterSkillAsync);

        return app;
    }

    /// <summary>列出已注册技能。</summary>
    private static IResult ListSkills(ISkillRegistry registry)
    {
        return Results.Ok(registry.List());
    }

    /// <summary>注册内置演示技能。</summary>
    private static async Task<IResult> RegisterSkillAsync(
        RegisterSkillRequest request,
        ISkillRegistry registry,
        EchoSkill echo,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Id, echo.Name, StringComparison.OrdinalIgnoreCase))
        {
            ApiError error = new ApiError();
            error.Error = "Skill '" + request.Id +
                "' has no built-in implementation; register an ISkill in DI first.";
            return Results.BadRequest(error);
        }

        await registry.RegisterAsync(echo, cancellationToken);

        SkillDescriptor descriptor = new SkillDescriptor();
        descriptor.Id = echo.Name;
        descriptor.Version = echo.Version;
        descriptor.Capabilities = new List<string>(echo.Capabilities);

        string location = "/skills/" + echo.Name;
        return Results.Created(location, descriptor);
    }
}
