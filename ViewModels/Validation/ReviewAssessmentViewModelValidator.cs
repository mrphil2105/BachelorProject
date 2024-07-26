using Apachi.ViewModels.Reviewer;
using FluentValidation;

namespace Apachi.ViewModels.Validation;

public class ReviewAssessmentViewModelValidator : AbstractValidator<ReviewAssessmentViewModel>
{
    public ReviewAssessmentViewModelValidator()
    {
        RuleFor(model => model.Review)
            .NotEmpty()
            .WithMessage("The review field is required.")
            .MaximumLength(10000)
            .WithMessage("The review must not be longer than 10000 characters.");
    }
}
