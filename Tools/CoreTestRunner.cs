using System;
using System.IO;
using System.Linq;
using System.Reflection;

internal static class CoreTestRunner
{
    private static string root;
    private static string binaryDirectory;

    private static int Main(string[] args)
    {
        root = args[0];
        binaryDirectory = args.Length > 1
            ? args[1]
            : Path.Combine(root, "Temp", "bin", "Debug");
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        Load("Library/PackageCache/com.unity.ext.nunit@1.0.6/net35/unity-custom/nunit.framework.dll");
        Load("Library/PackageCache/com.unity.nuget.newtonsoft-json@3.2.1/Runtime/Newtonsoft.Json.dll");
        LoadBinary("Mandate.Domain.dll");
        LoadBinary("Mandate.Persistence.dll");
        LoadBinary("Mandate.Simulation.dll");
        var assembly = LoadBinary("Mandate.Domain.Tests.dll");
        var type = assembly.GetType("Mandate.Tests.WorldKernelTests", true);
        var instance = Activator.CreateInstance(type);
        var passed = 0;
        var failed = 0;

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!method.GetCustomAttributes(false).Any(
                    item => item.GetType().FullName == "NUnit.Framework.TestAttribute"))
            {
                continue;
            }

            try
            {
                method.Invoke(instance, null);
                passed++;
                Console.WriteLine("PASS " + method.Name);
            }
            catch (TargetInvocationException exception)
            {
                failed++;
                Console.WriteLine(
                    "FAIL " + method.Name + ": " +
                    (exception.InnerException ?? exception).Message);
            }
        }

        Console.WriteLine("RESULT passed=" + passed + " failed=" + failed);
        return failed == 0 ? 0 : 1;
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        if (name == "netstandard")
        {
            return Assembly.LoadFrom(
                "C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Data/" +
                "NetStandard/ref/2.1.0/netstandard.dll");
        }

        var path = Path.Combine(binaryDirectory, name + ".dll");
        return File.Exists(path) ? Assembly.LoadFrom(path) : null;
    }

    private static Assembly Load(string path)
    {
        return Assembly.LoadFrom(Path.Combine(root, path));
    }

    private static Assembly LoadBinary(string filename)
    {
        return Assembly.LoadFrom(Path.Combine(binaryDirectory, filename));
    }
}
