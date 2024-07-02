using Apachi.ViewModels.Submitter;
using FluentValidation;

namespace Apachi.ViewModels.Validation;

public class SubmitViewModelValidator : AbstractValidator<SubmitViewModel>
{
    public SubmitViewModelValidator()
    {
        RuleFor(model => model.Title)
            .NotEmpty()
            .WithMessage("The title field is required.")
            .MaximumLength(100)
            .WithMessage("The title must not be longer than 100 characters.");
        RuleFor(model => model.Description)
            .NotEmpty()
            .WithMessage("The description field is required.")
            .MaximumLength(1000)
            .WithMessage("The description must not be longer than 1000 characters.");
        RuleFor(model => model.PaperFilePath)
            .Must(path => File.Exists(path))
            .WithMessage("The specified file path must exist.");
    }
}
