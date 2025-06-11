using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace MyWebApi.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
	private readonly RequestDelegate _next = next;
	private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;
	private readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		ReferenceHandler = ReferenceHandler.IgnoreCycles,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
	};

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (Exception ex)
		{
			_logger.LogError("Unhandled exception occurred. Exception: {Exception}", ex);

			var problem = new ProblemDetails
			{
				Type = "https://httpstatuses.com/500",
				Title = ex.GetType().Name,
				Status = DetermineStatusCode(ex),
				Detail = ex.Message,
				Instance = $"{context.Request.Method} {context.Request.Path}",
			};

			problem.Extensions.TryAdd("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
			problem.Extensions.TryAdd("requestId", context.TraceIdentifier);
			problem.Extensions.TryAdd("traceId", Activity.Current?.Id ?? null);

			context.Response.ContentType = "application/problem+json";
			context.Response.StatusCode = DetermineStatusCode(ex);

			await context.Response.WriteAsJsonAsync(problem, _jsonSerializerOptions);
		}
	}

	private static int DetermineStatusCode(Exception ex)
	{
		return ex switch
		{
			ArgumentNullException => StatusCodes.Status400BadRequest,
			ArgumentOutOfRangeException => StatusCodes.Status400BadRequest,
			ArgumentException => StatusCodes.Status400BadRequest,
			UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
			NotSupportedException => StatusCodes.Status405MethodNotAllowed,
			KeyNotFoundException => StatusCodes.Status404NotFound,
			_ => StatusCodes.Status500InternalServerError,
		};
	}
}
