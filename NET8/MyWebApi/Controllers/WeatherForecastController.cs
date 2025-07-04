using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyWebApi.Models.Responses;

namespace MyWebApi.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResultModel<IEnumerable<WeatherForecast>>), 200)]
[ProducesResponseType(typeof(ProblemDetailsModel), 400)]
[ProducesResponseType(typeof(ProblemDetailsModel), 401)]
[ProducesResponseType(typeof(ProblemDetailsModel), 403)]
[ProducesResponseType(typeof(ProblemDetailsModel), 404)]
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
	public async Task<IEnumerable<WeatherForecast>> Get(CancellationToken cancellationToken)
	{
		await Task.Delay(3000, cancellationToken);
		return
		[
			.. Enumerable
				.Range(1, 5)
				.Select(index => new WeatherForecast
				{
					Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
					TemperatureC = Random.Shared.Next(-20, 55),
					Summary = _summaries[Random.Shared.Next(_summaries.Length)],
				}),
		];
	}

	[HttpPost]
	public WeatherForecast Create(CancellationToken cancellationToken)
	{
		throw new KeyNotFoundException();
	}

	[HttpPut]
	public WeatherForecast Update(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	[HttpDelete]
	public WeatherForecast Delete(CancellationToken cancellationToken)
	{
		throw new UnauthorizedAccessException();
	}
}
