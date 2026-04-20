using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PpecbAssessment.Application.Products.Dtos;
using PpecbAssessment.Domain.Entities;
using PpecbAssessment.Infrastructure.Services;
using PpecbAssessment.Tests.Common;
using Xunit;

namespace PpecbAssessment.Tests.Products;

public class ProductServiceTests
{
    [Fact]
    public async Task CreateProductAsync_Should_Create_Product_When_Request_Is_Valid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var categoryId = Guid.NewGuid();

        context.Categories.Add(new Category
        {
            CategoryId = categoryId,
            Name = "Electronics",
            CategoryCode = "ABC123",
            IsActive = true,
            UserId = "user-1",
            CreatedBy = "user-1",
            CreatedDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new ProductService(context);

        var request = new CreateProductRequest
        {
            Name = "Laptop",
            Description = "Office laptop",
            Price = 15000,
            CategoryId = categoryId
        };

        // Act
        var result = await service.CreateProductAsync(
            request,
            "user-1",
            "user-1",
            Path.GetTempPath(), // uploads path (not used here)
            null // no image
        );

        // Assert
        result.Succeeded.Should().BeTrue();

        var product = await context.Products.FirstOrDefaultAsync();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Laptop");
        product.ProductCode.Should().StartWith(DateTime.UtcNow.ToString("yyyyMM"));
    }

    [Fact]
    public async Task CreateProductAsync_Should_Fail_When_Category_Does_Not_Belong_To_User()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var categoryId = Guid.NewGuid();

        context.Categories.Add(new Category
        {
            CategoryId = categoryId,
            Name = "Electronics",
            CategoryCode = "ABC123",
            IsActive = true,
            UserId = "another-user", // important
            CreatedBy = "another-user",
            CreatedDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new ProductService(context);

        var request = new CreateProductRequest
        {
            Name = "Laptop",
            Description = "Office laptop",
            Price = 15000,
            CategoryId = categoryId
        };

        // Act
        var result = await service.CreateProductAsync(
            request,
            "user-1", // different user
            "user-1",
            Path.GetTempPath(),
            null
        );

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Selected category was not found.");
    }

    [Fact]
    public async Task DeleteProductAsync_Should_Delete_Product_When_Found()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var productId = Guid.NewGuid();

        context.Products.Add(new Product
        {
            ProductId = productId,
            ProductCode = "202604-001",
            Name = "Laptop",
            Price = 1000,
            CategoryId = Guid.NewGuid(),
            UserId = "user-1",
            CreatedBy = "user-1",
            CreatedDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new ProductService(context);

        // Act
        var result = await service.DeleteProductAsync(productId, "user-1");

        // Assert
        result.Succeeded.Should().BeTrue();
        context.Products.Count().Should().Be(0);
    }

    [Fact]
    public async Task DeleteProductAsync_Should_Fail_When_Product_Not_Found()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        var service = new ProductService(context);

        // Act
        var result = await service.DeleteProductAsync(Guid.NewGuid(), "user-1");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Product not found.");
    }

    [Fact]
    public async Task GetPagedAsync_Should_Return_Correct_Pagination()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var categoryId = Guid.NewGuid();

        context.Categories.Add(new Category
        {
            CategoryId = categoryId,
            Name = "Electronics",
            CategoryCode = "ABC123",
            IsActive = true,
            UserId = "user-1",
            CreatedBy = "user-1",
            CreatedDate = DateTime.UtcNow
        });

        for (int i = 1; i <= 25; i++)
        {
            context.Products.Add(new Product
            {
                ProductId = Guid.NewGuid(),
                ProductCode = $"202604-{i:D3}",
                Name = $"Product {i}",
                Price = i * 10,
                CategoryId = categoryId,
                UserId = "user-1",
                CreatedBy = "user-1",
                CreatedDate = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await context.SaveChangesAsync();

        var service = new ProductService(context);

        // Act
        var result = await service.GetPagedAsync("user-1", 1);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.PageNumber.Should().Be(1);
    }
}