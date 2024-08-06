using Apachi.ViewModels.Models;

namespace Apachi.ViewModels.Services;

public interface IDiscussionService
{
    Task<List<DiscussableSubmissionModel>> GetDiscussableSubmissionsAsync();

    Task DownloadPaperAsync(byte[] paperHash, string paperFilePath);

    Task SendMessageAsync(byte[] paperHash, string message);

    Task<List<DiscussMessageModel>> GetMessagesAsync(byte[] paperHash);

    Task SendGradeAsync(byte[] paperHash, int grade);
}
