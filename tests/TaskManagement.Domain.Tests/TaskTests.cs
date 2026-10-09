using FluentAssertions;
using DomainTask = TaskManagement.Domain.Models.Task;
using DomainStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Domain.Tests;

/// <summary>
/// Unit-тесты для доменной модели Task.
/// </summary>
public class TaskTests
{
    /// <summary>
    /// Проверяет корректную инициализацию свойств задачи в конструкторе.
    /// </summary>
    [Fact]
    public void Constructor_ShouldCreateTaskWithCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var title = "Test Title";
        var description = "Test Description";

        // Act
        var task = new DomainTask(title, description, userId);

        // Assert
        task.Id.Should().NotBeEmpty();
        task.Title.Should().Be(title);
        task.Description.Should().Be(description);
        task.Status.Should().Be(DomainStatus.New);
        task.CreatedBy.Should().Be(userId);
        task.UpdatedBy.Should().Be(userId);
        task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        task.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        task.IsDeleted.Should().BeFalse();
        task.DeletedAt.Should().BeNull();
        task.RowVersion.Should().NotBeEmpty();
    }

    /// <summary>
    /// Проверяет, что конструктор выбрасывает исключение при null-заголовке.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenTitleIsNull()
    {
        // Act & Assert
        Action act = () => new DomainTask(null!, "Description", Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("Заголовок не может быть пустым.*");
    }

    /// <summary>
    /// Проверяет, что конструктор выбрасывает исключение при пустом заголовке.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenTitleIsEmpty()
    {
        // Act & Assert
        Action act = () => new DomainTask(string.Empty, "Description", Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("Заголовок не может быть пустым.*");
    }

    /// <summary>
    /// Проверяет, что конструктор выбрасывает исключение при заголовке из пробелов.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenTitleIsWhiteSpace()
    {
        // Act & Assert
        Action act = () => new DomainTask("   ", "Description", Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("Заголовок не может быть пустым.*");
    }

    /// <summary>
    /// Проверяет, что конструктор выбрасывает исключение при превышении максимальной длины заголовка.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenTitleExceedsMaxLength()
    {
        // Arrange
        var longTitle = new string('a', 201);

        // Act & Assert
        Action act = () => new DomainTask(longTitle, "Description", Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("Заголовок не должен превышать 200 символов.*");
    }

    /// <summary>
    /// Проверяет, что конструктор выбрасывает исключение при превышении максимальной длины описания.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var longDescription = new string('a', 2001);

        // Act & Assert
        Action act = () => new DomainTask("Title", longDescription, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("Описание не должно превышать 2000 символов.*");
    }

    /// <summary>
    /// Проверяет, что конструктор принимает заголовок максимальной длины.
    /// </summary>
    [Fact]
    public void Constructor_ShouldAllow_MaxLengthTitle()
    {
        // Arrange
        var title = new string('a', 200);

        // Act
        Action act = () => new DomainTask(title, "Description", Guid.NewGuid());

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что конструктор принимает описание максимальной длины.
    /// </summary>
    [Fact]
    public void Constructor_ShouldAllow_MaxLengthDescription()
    {
        // Arrange
        var description = new string('a', 2000);

        // Act
        Action act = () => new DomainTask("Title", description, Guid.NewGuid());

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что метод Create создаёт задачу со значениями по умолчанию.
    /// </summary>
    [Fact]
    public void Create_ShouldCreateTaskWithDefaults()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var task = DomainTask.Create(userId);

        // Assert
        task.Title.Should().Be("Default Task");
        task.Description.Should().BeEmpty();
        task.Status.Should().Be(DomainStatus.New);
    }

    /// <summary>
    /// Проверяет, что метод Create создаёт задачу с пользовательским заголовком.
    /// </summary>
    [Fact]
    public void Create_ShouldCreateTaskWithCustomTitle()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var task = DomainTask.Create(userId, title: "Custom Title");

        // Assert
        task.Title.Should().Be("Custom Title");
    }

    /// <summary>
    /// Проверяет, что метод Create создаёт задачу с пользовательским описанием.
    /// </summary>
    [Fact]
    public void Create_ShouldCreateTaskWithCustomDescription()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var task = DomainTask.Create(userId, description: "Custom Description");

        // Assert
        task.Description.Should().Be("Custom Description");
    }

    /// <summary>
    /// Проверяет, что метод UpdateTitle обновляет заголовок и временную метку.
    /// </summary>
    [Fact]
    public void UpdateTitle_ShouldUpdateTitleAndTimestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Old Title", "Description", userId);
        var oldUpdatedAt = task.UpdatedAt;

        Thread.Sleep(10); // Ensure timestamp changes

        // Act
        task.UpdateTitle("New Title", userId);

        // Assert
        task.Title.Should().Be("New Title");
        task.UpdatedAt.Should().BeAfter(oldUpdatedAt);
        task.UpdatedBy.Should().Be(userId);
        task.RowVersion.Should().NotBeEmpty();
    }

    /// <summary>
    /// Проверяет, что метод UpdateTitle выбрасывает исключение при null-заголовке.
    /// </summary>
    [Fact]
    public void UpdateTitle_ShouldThrow_WhenTitleIsNull()
    {
        // Arrange
        var task = new DomainTask("Title", "Description", Guid.NewGuid());

        // Act & Assert
        Action act = () => task.UpdateTitle(null!, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("Заголовок не может быть пустым.*");
    }

    /// <summary>
    /// Проверяет, что метод UpdateTitle выбрасывает исключение при превышении максимальной длины заголовка.
    /// </summary>
    [Fact]
    public void UpdateTitle_ShouldThrow_WhenTitleExceedsMaxLength()
    {
        // Arrange
        var task = new DomainTask("Title", "Description", Guid.NewGuid());
        var longTitle = new string('a', 201);

        // Act & Assert
        Action act = () => task.UpdateTitle(longTitle, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("Заголовок не должен превышать 200 символов.*");
    }

    /// <summary>
    /// Проверяет, что метод UpdateDescription обновляет описание и временную метку.
    /// </summary>
    [Fact]
    public void UpdateDescription_ShouldUpdateDescriptionAndTimestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Old Description", userId);
        var oldUpdatedAt = task.UpdatedAt;

        Thread.Sleep(10);

        // Act
        task.UpdateDescription("New Description", userId);

        // Assert
        task.Description.Should().Be("New Description");
        task.UpdatedAt.Should().BeAfter(oldUpdatedAt);
        task.UpdatedBy.Should().Be(userId);
    }

    /// <summary>
    /// Проверяет, что метод UpdateDescription выбрасывает исключение при null-описании.
    /// </summary>
    [Fact]
    public void UpdateDescription_ShouldThrow_WhenDescriptionIsNull()
    {
        // Arrange
        var task = new DomainTask("Title", "Description", Guid.NewGuid());

        // Act & Assert
        Action act = () => task.UpdateDescription(null!, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("Описание не может быть пустым.*");
    }

    /// <summary>
    /// Проверяет, что метод UpdateDescription выбрасывает исключение при превышении максимальной длины описания.
    /// </summary>
    [Fact]
    public void UpdateDescription_ShouldThrow_WhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var task = new DomainTask("Title", "Description", Guid.NewGuid());
        var longDescription = new string('a', 2001);

        // Act & Assert
        Action act = () => task.UpdateDescription(longDescription, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("Описание не должно превышать 2000 символов.*");
    }

    /// <summary>
    /// Проверяет переход статуса задачи из New в InProgress.
    /// </summary>
    [Fact]
    public void UpdateStatus_ShouldChangeStatusFromNewToInProgress()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);

        // Act
        task.UpdateStatus(DomainStatus.InProgress, userId);

        // Assert
        task.Status.Should().Be(DomainStatus.InProgress);
    }

    /// <summary>
    /// Проверяет переход статуса задачи из InProgress в Completed.
    /// </summary>
    [Fact]
    public void UpdateStatus_ShouldChangeStatusFromInProgressToCompleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);
        task.UpdateStatus(DomainStatus.InProgress, userId);

        // Act
        task.UpdateStatus(DomainStatus.Completed, userId);

        // Assert
        task.Status.Should().Be(DomainStatus.Completed);
    }

    /// <summary>
    /// Проверяет, что метод UpdateStatus выбрасывает исключение при переходе из New в Completed.
    /// </summary>
    [Fact]
    public void UpdateStatus_ShouldThrow_WhenTransitioningFromNewToCompleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);

        // Act & Assert
        Action act = () => task.UpdateStatus(DomainStatus.Completed, userId);
        act.Should().Throw<InvalidOperationException>().WithMessage("Задачу можно перевести только в статус InProgress.*");
    }

    /// <summary>
    /// Проверяет, что метод UpdateStatus выбрасывает исключение при переходе из InProgress в New.
    /// </summary>
    [Fact]
    public void UpdateStatus_ShouldThrow_WhenTransitioningFromInProgressToNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);
        task.UpdateStatus(DomainStatus.InProgress, userId);

        // Act & Assert
        Action act = () => task.UpdateStatus(DomainStatus.New, userId);
        act.Should().Throw<InvalidOperationException>().WithMessage("Задачу из статуса InProgress можно перевести только в Completed.*");
    }

    /// <summary>
    /// Проверяет, что метод UpdateStatus выбрасывает исключение при изменении статуса Completed.
    /// </summary>
    [Fact]
    public void UpdateStatus_ShouldThrow_WhenChangingCompletedStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);
        task.UpdateStatus(DomainStatus.InProgress, userId);
        task.UpdateStatus(DomainStatus.Completed, userId);

        // Act & Assert
        Action act = () => task.UpdateStatus(DomainStatus.New, userId);
        act.Should().Throw<InvalidOperationException>().WithMessage("Нельзя изменить статус завершённой задачи.*");
    }

    /// <summary>
    /// Проверяет, что метод UpdateStatus обновляет временную метку и пользователя.
    /// </summary>
    [Fact]
    public void UpdateStatus_ShouldUpdateTimestampAndUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);
        var oldUpdatedAt = task.UpdatedAt;

        Thread.Sleep(10);

        // Act
        task.UpdateStatus(DomainStatus.InProgress, userId);

        // Assert
        task.UpdatedAt.Should().BeAfter(oldUpdatedAt);
        task.UpdatedBy.Should().Be(userId);
    }

    /// <summary>
    /// Проверяет, что метод ApplyUpdates возвращает true при изменении заголовка.
    /// </summary>
    [Fact]
    public void ApplyUpdates_ShouldReturnTrue_WhenTitleChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Old Title", "Description", userId);

        // Act
        var result = task.ApplyUpdates("New Title", null, null, userId);

        // Assert
        result.Should().BeTrue();
        task.Title.Should().Be("New Title");
    }

    /// <summary>
    /// Проверяет, что метод ApplyUpdates возвращает true при изменении описания.
    /// </summary>
    [Fact]
    public void ApplyUpdates_ShouldReturnTrue_WhenDescriptionChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Old Description", userId);

        // Act
        var result = task.ApplyUpdates(null, "New Description", null, userId);

        // Assert
        result.Should().BeTrue();
        task.Description.Should().Be("New Description");
    }

    /// <summary>
    /// Проверяет, что метод ApplyUpdates возвращает true при изменении статуса.
    /// </summary>
    [Fact]
    public void ApplyUpdates_ShouldReturnTrue_WhenStatusChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);

        // Act
        var result = task.ApplyUpdates(null, null, DomainStatus.InProgress, userId);

        // Assert
        result.Should().BeTrue();
        task.Status.Should().Be(DomainStatus.InProgress);
    }

    /// <summary>
    /// Проверяет, что метод ApplyUpdates возвращает true при изменении всех полей.
    /// </summary>
    [Fact]
    public void ApplyUpdates_ShouldReturnTrue_WhenAllFieldsChange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Old Title", "Old Description", userId);
        task.UpdateStatus(DomainStatus.InProgress, userId);

        // Act
        var result = task.ApplyUpdates("New Title", "New Description", DomainStatus.Completed, userId);

        // Assert
        result.Should().BeTrue();
        task.Title.Should().Be("New Title");
        task.Description.Should().Be("New Description");
        task.Status.Should().Be(DomainStatus.Completed);
    }

    /// <summary>
    /// Проверяет, что метод ApplyUpdates возвращает false при null-значениях.
    /// </summary>
    [Fact]
    public void ApplyUpdates_ShouldReturnFalse_WhenAllFieldsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);
        var originalTitle = task.Title;
        var originalDescription = task.Description;
        var originalStatus = task.Status;
        var originalUpdatedAt = task.UpdatedAt;

        // Act
        var result = task.ApplyUpdates(null, null, null, userId);

        // Assert
        result.Should().BeFalse();
        task.Title.Should().Be(originalTitle);
        task.Description.Should().Be(originalDescription);
        task.Status.Should().Be(originalStatus);
        task.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    /// <summary>
    /// Проверяет, что метод Delete помечает задачу как удалённую.
    /// </summary>
    [Fact]
    public void Delete_ShouldMarkTaskAsDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);

        // Act
        task.Delete(userId);

        // Assert
        task.IsDeleted.Should().BeTrue();
        task.DeletedAt.Should().NotBeNull();
        task.UpdatedBy.Should().Be(userId);
    }

    /// <summary>
    /// Проверяет, что метод Delete выбрасывает исключение при повторном удалении.
    /// </summary>
    [Fact]
    public void Delete_ShouldThrow_WhenTaskAlreadyDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);
        task.Delete(userId);

        // Act & Assert
        Action act = () => task.Delete(userId);
        act.Should().Throw<InvalidOperationException>().WithMessage("Задача уже удалена.*");
    }

    /// <summary>
    /// Проверяет, что метод Delete обновляет временную метку.
    /// </summary>
    [Fact]
    public void Delete_ShouldUpdateTimestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var task = new DomainTask("Title", "Description", userId);
        var originalUpdatedAt = task.UpdatedAt;

        Thread.Sleep(10);

        // Act
        task.Delete(userId);

        // Assert
        task.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
}
