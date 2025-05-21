using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using CommandLine;
using System.Threading.Tasks;
using System.Threading;

namespace Sample;

public class CommandLineOptions
{
    public CommandLineOptions(string myCustomArg, bool verbose)
    {
        MyCustomArg = myCustomArg;
        Verbose = verbose;
    }

    [Option("my-custom-arg", Required = true, HelpText = "Example of a custom string argument")]
    public string MyCustomArg { get; }

    [Option("verbose", Required = false, HelpText = "Enable verbose tracing")]
    public bool Verbose { get; }
}

class Program
{
    public static async Task<int> Main(string[] args)
    {
        var program = new Program();

        return await RunWithCancellationAsync(token => program.Main(args, token));
    }

    private async Task<int> Main(string[] args, CancellationToken cancellationToken)
    {
        try
        {
            await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .MapResult(
                    (CommandLineOptions opts) => Run(opts, cancellationToken),
                    errors =>
                    {
                        foreach (var error in errors)
                        {
                            Console.Error.WriteLine(error.ToString());
                        }

                        return Task.FromResult(1);
                    });

            return 0;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteAsync(ex.ToString());
            return 1;
        }
    }

    private async Task<int> Run(CommandLineOptions options, CancellationToken cancellationToken)
    {
        if (options.Verbose)
        {
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
        }

        // your logic here

        return 0;
    }

    private static async Task<int> RunWithCancellationAsync(Func<CancellationToken, Task<int>> runFunc)
    {
        var cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            cancellationTokenSource.Cancel();
            e.Cancel = true;
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            cancellationTokenSource.Cancel();
        };

        try
        {
            return await runFunc(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationTokenSource.Token)
        {
            // this is expected - no need to rethrow
            return 1;
        }
    }
}