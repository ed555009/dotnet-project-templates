using System.Reflection;
using MyCustomWorker;
using Serilog;
using Serilog.Formatting.Elasticsearch;

try
{
	var builder = Host.CreateApplicationBuilder(args);
	var services = builder.Services;
	var env = builder.Environment;

	// configure serilog logger
	Log.Logger = new LoggerConfiguration()
		.ReadFrom.Configuration(builder.Configuration)
		.WriteTo.Async(env.IsDevelopment() ? x => x.Console() : x => x.Console(new ElasticsearchJsonFormatter()))
		.Enrich.WithProperty("Application", Assembly.GetExecutingAssembly().FullName)
		.CreateLogger();

	_ = builder.Logging.ClearProviders();
	_ = builder.Logging.AddSerilog(Log.Logger);

	// add services

	_ = services.AddHostedService<Worker>();

	var host = builder.Build();
	host.Run();
}
catch (Exception ex)
{
	Log.Fatal("Starting app failed.", ex);
}
finally
{
	Log.CloseAndFlush();
}
