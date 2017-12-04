using System;
using System.Threading.Tasks;
using Hangfire;

namespace SampleApp
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ExampleJob
    {
        [MaximumConcurrentExecutions(3)]
        public void SomeLongRunningActivityAsync(int i)
        {
            Console.WriteLine($"Starting job {i}");

            Task.Delay(TimeSpan.FromSeconds(5)).Wait();

            Console.WriteLine($"..Finished job {i}");
        }
    }
}