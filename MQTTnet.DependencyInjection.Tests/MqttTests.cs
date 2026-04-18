using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Server;
using NSubstitute;
using System.Buffers;

namespace MQTTnet.DependencyInjection.Tests
{
    public class MqttTests
    {
        private const string LocalAddress = "127.0.0.1";
        private readonly TimeSpan TestTimeout = System.Diagnostics.Debugger.IsAttached ? TimeSpan.FromMinutes(10) : TimeSpan.FromSeconds(2);


        /*
         * стоит протестировать
         * использование логгера
         * перезапуск сервера
         * подписка на сообщения с фильтрами
         * отправка сообщений
         * разные консьюмеры
         */

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

                    services.AddMqtt();
                    configureServices.Invoke(services);
                })
               .Build();
        }

        private IMqttConsumer CreateConsumer(out TaskCompletionSource<MqttApplicationMessage> consumerTcs)
        {
            var consumer = Substitute.For<IMqttConsumer>();
            var tsc = new TaskCompletionSource<MqttApplicationMessage>();
            consumer.Handle(Arg.Any<MqttApplicationMessage>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask)
                .AndDoes(ci => tsc.SetResult(ci.Arg<MqttApplicationMessage>()));

            consumerTcs = tsc;
            return consumer;
        }

        #endregion

        [Fact]
        public async void TestSimpleConsume()
        {
            // arrange
            var port = 1884;
            var topicName = "mqttditests/testbase";
            var testData = new Fixture().CreateMany<byte>(10).ToArray();
            var testCts = new CancellationTokenSource(TestTimeout);

            var consumer = CreateConsumer(out var consumerTcs);
            using var mqttServer = await StartServer(port);
            using var mqttClient = await StartClient(port, testCts.Token);

            using var host = CreateHost(port, services =>
            {
                services.RegisterMqttConsumer(_ => consumer, new MqttTopicFilterBuilder().WithTopic(topicName).Build());
            });

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topicName)
                .WithPayload(testData)
                .Build();

            // act
            await host.StartAsync(testCts.Token);
            await mqttClient.PublishAsync(message, testCts.Token);
            var result = await consumerTcs.Task.WaitAsync(testCts.Token);
            await host.StopAsync(testCts.Token);

            // asserts
            result.Should().NotBeNull();
            result.Payload.ToArray().Should().BeEquivalentTo(testData);
        }

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

            var consumer1 = CreateConsumer(out var consumerTcs1);
            var consumer2 = CreateConsumer(out var consumerTcs2);

            using var mqttServer = await StartServer(port);
            using var mqttClient = await StartClient(port, testCts.Token);

            using var host = CreateHost(port, services =>
            {
                services.RegisterMqttConsumer(_ => consumer1, new MqttTopicFilterBuilder().WithTopic(topicName1).Build());
                services.RegisterMqttConsumer(_ => consumer2, new MqttTopicFilterBuilder().WithTopic(topicName2).Build());
            });

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
            var result1 = await consumerTcs1.Task.WaitAsync(testCts.Token);
            var result2 = await consumerTcs2.Task.WaitAsync(testCts.Token);
            await host.StopAsync(testCts.Token);

            // asserts
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1.Payload.ToArray().Should().BeEquivalentTo(testData1);
            result2.Payload.ToArray().Should().BeEquivalentTo(testData2);
        }
    }
}
