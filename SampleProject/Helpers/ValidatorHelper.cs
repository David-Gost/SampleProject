using System.ComponentModel.DataAnnotations;
using FluentValidation;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace SampleProject.Helpers;

/// <summary>
/// 資料驗證
/// </summary>
public class ValidatorHelper
{
    public static ValidationResult Validate<T>(T model, Action<AbstractValidator<T>> rules)
    {
        var validator = new InlineValidator<T>();
        rules(validator);
        return validator.Validate(model);
    }
}