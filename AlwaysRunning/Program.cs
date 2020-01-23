using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AlwaysRunning
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new AlwaysRunning(), 
            };
            if (Environment.UserInteractive || System.Diagnostics.Debugger.IsAttached)
            {
                // Simulate the services execution
                RunInteractiveServices(ServicesToRun);
            }
            else
            {
                // Normal service execution
                ServiceBase.Run(ServicesToRun);
            }
        }

    static void RunInteractiveServices(ServiceBase[] servicesToRun)
    {
    Console.WriteLine();
    Console.WriteLine("Start the services in interactive mode.");
    Console.WriteLine();

    // Get the method to invoke on each service to start it
    MethodInfo onStartMethod = typeof(ServiceBase).GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);

        // Start services loop
        foreach (ServiceBase service in servicesToRun)
    {
        Console.Write("Starting {0} ... ", service.ServiceName);
        onStartMethod.Invoke(service, new object[] { new string[] { } });
        Console.WriteLine("Started");
    }

    // Waiting the end
    Console.WriteLine();
    Console.WriteLine("Press a key to stop services and finish process...");
    Console.ReadKey();
    Console.WriteLine();

    // Get the method to invoke on each service to stop it
    MethodInfo onStopMethod = typeof(ServiceBase).GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic);

        // Stop loop
        foreach (ServiceBase service in servicesToRun)
    {
        Console.Write("Stopping {0} ... ", service.ServiceName);
        onStopMethod.Invoke(service, null);
        Console.WriteLine("Stopped");
    }

    Console.WriteLine();
    Console.WriteLine("All services are stopped.");

    // Waiting a key press to not return to VS directly
    if (System.Diagnostics.Debugger.IsAttached)
    {
        Console.WriteLine();
        Console.Write("=== Press a key to quit ===");
        Console.ReadKey();
    }
    }
    }
}
