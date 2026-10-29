//program.cs configures the application before it receives requests
//import namespaces
using EmployeeManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

//create application builder
var builder = WebApplication.CreateBuilder(args);

// Set up the connection between the backend and SQL Server.
//connecting to sql server
// Register AppDbContext for dependency injection.
// Configure Entity Framework Core to connect to SQL Server.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

//register controllers
//allow backend to use controllers
// Add services to the container.
builder.Services.AddControllers();

// Enable OpenAPI documentation.
builder.Services.AddOpenApi();

 //CORS is a browser security mechanism that controls whether a website can make requests to a different origin.
 //configure cors
// Allow the React frontend to communicate with the backend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//build the application using all settings configured
var app = builder.Build();
//it applies migrations and handles SQL Server startup delays.
// Use a try-catch block to apply database migrations and retry if SQL Server is not ready.
// Apply pending database migrations when the application starts.
// Retry because SQL Server may take some time to become ready in Docker.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    const int maxAttempts = 20;

    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await dbContext.Database.MigrateAsync();
            break;
        }
        catch (Microsoft.Data.SqlClient.SqlException) when (attempt < maxAttempts)
        {
            Console.WriteLine(
                $"SQL Server is not ready. Retrying ({attempt}/{maxAttempts})...");

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

//configure the api and start it
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseHttpsRedirection();

// Apply the CORS policy before authorization.
app.UseCors("ReactApp");

app.UseAuthorization();

//connects URLs to controller methods.
app.MapControllers();
//starts the backend api
app.Run();