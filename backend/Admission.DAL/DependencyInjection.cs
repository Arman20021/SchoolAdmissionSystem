
using Admission.BLL.Interfaces.Repositories;
using Admission.DAL.Data;
using Admission.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Admission.DAL;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection string is missing."
            );
        }

        services.AddDbContext<ApplicationDbContext>(
            options => options.UseNpgsql(connectionString)
        );

        services.AddScoped<IClassRepository, ClassRepository>();

        return services;
    }
}
