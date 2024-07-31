namespace Apachi.UserApp.Data;

public abstract class User
{
    public Guid Id { get; set; }

    public required string Username { get; set; }

    public required byte[] PasswordSalt { get; set; }

    public required byte[] AuthenticationHash { get; set; }
}
