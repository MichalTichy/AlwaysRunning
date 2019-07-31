using Quartz;
using Quartz.Impl;
using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace AlwaysRunning
{
    public partial class AlwaysRunning : ServiceBase
    {
        public AlwaysRunning()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
#if DEBUG
            Debugger.Launch();
#endif
            Start();
        }

        public void Start()
        {
            SetupQuartz().GetAwaiter().GetResult();

            RegisterApps();
        }

        private static void RegisterApps()
        {
            AppInfoConfigurationSection serviceConfigSection =
                ConfigurationManager.GetSection("AppsSection") as AppInfoConfigurationSection;

            foreach (AppInfo appInfo in serviceConfigSection.Apps)
            {
                AliveCheckJob.RegisterApplication(appInfo);
            }
        }

        private async Task SetupQuartz()
        {
            await InitQuartz();
            await ScheduleAliveChecks();
            await Scheduler.Start();
        }

        private async Task ScheduleAliveChecks()
        {
            int interval = Convert.ToInt32(ConfigurationManager.AppSettings["SecondsInBetweenIsAliveCheck"]);

            IJobDetail job = JobBuilder.Create<AliveCheckJob>()
                .Build();
            ITrigger trigger = TriggerBuilder.Create()
                .WithSimpleSchedule(builder =>
                    builder.WithInterval(TimeSpan.FromSeconds(interval))
                        .RepeatForever()
                        .WithMisfireHandlingInstructionIgnoreMisfires())
                .StartNow()
                .Build();
            await Scheduler.ScheduleJob(job, trigger);
        }

        private async Task InitQuartz()
        {

            ISchedulerFactory schedFact = new StdSchedulerFactory();
            Scheduler = await schedFact.GetScheduler();
        }

        public IScheduler Scheduler { get; set; }

        protected override void OnStop()
        {
            Scheduler.Shutdown().GetAwaiter().GetResult();
        }
    }
}
