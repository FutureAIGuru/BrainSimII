using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Diagnostics;
using NetFwTypeLib;

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

        public static bool Confirm(string prompt)
        {
            Console.WriteLine(prompt);
            Console.WriteLine("    Do you want to continue? Y/N");
            ConsoleKeyInfo xx = new ConsoleKeyInfo();
            while (xx.Key != ConsoleKey.Y && xx.Key != ConsoleKey.N)
                xx = Console.ReadKey();
            if (xx.Key.ToString().ToLower() == "n") return false;
            Console.WriteLine();
            return true;
        }

        static void Main(string[] args)
        {
            if (!Confirm("This program will modify your firewall rules to enable using the Brain Simulator II with the NeuronServer")) return;

            if (!IsAdministrator())
            {
                if (!Confirm("Administrative privelage is needed")) return;

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

            if (!Confirm("If the rules already exist, delete them")) return;

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            INetFwRule theRule;
            string[] theName = { "BrainSimulator.exe", "NeuronServer.exe" };
            for (int i = 0; i < theName.Length; i++)
            {
                try
                {
                    theRule = firewallPolicy.Rules.Item(theName[i]);

                    while (theRule != null)
                    {
                        firewallPolicy.Rules.Remove(theName[i]);
                        Console.WriteLine("Deleted rule: " + theName[i]);
                        theRule = firewallPolicy.Rules.Item(theName[i]);
                    }
                }
                catch (Exception e)
                {
                    //get here if the rule doesn't exist
                }
            }

            if (!Confirm("Creating new rules")) return;

            try
            {
                for (int i = 0; i < theName.Length; i++)
                {
                    INetFwRule firewallRule = (INetFwRule)Activator.CreateInstance(
                        Type.GetTypeFromProgID("HNetCfg.FWRule"));
                    firewallRule.ApplicationName = theName[i];
                    firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    firewallRule.Description = "Allow " + theName[i] + " to receive UDP packets";
                    firewallRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN; //or OUT
                    firewallRule.Enabled = true;
                    firewallRule.Protocol = 17;
                    firewallRule.Profiles = 2;
                    firewallRule.InterfaceTypes = "all";
                    firewallRule.Name = theName[i];
                    firewallPolicy.Rules.Add(firewallRule);
                    Console.WriteLine("Created rule: " + theName[i]);

                }

                Console.Write("Success...  press any key");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.Write("Failed because: "+e.Message +"press any key");
                Console.ReadKey();
            }
        }
    }
}
