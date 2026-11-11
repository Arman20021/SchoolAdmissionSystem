using Admission.BLL.Interfaces.Repositories;
using Admission.DAL;
using Admission.BLL.Interfaces.Services;
using Admission.BLL.Services;
using Scalar.AspNetCore;
using Admission.Api.Filters;
using Admission.Api.Services;
using Admission.DAL.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS
var frontendOrigin =
    builder.Configuration["FRONTEND_ORIGIN"]
    ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});



//var connectionString =
//    builder.Configuration.GetConnectionString(
//        "DefaultConnection"
//    );

var connectionString =
    builder.Configuration["DATABASE_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

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
//var holdDurationSeconds =
//    builder.Configuration.GetValue<int>(
//        "AdmissionHold:DurationSeconds"
//    );

//if (holdDurationSeconds <= 0)
//{
//    throw new InvalidOperationException(
//        "Admission Hold duration must be greater than zero."
//    );
//}

const int defaultHoldDurationSeconds = 600;

var holdDurationSetting =
    builder.Configuration["HOLD_DURATION_SECONDS"];

var holdDurationSeconds = defaultHoldDurationSeconds;

if (!string.IsNullOrWhiteSpace(holdDurationSetting))
{
    if (!int.TryParse(
            holdDurationSetting,
            out holdDurationSeconds) ||
        holdDurationSeconds <= 0)
    {
        throw new InvalidOperationException(
            "HOLD_DURATION_SECONDS must be a positive integer."
        );
    }
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

// Automatically apply pending EF Core migrations
// when the backend starts.
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.UseCors("FrontendPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();
}

if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}
if (app.Environment.IsDevelopment() ||
    app.Environment.IsEnvironment("Docker"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
