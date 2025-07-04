using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MyWebApi.Extensions;
using MyWebApi.Middlewares;
using Serilog;
using Serilog.Formatting.Elasticsearch;

try
{
	var builder = WebApplication.CreateBuilder(args);
	var services = builder.Services;
	var env = builder.Environment;

	// Configure Serilog logger
	_ = builder.Host.UseSerilog(
		(context, configuration) =>
		{
			configuration
				.ReadFrom.Configuration(context.Configuration)
				.WriteTo.Async(
					env.IsDevelopment()
						? x => x.Console()
						: x => x.Console(new Serilog.Formatting.Elasticsearch.ElasticsearchJsonFormatter())
				)
				.Enrich.WithProperty("Assembly", Assembly.GetExecutingAssembly());
		}
	);

	// Add services to the container.

	_ = services
		.AddControllers()
		.AddJsonOptions(options =>
		{
			options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
			options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
			options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
			options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
			options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
		});

	// problem details
	// services.AddProblemDetails(options =>
	// {
	// 	options.CustomizeProblemDetails = context =>
	// 	{
	// 		context.ProblemDetails.Instance =
	// 			$"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
	// 		context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
	// 		context.ProblemDetails.Extensions.TryAdd(
	// 			"traceId",
	// 			context.HttpContext.Features.Get<IHttpRequestIdentifierFeature>()?.TraceIdentifier
	// 		);
	// 	};
	// });

	if (!env.IsProduction())
	{
		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		_ = services.AddEndpointsApiExplorer();
		_ = services.AddSwaggerGen(options =>
		{
			options.MapType<DateOnly>(() =>
				new OpenApiSchema
				{
					Type = "string",
					Format = "date",
					Example = new OpenApiString(DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd")),
				}
			);
			options.MapType<TimeOnly>(() =>
				new OpenApiSchema
				{
					Type = "string",
					Format = "time",
					Example = new OpenApiString(TimeOnly.FromDateTime(DateTime.Now).ToString("HH:mm:ss")),
				}
			);
		});
	}

	// health check
	services.AddHealthChecks();

	var app = builder.Build();

	_ = app.UseMiddleware<RequestBodyLoggingMiddleware>();
	_ = app.UseSerilogRequestLogging(options =>
	{
		options.MessageTemplate =
			"{User} from {RemoteIpAddress} {RequestScheme} {RequestHost} {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
		options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
		{
			string user = httpContext.User.Identity?.Name ?? "Anonymous";

			diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
			diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme.ToUpper());
			diagnosticContext.Set("RemoteIpAddress", httpContext.ClientIP().ToString());
			diagnosticContext.Set("User", user);
			diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent);
			diagnosticContext.Set("RequestHeaderAuthorization", httpContext.Request.Headers.Authorization);
			diagnosticContext.Set("ResponseContentType", httpContext.Response.ContentType);
			diagnosticContext.Set("ResponseContentLength", httpContext.Response.ContentLength);

			if (httpContext.Request.QueryString.HasValue)
				diagnosticContext.Set("RequestQueryString", httpContext.Request.QueryString.Value);
		};
	});

	// global exception handler
	// app.UseMiddleware<ExceptionHandlingMiddleware>();

	// global response wrapper
	_ = app.UseMiddleware<ResponseWrapperMiddleware>();

	// Configure the HTTP request pipeline.
	if (!env.IsProduction())
	{
		_ = app.UseSwagger();
		_ = app.UseSwaggerUI();
	}

	_ = app.UseHttpsRedirection();

	_ = app.UseAuthorization();

	_ = app.MapControllers();
	_ = app.MapHealthChecks("/health");

	app.Run();
}
catch (Exception ex)
{
	Log.Fatal("Starting app failed.", ex);
}
finally
{
	Log.CloseAndFlush();
}
