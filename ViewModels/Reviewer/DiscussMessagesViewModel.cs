using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;
using Apachi.ViewModels.Validation;

namespace Apachi.ViewModels.Reviewer;

public class DiscussMessagesViewModel : Conductor<DiscussMessageModel>.Collection.AllActive
{
    private readonly IViewService _viewService;
    private readonly IDiscussionService _discussionService;

    private DiscussableSubmissionModel? _model;
    private bool _isLoading;
    private string _message = string.Empty;

    public DiscussMessagesViewModel(IViewService viewService, IDiscussionService discussionService)
    {
        _viewService = viewService;
        _discussionService = discussionService;
    }

    public DiscussableSubmissionModel? Model
    {
        get => _model;
        set => Set(ref _model, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    public string Message
    {
        get => _message;
        set => Set(ref _message, value);
    }

    public async Task RefreshMessages()
    {
        try
        {
            IsLoading = true;
            var messageModels = await _discussionService.GetMessagesAsync(Model!.PaperHash);
            Items.Clear();
            Items.AddRange(messageModels);
        }
        catch (Exception exception)
        {
            await _viewService.ShowMessageBoxAsync(
                $"Unable to refresh messages: {exception.Message}",
                "Refresh Failure",
                kind: MessageBoxKind.Error
            );
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SendMessage()
    {
        var message = Message.Trim();

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        try
        {
            await _discussionService.SendMessageAsync(Model!.PaperHash, message);
            Message = string.Empty;
            await RefreshMessages();
        }
        catch (Exception exception)
        {
            await _viewService.ShowMessageBoxAsync(
                this,
                $"Unable to send message: {exception.Message}",
                "Send Failure",
                kind: MessageBoxKind.Error
            );
        }
    }

    public Task Back()
    {
        return ((DiscussViewModel)Parent!).GoToList();
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        return RefreshMessages();
    }
}
