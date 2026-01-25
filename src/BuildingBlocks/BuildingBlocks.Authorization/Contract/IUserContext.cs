namespace BuildingBlocks.Authentication.Contract;

public interface IUserContext
{
    public Guid UserId { get; }

    public string UserName { get; }

    public string UserAccount { get; }

    bool IsAuthenticated { get; }

    string[] Roles { get; }
}