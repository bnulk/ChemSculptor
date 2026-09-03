using ChemSculptor.InputProcessor;

namespace ChemSculptor.Api;

public static class GeometryEndpoints
{
    public static IEndpointRouteBuilder MapGeometryEndpoints(this IEndpointRouteBuilder app)
    {
        var geometries = app.MapGroup("/geometries");

        geometries.MapPost("/", async (
            HttpRequest request,
            IGeometryTextParser parser,
            CancellationToken ct) =>
        {
            string rawText;
            using (var reader = new StreamReader(request.Body))
            {
                rawText = await reader.ReadToEndAsync(ct);
            }

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return Results.BadRequest(new { error = "请提供非空的坐标文本。" });
            }

            var geometry = await parser.ParseAsync(rawText, ct);
            if (geometry.Atoms.Count == 0)
            {
                return Results.BadRequest(new
                {
                    error = "未能从文本中解析出任何原子坐标。",
                    geometry.Diagnostics
                });
            }

            return Results.Ok(new
            {
                geometry.SourceName,
                geometry.Formula,
                AtomCount = geometry.Atoms.Count,
                Atoms = geometry.Atoms.Select(atom => new
                {
                    atom.Element,
                    atom.X,
                    atom.Y,
                    atom.Z
                }),
                geometry.Diagnostics
            });
        });

        return app;
    }
}
