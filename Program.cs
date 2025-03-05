using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add services
builder.Services.AddControllersWithViews();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5056); // Listen on all network interfaces
});

// Register CosmosClient with connection string and container
builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var connectionString = builder.Configuration.GetSection("CosmosDB:ConnectionString").Value;
    return new CosmosClient(connectionString);
});

// Add this section to ensure database and container exist
builder.Services.AddSingleton<Container>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    var databaseName = builder.Configuration.GetSection("CosmosDB:DatabaseName").Value;
    // Use the correct container name with lowercase 'd'
    return cosmosClient.GetContainer(databaseName, "Sensor_data");
});

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Use CORS before other middleware
app.UseCors("AllowReactApp");

// HTTPS redirection is commented out to allow HTTP calls from ESP32
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add this before app.Run()
try
{
    var cosmosClient = builder.Services.BuildServiceProvider().GetRequiredService<CosmosClient>();
    var databaseName = builder.Configuration.GetSection("CosmosDB:DatabaseName").Value;
    var containerName = builder.Configuration.GetSection("CosmosDB:ContainerName").Value;

    // Create database if it doesn't exist
    var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);

    // Create container with timestamp as partition key if it doesn't exist
    await database.Database.CreateContainerIfNotExistsAsync(
        id: containerName,
        partitionKeyPath: "/timestamp",
        throughput: 400
    );

    Console.WriteLine("CosmosDB resources created successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Error setting up CosmosDB: {ex.Message}");
}

app.Run();