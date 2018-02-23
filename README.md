Hangfire.MaximumConcurrentExecutions
====================================

[![GitHub issues](https://img.shields.io/github/issues/alastairtree/Hangfire.MaximumConcurrentExecutions.svg)](https://github.com/alastairtree/Hangfire.MaximumConcurrentExecutions/issues)
[![GitHub stars](https://img.shields.io/github/stars/alastairtree/Hangfire.MaximumConcurrentExecutions.svg)](https://github.com/alastairtree/Hangfire.MaximumConcurrentExecutions/stargazers)
[![NuGet Badge](https://buildstats.info/nuget/Hangfire.MaximumConcurrentExecutions)](https://www.nuget.org/packages/Hangfire.MaximumConcurrentExecutions)


Extends [hangfire.io](https://github.com/HangfireIO/Hangfire) with an new MaximumConcurrentExecutions job filter attribute to allow you to 
easily throttle the number of a given job running at the same time. Uses a distributed lock so works even under multi node environments

For example 
* Job A could have a maximum of 3 executions running by decorating it with `[MaximumConcurrentExecutions(3)]` attribute
* Job B could have a maximum of 10 executions running by decorating it with `[MaximumConcurrentExecutions(10)]` attribute
* Job C could have a maximum of 20 executions because there are 20 workers by default
* Job D could have a maximum of 1 executions running by decorating it with `[DisableConcurrentExecution]` attribute (from Hangfire.Core)

## Getting started

If not already, setup hangfire. See http://docs.hangfire.io/en/latest/quick-start.html

Next install the attribute from nuget - type the following at a package manager console

```powershell
PM> Install-Package Hangfire.MaximumConcurrentExecutions
```

Decoreate your job with the attribute

```csharp
public class ExampleJob
{
    [MaximumConcurrentExecutions(3)]
    public void SomeLongRunningActivity()
    {
        Console.WriteLine("Starting job");
        Task.Delay(TimeSpan.FromSeconds(5)).Wait();
    }
}
```

And enqueue your job

```csharp
BackgroundJob.Enqueue<ExampleJob>(job => job.SomeLongRunningActivity());
```

## Sample application

See [the SampleApp](/SampleApp) for a complete application using MaximumConcurrentExecutions

## Advanced config

### Setting the timeout

When using the MaximumConcurrentExecutions attribute every available worker will still pickup new jobs and place
them into the in-progress state, however they will not start if there are more than maxConcurrentJobs already 
running. By default the job will throw an exception after 60 seconds if no free distibuted locks become available
by other workers finishing the same job. To change this set the `timeoutInSeconds` argument, e.g. for 2 minutes:

```csharp
public class ExampleJob
{
    [MaximumConcurrentExecutions(3,timeoutInSeconds = 120)]
    public void SomeLongRunningActivity()
    {
	...
```
### Setting the polling interval

Once all available locks are taken and the maximum number of jobs are running at the same time, we must poll 
the storage to check for released locks. As this can consume unnecessary resources you may want to configure 
how often we should retry all the availble locks. If your jobs take 2 minutes to run you should skip polling 
more than every minute:

```csharp
public class ExampleJob
{
    [MaximumConcurrentExecutions(3, timeoutInSeconds = 120, pollingIntervalInSeconds = 60)]
    public void SomeLongRunningActivity()
    {
	...
```

### Known issues
Because this implementation relies on polling storage there is no fair allocation of locks once they 
are all consumed. If the maximum job locks have been taken, there is no guarantee that the next job will get 
the first available lock.
