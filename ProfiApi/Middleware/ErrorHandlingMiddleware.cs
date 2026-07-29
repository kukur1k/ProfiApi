namespace ProfiApi.Middleware;

public class ErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<ErrorHandlingMiddleware> logger
)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await next(ctx);
            sw.Stop();

            logger.LogInformation("{Method} {Path} -> {Status} ({Ms}ms)",
            ctx.Request.Method, ctx.Request.Path,
            ctx.Response.StatusCode, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "{Method} {Path} → 500 ({Ms}ms)",
                ctx.Request.Method, ctx.Request.Path, sw.ElapsedMilliseconds);

            ctx.Response.StatusCode = 500;
            ctx.Response.ContentType = "application/json";

            var response = new ProfiApi.DTOs.ApiResponce<object>(
                false, null, "Внутренняя ошибка сервера", "INTERNAL_ERROR"
            );

            await ctx.Response.WriteAsJsonAsync(response);
        }
    }
}