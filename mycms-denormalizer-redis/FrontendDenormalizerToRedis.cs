using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mycms_shared.Entities;
using mycms_shared.Events;
using mycms_shared.Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace mycms_denormalizer_redis
{
    public class FrontendDenormalizerToRedis : BackgroundService
    {
        private readonly ILogger logger;
        private IConnection connection;
        private IModel channel;
        private IDatabase cache;
        private string queueName;

        public FrontendDenormalizerToRedis(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<FrontendDenormalizerToRedis>();
            this.initRabbitMQ();
            this.initRedis();
        }

        private void initRedis()
        {
            var redisConnString = Environment.GetEnvironmentVariable("REDIS_CONNECTIONSTRING");
            var password = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
            redisConnString = redisConnString.Replace("{password}", password);
            cache = ConnectionMultiplexer.Connect(redisConnString).GetDatabase();
        }

        private void initRabbitMQ()
        {
            var hostName = Environment.GetEnvironmentVariable("RABBIT_HOSTNAME");
            var userName = Environment.GetEnvironmentVariable("RABBIT_USER");
            var password = Environment.GetEnvironmentVariable("RABBIT_PASSWORD");
            var port = Environment.GetEnvironmentVariable("RABBIT_PORT");
            queueName = Environment.GetEnvironmentVariable("FRONTEND_QUEUE_NAME");

            var factory = new ConnectionFactory()
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                Port = int.Parse(port)
            };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: "mycms", type: ExchangeType.Fanout);
            channel.QueueBind(queue: queueName, exchange: "mycms", routingKey: "");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                Process(message);
            };
            channel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);

            return Task.CompletedTask;
        }

        private void Process(string message)
        {
            var articleEvent = JsonSerializer.Deserialize<ArticleCRUDEvent>(message);
            var json = cache.StringGet(typeof(Article).Name);
            var articles = JsonSerializer.Deserialize<IList<Article>>(json);

            switch (articleEvent.Operation)
            {
                case CRUDOperation.CREATE:
                    {
                        articles.Add(articleEvent.Entity);
                    }
                    break;

                case CRUDOperation.UPDATE:
                    {
                        var article = articles.FirstOrDefault(x => x.Id == articleEvent.Entity.Id);
                        if (article != null)
                        {
                            article.Title = articleEvent.Entity.Title;
                            article.Subtitle = articleEvent.Entity.Subtitle;
                            article.Content = articleEvent.Entity.Content;
                            article.Author = articleEvent.Entity.Author;
                        }
                    }
                    break;

                case CRUDOperation.DELETE:
                    {
                        var article = articles.FirstOrDefault(x => x.Id == articleEvent.Entity.Id);
                        if (article != null)
                        {
                            articles.Remove(article);
                        }
                    }
                    break;
            }

            var articlesJson = JsonSerializer.Serialize(articles);
            cache.StringSet(typeof(Article).Name, articlesJson);
        }
    }
}