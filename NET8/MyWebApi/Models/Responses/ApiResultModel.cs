using System.Net;

namespace MyWebApi.Models.Responses;

public class ApiResultModel<T>
{
	/// <summary>
	/// 執行是否成功
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// 錯誤訊息，當Success為false時有值
	/// </summary>
	public string? Message { get; set; }

	/// <summary>
	/// 回傳內容，可能為Null（執行失敗或無返回資料）
	/// </summary>
	public T? Data { get; set; }

	/// <summary>
	/// 時間戳（Millisecond）
	/// </summary>
	public long Timestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	/// <summary>
	/// 請求ID，用於追蹤請求
	/// </summary>
	public string? RequestId { get; set; }

	/// <summary>
	/// 追蹤ID
	/// </summary>
	public string? TraceId { get; set; }
}
