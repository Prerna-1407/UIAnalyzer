using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UIAnalyzerAPI.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
// Register services
builder.Services.AddScoped<SolutionExtractor>();
builder.Services.AddScoped<ProjectParser>();
builder.Services.AddScoped<RoslynAnalyzer>();
builder.Services.AddScoped<RazorParser>();
builder.Services.AddScoped<RazorParser>();  

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
app.UseRouting();


app.Run();
