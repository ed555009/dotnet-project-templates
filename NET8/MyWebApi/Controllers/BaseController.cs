using Microsoft.AspNetCore.Mvc;
using MyWebApi.Models.Responses;

namespace MyWebApi.Controllers;

[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
[ProducesResponseType(typeof(ProblemDetailsModel), 400)]
[ProducesResponseType(typeof(ProblemDetailsModel), 401)]
[ProducesResponseType(typeof(ProblemDetailsModel), 403)]
[ProducesResponseType(typeof(ProblemDetailsModel), 404)]
[ProducesResponseType(typeof(ProblemDetailsModel), 405)]
[ProducesResponseType(typeof(ProblemDetailsModel), 500)]
public abstract class BaseController : ControllerBase { }
