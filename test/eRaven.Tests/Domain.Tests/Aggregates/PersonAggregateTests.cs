//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonAggregate
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Enums;
using eRaven.Domain.Events;
using eRaven.Domain.Exceptions;
using eRaven.Domain.Services;
using eRaven.Domain.ValueObjects;
using Moq;

namespace eRaven.Tests.Domain.Tests.Aggregates;

public class PersonAggregateTests
{
    private readonly Mock<IStatusTransitionValidator> _transitionValidator;
    private readonly Mock<IPositionAssignmentPolicy> _assignmentPolicy;

    public PersonAggregateTests()
    {
        _transitionValidator = new Mock<IStatusTransitionValidator>();
        _assignmentPolicy = new Mock<IPositionAssignmentPolicy>();

        // Дозволяємо початковий статус "В районі" (id=1)
        _transitionValidator
            .Setup(v => v.IsValidInitialStatus(1))
            .Returns(true);
    }

    [Fact]
    public void Create_ShouldSucceed_WhenValidData()
    {
        // Arrange
        var personalInfo = new PersonalInfo("1234567890", "Іванов", "Іван", "Іванович");
        var militaryDetails = new MilitaryDetails("Майор", "123456");

        // Act
        var person = PersonAggregate.Create(
            personalInfo,
            militaryDetails,
            initialStatusKindId: 1,
            _transitionValidator.Object
        );

        // Assert
        Assert.NotEqual(Guid.Empty, person.Id);
        Assert.Equal("Іванов Іван Іванович", person.PersonalInfo.FullName);
        Assert.Equal(1, person.StatusKindId);
        Assert.Single(person.StatusHistory);
        Assert.Single(person.DomainEvents);
        Assert.IsType<PersonCreatedEvent>(person.DomainEvents[0]);
    }

    [Fact]
    public void Create_ShouldThrowDomainException_WhenInvalidInitialStatus()
    {
        // Arrange
        var personalInfo = new PersonalInfo("1234567890", "Іванов", "Іван");
        var militaryDetails = new MilitaryDetails("Майор", "123456");

        _transitionValidator
            .Setup(v => v.IsValidInitialStatus(99))
            .Returns(false);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            PersonAggregate.Create(
                personalInfo,
                militaryDetails,
                initialStatusKindId: 99,
                _transitionValidator.Object
            )
        );

