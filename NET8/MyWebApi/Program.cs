using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
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
		_ = services.AddSwaggerGen();
	}

	// health check
	services.AddHealthChecks();

	var app = builder.Build();

	// global exception handler
	// app.UseMiddleware<ExceptionHandlingMiddleware>();

	// global response wrapper
	app.UseMiddleware<ResponseWrapperMiddleware>();

	// Configure the HTTP request pipeline.
	if (!env.IsProduction())
	{
		app.UseSwagger();
		app.UseSwaggerUI();
	}

	app.UseHttpsRedirection();

	app.UseAuthorization();

	app.MapControllers();
	app.MapHealthChecks("/health");

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
