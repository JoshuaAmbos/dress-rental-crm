using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;

var builder = WebApplication.CreateBuilder(args);

// Register Database Context with SQL Server
builder.Services.AddDbContext<MasterCrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MasterCrm")));

// Add Controller and OpenAPI Services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure HTTP Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Seed / Initial Test Endpoint (Lab 3 & 4)
app.MapPost("/companies", async (Company company, MasterCrmDbContext db) =>
{
    db.Companies.Add(company);
    await db.SaveChangesAsync();
    return Results.Created($"/companies/{company.CompanyId}", company);
});

app.Run();