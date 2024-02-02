using Serilog;
using StackExchange.Redis;

namespace AMR
{
    class Program
    {
        private static ConnectionMultiplexer? redisConnection;
        private static ISubscriber? publisher;
        private static ISubscriber? subscriber;
        private const string AmrToAppChannel = "AMRAPP";
        private const string AppToAmrChannel = "APPAMR";
        private static string AmrStatus = "idle";
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
                
            
            redisConnection = ConnectionMultiplexer.Connect("localhost");
            publisher = redisConnection.GetSubscriber();
            subscriber = redisConnection.GetSubscriber();

            SubRedisAMR();
    
            Console.WriteLine("Amr ready...");

            while (true)
            {   
                if(Console.ReadLine() == "exit")
                {
                    break;
                }
            }   
        }

        static void PubRedis(string cmd)
        {
            publisher?.Publish(RedisChannel.Literal(AmrToAppChannel), cmd);
            Log.Information($"AMR publish to APP Redis:{AmrToAppChannel} {cmd}");
        }

        static void SubRedisAMR()
        {
            subscriber?.Subscribe(RedisChannel.Literal(AppToAmrChannel), (channel, value) =>
            {
                Log.Information($"AMR subscribe from APP Redis: {channel} --({value})");
                string command = value.ToString().Split(" ")[0];

                if (AmrStatus == "idle")
                {
                    switch (command)
                    {
                        case "move_start":
                            Task.Run(async () =>
                            {
                                AmrStatus = "move";
                                await Task.Delay(10000);                            
                                PubRedis("move_done");                               
                                AmrStatus = "idle";
                            });
                            break;
                        case "load_start":
                            Task.Run(async () =>
                            {
                                AmrStatus = "load";
                                await Task.Delay(5000);
                                PubRedis("load_done");
                                AmrStatus = "idle";
                            });

                            break;
                        case "unload_start":
                            Task.Run(async () =>
                            {
                                AmrStatus = "unload";
                                await Task.Delay(5000);
                                PubRedis("unload_done");
                                AmrStatus = "idle";
                            });
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Amr is busy...");
                    PubRedis("busy");
                    Log.Warning("AMR is busy...");
                }
            });
        }
    }
}
