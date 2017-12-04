using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Hosting;

namespace SampleApp
{
    internal static class Program
    {
        public static void Main()
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            // once everything is running queue up 20 jobs
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(2000);
                foreach (var i in Enumerable.Range(0, 20))
                    BackgroundJob.Enqueue<ExampleJob>(job => job.SomeLongRunningActivityAsync(i));
            });

            host.Run();
        }
    }
}