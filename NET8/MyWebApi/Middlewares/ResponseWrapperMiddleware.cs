using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
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

			var result = new ApiResultModel<object>
			{
				Success = context.Response.StatusCode >= 200 && context.Response.StatusCode < 300,
				Data = context.Response.StatusCode < 300 ? data : null,
				RequestId = context.TraceIdentifier,
				TraceId = Activity.Current?.Id,
			};

			await context.Response.WriteAsJsonAsync(result, _jsonSerializerOptions);

			// if (context.Response.StatusCode >= 400)
			// {
			// 	var problem = new ProblemDetailsModel
			// 	{
			// 		Type = "https://httpstatuses.com/" + context.Response.StatusCode,
			// 		Title = nameof(context.Response.StatusCode),
			// 		Status = context.Response.StatusCode,
			// 		Detail = "Request processed successfully.",
			// 		Instance = $"{context.Request.Method} {context.Request.Path}",
			// 		RequestId = context.TraceIdentifier,
			// 		TraceId = Activity.Current?.Id,
			// 	};

			// 	context.Response.ContentType = "application/problem+json";

			// 	await context.Response.WriteAsJsonAsync(problem, _jsonSerializerOptions);
			// }
			// else
			// {
			// 	var result = new ApiResultModel<object>
			// 	{
			// 		Success = context.Response.StatusCode >= 200 && context.Response.StatusCode < 300,
			// 		Data = data,
			// 		RequestId = context.TraceIdentifier,
			// 		TraceId = Activity.Current?.Id,
			// 	};

			// 	await context.Response.WriteAsJsonAsync(result, _jsonSerializerOptions);
			// }
		}
		catch (Exception ex)
		{
			_logger.LogError("Exception occurred. Exception: {Exception}", ex);

			var result = new ApiResultModel<object>
			{
				Success = false,
				Data = null,
				Message =
					$"{ex.Message}{(!string.IsNullOrEmpty(ex.InnerException?.Message) ? $" {ex.InnerException.Message}" : string.Empty)}",
				RequestId = context.TraceIdentifier,
				TraceId = Activity.Current?.Id,
			};
			context.Response.Body = originalBodyStream;
			context.Response.ContentType = "application/json";
			context.Response.StatusCode = DetermineStatusCode(ex);

			await context.Response.WriteAsJsonAsync(result, _jsonSerializerOptions);
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
			OperationCanceledException => StatusCodes.Status400BadRequest,
			UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
			NotSupportedException => StatusCodes.Status405MethodNotAllowed,
			KeyNotFoundException => StatusCodes.Status404NotFound,
			_ => StatusCodes.Status500InternalServerError,
		};
	}
}
