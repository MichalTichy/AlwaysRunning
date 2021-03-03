using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl.Matchers;

namespace AlwaysRunning
{
    [DisallowConcurrentExecution]
    internal class AliveCheckJob : IJob
    {

        public Task Execute(IJobExecutionContext context)
        {
            var jobDetailKey = context.JobDetail.Key;
            var rnd = new Random();
            var applicationRegister = (ApplicationRegister)context.Scheduler.Context.Get(nameof(ApplicationRegister));
            var notRunning = applicationRegister.Where(t => t.ProcessId <= 0 || !IsProcessRunning(t.ProcessId));
            foreach (var applicationInfo in notRunning)
            {
                ApplicationLoader.PROCESS_INFORMATION processInfo;
                ApplicationLoader.StartProcessAndBypassUAC(applicationInfo, out processInfo);
                var processId = (int)processInfo.dwProcessId;
                applicationInfo.ProcessId = processId;

                if (applicationInfo.TimeBetweenRestartsInMinutes != null)
                {
                    applicationInfo.NextRestart = DateTime.Now.AddMinutes(applicationInfo.TimeBetweenRestartsInMinutes.Value);

                    if (applicationInfo.RestartVarianceInMinutes.HasValue)
                    {
                        applicationInfo.NextRestart = applicationInfo.NextRestart.Value.AddMinutes(rnd.Next(-applicationInfo.RestartVarianceInMinutes.Value, applicationInfo.RestartVarianceInMinutes.Value));
                    }
                }
            }

            return Task.FromResult(false);
        }

        private bool IsProcessRunning(int processId)
        {
            return !Process.GetProcesses().FirstOrDefault(t => t.Id == processId)?.HasExited ?? false;
        }
    }
}