using System;
using System.Reflection;
using Babodayo.Tests;
using NUnit.Framework;

// Runs only engine-independent tests when Unity Editor automation is unavailable.
internal static class PureInputTestRunner
{
    private static int Main()
    {
        int passed = 0, failed = 0;
        var fixture = new InputPipelineTests();
        foreach (var method in typeof(InputPipelineTests).GetMethods())
        {
            var cases = method.GetCustomAttributes(typeof(TestCaseAttribute), true);
            if (cases.Length == 0 && !method.IsDefined(typeof(TestAttribute), true)) continue;
            if (cases.Length == 0) Run(fixture, method, new object[0], ref passed, ref failed);
            foreach (TestCaseAttribute testCase in cases)
                Run(fixture, method, testCase.Arguments, ref passed, ref failed);
        }
        Console.WriteLine("Pure input tests: " + passed + " passed, " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }

    private static void Run(object fixture, MethodInfo method, object[] args, ref int passed, ref int failed)
    {
        try { method.Invoke(fixture, args); passed++; }
        catch (Exception exception)
        {
            failed++;
            Console.Error.WriteLine(method.Name + ": " + (exception.InnerException ?? exception));
        }
    }
}
