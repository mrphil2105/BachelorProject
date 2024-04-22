using FluentValidation;

namespace Apachi.ViewModels.Validation
{
    public class ValidationAdapter<T> : IModelValidator<T>
    {
        private readonly IValidator<T> _validator;
        private T? _model;

        public ValidationAdapter(IValidator<T> validator)
        {
            _validator = validator;
        }

        public void Initialize(object model)
        {
            _model = (T)model;
        }

        public async Task<IReadOnlyDictionary<string, IEnumerable<string>>?> ValidateAsync(
            CancellationToken cancellationToken = default
        )
        {
            if (_model == null)
            {
                throw new InvalidOperationException($"{nameof(Initialize)} must be called before validating.");
            }

            var validationResult = await _validator.ValidateAsync(_model, cancellationToken);
            var errorMessages = validationResult
                .Errors.GroupBy(failure => failure.PropertyName)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.Select(failure => failure.ErrorMessage));
            return errorMessages;
        }

        public async Task<IEnumerable<string>?> ValidatePropertyAsync(
            string? propertyName,
            CancellationToken cancellationToken = default
        )
        {
            if (_model == null)
            {
                throw new InvalidOperationException($"{nameof(Initialize)} must be called before validating.");
            }

            var validationResult = await _validator.ValidateAsync(
                _model,
                options => options.IncludeProperties(propertyName),
                cancellationToken
            );
            var errorMessages = validationResult.Errors.Select(failure => failure.ErrorMessage);
            return errorMessages;
        }
    }
}
