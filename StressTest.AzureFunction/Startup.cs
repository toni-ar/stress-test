using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using StressTest.AzureFunction.Configuration;
using StressTest.AzureFunction.Exceptions;
using StressTest.AzureFunction.Interfaces;
using StressTest.AzureFunction.Services;

[assembly: FunctionsStartup(typeof(StressTest.AzureFunction.Startup))]
namespace StressTest.AzureFunction
{
    public class Startup : FunctionsStartup
    {
        public IConfiguration Configuration { get; set; }
        
        public Startup()
        {
        }

        private void ShowConfig(IConfiguration config)
        {
            foreach (var pair in config.GetChildren())
            {
                System.Diagnostics.Debug.WriteLine($"{pair.Path} - {pair.Value}");
                ShowConfig(pair);
            }
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var localRoot = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
            var azureRoot = $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot";

            var actualRoot = localRoot ?? azureRoot;

            var environment = new EnvironmentProvider().Environment();

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(actualRoot);

            if (environment == ApplicationEnvironment.Development)
                configBuilder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);

            configBuilder.AddEnvironmentVariables();    // This must occur after loading local.settings.json, or it won't override

            Configuration = configBuilder.Build();

            if (environment == ApplicationEnvironment.Development)
                ShowConfig(Configuration);
            
            // Prevent Azure Function to automatically complete the message, because we are doing it manually
            builder.Services.Configure(delegate (ServiceBusOptions options)
            {
                options.MessageHandlerOptions.AutoComplete = false;
            });

            AddMessaging(builder.Services, Configuration);

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.ApplicationInsights(
                    TelemetryConfiguration.Active,
                    TelemetryConverter.Traces,
                    LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .CreateLogger();

            builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            builder.Services.AddApplicationInsightsTelemetry();
        }
        
        private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
        {
            var serviceBusConnectionString = configuration.GetConnectionStringOrSetting("ServiceBusConnectionString");
            if (string.IsNullOrEmpty(serviceBusConnectionString))
            {
                throw new MissingEnvironmentVariableException("ServiceBusConnectionString");
            }
            services.AddSingleton(serviceProvider =>
            {
                var serviceBusConnectionStringBuilder =
                    new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
                return new ServiceBusConnection(serviceBusConnectionStringBuilder);
            });

            services.AddScoped<IMessagingService, ServiceBusMessagingService>();
        }
    }
}
