using Platform.Application.DependencyInjection;
using Platform.Api.Extensions;
using Platform.Payment.API.Infrastructure.Data;
using Platform.Payment.API.Infrastructure.DependencyInjection;
using Platform.Payment.API.Presentation.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(typeof(Program).Assembly);
builder.Services.AddPaymentInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddPlatformRuntime();
builder.Services.AddPlatformAuthentication(builder.Configuration);
builder.Services.AddPlatformSwaggerJwt("Platform Payment API");

var app = builder.Build();

await app.ApplyMigrationsAsync<PaymentDbContext>();

app.UsePlatformRuntime();
app.UseHttpsRedirection();
app.UsePlatformSwagger();
app.UsePlatformAuthentication();

app.MapControllers();
app.MapGrpcService<PaymentIntegrationService>();

app.Run();
