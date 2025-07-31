using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

class Program
{
    [DllImport("kernel32.dll")]
    static extern void RaiseFailFastException(IntPtr pExceptionRecord, IntPtr pContextRecord, uint dwFlags);

    static void TestAbortBug(string data)
    {
        if (data.StartsWith("bug"))
        {
            Console.WriteLine("Found 'bug' abort corner case!");
            Environment.FailFast("Simulated abort");
        }
    }

    static void TestNullDerefMom(string data)
    {
        if (data.StartsWith("mom"))
        {
            Console.WriteLine("Found 'mom' null deref corner case!");
            unsafe
            {
                int* ptr = null;
                *ptr = 42;
            }
        }
    }

    static void TestRuntimeErrorDad(string data)
    {
        if (data.StartsWith("dad"))
        {
            Console.WriteLine("Found 'dad' runtime_error corner case!");
            throw new Exception("Simulated runtime error");
        }
    }

    static void TestAssertCab(string data)
    {
        if (data.StartsWith("cab"))
        {
            Console.WriteLine("Found 'cab' assert corner case!");
            Debug.Assert(false, "Assertion failed for input 'cab'");
        }
    }

    static void TestRaiseFailFastDog(string data)
    {
        if (data.StartsWith("dog"))
        {
            Console.WriteLine("Found 'dog' RaiseFailFastException!");
            RaiseFailFastException(IntPtr.Zero, IntPtr.Zero, 0);
        }
    }

    static void TestAddressSanitizerBoo(string data)
    {
        if (data.StartsWith("boo"))
        {
            Console.WriteLine("Found 'boo' address sanitizer corner case!");
            int[] x = new int[100];
            x[100] = 5; // Out of bounds write
        }
    }

    static void TestAbortSetBehaviorSet(string data)
    {
        if (data.StartsWith("set"))
        {
            Console.WriteLine("Found 'set' FailFast corner case!");
            Environment.FailFast("set_abort_behavior + abort simulation");
        }
    }

    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: dotnet_fuzz_target <input_file>");
            return;
        }

        string input = File.ReadAllText(args[0]);
        Console.WriteLine($"Read '{input}' from input file.");

        TestAbortBug(input);
        TestNullDerefMom(input);
        TestRuntimeErrorDad(input);
        TestAbortSetBehaviorSet(input);
        TestAddressSanitizerBoo(input);
        TestRaiseFailFastDog(input);
        TestAssertCab(input);
    }
}
