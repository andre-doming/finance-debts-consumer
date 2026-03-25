using finance.debts.consumer;
using finance.debts.consumer.Domain.Interfaces;
using finance.debts.consumer.Infrastructure.Repositories;
using finance.debts.consumer.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<IDebtRepository, DebtRepository>();
builder.Services.AddScoped<IProcessingLogRepository, ProcessingLogRepository>();
builder.Services.AddScoped<DebtService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
