﻿using RabbitMQ.Client;
using ReportingService.Api.Domain.Entities.KeyVault;
using ReportingService.Api.Helpers.Base;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ReportingService.Api.Helpers
{
    public class MessageQueueHelper : BaseHelper
    {
        public async Task QueueMessageAsync<T>(T model, string queue, KeyVaultConnectionInfo keyVaultConnection)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.UserName = ApplicationSettings.RabbitMQUsername;
            factory.Password = ApplicationSettings.RabbitMQPassword;
            factory.HostName = ApplicationSettings.RabbitMQHostname;
            factory.Port = ApplicationSettings.RabbitMQPort;

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            var encrypted = string.Empty;

            using (KeyVaultHelper keyVaultHelper = new KeyVaultHelper(keyVaultConnection))
            {
                string secret = await keyVaultHelper.GetVaultKeyAsync(ApplicationSettings.KeyVaultEncryptionKey);
                encrypted = NETCore.Encrypt.EncryptProvider.AESEncrypt(json, secret);
            }

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queue,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                string message = encrypted;
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                    routingKey: queue,
                                    basicProperties: properties,
                                    body: body);

                Console.WriteLine("Sent: {0}", message);
            }
        }
    }
}