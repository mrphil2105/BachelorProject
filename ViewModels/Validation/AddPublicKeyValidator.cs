using Apachi.ViewModels.Dialogs;
using FluentValidation;

public class AddPublicKeyValidator : AbstractValidator<AddPublicKeyViewModel>
{
    public AddPublicKeyValidator()
    {
        RuleFor(model => model.Owner).NotEmpty().WithMessage("A key owner must be specified.");
        RuleFor(model => model.Name).NotEmpty().WithMessage("A key name must be specified.");
        RuleFor(model => model.PublicKeyPath)
            .Must(path => File.Exists(path))
            .WithMessage("The specified file does not exist.");
    }
}
