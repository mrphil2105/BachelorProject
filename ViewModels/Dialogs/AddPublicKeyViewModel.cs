namespace Apachi.ViewModels.Dialogs;

public class AddPublicKeyViewModel : DialogViewModelBase
{
    private readonly IViewService _viewService;
    private string? _owner;
    private string? _name;
    private string? _publicKeyPath;

    public AddPublicKeyViewModel(IViewService viewService)
        : base(new AddPublicKeyValidator())
    {
        _viewService = viewService;
    }

    public string? Owner
    {
        get => _owner;
        set => Set(ref _owner, value);
    }

    public string? Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    public string? PublicKeyPath
    {
        get => _publicKeyPath;
        set => Set(ref _publicKeyPath, value);
    }

    public async Task BrowseFile()
    {
        var filePaths = await _viewService.ShowOpenFileDialogAsync(this);
        PublicKeyPath = filePaths?.FirstOrDefault();
    }
}
