using FluentValidation;
using TaskManagement.Application.DTOs;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Validation;

/// <summary>
/// Валидатор для CreateTaskDto.
/// </summary>
public class CreateTaskValidator : AbstractValidator<CreateTaskDto>
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="CreateTaskValidator"/>.
    /// </summary>
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Заголовок обязателен.")
            .MaximumLength(200).WithMessage("Заголовок не должен превышать 200 символов.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Описание не должно превышать 2000 символов.");
    }
}

/// <summary>
/// Валидатор для UpdateTaskDto.
/// </summary>
public class UpdateTaskValidator : AbstractValidator<UpdateTaskDto>
{
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UpdateTaskValidator"/>.
    /// </summary>
    public UpdateTaskValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Заголовок не должен превышать 200 символов.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Описание не должно превышать 2000 символов.");

        RuleFor(x => x.Status)
            .Must(status => status is null || Enum.IsDefined(typeof(TaskStatus), status.Value))
            .WithMessage("Недопустимое значение статуса.");
    }
}
