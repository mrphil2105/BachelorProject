using Apachi.Shared.Crypt;
using Apachi.ViewModels.Dialogs;

namespace Apachi.ViewModels;

public class MainViewModel
{
    private readonly IViewService _viewService;
    private readonly PublicKeyStore _keyStore;
    private readonly Func<AddPublicKeyViewModel> _addKeyViewModelFactory;

    public MainViewModel(
        IViewService viewService,
        PublicKeyStore keyStore,
        Func<AddPublicKeyViewModel> addKeyViewModelFactory
    )
    {
        _viewService = viewService;
        _keyStore = keyStore;
        _addKeyViewModelFactory = addKeyViewModelFactory;
    }

    public async Task AddPublicKey()
    {
        var addKeyViewModel = _addKeyViewModelFactory();
        var dialogResult = await _viewService.ShowDialogAsync(this, addKeyViewModel);

        if (dialogResult.GetValueOrDefault())
        {
            var keyBytes = await ReadKeyBytesAsync(addKeyViewModel.PublicKeyPath!);

            if (!keyBytes.HasValue)
            {
                return;
            }

            await _keyStore.AddPublicKeyAsync(addKeyViewModel.Owner!, addKeyViewModel.Name!, keyBytes.Value);
        }
    }

    private async Task<ReadOnlyMemory<byte>?> ReadKeyBytesAsync(string publicKeyPath)
    {
        try
        {
            var keyBytes = await File.ReadAllBytesAsync(publicKeyPath);
            return keyBytes;
        }
        catch (Exception exception)
        {
            await _viewService.ShowMessageBoxAsync(
                this,
                $"Unable to read public key: {exception.Message}",
                "Public Key Failure",
                kind: MessageBoxKind.Error
            );
            return null;
        }
    }
}
