using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;

namespace Apachi.UnitTests;

public class ArraySizeAttribute : CustomizeAttribute
{
    private readonly int _size;

    public ArraySizeAttribute(int size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Value cannot be smaller than 0.");
        }

        _size = size;
    }

    public override ICustomization GetCustomization(ParameterInfo parameter)
    {
        if (parameter is null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        if (!parameter.ParameterType.IsArray)
        {
            throw new ArgumentException("The parameter must be an array.", nameof(parameter));
        }

        var elementType = parameter.ParameterType.GetElementType()!;
        var customizationType = typeof(ArraySizeCustomization<>).MakeGenericType(elementType);
        var customization = (ICustomization)Activator.CreateInstance(customizationType, parameter, _size)!;
        return customization;
    }

    private class ArraySizeCustomization<T> : ICustomization
    {
        private readonly ParameterInfo _parameter;
        private readonly int _size;

        public ArraySizeCustomization(ParameterInfo parameter, int size)
        {
            _parameter = parameter;
            _size = size;
        }

        public void Customize(IFixture fixture)
        {
            var listBuilder = new FixedBuilder(fixture.CreateMany<T>(_size).ToArray());
            var filteringBuilder = new FilteringSpecimenBuilder(listBuilder, new EqualRequestSpecification(_parameter));
            fixture.Customizations.Add(filteringBuilder);
        }
    }
}
