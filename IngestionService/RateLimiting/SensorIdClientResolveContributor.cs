using AspNetCoreRateLimit;

namespace IngestionService.RateLimiting
{
    public class SensorIdClientResolveContributor : IClientResolveContributor
    {
        public async Task<string> ResolveClientAsync(HttpContext httpContext)
        {
            if (httpContext.Request.Path.Equals("/api/ingest/reading", StringComparison.OrdinalIgnoreCase)
                && httpContext.Request.Method == "POST")
            {
                string? sensorId = await ExtractSensorIdFromBodyAsync(httpContext.Request);
                if (!string.IsNullOrWhiteSpace(sensorId))
                    return sensorId;
            }

            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }


        private static async Task<string?> ExtractSensorIdFromBodyAsync(HttpRequest request)
        {
            try
            {
                request.EnableBuffering();
                request.Body.Position = 0;

                using var doc = await System.Text.Json.JsonDocument.ParseAsync(
                    request.Body, cancellationToken: CancellationToken.None);

                request.Body.Position = 0;

                if (doc.RootElement.TryGetProperty("sensorId", out var idElement))
                    return idElement.GetString();
            }
            catch
            {
                try { request.Body.Position = 0; } catch {  }
            }

            return null;
        }
    }
}
