using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace IO.Swagger
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            // Log request
            _logger.LogInformation($"Incoming {context.Request.Method} request to {context.Request.Path}");

            await _next(context); // Call next middleware

            // Log response
            _logger.LogInformation($"Outgoing response: {context.Response.StatusCode}");
        }
    }

}
