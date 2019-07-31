﻿using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.ServiceProcess;
using System.Threading;
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
            //    var config = ConfigurationManager.GetSection("MonitoredApps") as KeyValueConfigurationCollection;
            //    foreach (var key in config.AllKeys)
            //    {
            //        AliveCheckJob.RegisterApplication(config[key]);
            //    }
            AliveCheckJob.RegisterApplication(new AppInfo(){Path = @"C:\Program Files\Java\jre1.8.0_201\bin\java.exe -jar C:\TranslatorDist\SecurityClientGUI.jar", WorkSpace = @"C:\TranslatorDist\" });
            }

        private async Task SetupQuartz()
        {
            await InitQuartz();
            await ScheduleAliveChecks();
            await Scheduler.Start();
        }

        private async Task ScheduleAliveChecks()
        {
            IJobDetail job = JobBuilder.Create<AliveCheckJob>()
                .Build();
            ITrigger trigger = TriggerBuilder.Create()
                .WithSimpleSchedule(builder =>
                    builder.WithInterval(TimeSpan.FromSeconds(60))
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

    public class AppInfo
    {
        public string Path { get; set; }
        public string WorkSpace { get; set; }
    }

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
        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(string commandLine)
        {
            int argc;
            var argv = CommandLineToArgvW(commandLine, out argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }
        private void ExtractProcessNameAndArguments(string applicationInfo, out string childProcName, out string arguments)
        {
            var commandLineToArgs = CommandLineToArgs(applicationInfo);
            
            childProcName = commandLineToArgs[0].Replace("\"","");
            arguments = string.Join(" ", commandLineToArgs.Skip(1));
        }

        private bool IsProcessRunning(int processId)
        {
            return !Process.GetProcesses().FirstOrDefault(t => t.Id == processId)?.HasExited ?? false;
        }
    }
    // <summary>
    /// Class that allows running applications with full admin rights. In
    /// addition the application launched will bypass the Vista UAC prompt.
    /// </summary>
    public class ApplicationLoader
    {
        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public String lpReserved;
            public String lpDesktop;
            public String lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        #endregion

        #region Enumerations

        enum TOKEN_TYPE : int
        {
            TokenPrimary = 1,
            TokenImpersonation = 2
        }

        enum SECURITY_IMPERSONATION_LEVEL : int
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3,
        }

        #endregion

        #region Constants

        public const int TOKEN_DUPLICATE = 0x0002;
        public const uint MAXIMUM_ALLOWED = 0x2000000;
        public const int CREATE_NEW_CONSOLE = 0x00000010;

        public const int IDLE_PRIORITY_CLASS = 0x40;
        public const int NORMAL_PRIORITY_CLASS = 0x20;
        public const int HIGH_PRIORITY_CLASS = 0x80;
        public const int REALTIME_PRIORITY_CLASS = 0x100;

        #endregion

        #region Win32 API Imports

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public extern static bool CreateProcessAsUser(IntPtr hToken, String lpApplicationName, String lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvironment,
            String lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        public extern static bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess,
            ref SECURITY_ATTRIBUTES lpThreadAttributes, int TokenType,
            int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("advapi32", SetLastError = true), SuppressUnmanagedCodeSecurity]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

        #endregion

        #region MyRegion

        [DllImport("wtsapi32.dll")]
        public static extern bool WTSEnumerateSessions(
            IntPtr hServer,
            uint reserved,
            uint version,
            out IntPtr ppSessionInfo,
            out uint pCount);

        [DllImport("wtsapi32.dll")]
        public static extern bool WTSQuerySessionInformation(
            IntPtr hServer,
            uint sessionId,
            WTS_INFO_CLASS wtsInfoClass,
            out IntPtr ppBuffer,
            out uint iBytesReturned);

        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_CLIENT_ADDRESS
        {
            public AddressFamilyType AddressFamily;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Address;
        }

        public enum AddressFamilyType
        {
            AF_INET,
            AF_INET6,
            AF_IPX,
            AF_NETBIOS,
            AF_UNSPEC
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public uint SessionID;
            [MarshalAs(UnmanagedType.LPStr)]
            public string WinStationName;
            public WTS_CONNECTSTATE_CLASS State;
        }

        public enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        public enum WTS_INFO_CLASS
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType,
            WTSIdleTime,
            WTSLogonTime,
            WTSIncomingBytes,
            WTSOutgoingBytes,
            WTSIncomingFrames,
            WTSOutgoingFrames,
            WTSClientInfo,
            WTSSessionInfo,
            WTSConfigInfo,
            WTSValidationInfo,
            WTSSessionAddressV4,
            WTSIsRemoteSession
        }

    #endregion
    /// <summary>
    /// Launches the given application with full admin rights, and in addition bypasses the Vista UAC prompt
    /// </summary>
    /// <param name="appInfo">The name of the application to launch</param>
    /// <param name="procInfo">Process information regarding the launched application that gets returned to the caller</param>
    /// <returns></returns>
    public static bool StartProcessAndBypassUAC(AppInfo appInfo, out PROCESS_INFORMATION procInfo)
        {
            uint winlogonPid = 0;
            IntPtr hUserTokenDup = IntPtr.Zero, hPToken = IntPtr.Zero, hProcess = IntPtr.Zero;
            procInfo = new PROCESS_INFORMATION();

            // obtain the currently active session id; every logged on user in the system has a unique session id
            uint dwSessionId = GetCurrentSession();
            // obtain the process id of the winlogon process that is running within the currently active session
            Process[] processes = Process.GetProcessesByName("winlogon");
            foreach (Process p in processes)
            {
                if ((uint)p.SessionId == dwSessionId)
                {
                    winlogonPid = (uint)p.Id;
                }
            }

            // obtain a handle to the winlogon process
            hProcess = OpenProcess(MAXIMUM_ALLOWED, false, winlogonPid);

            // obtain a handle to the access token of the winlogon process
            if (!OpenProcessToken(hProcess, TOKEN_DUPLICATE, ref hPToken))
            {
                CloseHandle(hProcess);
                return false;
            }

            // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser
            // I would prefer to not have to use a security attribute variable and to just 
            // simply pass null and inherit (by default) the security attributes
            // of the existing token. However, in C# structures are value types and therefore
            // cannot be assigned the null value.
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);

            // copy the access token of the winlogon process; the newly created token will be a primary token
            if (!DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, ref sa, (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int)TOKEN_TYPE.TokenPrimary, ref hUserTokenDup))
            {
                CloseHandle(hProcess);
                CloseHandle(hPToken);
                return false;
            }

            // By default CreateProcessAsUser creates a process on a non-interactive window station, meaning
            // the window station has a desktop that is invisible and the process is incapable of receiving
            // user input. To remedy this we set the lpDesktop parameter to indicate we want to enable user 
            // interaction with the new process.
            STARTUPINFO si = new STARTUPINFO();
            si.cb = (int)Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default"; // interactive window station parameter; basically this indicates that the process created can display a GUI on the desktop

            // flags that specify the priority and creation method of the process
            int dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;

            // create a new process in the current user's logon session
            bool result = CreateProcessAsUser(hUserTokenDup,        // client's access token
                                            null,                   // file to execute
                                            appInfo.Path,        // command line
                                            ref sa,                 // pointer to process SECURITY_ATTRIBUTES
                                            ref sa,                 // pointer to thread SECURITY_ATTRIBUTES
                                            false,                  // handles are not inheritable
                                            dwCreationFlags,        // creation flags
                                            IntPtr.Zero,            // pointer to new environment block 
                                            appInfo.WorkSpace,                   // name of current directory 
                                            ref si,                 // pointer to STARTUPINFO structure
                                            out procInfo            // receives information about new process
                                            );

            // invalidate the handles
            CloseHandle(hProcess);
            CloseHandle(hPToken);
            CloseHandle(hUserTokenDup);

            return result; // return the result
        }

    private static uint GetCurrentSession()
    {
        var WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;
        IntPtr ppSessionInfo;
        uint pCount;
        if (!WTSEnumerateSessions(
            WTS_CURRENT_SERVER_HANDLE,
            0,
            1,
            out ppSessionInfo,
            out pCount))
        {
                return WTSGetActiveConsoleSessionId();
        }

        var iDataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
        var current = new IntPtr(ppSessionInfo.ToInt32());
        for (var i = 0; i < pCount; i++)
        {
            var sessionInfo = (WTS_SESSION_INFO)Marshal.PtrToStructure(current, typeof(WTS_SESSION_INFO));
            //shift pointer value by the size of struct because of array
            current = current + iDataSize;
            if (sessionInfo.State==WTS_CONNECTSTATE_CLASS.WTSActive)
            {
                return sessionInfo.SessionID;
            }
        }
            return WTSGetActiveConsoleSessionId();
        }
    }
}
