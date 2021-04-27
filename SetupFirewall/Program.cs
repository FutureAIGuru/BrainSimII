using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Diagnostics;

namespace SetupFirewall
{
    class Program
    {
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("This program will modify your firewall rules to enable using the Brain Simulator II with the NeuronServer");
            Console.WriteLine("Do you want to continue? Y/N");
            ConsoleKeyInfo xx = new ConsoleKeyInfo();
            while (xx.Key != ConsoleKey.Y && xx.Key != ConsoleKey.N)
                xx = Console.ReadKey();
            if (xx.Key.ToString().ToLower() == "n") return;

            if (!IsAdministrator())
            {
                var proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = System.Reflection.Assembly.GetExecutingAssembly().Location;// Application.ExecutablePath;
                proc.Verb = "runas";

                try
                {
                    Process.Start(proc);
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to elevate.");
                    return;
                }
                return;
            }
            ////test of firewall rule addition
            //INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
            //    Type.GetTypeFromProgID("HNetCfg.FWRule"));

            //INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
            //    Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            //string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //firewallRule.ApplicationName = "//App Executable Path";
            //firewallRule.ApplicationName = "//NeuronServer";

            //firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            //firewallRule.Description = "Allow Neuron Server Test";
            //firewallRule.Enabled = true;
            //firewallRule.InterfaceTypes = "All";
            //firewallRule.Name = $"// NeuronServer";
            //firewallPolicy.Rules.Add(firewallRule);



        }
    }
}
