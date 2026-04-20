using FluentAssertions;
using PpecbAssessment.Application.Categories.Dtos;
using PpecbAssessment.Infrastructure.Services;
using PpecbAssessment.Tests.Common;
using Xunit;

namespace PpecbAssessment.Tests.Categories;

public class CategoryServiceTests
{
    [Fact]
    public async Task CreateCategoryAsync_Should_Create_Category_When_Request_Is_Valid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryService(context);

        var request = new CreateCategoryRequest
        {
            Name = "Electronics",
            CategoryCode = "ABC123",
            IsActive = true
        };

        // Act
        var result = await service.CreateCategoryAsync(request, "user-1", "user-1");

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        context.Categories.Count().Should().Be(1);
    }

    [Fact]
    public async Task CreateCategoryAsync_Should_Fail_When_Code_Is_Invalid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new CategoryService(context);

        var request = new CreateCategoryRequest
        {
            Name = "Electronics",
            CategoryCode = "BAD1",
            IsActive = true
        };

        // Act
        var result = await service.CreateCategoryAsync(request, "user-1", "user-1");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Category code must be in the format ABC123.");
    }

    [Fact]
    public async Task CreateCategoryAsync_Should_Fail_When_Code_Already_Exists()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Categories.Add(new PpecbAssessment.Domain.Entities.Category
        {
            CategoryId = Guid.NewGuid(),
            Name = "Existing",
            CategoryCode = "ABC123",
            IsActive = true,
            UserId = "user-1",
            CreatedBy = "user-1",
            CreatedDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new CategoryService(context);

        var request = new CreateCategoryRequest
        {
            Name = "New Category",
            CategoryCode = "ABC123",
            IsActive = true
        };

        // Act
        var result = await service.CreateCategoryAsync(request, "user-1", "user-1");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Category code already exists.");
    }
}