using System.Diagnostics;

namespace EcoGoodz.Web.Diagnostics;

public sealed class SlowRequestLoggingMiddleware
{
    private static readonly TimeSpan DefaultThreshold = TimeSpan.FromSeconds(2);

    private readonly RequestDelegate _next;
    private readonly ILogger<SlowRequestLoggingMiddleware> _logger;
    private readonly TimeSpan _threshold;

    public SlowRequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<SlowRequestLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        var configuredMs = configuration.GetValue<int?>("Diagnostics:SlowRequestThresholdMs");
        _threshold = configuredMs is > 0 ? TimeSpan.FromMilliseconds(configuredMs.Value) : DefaultThreshold;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.Elapsed >= _threshold)
            {
                _logger.LogWarning(
                    "Slow request {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
