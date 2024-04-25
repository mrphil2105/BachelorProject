using Apachi.ViewModels.Submitter;
using FluentValidation;

namespace Apachi.ViewModels.Validation;

public class SubmitViewModelValidator : AbstractValidator<SubmitViewModel>
{
    public SubmitViewModelValidator()
    {
        RuleFor(model => model.PaperFilePath)
            .Must(path => File.Exists(path))
            .WithMessage("The specified file path must exist.");
    }
}
