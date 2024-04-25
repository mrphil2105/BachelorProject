using Apachi.Shared.Crypto;
using Apachi.WebApi.Data;
using Microsoft.EntityFrameworkCore;

if (args.Length > 0 && args[0] == "--generate-keypair")
{
    var (publicKey, privateKey) = KeyUtils.GenerateKeyPair();
    var publicKeyBase64 = Convert.ToBase64String(publicKey);
    var privateKeyBase64 = Convert.ToBase64String(privateKey);
    Console.WriteLine("Public Key: {0}", publicKeyBase64);
    Console.WriteLine("Private Key: {0}", privateKeyBase64);
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=App.db"));
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration.AddEnvironmentVariables("APACHI_");

var app = builder.Build();

using (var serviceScope = app.Services.CreateScope())
using (var dbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>())
{
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
