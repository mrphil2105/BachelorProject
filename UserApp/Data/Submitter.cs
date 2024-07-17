namespace Apachi.AvaloniaApp.Data;

public class Submitter : User
{
    public List<Submission> Submissions { get; set; } = new List<Submission>();
}
