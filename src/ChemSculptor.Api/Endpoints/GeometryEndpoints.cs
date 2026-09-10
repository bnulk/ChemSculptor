using ChemSculptor.InputProcessor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

public static class GeometryEndpoints
{
    public static IEndpointRouteBuilder MapGeometryEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder geometries = EndpointRouteBuilderExtensions.MapGroup(app, "/geometries");

        EndpointRouteBuilderExtensions.MapPost(geometries, "/", SubmitGeometryAsync);

        return app;
    }

    private static async Task<IResult> SubmitGeometryAsync(
        HttpRequest request,
        IGeometryTextParser parser,
        CancellationToken cancellationToken)
    {
        string rawText;

        using (StreamReader reader = new StreamReader(request.Body))
        {
            rawText = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(rawText))
        {
            GeometryErrorResponse error = new GeometryErrorResponse();
            error.Error = "请提供非空的坐标文本。";
            return Results.BadRequest(error);
        }

        MolecularGeometry geometry = await parser.ParseAsync(rawText, cancellationToken);

        if (geometry.Atoms.Count == 0)
        {
            GeometryErrorResponse error = new GeometryErrorResponse();
            error.Error = "未能从文本中解析出任何原子坐标。";
            error.Diagnostics = new List<string>(geometry.Diagnostics);
            return Results.BadRequest(error);
        }

        GeometrySubmitResponse response = new GeometrySubmitResponse();
        response.SourceName = geometry.SourceName;
        response.Formula = geometry.Formula;
        response.AtomCount = geometry.Atoms.Count;
        response.Diagnostics = new List<string>(geometry.Diagnostics);
        response.Atoms = new List<GeometryAtomResponse>();

        for (int index = 0; index < geometry.Atoms.Count; index++)
        {
            GeometryAtom atom = geometry.Atoms[index];
            GeometryAtomResponse atomResponse = new GeometryAtomResponse();
            atomResponse.Element = atom.Element;
            atomResponse.X = atom.X;
            atomResponse.Y = atom.Y;
            atomResponse.Z = atom.Z;
            response.Atoms.Add(atomResponse);
        }

        return Results.Ok(response);
    }
}
