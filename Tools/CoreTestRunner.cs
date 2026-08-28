using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

internal static class CoreTestRunner
{
    private static string root;
    private static string binaryDirectory;

    private static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FATAL CoreTestRunner: " + exception);
            return 3;
        }
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine(
                "Usage: CoreTestRunner <root> [binary-directory] " +
                "[substring-filter|exclude:substring|batch-exclude:substring;start;count|" +
                "exact:name1;name2|--list]");
            return 2;
        }

        root = args[0];
        binaryDirectory = args.Length > 1
            ? args[1]
            : Path.Combine(root, "Temp", "bin", "Debug");
        var filter = args.Length > 2 ? args[2] : string.Empty;
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        Load("Library/PackageCache/com.unity.ext.nunit@1.0.6/net35/unity-custom/nunit.framework.dll");
        Load("Library/PackageCache/com.unity.nuget.newtonsoft-json@3.2.1/Runtime/Newtonsoft.Json.dll");
        LoadBinary("Mandate.Domain.dll");
        LoadBinary("Mandate.Persistence.dll");
        LoadBinary("Mandate.Simulation.dll");
        var assembly = LoadBinary("Mandate.Domain.Tests.dll");
        var type = assembly.GetType("Mandate.Tests.WorldKernelTests", true);
        var instance = Activator.CreateInstance(type);
        var methods = type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.GetCustomAttributes(false).Any(
                item => item.GetType().FullName ==
                    "NUnit.Framework.TestAttribute"))
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .ToArray();

        if (string.Equals(filter, "--list", StringComparison.Ordinal))
        {
            foreach (var method in methods)
            {
                Console.WriteLine("TEST " + method.Name);
            }

            Console.WriteLine("RESULT listed=" + methods.Length);
            return methods.Length == 0 ? 2 : 0;
        }

        HashSet<string> exactNames = null;
        string excludedSubstring = null;
        var batchStart = 0;
        var batchCount = int.MaxValue;
        var batchMode = false;
        if (filter.StartsWith("batch-exclude:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = filter.Substring("batch-exclude:".Length).Split(';');
            if (parts.Length != 3 || string.IsNullOrWhiteSpace(parts[0]) ||
                !int.TryParse(parts[1], out batchStart) || batchStart < 0 ||
                !int.TryParse(parts[2], out batchCount) || batchCount <= 0)
            {
                Console.WriteLine(
                    "Batch filter must be batch-exclude:substring;start;count.");
                return 2;
            }
            excludedSubstring = parts[0];
            batchMode = true;
        }
        else if (filter.StartsWith("exclude:", StringComparison.OrdinalIgnoreCase))
        {
            excludedSubstring = filter.Substring("exclude:".Length);
            if (string.IsNullOrWhiteSpace(excludedSubstring))
            {
                Console.WriteLine("No excluded core-test substring was supplied.");
                return 2;
            }
        }
        if (filter.StartsWith("exact:", StringComparison.OrdinalIgnoreCase))
        {
            exactNames = new HashSet<string>(
                filter.Substring("exact:".Length)
                    .Split(new[] { ';' },
                        StringSplitOptions.RemoveEmptyEntries),
                StringComparer.Ordinal);
            if (exactNames.Count == 0)
            {
                Console.WriteLine("No exact core-test names were supplied.");
                return 2;
            }
        }

        var passed = 0;
        var failed = 0;
        var eligibleIndex = 0;

        foreach (var method in methods)
        {
            if (excludedSubstring != null && method.Name.IndexOf(
                    excludedSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            if (batchMode)
            {
                var currentIndex = eligibleIndex++;
                if (currentIndex < batchStart ||
                    currentIndex >= batchStart + batchCount)
                    continue;
            }
            if (exactNames != null && !exactNames.Contains(method.Name))
            {
                continue;
            }
            if (exactNames == null && excludedSubstring == null &&
                !string.IsNullOrEmpty(filter) &&
                method.Name.IndexOf(
                    filter, StringComparison.OrdinalIgnoreCase) < 0)
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

        var selected = passed + failed;
        if (selected == 0)
        {
            Console.WriteLine("RESULT passed=0 failed=0");
            Console.WriteLine("No core tests matched the requested filter.");
            return 2;
        }
        if (exactNames != null && selected != exactNames.Count)
        {
            var discoveredNames = new HashSet<string>(
                methods.Select(method => method.Name),
                StringComparer.Ordinal);
            foreach (var missing in exactNames
                .Where(name => !discoveredNames.Contains(name))
                .OrderBy(name => name, StringComparer.Ordinal))
            {
                Console.WriteLine("MISSING " + missing);
            }

            Console.WriteLine(
                "Exact core-test coverage mismatch: requested=" +
                exactNames.Count + " selected=" + selected);
            return 2;
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
                "MonoBleedingEdge/lib/mono/unityjit-win32/Facades/netstandard.dll");
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
