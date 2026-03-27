using finance.debts.consumer;
using finance.debts.consumer.Services;
using finance.debts.consumer.Infrastructure.Repositories.External;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddScoped<DebtService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHttpClient<DebtApi>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7029");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var host = builder.Build();
host.Run();
