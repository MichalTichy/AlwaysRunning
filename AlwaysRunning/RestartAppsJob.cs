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
    internal class RestartAppsJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {

            var applicationRegister = (ApplicationRegister)context.Scheduler.Context.Get(nameof(ApplicationRegister));
            foreach (var appInfo in applicationRegister.Where(t => t.ProcessId > 0 && t.NextRestart.HasValue && t.NextRestart.Value <= DateTime.Now))
            {
                var process = Process.GetProcessById(appInfo.ProcessId);
                process?.Kill();
                appInfo.NextRestart = null;
            }
            return Task.FromResult(false);

        }
    }
}