using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Quartz;

namespace AlwaysRunning
{
    [DisallowConcurrentExecution]
    internal class AliveCheckJob : IJob
    {
        static Dictionary<AppInfo, int> RegisteredApplications = new Dictionary<AppInfo, int>();

        public static void RegisterApplication(AppInfo appInfo)
        {

            if (!RegisteredApplications.ContainsKey(appInfo))
            {
                RegisteredApplications.Add(appInfo, 0);
            }
        }

        public Task Execute(IJobExecutionContext context)
        {
            var notRunning = RegisteredApplications.Where(t => t.Value == 0 || !IsProcessRunning(t.Value));
            foreach (var applicationInfo in notRunning.Select(t => t.Key))
            {

                ApplicationLoader.PROCESS_INFORMATION processInfo;
                ApplicationLoader.StartProcessAndBypassUAC(applicationInfo, out processInfo);
                var processId = (int)processInfo.dwProcessId;
                RegisteredApplications[applicationInfo] = processId;
            }

            return Task.FromResult(false);
        }

        private bool IsProcessRunning(int processId)
        {
            return !Process.GetProcesses().FirstOrDefault(t => t.Id == processId)?.HasExited ?? false;
        }
    }
}