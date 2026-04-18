using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MQTTnet.Adapter;
using MQTTnet.DependencyInjection.Options;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Packets;

namespace MQTTnet.DependencyInjection
{
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// Нстройка MqttClientOptionsBuilder
        /// </summary>
        public static IServiceCollection ConfigureMqttClientOptions(this IServiceCollection services, Action<MqttClientOptionsBuilder> configure)
            => services
            .AddOptions<MqttClientOptionsBuilder>()
            .Configure(configure)
            .Services;

        /// <summary>
        /// Нстройка MqttClientOptionsBuilder
        /// </summary>
        public static IServiceCollection ConfigureMqttClientOptions<TDep>(this IServiceCollection services, Action<MqttClientOptionsBuilder, TDep> configure)
            where TDep : class
            => services
            .AddOptions<MqttClientOptionsBuilder>()
            .Configure(configure)
            .Services;

        /// <summary>
        /// Настройка Mqtt клиента с использование файла конфигурации
        /// </summary>
        public static IServiceCollection ConfigureMqtt(
            this IServiceCollection services,
            IConfiguration configuration,
            string? sectionName = null)
            => services
            .Configure<MqttOptions>(configuration.GetSection(sectionName ?? MqttOptions.SectionName))
            .Configure<MqttLifetimeOptions>(configuration.GetSection(sectionName ?? MqttOptions.SectionName))
            .ConfigureMqttClientOptions<IOptions<MqttOptions>>((cfgBuilder, mqttOptions) =>
            {
                if (!string.IsNullOrWhiteSpace(mqttOptions.Value.TcpAddress) || mqttOptions.Value.TcpPort.HasValue)
                    cfgBuilder.WithTcpServer(mqttOptions.Value.TcpAddress ?? "127.0.0.1", mqttOptions.Value.TcpPort);
            });

        /// <summary>
        /// Добавление необходимых для работы Mqtt служб
        /// </summary>
        public static IServiceCollection AddMqtt(this IServiceCollection services)
        {
            services.TryAddSingleton<IMqttNetLogger, MqttNetNullLogger>();

            return services
                .AddHostedService<MqttClientLifetimeService>()
                .AddSingleton<MqttClientFactory>()
                .AddSingleton(sp => sp.GetRequiredService<MqttClientFactory>().CreateMqttClient());
        }

        /// <summary>
        /// Регистрация консьюмера
        /// </summary>
        public static IServiceCollection RegisterMqttConsumer<TConsumer>(this IServiceCollection services)
            where TConsumer : class, IMqttConsumer
            => services.AddTransient<IMqttConsumer, TConsumer>();

        /// <summary>
        /// Регистрация консьюмера
        /// </summary>
        public static IServiceCollection RegisterMqttConsumer(this IServiceCollection services,
            Func<IServiceProvider, IMqttConsumer> factory,
            MqttTopicFilter filter)
            => services.AddSingleton(new Subscription(filter, factory));
    }
}
