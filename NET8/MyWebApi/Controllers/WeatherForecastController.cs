using Microsoft.AspNetCore.Mvc;
using MyWebApi.Models.Responses;

namespace MyWebApi.Controllers;

[ApiController]
[Route("[controller]")]
[ProducesResponseType(typeof(ApiResultModel<IEnumerable<WeatherForecast>>), 200)]
[ProducesResponseType(typeof(ProblemDetailsModel), 400)]
[ProducesResponseType(typeof(ProblemDetailsModel), 405)]
[ProducesResponseType(typeof(ProblemDetailsModel), 500)]
public class WeatherForecastController(ILogger<WeatherForecastController> logger) : ControllerBase
{
	private static readonly string[] _summaries =
	[
		"Freezing",
		"Bracing",
		"Chilly",
		"Cool",
		"Mild",
		"Warm",
		"Balmy",
		"Hot",
		"Sweltering",
		"Scorching",
	];

	private readonly ILogger<WeatherForecastController> _logger = logger;

	[HttpGet(Name = "GetWeatherForecast")]
	public IEnumerable<WeatherForecast> Get()
	{
		return Enumerable
			.Range(1, 5)
			.Select(index => new WeatherForecast
			{
				Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
				TemperatureC = Random.Shared.Next(-20, 55),
				Summary = _summaries[Random.Shared.Next(_summaries.Length)],
			})
			.ToArray();
	}

	[HttpPost]
	public WeatherForecast Create(CancellationToken cancellationToken)
	{
		throw new ArgumentException("Invalid argument.");
	}
}