        Assert.Contains("Початковим статусом", ex.Message);
    }

    [Fact]
    public void ChangeStatus_ShouldSucceed_WhenTransitionAllowed()
    {
        // Arrange
        var person = CreateTestPerson();
        var newStatusId = 7; // "Відрядження"
        var effectiveAt = DateTime.UtcNow.AddDays(1);

        _transitionValidator
            .Setup(v => v.IsTransitionAllowed(1, 7))
            .Returns(true);

        // Act
        person.ChangeStatus(
            newStatusKindId: newStatusId,
            effectiveAtUtc: effectiveAt,
            transitionValidator: _transitionValidator.Object,
            note: "Відрядження до Києва"
        );

        // Assert
        Assert.Equal(newStatusId, person.StatusKindId);
        Assert.Equal(2, person.StatusHistory.Count);
        Assert.Contains(person.DomainEvents, e => e is PersonStatusChangedEvent);
    }

    [Fact]
    public void ChangeStatus_ShouldThrowDomainException_WhenTransitionForbidden()
    {
        // Arrange
        var person = CreateTestPerson();

        _transitionValidator
            .Setup(v => v.IsTransitionAllowed(1, 99))
            .Returns(false);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            person.ChangeStatus(99, DateTime.UtcNow, _transitionValidator.Object)
        );

        Assert.Contains("Перехід", ex.Message);
    }

    [Fact]
    public void ChangeStatus_ShouldThrowDomainException_WhenDateInPast()
    {
        // Arrange
        var person = CreateTestPerson();
        var pastDate = DateTime.UtcNow.AddDays(-1);

        _transitionValidator
            .Setup(v => v.IsTransitionAllowed(It.IsAny<int?>(), It.IsAny<int>()))
            .Returns(true);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            person.ChangeStatus(7, pastDate, _transitionValidator.Object)
        );

        Assert.Contains("пізніший", ex.Message);
    }

    [Fact]
    public void AssignToPosition_ShouldSucceed_WhenPolicyAllows()
    {
        // Arrange
        var person = CreateTestPerson();
        var positionId = Guid.NewGuid();
        var assignDate = DateTime.UtcNow;

        _assignmentPolicy
            .Setup(p => p.CanAssignToPosition(positionId))
            .Returns(true);

        // Act
        person.AssignToPosition(positionId, assignDate, _assignmentPolicy.Object);

        // Assert
        Assert.Equal(positionId, person.PositionUnitId);
        Assert.Single(person.PositionAssignments);
        Assert.Contains(person.DomainEvents, e => e is PersonAssignedToPositionEvent);
    }

    [Fact]
    public void AssignToPosition_ShouldThrowDomainException_WhenPolicyForbids()
    {
        // Arrange
        var person = CreateTestPerson();
        var positionId = Guid.NewGuid();

        _assignmentPolicy
            .Setup(p => p.CanAssignToPosition(positionId))
            .Returns(false);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            person.AssignToPosition(positionId, DateTime.UtcNow, _assignmentPolicy.Object)
        );

        Assert.Contains("Неможливо призначити", ex.Message);
    }

    [Fact]
    public void AssignToPosition_ShouldCloseCurrentAssignment_WhenAlreadyAssigned()
    {
        // Arrange
        var person = CreateTestPerson();
        var firstPositionId = Guid.NewGuid();
        var secondPositionId = Guid.NewGuid();
        var firstDate = DateTime.UtcNow;
        var secondDate = firstDate.AddDays(10);

        _assignmentPolicy
            .Setup(p => p.CanAssignToPosition(It.IsAny<Guid>()))
            .Returns(true);

        // Act
        person.AssignToPosition(firstPositionId, firstDate, _assignmentPolicy.Object);
        person.AssignToPosition(secondPositionId, secondDate, _assignmentPolicy.Object);

        // Assert
        Assert.Equal(2, person.PositionAssignments.Count);

        var firstAssignment = person.PositionAssignments[0];
        Assert.NotNull(firstAssignment.CloseUtc);
        Assert.Equal(secondDate.AddDays(-1), firstAssignment.CloseUtc);

        var secondAssignment = person.PositionAssignments[1];
        Assert.Null(secondAssignment.CloseUtc);
    }

    [Fact]
    public void CreatePlanAction_ShouldSucceed_WhenValid()
    {
        // Arrange
        var person = CreateTestPerson();
        var effectiveAt = DateTime.UtcNow.AddDays(5);

        // Act
        person.CreatePlanAction(
            planActionName: "Рапорт №123",
            effectiveAtUtc: effectiveAt,
            moveType: MoveType.Dispatch,
            location: "Київ"
        );

        // Assert
        Assert.Single(person.PlanActions);
        var action = person.PlanActions[0];
        Assert.Equal("Рапорт №123", action.PlanActionName);
        Assert.Equal(MoveType.Dispatch, action.MoveType);
        Assert.Contains(person.DomainEvents, e => e is PlanActionCreatedEvent);
    }

    [Fact]
    public void CreatePlanAction_ShouldThrowDomainException_WhenDateBeforeLastAction()
    {
        // Arrange
        var person = CreateTestPerson();
        var firstDate = DateTime.UtcNow.AddDays(5);
        var secondDate = DateTime.UtcNow.AddDays(3); // раніше!

        person.CreatePlanAction("Рапорт №1", firstDate, MoveType.Dispatch, "Київ");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            person.CreatePlanAction("Рапорт №2", secondDate, MoveType.Return, "Львів")
        );

        Assert.Contains("пізніше", ex.Message);
    }

    // Helper
    private PersonAggregate CreateTestPerson()
    {
        var personalInfo = new PersonalInfo("1234567890", "Тестов", "Тест");
        var militaryDetails = new MilitaryDetails("Солдат", "999999");

        return PersonAggregate.Create(
            personalInfo,
            militaryDetails,
            initialStatusKindId: 1,
            _transitionValidator.Object
        );
    }
}
