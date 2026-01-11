using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Server;
using Xunit;
using AwtrixHub.Functions.Services;

namespace AwtrixHub.Functions.Tests.Services
{
    public class MqttServiceTests
    {
        // Helper to create configuration
        private static IConfiguration CreateConfiguration(string host, int port, string topicPrefix = "testprefix")
        {
            var inMemory = new Dictionary<string, string?>
            {
                ["MQTT:BrokerUrl"] = host,
                ["MQTT:Port"] = port.ToString(),
                ["MQTT:Username"] = string.Empty,
                ["MQTT:Password"] = string.Empty,
                ["MQTT:UseTls"] = "false",
                ["MQTT:TopicPrefix"] = topicPrefix
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemory)
                .Build();
        }

        [Fact(DisplayName = "PublishAsync publishes message to server")]
        public async Task PublishAsync_PublishesMessage_WhenServerAvailable()
        {
            // Arrange
            const string topicSuffix = "mytopic";
            const string payloadText = "hello-world";
            const string topicPrefix = "testprefix";

            // Use an ephemeral port to avoid conflicts
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var config = CreateConfiguration("127.0.0.1", port, topicPrefix);

            var tcs = new TaskCompletionSource<MqttApplicationMessage?>();

            var serverOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(port)
                .Build();

            var mqttFactory = new MqttFactory();
            var mqttServer = mqttFactory.CreateMqttServer(serverOptions);

            mqttServer.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
            {
                // Signal arrival
                tcs.TrySetResult(e.ApplicationMessage);
                return Task.CompletedTask;
            });

            try
            {
                await mqttServer.StartAsync();

                var sut = new MqttService(config);

                // Act
                var publishTask = sut.PublishAsync(topicSuffix, payloadText);

                // Wait for the message to be received (timeout to avoid hanging tests)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));

                if (completedTask != tcs.Task)
                {
                    // If no message received within timeout, fail the test
                    throw new TimeoutException("Timed out waiting for MQTT message on server.");
                }

                var received = await tcs.Task; // already completed

                // Await the publish task to ensure no exceptions
                await publishTask;

                // Assert
                Assert.NotNull(received);
                var expectedTopic = topicPrefix + "/" + topicSuffix;
                Assert.Equal(expectedTopic, received.Topic);

                var receivedPayload = received.Payload == null ? string.Empty : Encoding.UTF8.GetString(received.Payload);
                Assert.Equal(payloadText, receivedPayload);
            }
            finally
            {
                // Cleanup
                try
                {
                    await mqttServer.StopAsync();
                }
                catch
                {
                    // ignore cleanup errors
                }
            }
        }

        [Fact(DisplayName = "PublishAsync throws when broker not available")]
        public async Task PublishAsync_Throws_WhenServerNotAvailable()
        {
            // Arrange
            // Choose a likely-unused high port (or 0 to pick ephemeral and then don't start server)
            // Here pick an ephemeral port then do NOT start a server on it.
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var config = CreateConfiguration("127.0.0.1", port, "prefix");

            var sut = new MqttService(config);

            // Act & Assert
            // Expect an exception when trying to connect to a non-listening port
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                // Use a short cancellation token to keep the test fast if the library blocks
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var task = sut.PublishAsync("topic", "payload");
                using (cts.Token.Register(() => { /* no-op */ }))
                {
                    await task;
                }
            });
        }
    }
}using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Server;
using Xunit;
using AwtrixHub.Functions.Services;

namespace AwtrixHub.Functions.Tests.Services
{
    public class MqttServiceTests
    {
        // Helper to create configuration
        private static IConfiguration CreateConfiguration(string host, int port, string topicPrefix = "testprefix")
        {
            var inMemory = new Dictionary<string, string?>
            {
                ["MQTT:BrokerUrl"] = host,
                ["MQTT:Port"] = port.ToString(),
                ["MQTT:Username"] = string.Empty,
                ["MQTT:Password"] = string.Empty,
                ["MQTT:UseTls"] = "false",
                ["MQTT:TopicPrefix"] = topicPrefix
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemory)
                .Build();
        }

        [Fact(DisplayName = "PublishAsync publishes message to server")]
        public async Task PublishAsync_PublishesMessage_WhenServerAvailable()
        {
            // Arrange
            const string topicSuffix = "mytopic";
            const string payloadText = "hello-world";
            const string topicPrefix = "testprefix";

            // Use an ephemeral port to avoid conflicts
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var config = CreateConfiguration("127.0.0.1", port, topicPrefix);

            var tcs = new TaskCompletionSource<MqttApplicationMessage?>();

            var serverOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(port)
                .Build();

            var mqttFactory = new MqttFactory();
            var mqttServer = mqttFactory.CreateMqttServer(serverOptions);

            mqttServer.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
            {
                // Signal arrival
                tcs.TrySetResult(e.ApplicationMessage);
                return Task.CompletedTask;
            });

            try
            {
                await mqttServer.StartAsync();

                var sut = new MqttService(config);

                // Act
                var publishTask = sut.PublishAsync(topicSuffix, payloadText);

                // Wait for the message to be received (timeout to avoid hanging tests)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, cts.Token));

                if (completedTask != tcs.Task)
                {
                    // If no message received within timeout, fail the test
                    throw new TimeoutException("Timed out waiting for MQTT message on server.");
                }

                var received = await tcs.Task; // already completed

                // Await the publish task to ensure no exceptions
                await publishTask;

                // Assert
                Assert.NotNull(received);
                var expectedTopic = topicPrefix + "/" + topicSuffix;
                Assert.Equal(expectedTopic, received.Topic);

                var receivedPayload = received.Payload == null ? string.Empty : Encoding.UTF8.GetString(received.Payload);
                Assert.Equal(payloadText, receivedPayload);
            }
            finally
            {
                // Cleanup
                try
                {
                    await mqttServer.StopAsync();
                }
                catch
                {
                    // ignore cleanup errors
                }
            }
        }

        [Fact(DisplayName = "PublishAsync throws when broker not available")]
        public async Task PublishAsync_Throws_WhenServerNotAvailable()
        {
            // Arrange
            // Choose a likely-unused high port (or 0 to pick ephemeral and then don't start server)
            // Here pick an ephemeral port then do NOT start a server on it.
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var config = CreateConfiguration("127.0.0.1", port, "prefix");

            var sut = new MqttService(config);

            // Act & Assert
            // Expect an exception when trying to connect to a non-listening port
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                // Use a short cancellation token to keep the test fast if the library blocks
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var task = sut.PublishAsync("topic", "payload");
                using (cts.Token.Register(() => { /* no-op */ }))
                {
                    await task;
                }
            });
        }
    }
}