using MQTTnet;
using MQTTnet.Client;
using Serilog;
using StackExchange.Redis;

namespace APP
{
    class Program
    {
        private static ConnectionMultiplexer? redisConnection;
        private static ISubscriber? publisher;
        private static ISubscriber? subscriber;
        private static MqttFactory? mqttFactory;
        private static IMqttClient? mqttClient;
        private const string AmrToAppChannel = "AMRAPP";
        private const string AppToAmrChannel = "APPAMR";
        private const string AppToAcsChannel = "APPACS";
        private const string AcsToAppChannel = "ACSAPP";
        private static string state = "initializing";

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            redisConnection = ConnectionMultiplexer.Connect("localhost");
            subscriber = redisConnection.GetSubscriber();
            publisher = redisConnection.GetSubscriber();
            mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                   .WithTcpServer("localhost")
                   .Build();

            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            init();


            while (true)
            {
                if (Console.ReadLine() == "exit")
                {
                    break;
                }
            }
        }

        static void PubMqttAPP(string cmd)
        {
            var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(AppToAcsChannel)
            .WithPayload(cmd)
            .Build();

            mqttClient?.PublishAsync(applicationMessage, CancellationToken.None);
        }

        static void SubMqttAPP()
        {
            if(mqttClient == null) 
            {
                return;
            }
            
            mqttClient.ApplicationMessageReceivedAsync += ea =>
            {
                Log.Information($"APP subscribe from ACS MQTT: {AcsToAppChannel}" + "--(" + ea.ApplicationMessage.ConvertPayloadToString() + ")");
                var res = ea.ApplicationMessage.ConvertPayloadToString();

                switch(res)
                {
                    case "move":
                        state = "move";
                        PubRedisAPP("move_start");
                        break;
                    case "load":
                        state = "load";
                        PubRedisAPP("load_start");
                        break;
                    case "unload":
                        state = "unload";
                        PubRedisAPP("unload_start");
                        break;
                    case "stop":
                        PubRedisAPP("stop_task");
                        break;
                }
                return Task.CompletedTask;
            };

            var mqttSubscribeOptions = mqttFactory?.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(
                    f =>
                    {
                        f.WithTopic(AcsToAppChannel);
                    })
                .Build();
            mqttClient?.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
        }

        static void PubRedisAPP(string cmd)
        {
            string command = $"{cmd} {DateTime.Now}";
            publisher?.Publish(RedisChannel.Literal(AppToAmrChannel), command);
            Log.Information($"APP published to AMR Redis: {command}");
        }
        static void SubRedisAPP()
        {
            subscriber?.Subscribe(RedisChannel.Literal(AmrToAppChannel), (channel, value) =>
            {
                Log.Information($"APP subscribe form AMR Redis: {channel} --({value})");
                var res = value.ToString(); 

                switch (res.Trim())
                {
                    case "move_done":
                    case "load_done":
                    case "unload_done":
                        state = "idle";
                        break;
                    case "busy":
                        state = "busy";
                        break;
                }
            });
        }

        static void init() {
            SubRedisAPP();
            SubMqttAPP();

            Task.Run(async () =>
            {
                var begin = DateTime.Now;
                while (true)
                {
                    var sec = (DateTime.Now - begin).TotalSeconds;
                    Console.WriteLine($"initializing...({sec})");

                    if (sec > 5)
                    {
                        state = "idle";
                        Console.WriteLine($"initialization done...");
                        Console.WriteLine($"Waiting acs command...");
                        
                        Timer mqttTimer = new Timer(_ => PubMqttAPP(state), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
                        break;
                    }
                    await Task.Delay(1000);
                }
            });
        }
    }
}
