using Confluent.Kafka;
using System.Net;
using System.Text.Json;
using static Confluent.Kafka.ConfigPropertyNames;
using training_service_db.Models;
using training_service_db.Services;

namespace training_service_db
{
    public class BrokerService
    {
        private readonly string broker = "localhost:9092";

        private readonly string topic = "traning-topic";

        private readonly string groupId = "traning-group";

        private CoachService _service;

        public BrokerService(CoachService service) {
            service = _service;
        }

        public async Task<bool> SendMessage(ProducerMessage coachRequest)
        {
            string message = JsonSerializer.Serialize(coachRequest);

            ProducerConfig config = new ProducerConfig
            {
                BootstrapServers = broker,
                ClientId = Dns.GetHostName()
            };

            var producer = new ProducerBuilder<Null, string>(config).Build();
            producer.ProduceAsync(topic, new Message<Null, string> { Value = message });

            return await Task.FromResult(true);
        }

        public Task РrocessMessage(CancellationToken cancellationToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = broker,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var consumerBuilder = new ConsumerBuilder<Null, string>(config).Build();
            consumerBuilder.Subscribe(topic);
            try
            {
                while (true)
                {
                    var consumer = consumerBuilder.Consume(cancellationToken);
                    var message = JsonSerializer.Deserialize<ConsumerMessage>(consumer.Message.Value);
                    if (message != null) {
                        var coach = new CoachMessageInput
                        {
                            UserId = message.UserId,
                            TimeConfirm = message.TimeConfirm
                        };
                        _service.UpdateAsync(message.CoachId, coach);
                    }

                }
            }
            catch (OperationCanceledException)
            {
                consumerBuilder.Close();
            }
            return Task.CompletedTask;
        }

    }
}
