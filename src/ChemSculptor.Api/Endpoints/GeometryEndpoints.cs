using ChemSculptor.InputProcessor;

namespace ChemSculptor.Api;

public static class GeometryEndpoints
{
    public static IEndpointRouteBuilder MapGeometryEndpoints(this IEndpointRouteBuilder app)
    {
        var geometries = app.MapGroup("/geometries");

        // 语法糖说明：async Lambda 等价于定义命名方法后传入。
        // 参数从框架注入：HttpRequest 是当前请求，parser 来自 DI。
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
                // 语法糖说明：geometry.Atoms.Select(atom => new { ... })
                // 是 LINQ + Lambda，等价于 foreach 逐个取出元素并
                // 放进一个新的匿名对象列表。
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
