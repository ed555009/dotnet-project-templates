using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using MyWebApi.Models.Responses;

namespace MyWebApi.Middlewares;

public class ResponseWrapperMiddleware(RequestDelegate next, ILogger<ResponseWrapperMiddleware> logger)
{
	private readonly RequestDelegate _next = next;
	private readonly ILogger<ResponseWrapperMiddleware> _logger = logger;
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
		var path = context.Request.Path;

		if (path == "/" || path == "/health" || path.StartsWithSegments("/swagger"))
		{
			await _next(context);
			return;
		}

		var originalBodyStream = context.Response.Body;
		await using var newBodyStream = new MemoryStream();
		context.Response.Body = newBodyStream;

		try
		{
			await _next(context);

			newBodyStream.Position = 0;
			var responseBody = await new StreamReader(newBodyStream).ReadToEndAsync();
			context.Response.Body = originalBodyStream;

			object? data = null;

			if (!string.IsNullOrEmpty(responseBody) && IsJsonResponse(context.Response))
			{
				try
				{
					data = JsonSerializer.Deserialize<object>(responseBody);
				}
				catch
				{
					data = responseBody;
				}
			}
			else
			{
				data = responseBody;
			}

			if (context.Response.StatusCode < 400)
			{
				var result = new ApiResultModel<object>
				{
					Success = context.Response.StatusCode >= 200 && context.Response.StatusCode < 300,
					Data = context.Response.StatusCode < 300 ? data : null,
					RequestId = context.TraceIdentifier,
					TraceId = Activity.Current?.Id,
				};

				await context.Response.WriteAsJsonAsync(result, _jsonSerializerOptions);
			}
			else
			{
				var problemDetail = new ProblemDetailsModel
				{
					Type =
						$"https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/{context.Response.StatusCode}",
					Title = ReasonPhrases.GetReasonPhrase(context.Response.StatusCode) ?? "Unknown Error",
					Status = context.Response.StatusCode,
					Detail = string.Empty,
					Instance = $"{context.Request.Method} {context.Request.Path}",
					RequestId = context.TraceIdentifier,
					TraceId = Activity.Current?.Id,
				};
				context.Response.Body = originalBodyStream;

				await context.Response.WriteAsJsonAsync(
					problemDetail,
					_jsonSerializerOptions,
					contentType: "application/problem+json; charset=utf-8"
				);
			}
		}
		catch (Exception ex)
		{
			if (ex is OperationCanceledException || ex is TaskCanceledException)
				// If the request was cancelled, we don't need to log it as an error.
				_logger.LogInformation("Request was cancelled. RequestId: {RequestId}", context.TraceIdentifier);
			else
				_logger.LogError("Exception occurred. Exception: {Exception}", ex.Message);

			var statusCode = DetermineStatusCode(ex);
			var problemDetail = new ProblemDetailsModel
			{
				Type = $"https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/{statusCode}",
				Title = ReasonPhrases.GetReasonPhrase(statusCode) ?? "Unknown Error",
				Status = statusCode,
				Detail =
					$"{ex.Message}{(!string.IsNullOrEmpty(ex.InnerException?.Message) ? $" {ex.InnerException.Message}" : string.Empty)}",
				Instance = $"{context.Request.Method} {context.Request.Path}",
				RequestId = context.TraceIdentifier,
				TraceId = Activity.Current?.Id,
			};
			context.Response.Body = originalBodyStream;
			context.Response.StatusCode = statusCode;

			await context.Response.WriteAsJsonAsync(
				problemDetail,
				_jsonSerializerOptions,
				contentType: "application/problem+json; charset=utf-8"
			);
		}
		finally
		{
			await context.Response.Body.FlushAsync();
		}
	}

	private static bool IsJsonResponse(HttpResponse response)
	{
		return response.ContentType != null
			&& response.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
	}

	private static int DetermineStatusCode(Exception ex)
	{
		return ex switch
		{
			ArgumentException => StatusCodes.Status400BadRequest,
			OperationCanceledException => StatusCodes.Status499ClientClosedRequest,
			UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
			NotSupportedException => StatusCodes.Status405MethodNotAllowed,
			KeyNotFoundException => StatusCodes.Status404NotFound,
			_ => StatusCodes.Status500InternalServerError,
		};
	}
}
