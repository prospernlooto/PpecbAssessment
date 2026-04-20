using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PpecbAssessment.Application.Auth.Interfaces;
using PpecbAssessment.Application.Categories.Interfaces;
using PpecbAssessment.Application.Products.Interfaces;
using PpecbAssessment.Infrastructure.Persistence;
using PpecbAssessment.Infrastructure.Services;

namespace PpecbAssessment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}