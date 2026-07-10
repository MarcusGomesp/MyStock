using InventoryApi.Data;
using InventoryApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Configurações vindas do appsettings.json ----
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<SheetMappingSettings>(builder.Configuration.GetSection("SheetMappings"));

// ---- MongoDB ----
builder.Services.AddSingleton<MongoDbContext>();

// ---- Serviços de negócio ----
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddScoped<CsvExportService>();

// ---- Controllers + JSON ----
builder.Services.AddControllers();

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Inventory API", Version = "v1" });
});

// ---- CORS (libera geral; ajuste em produção) ----
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
