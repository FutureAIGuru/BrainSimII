using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace GetVersionInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            string theExeFile = args[0];
            //you can do this instead...
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(theExeFile);
            //pre- .net6.0:
            //Version v = AssemblyName.GetAssemblyName(theExeFile).Version;
            Console.WriteLine(fileVersionInfo.FileVersion.ToString());
        }
    }
}
