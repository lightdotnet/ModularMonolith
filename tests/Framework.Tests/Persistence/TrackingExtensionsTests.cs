using Framework.Tests.Persistence.TestSupport;
using Microsoft.EntityFrameworkCore;
using StarterKit.Persistence.Extensions;
using Xunit;

namespace Framework.Tests.Persistence;

public class TrackingExtensionsTests
{
    [Fact]
    public void AuditEntries_ShouldSetCreationFields_ForAddedEntities()
    {
        // Arrange
        using var context = TestDbContext.CreateInMemory();
        var aggregate = new TestAggregate();
        context.Add(aggregate);
        var auditTime = DateTimeOffset.UtcNow;

        // Act
        context.AuditEntries("user-1", auditTime);

        // Assert
        Assert.Equal(auditTime, aggregate.Created);
        Assert.Equal("user-1", aggregate.CreatedBy);
        Assert.Null(aggregate.LastModified);
        Assert.Null(aggregate.LastModifiedBy);
    }

    [Fact]
    public void AuditEntries_ShouldSetModificationFields_ForModifiedEntities()
    {
        // Arrange
        using var context = TestDbContext.CreateInMemory();
        var aggregate = new TestAggregate();
        context.Add(aggregate);
        context.Entry(aggregate).State = EntityState.Modified;
        var auditTime = DateTimeOffset.UtcNow;

        // Act
        context.AuditEntries("user-1", auditTime);

        // Assert
        Assert.Equal(auditTime, aggregate.LastModified);
        Assert.Equal("user-1", aggregate.LastModifiedBy);
        Assert.Equal(default(DateTimeOffset), aggregate.Created);
    }

    [Fact]
    public void AuditEntries_ShouldNotThrow_WhenUserIdIsNull()
    {
        // Arrange
        using var context = TestDbContext.CreateInMemory();
        var aggregate = new TestAggregate();
        context.Add(aggregate);

        // Act
        context.AuditEntries(null, DateTimeOffset.UtcNow);

        // Assert
        Assert.Null(aggregate.CreatedBy);
    }

    [Fact]
    public void AuditEntries_ShouldLeaveDeletedEntityUntouched_WhenSoftDeleteDisabled()
    {
        // Arrange
        using var context = TestDbContext.CreateInMemory();
        var aggregate = new TestAggregate();
        context.Add(aggregate);
        context.Entry(aggregate).State = EntityState.Deleted;

        // Act
        context.AuditEntries("user-1", DateTimeOffset.UtcNow, enableSoftDelete: false);

        // Assert
        Assert.Equal(EntityState.Deleted, context.Entry(aggregate).State);
        Assert.Null(aggregate.Deleted);
        Assert.Null(aggregate.DeletedBy);
    }

    [Fact]
    public void AuditEntries_ShouldSoftDeleteAndFlipToModified_WhenSoftDeleteEnabled()
    {
        // Arrange
        using var context = TestDbContext.CreateInMemory();
        var aggregate = new TestAggregate();
        context.Add(aggregate);
        context.Entry(aggregate).State = EntityState.Deleted;
        var auditTime = DateTimeOffset.UtcNow;

        // Act
        context.AuditEntries("user-1", auditTime, enableSoftDelete: true);

        // Assert
        Assert.Equal(EntityState.Modified, context.Entry(aggregate).State);
        Assert.Equal(auditTime, aggregate.Deleted);
        Assert.Equal("user-1", aggregate.DeletedBy);
    }

    [Fact]
    public void AuditEntries_ShouldSetCreationTime_ForEntityImplementingOnlyCreationTimeInterface()
    {
        // Arrange
        using var context = TestDbContext.CreateInMemory();
        var entity = new TestCreationOnlyEntity();
        context.Add(entity);
        var auditTime = DateTimeOffset.UtcNow;

        // Act
        context.AuditEntries("user-1", auditTime);

        // Assert
        Assert.Equal(auditTime, entity.Created);
    }

    [Fact]
    public void AuditEntries_ShouldNotThrow_WhenModifiedEntityLacksModificationOrAuditUserInterfaces()
    {
        // Arrange
        using var context = TestDbContext.CreateInMemory();
        var entity = new TestCreationOnlyEntity();
        context.Add(entity);
        context.Entry(entity).State = EntityState.Modified;

        // Act
        var exception = Record.Exception(() => context.AuditEntries("user-1", DateTimeOffset.UtcNow));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void AuditEntries_ShouldResetDeletedValueObjectEntries_ToUnchanged_RegardlessOfSoftDelete()
    {
        // Arrange
        using var context = TestDbContext.CreateInMemory();
        var aggregate = new TestAggregate { Note = new TestNote { Text = "hello" } };
        context.Add(aggregate);
        var noteEntry = context.ChangeTracker.Entries().Single(e => e.Entity is TestNote);
        noteEntry.State = EntityState.Deleted;

        // Act
        context.AuditEntries("user-1", DateTimeOffset.UtcNow, enableSoftDelete: true);

        // Assert
        Assert.Equal(EntityState.Unchanged, noteEntry.State);
    }
}
