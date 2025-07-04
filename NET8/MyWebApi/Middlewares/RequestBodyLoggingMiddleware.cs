using System.Text;

namespace MyWebApi.Middlewares;

public class RequestBodyLoggingMiddleware(ILogger<RequestBodyLoggingMiddleware> logger, RequestDelegate next)
{
	private readonly RequestDelegate _next = next;
	private readonly ILogger<RequestBodyLoggingMiddleware> _logger = logger;

	public async Task InvokeAsync(HttpContext context)
	{
		var methods = new List<string> { "POST", "PUT", "DELETE", "PATCH" };

		if (
			methods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase)
			&& !context.Request.Path.ToString().EndsWith("/user")
			&& !context.Request.Path.ToString().EndsWith("/user/login")
			&& !context.Request.Path.ToString().EndsWith("/user/password")
			&& !context.Request.Path.ToString().EndsWith("/resetpassword")
		)
		{
			// enable buffering, so request body can be read again
			context.Request.EnableBuffering();

			using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, false, 1024, true))
			{
				var requestBody = await reader.ReadToEndAsync();

				_logger.LogInformation("{RequestMethod}: {RequestBody}", context.Request.Method, requestBody);
			}

			// put data pointer back to start
			context.Request.Body.Position = 0;
		}

		await _next(context);
	}
}
