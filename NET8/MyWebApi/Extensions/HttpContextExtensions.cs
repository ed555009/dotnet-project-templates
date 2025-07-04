using System.Net;

namespace MyWebApi.Extensions;

public static class HttpContextExtensions
{
	public static IPAddress ClientIP(this HttpContext context)
	{
		var hasCfHeader = context.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfip);
		var hasXForwardedForHeader = context.Request.Headers.TryGetValue("X-Forwarded-For", out var xffip);
		string? header = null;

		if (hasCfHeader)
			header = cfip.FirstOrDefault();
		else if (hasXForwardedForHeader)
			header = xffip.FirstOrDefault();

		if (IPAddress.TryParse(header ?? string.Empty, out IPAddress? ip))
			return ip;

		ip = context.Connection.RemoteIpAddress;

		if (ip != null && ip.IsIPv4MappedToIPv6)
			return ip.MapToIPv4();

		return ip ?? IPAddress.None;
	}
}
