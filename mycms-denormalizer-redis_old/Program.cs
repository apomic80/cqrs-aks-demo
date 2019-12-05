using System;
using System.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using mycms_shared.Events;
using mycms_shared.Infrastructure;
using StackExchange.Redis;
using mycms_shared.Entities;
using System.Collections.Generic;

namespace mycms_denormalizer_redis
{
    class Program
    {
        public static void Main(string[] args)
        {
            var hostName = Environment.GetEnvironmentVariable("RABBIT_HOSTNAME");
            var userName = Environment.GetEnvironmentVariable("RABBIT_USER");
            var password = Environment.GetEnvironmentVariable("RABBIT_PASSWORD");
            var port = Environment.GetEnvironmentVariable("RABBIT_PORT");
            var queueName = Environment.GetEnvironmentVariable("FRONTEND_QUEUE_NAME");

            Console.WriteLine(hostName);
            Console.WriteLine(userName);
            Console.WriteLine(password);
            Console.WriteLine(port);
            Console.WriteLine(queueName);

            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = hostName,
                    UserName = userName,
                    Password = password,
                    Port = int.Parse(port)
                };
                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        channel.ExchangeDeclare(exchange: "mycms", type: ExchangeType.Fanout);

                        channel.QueueBind(queue: queueName,
                                          exchange: "mycms",
                                          routingKey: "");

                        Console.WriteLine(" [*] Waiting for messages.");

                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body;
                            var message = Encoding.UTF8.GetString(body);
                            Process(message);
                            Console.WriteLine(" [x] {0}", message);
                        };
                        channel.BasicConsume(queue: queueName,
                                             autoAck: true,
                                             consumer: consumer);

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
            finally
            {
                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }

        public static void Process(string message)
        {
            var articleEvent = JsonSerializer.Deserialize<ArticleCRUDEvent>(message);

            var redisConnString = Environment.GetEnvironmentVariable("REDIS_CONNECTIONSTRING");
            var password = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
            redisConnString = redisConnString.Replace("{password}", password);
            Console.WriteLine(redisConnString);
            IDatabase cache = ConnectionMultiplexer.Connect(redisConnString).GetDatabase();

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
