using FluentValidation;

namespace Apachi.ViewModels.Validation
{
    public class RegisterViewModelValidator : AbstractValidator<RegisterViewModel>
    {
        public RegisterViewModelValidator()
        {
            RuleFor(model => model.Username)
                .Length(1, 20)
                .WithMessage("Username must be between 1 and 20 characters.")
                .Matches(@"^[a-z\d]*$")
                .WithMessage("Username must only contain lowercase letters and numbers.");
            RuleFor(model => model.Password).NotEmpty().WithMessage("The password field is required.");
            RuleFor(model => model.PasswordConfirmation)
                .Must((model, confirmation) => confirmation == model.Password)
                .WithMessage("Confirm Password must equal Password.");
        }
    }
}
