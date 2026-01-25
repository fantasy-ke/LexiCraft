namespace BuildingBlocks.OSS.Models.Dto;

public class ObjectOutPut
{
    public ObjectOutPut()
    {
    }

    public ObjectOutPut(string name, Stream stream, string contentType)
    {
        Name = name;
        Stream = stream;
        ContentType = contentType;
    }

    /// <summary>
    ///     文件名--对象名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     文件流
    /// </summary>
    public Stream Stream { get; set; } = Stream.Null;

    /// <summary>
    ///     上传文件格式
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}