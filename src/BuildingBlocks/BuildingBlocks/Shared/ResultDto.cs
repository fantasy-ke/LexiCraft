using System.Text.Json.Serialization;

namespace BuildingBlocks.Model;

[Serializable]
public class ResponseBase
{
    /// <summary>
    ///     是否成功
    /// </summary>

    public bool Status { get; protected set; }

    /// <summary>
    ///     错误信息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     状态码
    /// </summary>
    public int StatusCode { get; set; }


    /// <summary>
    ///     扩展
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, object?> Extensions { get; set; } = new Dictionary<string, object?>();
}

[Serializable]
public class ResultDto<TResult> : ResponseBase
{
    public ResultDto(TResult result)
    {
        Data = result;
        Status = true;
        StatusCode = 200;
    }

    public ResultDto(string errorMessage = "", int code = 500)
    {
        Status = false;
        Message = errorMessage;
        StatusCode = code;
    }

    public ResultDto()
    {
    }

    public TResult Data { get; set; } = default!;

    public void SetExtensions(IDictionary<string, object?> extensions)
    {
        Extensions = extensions;
    }
}

[Serializable]
public class ResultDto : ResultDto<object?>
{
    public ResultDto(object? result) : base(result)
    {
    }

    public ResultDto(string errorMessage, int code = 500) : base(errorMessage, code)
    {
    }

    public ResultDto()
    {
    }

    public static ResultDto Sucess(object? result)
    {
        return new ResultDto(result);
    }

    public static ResultDto Fail(string errorMessage, int errorCode = 500)
    {
        return new ResultDto(errorMessage, errorCode);
    }

    public static ResultDto FailExt(string errorMessage, IDictionary<string, object?> extensions, int errorCode = 500)
    {
        var result = new ResultDto(errorMessage, errorCode);
        result.SetExtensions(extensions);
        return result;
    }
}