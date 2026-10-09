using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TaskManagement.Api.Filters;

/// <summary>
/// Action filter для автоматической валидации DTO через FluentValidation.
/// Использует первый аргумент действия как объект для валидации.
/// </summary>
public class ValidationActionFilter<TRequest> : IAsyncActionFilter
    where TRequest : class
{
    private readonly IValidator<TRequest> _validator;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ValidationActionFilter{TRequest}"/>.
    /// </summary>
    /// <param name="validator">Валидатор для запроса.</param>
    public ValidationActionFilter(IValidator<TRequest> validator)
    {
        _validator = validator;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Берём первый аргумент действия (независимо от имени)
        var dto = context.ActionArguments.Values.FirstOrDefault() as TRequest;

        if (dto is null)
        {
            await next();
            return;
        }

        var validationResult = await _validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key ?? "(неизвестное поле)",
                    g => g.Select(e => e.ErrorMessage).ToArray());

            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors));
            return;
        }

        await next();
    }
}
