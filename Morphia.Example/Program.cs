using Microsoft.EntityFrameworkCore;
using Morphia.Example.Context;
using Morphia.Example.Repositories;

var builder = WebApplication.CreateBuilder(args);

//db context
builder.Services.AddDbContext<ApiDbContext>(options => options.UseNpgsql("Host=localhost;Port=5432;Database=morphia;User Id=postgres;Password=postgres"));
builder.Services.AddScoped<CompanyRepository>();
builder.Services.AddScoped<EmployeRepository>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.MapGet("/", () => "Hello World");

await app.RunAsync();