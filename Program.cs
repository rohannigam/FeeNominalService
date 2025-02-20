using FeeNominalService;
using Destructurama;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
IConfigurationRoot config;
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional:false, reloadOnChange: true)
    .Build();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    //.Enrich.FromLogContext()
    //.WriteTo.Console()
    .Destructure.UsingAttributes()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient("InterpaymentsClient", client =>
{
    client.BaseAddress = new Uri(ApiConstants.InterpaymentsBaseAddress); // Base URL from the API documentation
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(ApiConstants.AcceptHeader));
    //The following needs to come from DB - configured per merchant
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
        "Bearer", 
        builder.Configuration["Interpayments:TransactionFeeAPIToken"]
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
