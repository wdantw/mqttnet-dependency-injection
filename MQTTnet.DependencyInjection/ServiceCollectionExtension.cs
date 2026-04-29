using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
                cfgBuilder.WithKeepAlivePeriod(mqttOptions.Value.KeepAlivePeriod);
                cfgBuilder.WithTcpServer(mqttOptions.Value.TcpAddress, mqttOptions.Value.TcpPort);
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
                .AddSingleton(sp => sp.GetRequiredService<MqttClientFactory>().CreateMqttClient())
                .AddSingleton<IMqttPublisher, MqttPublisher>();
        }

        /// <summary>
        /// Конфигурация фильтра для консьюмера
        /// </summary>
        public static IServiceCollection BuildMqttConsumerFilter<TConsumer>(this IServiceCollection services, Action<MqttTopicFilterBuilder>? configure)
        {
            services
                .AddOptions<ConsumerFilterOptions<TConsumer>>()
                .Configure(opt => configure?.Invoke(opt.FilterBuilder));

            return services;
        }

        /// <summary>
        /// Конфигурация фильтра для консьюмера
        /// </summary>
        public static IServiceCollection BuildMqttConsumerFilter<TConsumer, TDep>(this IServiceCollection services, Action<MqttTopicFilterBuilder, TDep> configure)
            where TDep : class
        {
            services
                .AddOptions<ConsumerFilterOptions<TConsumer>>()
                .Configure<TDep>((opt, dep) => configure(opt.FilterBuilder, dep));

            return services;
        }

        /// <summary>
        /// Регистрация консьюмера
        /// </summary>
        public static IServiceCollection RegisterMqttConsumerScoped<TConsumer>(this IServiceCollection services, Action<MqttTopicFilterBuilder>? configureFilter)
            where TConsumer : class, IMqttConsumer
            => services
            .AddScoped<TConsumer>()
            .AddSingleton<ISubscription, ScopedSubscription<TConsumer>>()
            .BuildMqttConsumerFilter<TConsumer>(configureFilter);

        /// <summary>
        /// Регистрация консьюмера
        /// </summary>
        public static IServiceCollection RegisterMqttConsumerSingleton<TConsumer>(this IServiceCollection services, Action<MqttTopicFilterBuilder>? configureFilter)
            where TConsumer : class, IMqttConsumer
            => services
            .AddSingleton<TConsumer>()
            .AddSingleton<ISubscription, SingletonSubscription<TConsumer>>()
            .BuildMqttConsumerFilter<TConsumer>(configureFilter);
    }
}
