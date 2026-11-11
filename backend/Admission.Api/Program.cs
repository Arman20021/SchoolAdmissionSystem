using Admission.BLL.Interfaces.Repositories;
using Admission.DAL;
using Admission.BLL.Interfaces.Services;
using Admission.BLL.Services;
using Scalar.AspNetCore;
using Admission.Api.Filters;
using Admission.Api.Services;


var builder = WebApplication.CreateBuilder(args);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});



var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection"
    );

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is not configured."
    );
}

builder.Services.AddDataAccess(connectionString);
builder.Services.AddScoped<
    IStudentIdentityValidator,
    StudentIdentityValidator
>();

// Admission Hold Configuration
var holdDurationSeconds =
    builder.Configuration.GetValue<int>(
        "AdmissionHold:DurationSeconds"
    );

if (holdDurationSeconds <= 0)
{
    throw new InvalidOperationException(
        "Admission Hold duration must be greater than zero."
    );
}

// Admission Hold Service Registration
builder.Services.AddScoped<IAdmissionHoldService>(
    serviceProvider =>
    {
        var repository =
            serviceProvider.GetRequiredService<
                IAdmissionHoldRepository
            >();

        return new AdmissionHoldService(
            repository,
            TimeSpan.FromSeconds(holdDurationSeconds)
        );
    }
);
builder.Services.AddHostedService<
    HoldExpirationBackgroundService
>();
builder.Services.AddScoped<StudentIdentityFilter>();
builder.Services.AddScoped<IClassService, ClassService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseCors("FrontendPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
