using Platform.Api.Extensions;
using Platform.Application.DependencyInjection;
using Platform.Payment.API.Infrastructure.Data;
using Platform.Payment.API.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(typeof(Program).Assembly);
builder.Services.AddPaymentInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddPlatformAuthentication(builder.Configuration);
builder.Services.AddPlatformSwaggerJwt("Platform Payment API");

var app = builder.Build();

await app.ApplyMigrationsAsync<PaymentDbContext>();

app.UseHttpsRedirection();
app.UsePlatformSwagger();
app.UsePlatformAuthentication();

app.MapControllers();
app.MapGrpcService<Platform.Payment.API.Presentation.Grpc.PaymentIntegrationService>();

app.Run();
