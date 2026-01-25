namespace BuildingBlocks.OSS.Models.Exceptions;

public class BucketExistException : Exception
{
    public BucketExistException()
    {
    }

    public BucketExistException(string message) : base(message)
    {
    }

    public BucketExistException(string message, Exception innerException) : base(message, innerException)
    {
    }
}