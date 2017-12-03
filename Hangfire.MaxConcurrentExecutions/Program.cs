using System;
using Microsoft.AspNetCore.Hosting;

namespace Hangfire.MaxConcurrentExecutions
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
