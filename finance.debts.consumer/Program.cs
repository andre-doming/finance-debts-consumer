using MassTransit;
using finance.debts.consumer.Consumers;
using finance.debts.consumer.Infrastructure.Repositories.External;
using finance.debts.consumer.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScoped<DebtService>();

var debtsApiConfig = builder.Configuration.GetSection("DebtsApiConfig");

var endpoint = debtsApiConfig["Endpoint"]
    ?? throw new Exception("Endpoint da API de dívidas não configurado");

var timeout = debtsApiConfig["Timeout"]
    ?? throw new Exception("Timeout da API de dívidas não configurado");

builder.Services.AddHttpClient<DebtApi>(client =>
{
    client.BaseAddress = new Uri(endpoint);
    client.Timeout = TimeSpan.FromSeconds(Convert.ToInt32(timeout));
});


var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");

var rabbitHost = rabbitConfig["Host"]
    ?? throw new Exception("Rabbit host não configurado");

var rabbitVhost = rabbitConfig["VirtualHost"]
    ?? throw new Exception("Rabbit vhost não configurado");

var rabbitUser = rabbitConfig["Username"]
    ?? throw new Exception("Rabbit username não configurado");

var rabbitPass = rabbitConfig["Password"]
    ?? throw new Exception("Rabbit password não configurado");

var rabbitQueue = rabbitConfig["Queue"]
    ?? throw new Exception("Rabbit queue não configurado");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DebtCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, rabbitVhost, h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ReceiveEndpoint(rabbitQueue, e =>
        {
            e.PrefetchCount = 1;

            e.UseDelayedRedelivery(r =>
            {
                r.Intervals(
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromSeconds(30)
                );
            });

            e.UseMessageRetry(r =>
            {
                r.Handle<Exception>(ex =>
                    ex is TaskCanceledException || ex is HttpRequestException
                );

                r.Interval(3, TimeSpan.FromSeconds(5));
            });

            e.UseInMemoryOutbox(context);

            e.UseCircuitBreaker(cb =>
            {
                cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                cb.TripThreshold = 5;
                cb.ActiveThreshold = 10;
                cb.ResetInterval = TimeSpan.FromMinutes(5);
            });

            e.ConfigureConsumer<DebtCreatedConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();