using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.DependencyInjection.Options;
using MQTTnet.Server;
using NSubstitute;
using NSubstitute.ClearExtensions;
using System.Buffers;

namespace MQTTnet.DependencyInjection.Tests
{
    public class MqttTests
    {
        private const string LocalAddress = "127.0.0.1";
        private readonly TimeSpan TestTimeout = System.Diagnostics.Debugger.IsAttached ? TimeSpan.FromMinutes(10) : TimeSpan.FromSeconds(5);

        #region common parts

        private async Task<MqttServer> StartServer(int port)
        {
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(port);

            var mqttServer = new MqttServerFactory().CreateMqttServer(optionsBuilder.Build());

            await mqttServer.StartAsync();

            return mqttServer;
        }

        private async Task<IMqttClient> StartClient(int port, CancellationToken cancellationToken)
        {
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(LocalAddress, port);

            var mqttClient = new MqttClientFactory().CreateMqttClient();

            await mqttClient.ConnectAsync(optionsBuilder.Build(), cancellationToken);

            return mqttClient;
        }

        private IHost CreateHost(int port, Action<IServiceCollection> configureServices)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.ConfigureMqttClientOptions(cfgBuilder =>
                    {
                        cfgBuilder.WithTcpServer(LocalAddress, port);
                    });

                    services.Configure<MqttLifetimeOptions>(opt =>
                    {
                        opt.AutoReconnectDelay = TimeSpan.FromMilliseconds(100);
                    });

                    services.AddMqtt();
                    configureServices.Invoke(services);
                })
               .Build();
        }

        private class ConsumerBase : IMqttConsumer
        {
            private TaskCompletionSource<MqttApplicationMessage> _taskCompletionSource = new();

            public void Reset() => _taskCompletionSource = new();

            public Task<MqttApplicationMessage> WaitMessageReceivedAsync(CancellationToken cancellationToken) => _taskCompletionSource.Task.WaitAsync(cancellationToken);

            public Task Handle(MqttApplicationMessage message, CancellationToken cancellationToken)
            {
                _taskCompletionSource.SetResult(message);
                return Task.CompletedTask;
            }
        }

        private class Consumer1 : ConsumerBase { }

        private class Consumer2 : ConsumerBase { }

        #endregion

        /// <summary>
        /// Базовая проверка приема сообщения
        /// </summary>
        [Fact]
        public async void TestSimpleConsume()
        {
            // arrange
            var port = 1884;
            var topicName = "mqttditests/testbase";
            var testData = new Fixture().CreateMany<byte>(10).ToArray();
            var testCts = new CancellationTokenSource(TestTimeout);

            using var mqttServer = await StartServer(port);
            using var mqttClient = await StartClient(port, testCts.Token);

            using var host = CreateHost(port, services =>
            {
                services.RegisterMqttConsumerSingleton<Consumer1>(b => b.WithTopic(topicName));
            });

            var consumer = host.Services.GetRequiredService<Consumer1>();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topicName)
                .WithPayload(testData)
                .Build();

            // act
            await host.StartAsync(testCts.Token);
            await mqttClient.PublishAsync(message, testCts.Token);
            var result = await consumer.WaitMessageReceivedAsync(testCts.Token);
            await host.StopAsync(testCts.Token);

            // asserts
            result.Should().NotBeNull();
            result.Payload.ToArray().Should().BeEquivalentTo(testData);
        }

        /// <summary>
        /// Проверка раздельной работы нескольких косьюмеров
        /// </summary>
        [Fact]
        public async void TestMultiConsume()
        {
            // arrange
            var port = 1885;
            var topicName1 = "mqttditests/multiconsumertest1";
            var topicName2 = "mqttditests/multiconsumertest2";
            var testData1 = new Fixture().CreateMany<byte>(10).ToArray();
            var testData2 = new Fixture().CreateMany<byte>(10).ToArray();
            var testCts = new CancellationTokenSource(TestTimeout);

            using var mqttServer = await StartServer(port);
            using var mqttClient = await StartClient(port, testCts.Token);

            using var host = CreateHost(port, services =>
            {
                services.RegisterMqttConsumerSingleton<Consumer1>(b => b.WithTopic(topicName1));
                services.RegisterMqttConsumerSingleton<Consumer2>(b => b.WithTopic(topicName2));
            });

            var consumer1 = host.Services.GetRequiredService<Consumer1>();
            var consumer2 = host.Services.GetRequiredService<Consumer2>();

            var message1 = new MqttApplicationMessageBuilder()
                .WithTopic(topicName1)
                .WithPayload(testData1)
                .Build();

            var message2 = new MqttApplicationMessageBuilder()
                .WithTopic(topicName2)
                .WithPayload(testData2)
                .Build();

            // act
            await host.StartAsync(testCts.Token);
            await mqttClient.PublishAsync(message1, testCts.Token);
            await mqttClient.PublishAsync(message2, testCts.Token);
            var result1 = await consumer1.WaitMessageReceivedAsync(testCts.Token);
            var result2 = await consumer2.WaitMessageReceivedAsync(testCts.Token);
            await host.StopAsync(testCts.Token);

            // asserts
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1.Payload.ToArray().Should().BeEquivalentTo(testData1);
            result2.Payload.ToArray().Should().BeEquivalentTo(testData2);
        }

        /// <summary>
        /// Проверка восстановления соединения после потери связи
        /// </summary>
        [Fact]
        public async void TestReconnect()
        {
            // arrange
            var port = 1886;
            var topicName = "mqttditests/reconnecttest";
            var testData1 = new Fixture().CreateMany<byte>(10).ToArray();
            var testData2 = new Fixture().CreateMany<byte>(10).ToArray();
            var testCts = new CancellationTokenSource(TestTimeout);

            using var mqttServer = await StartServer(port);

            using var host = CreateHost(port, services =>
            {
                services.RegisterMqttConsumerSingleton<Consumer1>(b => b.WithTopic(topicName));
            });

            var consumer = host.Services.GetRequiredService<Consumer1>();

            var message1 = new MqttApplicationMessageBuilder()
                .WithTopic(topicName)
                .WithPayload(testData1)
                .WithRetainFlag(false)
                .Build();

            var message2 = new MqttApplicationMessageBuilder()
                .WithTopic(topicName)
                .WithPayload(testData2)
                .WithRetainFlag(false)
                .Build();

            // act
            await host.StartAsync(testCts.Token);
            using var mqttClient1 = await StartClient(port, testCts.Token);
            await mqttClient1.PublishAsync(message1, testCts.Token);
            var result1 = await consumer.WaitMessageReceivedAsync(testCts.Token);
            await mqttServer.StopAsync();
            consumer.Reset();
            await mqttServer.StartAsync();
            await Task.Delay(200);
            using var mqttClient2 = await StartClient(port, testCts.Token);
            await mqttClient2.PublishAsync(message2, testCts.Token);
            var result2 = await consumer.WaitMessageReceivedAsync(testCts.Token);
            await host.StopAsync(testCts.Token);

            // asserts
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1.Payload.ToArray().Should().BeEquivalentTo(testData1);
            result2.Payload.ToArray().Should().BeEquivalentTo(testData2);
        }
    }
}
