using MyCustomWorker;
using Serilog;
using Serilog.Formatting.Elasticsearch;

try
{
	IHost host = Host.CreateDefaultBuilder(args)
		.UseSerilog((hostContext, config) =>
		{
			_ = config.ReadFrom.Configuration(hostContext.Configuration)
				.WriteTo.Async(hostContext.HostingEnvironment.IsDevelopment()
					? x => x.Console()
					: x => x.Console(new ElasticsearchJsonFormatter()));
		})
		.ConfigureServices(services =>
		{
			services.AddHostedService<Worker>();
		})
		.Build();

	await host.RunAsync();
}
catch (Exception ex)
{
	Log.Fatal("Starting app failed.", ex);
}
finally
{
	Log.CloseAndFlush();
}
