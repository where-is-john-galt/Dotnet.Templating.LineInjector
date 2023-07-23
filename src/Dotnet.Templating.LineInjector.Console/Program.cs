using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommandLine;

namespace Dotnet.Templating.LineInjector.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            if (result.Errors.Any())
            {
                return;
            }

            var options = result.Value;
            
            if (string.IsNullOrEmpty(options.JsonFilePath))
            {
                System.Console.WriteLine("Please provide the path to the JSON file as an argument.");
                return;
            }
            
            var requests = await LoadRequestsFromJson(options.JsonFilePath);
            foreach (var request in requests)
            {
                var processingRequest = await (await RequestProcessor.ProcessRequest(request));
                processingRequest.IfLeft(x => System.Console.WriteLine(x.Message));
            }
            
            if (options.DeleteJsonFilePath)
            {
                File.Delete(options.JsonFilePath);
            }
            
            if (options.SelfDelete)
            {
                DeleteSelf();
            }
        }
        
        static async Task<ImmutableList<Request>> LoadRequestsFromJson(string jsonFilePath)
        {
            var jsonData = await File.ReadAllTextAsync(jsonFilePath);
            return JsonSerializer.Deserialize<List<Request>>(jsonData).ToImmutableList();
        }
        
        static void DeleteSelf()
        {
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            var processInfo = new ProcessStartInfo
            {
                Arguments = $"/C choice /C Y /N /D Y /T 3 & Del {exePath}",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            };
            Process.Start(processInfo);

            Environment.Exit(0);
        }
    }
}