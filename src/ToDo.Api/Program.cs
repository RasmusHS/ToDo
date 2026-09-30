using ToDo.Application;
using ToDo.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// open Package Manager Console
// Add-Migration
// Name: Initial
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowBlazorClient", policy =>
//    {
//        policy.WithOrigins(/*"https://localhost:7235",*/ "http://localhost:5038")
//              .AllowAnyMethod()
//              .AllowAnyHeader();
//    });
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    //app.ApplyMigrations();
}

//app.UseCors("AllowBlazorClient");

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();


public partial class Program { }
