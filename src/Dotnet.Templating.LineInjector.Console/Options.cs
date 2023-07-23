using CommandLine;

namespace Dotnet.Templating.LineInjector.Console
{
    public class Options
    {
        [Option('f', "jsonfilepath", Required = true, HelpText = "Path to json with lines to inject")]
        public string JsonFilePath { get; set; }

        [Option('j', "deletejsonfilepath", Required = false, HelpText = "Do you want to delete the json file with the lines to be jumped in?")]
        public bool DeleteJsonFilePath { get; set; } = true;

        [Option('d', "selfdelete", Required = false, HelpText = "Do you want to delete this program after execution?")]
        public bool SelfDelete { get; set; } = true;
    }
}