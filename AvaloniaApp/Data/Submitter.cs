namespace Apachi.AvaloniaApp.Data;

public class Submitter
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public byte[] PasswordSalt { get; set; } = null!;

    public byte[] AuthenticationHash { get; set; } = null!;

    public List<Submission> Submissions { get; set; } = new List<Submission>();
}
