using Apachi.ViewModels.Reviewer;
using FluentValidation;

namespace Apachi.ViewModels.Validation;

public class ReviewAssessmentViewModelValidator : AbstractValidator<ReviewAssessmentViewModel>
{
    public ReviewAssessmentViewModelValidator()
    {
        RuleFor(model => model.Assessment)
            .NotEmpty()
            .WithMessage("The assessment field is required.")
            .MaximumLength(10000)
            .WithMessage("The assessment must not be longer than 10000 characters.");
    }
}
