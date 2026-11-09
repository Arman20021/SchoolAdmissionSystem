using Admission.DAL;
using Admission.BLL.Interfaces.Services;
using Admission.BLL.Services;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);



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
builder.Services.AddScoped<IClassService, ClassService>();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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
