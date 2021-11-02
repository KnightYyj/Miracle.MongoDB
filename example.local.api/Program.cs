using example.local.api;
using Miracle.MongoDB.Gen;
using Miracle.MongoDB.GridFS;
using Miracle.WebApi.Filters;
using Miracle.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "example.local.api", Version = "v1" }));
builder.Services.AddCors(c => c.AddPolicy("AllowedHosts", c => c.WithOrigins(builder.Configuration.GetValue<string>("AllowedHosts").Split(",")).AllowAnyMethod().AllowAnyHeader()));

var db = builder.Services.AddMongoDbContext<DbContext>(builder.Configuration);

builder.Services.AddMiracleGridFS(db._database, businessApp: "MiracleFS");

builder.Services.AddControllers(c => c.Filters.Add<ActionExecuteFilter>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();
app.UseGlobalException();
app.UseResponseTime();
app.UseCors("AllowedHosts");

app.UseAuthorization();

app.MapControllers();
app.UseSwagger().UseSwaggerUI();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async () =>
{
    var o = Enumerable.Range(1, 5).Select(index => new Test
    {
        Sex = Random.Shared.Next(-20, 55),
        Name = summaries[Random.Shared.Next(summaries.Length)]
    }).ToArray();
    await db.Test.InsertManyAsync(o);
    return o;
})
.WithName("GetWeatherForecast");

app.Run();