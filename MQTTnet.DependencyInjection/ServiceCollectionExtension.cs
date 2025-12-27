using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using MQTTnet.DependencyInjection.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;

namespace MQTTnet.DependencyInjection
{
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// Настройка MqttClientOptionsBuilder
        /// </summary>
        public static IServiceCollection ConfigureMqttManagedClientOptions(this IServiceCollection services, Action<ManagedMqttClientOptionsBuilder> configure)
            => services
            .AddOptions<ManagedMqttClientOptionsBuilder>()
            .Configure(configure)
            .Services;

        /// <summary>
        /// Настройка MqttClientOptionsBuilder
        /// </summary>
        public static IServiceCollection ConfigureMqttManagedClientOptions<TDep>(this IServiceCollection services, Action<ManagedMqttClientOptionsBuilder, TDep> configure)
            where TDep : class
            => services
            .AddOptions<ManagedMqttClientOptionsBuilder>()
            .Configure(configure)
            .Services;

        public static IServiceCollection ConfigureMqtt(
            this IServiceCollection services,
            IConfiguration configuration,
            string? sectionName = null)
            => services
            .Configure<MqttOptions>(configuration.GetSection(sectionName ?? MqttOptions.SectionName))
            .ConfigureMqttManagedClientOptions<IOptions<MqttOptions>>((cfgBuilder, mqttOptions) => {

                if (!string.IsNullOrWhiteSpace(mqttOptions.Value.TcpAddress) || mqttOptions.Value.TcpPort.HasValue)
                    cfgBuilder.WithClientOptions(clientOptBuilder => clientOptBuilder.WithTcpServer(mqttOptions.Value.TcpAddress ?? "127.0.0.1", mqttOptions.Value.TcpPort));

                if (mqttOptions.Value.AutoReconnectDelay.HasValue)
                    cfgBuilder.WithAutoReconnectDelay(mqttOptions.Value.AutoReconnectDelay.Value);

                cfgBuilder.WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage);
            });

        public static IServiceCollection AddMqtt(this IServiceCollection services)
            => services
            .AddHostedService<MqttClientLifetimeService>()
            .AddSingleton<IManagedMqttClient>(sp => new MqttFactory().CreateManagedMqttClient());


    }
}
