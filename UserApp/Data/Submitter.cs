namespace Apachi.UserApp.Data;

public class Submitter : User
{
    public List<Submission> Submissions { get; set; } = new List<Submission>();
}
