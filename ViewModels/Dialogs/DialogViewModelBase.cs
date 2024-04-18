using System.ComponentModel;
using FluentValidation;

public abstract class DialogViewModelBase : Screen
{
    protected DialogViewModelBase(IValidator? validator = null)
    {
        if (validator == null)
        {
            return;
        }

        var validatorType = validator.GetType();
        var modelType = validatorType.BaseType!.GetGenericArguments()[0];
        var adapterType = typeof(ValidationAdapter<>).MakeGenericType(modelType);
        Validator = (IModelValidator)Activator.CreateInstance(adapterType, validator)!;
    }

    public bool CanSubmit => !HasErrors;

    public async Task Submit()
    {
        await TryCloseAsync(true);
    }

    public async Task Cancel()
    {
        await TryCloseAsync(false);
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        return ValidateAsync(cancellationToken);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(HasErrors))
        {
            RaisePropertyChanged(nameof(CanSubmit));
        }
    }
}
