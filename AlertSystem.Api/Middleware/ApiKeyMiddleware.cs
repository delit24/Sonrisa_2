namespace AlertSystem.Api.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to /api/internal/* routes
        if (context.Request.Path.StartsWithSegments("/api/internal"))
        {
            var apiKey = _configuration["InternalApi:Key"];

            if (string.IsNullOrEmpty(apiKey))
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Internal API key not configured",
                    code = "CONFIG_ERROR"
                });
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Internal-Api-Key", out var providedKey)
                || providedKey != apiKey)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Invalid or missing API key",
                    code = "UNAUTHORIZED"
                });
                return;
            }
        }

        await _next(context);
    }
}
