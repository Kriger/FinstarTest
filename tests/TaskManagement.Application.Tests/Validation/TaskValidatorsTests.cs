using FluentAssertions;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Validation;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;
using Xunit;

namespace TaskManagement.Application.Tests.Validation;

/// <summary>
/// Модульные тесты для FluentValidation валидаторов.
/// </summary>
public class TaskValidatorsTests
{
    private readonly CreateTaskValidator _createValidator;
    private readonly UpdateTaskValidator _updateValidator;

    public TaskValidatorsTests()
    {
        _createValidator = new CreateTaskValidator();
        _updateValidator = new UpdateTaskValidator();
    }

    #region CreateTaskValidator

    /// <summary>
    /// Проверяет, что CreateTaskValidator не возвращает ошибок для валидных данных.
    /// </summary>
    [Fact]
    public void CreateValidator_ShouldHaveNoErrors_WhenValid()
    {
        // Arrange
        var dto = new CreateTaskDto
        {
            Title = "Валидная задача",
            Description = "Валидное описание"
        };

        // Act
        var result = _createValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что CreateTaskValidator возвращает ошибку при пустом заголовке.
    /// </summary>
    [Fact]
    public void CreateValidator_ShouldFail_WhenTitleIsEmpty()
    {
        // Arrange
        var dto = new CreateTaskDto
        {
            Title = string.Empty
        };

        // Act
        var result = _createValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
    }

    /// <summary>
    /// Проверяет, что CreateTaskValidator возвращает ошибку при null-заголовке.
    /// </summary>
    [Fact]
    public void CreateValidator_ShouldFail_WhenTitleIsNull()
    {
        // Arrange
        var dto = new CreateTaskDto
        {
            Title = null!
        };

        // Act
        var result = _createValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    /// <summary>
    /// Проверяет, что CreateTaskValidator возвращает ошибку при слишком длинном заголовке.
    /// </summary>
    [Fact]
    public void CreateValidator_ShouldFail_WhenTitleTooLong()
    {
        // Arrange
        var dto = new CreateTaskDto
        {
            Title = new string('a', 201)
        };

        // Act
        var result = _createValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    /// <summary>
    /// Проверяет, что CreateTaskValidator возвращает ошибку при слишком длинном описании.
    /// </summary>
    [Fact]
    public void CreateValidator_ShouldPass_WhenDescriptionTooLong()
    {
        // Arrange
        var dto = new CreateTaskDto
        {
            Title = "Валидный",
            Description = new string('a', 2001)
        };

        // Act
        var result = _createValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    /// <summary>
    /// Проверяет, что CreateTaskValidator не возвращает ошибок для валидного описания.
    /// </summary>
    [Fact]
    public void CreateValidator_ShouldPass_WhenDescriptionValid()
    {
        // Arrange
        var dto = new CreateTaskDto
        {
            Title = "Валидный",
            Description = new string('a', 2000)
        };

        // Act
        var result = _createValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region UpdateTaskValidator

    /// <summary>
    /// Проверяет, что UpdateTaskValidator не возвращает ошибок при пустом DTO.
    /// </summary>
    [Fact]
    public void UpdateValidator_ShouldHaveNoErrors_WhenAllEmpty()
    {
        // Arrange
        var dto = new UpdateTaskDto();

        // Act
        var result = _updateValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что UpdateTaskValidator возвращает ошибку при слишком длинном заголовке.
    /// </summary>
    [Fact]
    public void UpdateValidator_ShouldPass_WhenTitleTooLong()
    {
        // Arrange
        var dto = new UpdateTaskDto
        {
            Title = new string('a', 201)
        };

        // Act
        var result = _updateValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    /// <summary>
    /// Проверяет, что UpdateTaskValidator возвращает ошибку при слишком длинном описании.
    /// </summary>
    [Fact]
    public void UpdateValidator_ShouldPass_WhenDescriptionTooLong()
    {
        // Arrange
        var dto = new UpdateTaskDto
        {
            Description = new string('a', 2001)
        };

        // Act
        var result = _updateValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    /// <summary>
    /// Проверяет, что UpdateTaskValidator не возвращает ошибок при null-статусе.
    /// </summary>
    [Fact]
    public void UpdateValidator_ShouldPass_WhenStatusNull()
    {
        // Arrange
        var dto = new UpdateTaskDto
        {
            Status = null
        };

        // Act
        var result = _updateValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Проверяет, что UpdateTaskValidator не возвращает ошибок при валидном статусе.
    /// </summary>
    [Fact]
    public void UpdateValidator_ShouldPass_WhenStatusValid()
    {
        // Arrange
        var dto = new UpdateTaskDto
        {
            Status = TaskStatus.InProgress
        };

        // Act
        var result = _updateValidator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
