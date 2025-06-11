using Microsoft.AspNetCore.Mvc;

namespace MyWebApi.Models.Responses;

public class ProblemDetailsModel : ProblemDetails
{
	public long? Timestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	public string? RequestId { get; set; }
	public string? TraceId { get; set; }
}
