namespace LexiCraft.Infrastructure.Model;

[Serializable]
public class ResponseBase
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; protected set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 状态码
    /// </summary>
    public int Status { get; set; }
}

[Serializable]
public class ResultDto<TResult> : ResponseBase
{
    public TResult Data { get; set; }

    public ResultDto(TResult result)
    {
        Data = result;
        IsSuccess = true;
        Status = 200;
    }

    public ResultDto(string errorMessage = "", int code = 500)
    {
        IsSuccess = false;
        Message = errorMessage;
        Status = code;
    }

    public ResultDto()
    {
    }
}

[Serializable]
public class ResultDto : ResultDto<object>
{
    public ResultDto(object result) : base(result)
    {
    }

    public ResultDto(string errorMessage, int code = 500) : base(errorMessage,code)
    {
    }

    public ResultDto() : base()
    {
    }

    public static ResultDto Sucess(object result)
    {
        return new ResultDto(result);
    }

    public static ResultDto Fail(string errorMessage,int errorCode = 500)
    {
        return new ResultDto(errorMessage,errorCode);
    }
}