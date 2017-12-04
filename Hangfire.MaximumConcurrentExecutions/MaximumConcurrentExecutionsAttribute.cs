using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Hangfire.MaximumConcurrentExecutionsInfrastructure;

namespace Hangfire
{
    public class MaximumConcurrentExecutionsAttribute : JobFilterAttribute, IServerFilter
    {
        // keep a local cache of the locks we already have so we can skip those we know about in this process
        // this saves catching DistributedLockTimeoutException for resources that are already locked in this process 
        private readonly ConcurrentDictionary<string, byte> locallyAcquiredLocks =
            new ConcurrentDictionary<string, byte>();

        private readonly int maxConcurrentJobs;
        private readonly int pollingIntervalInSeconds;
        private readonly int timeoutInSeconds;

        /// <summary>
        ///     Job execution will be blocked if more than maxConcurrentJobs are running at the same time
        /// </summary>
        /// <param name="maxConcurrentJobs">The number of jobs allowed to run in parallel</param>
        /// <param name="timeoutInSeconds">throw DistributedLockTimeoutException if job cannot be started before this timeout</param>
        /// <param name="pollingIntervalInSeconds">
        ///     How long to pause between cycles of checking all the available concurrent job locks.
        ///     If zero is passed locks will be continously checked in a tight loop but expect higher CPU usage
        /// </param>
        public MaximumConcurrentExecutionsAttribute(int maxConcurrentJobs, int timeoutInSeconds = 60,
            int pollingIntervalInSeconds = 3)
        {
            if (maxConcurrentJobs <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrentJobs));
            if (pollingIntervalInSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(pollingIntervalInSeconds));
            if (timeoutInSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeoutInSeconds));
            this.maxConcurrentJobs = maxConcurrentJobs;
            this.timeoutInSeconds = timeoutInSeconds;
            this.pollingIntervalInSeconds = pollingIntervalInSeconds;
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            // use a set of locks 1,2...n to limit concurrent jobs
            // try and get the first available one, looping round all locks until we acquire one or timeout
            // try each lock at least once
            // if unsucessfull to aquire before timeout then throw the DistributedLockTimeoutException

            var resourceSetName = GetResource(filterContext.BackgroundJob.Job);
            var timeout = TimeSpan.FromSeconds(timeoutInSeconds);
            var timeoutPerLock = TimeSpan.FromSeconds(0);
            var start = Stopwatch.StartNew();

            do
            {
                for (var i = 1; i <= maxConcurrentJobs; i++) //non zero based - 1,2,..maxConcurrentJobs
                {
                    // name the locks sequentially e.g. Namespace.Method-1/3, Namespace.Method-2/3,Namespace.Method-3/3
                    var resourceName = $"{resourceSetName}-{i}/{maxConcurrentJobs}";

                    try
                    {
                        // Try and get a local lock for this resource. If that fails no need to try to get the distributed lock
                        if (locallyAcquiredLocks.TryAdd(resourceName, byte.MinValue))
                        {
                            // try and get the distrubuted lock
                            // throw timeout immediately if not available so we can try the next lock
                            var distributedLock =
                                filterContext.Connection.AcquireDistributedLock(resourceName, timeoutPerLock);

                            // Got it! Store the lock so we can dispose of it later
                            filterContext.Items["DistributedLock"] = distributedLock;
                            filterContext.Items["DistributedLockName"] = resourceName;

                            return;
                        }
                    }
                    catch (DistributedLockTimeoutException)
                    {
                        // we could not get this lock, but there are n others to try so we swallow the exception from storage
                        // we will throw later if all locks could not be aquired before timeout

                        // we failed to get the distributed lock so we should remove the corresponding local lock
                        locallyAcquiredLocks.TryRemove(resourceName, out _);
                    }
                }

                // We have tried every lock once and failed so all possible jobs are in progress. 
                // Try again after a delay otherwsie we needlessly max CPU. Sensible delay would be 1/2 of the average job duration
                if (pollingIntervalInSeconds > 0)
                    Task.Delay(TimeSpan.FromSeconds(pollingIntervalInSeconds)).Wait(timeout);

            } while (start.Elapsed < timeout);

            //timed out without acquiring a lock
            throw new DistributedLockTimeoutException(resourceSetName);
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            if (!filterContext.Items.ContainsKey("DistributedLock"))
                throw new InvalidOperationException("Can not release a distributed lock: it was not acquired.");

            // be sure to dispose the distributed lock when the job is done 
            var distributedLock = (IDisposable) filterContext.Items["DistributedLock"];
            distributedLock.Dispose();

            // and also release the lock from the local lock cache
            var distributedLockName = (string) filterContext.Items["DistributedLockName"];
            locallyAcquiredLocks.TryRemove(distributedLockName, out _);
        }

        private static string GetResource(Job job)
        {
            return $"{job.Type.ToGenericTypeString()}.{job.Method.Name}";
        }
    }
}