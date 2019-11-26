using SonarQube.Commandline.StepsExecutor;
using System;
using static SonarQube.Commandline.StepsExecutor.CommandlineExecutor;

namespace SonarQubeCommandlineExecutor
{
    internal static class Program
    {
        private static void Main()
        {            
            var solutionFilename = @"C:\Users\c102116\Source\Repos\MovieService\MovieService.sln";
            var solutionName = CommandlineExecutor.GetSolutionName(solutionFilename);

            CommandlineExecutor.SetLoggerCallback(LoggerCallback);
            CommandlineExecutor.CleanProjectFolders(solutionFilename);
            CommandlineExecutor.SonarScannerBegin(solutionFilename, solutionName);
            CommandlineExecutor.BuildSolution(solutionFilename);
            CommandlineExecutor.RunTestsUsingVsTest(solutionFilename, "CodeCoverage.runsettings", "Priority != -1");            
            CommandlineExecutor.ConvertCoverageFilesToXml(solutionFilename);
            CommandlineExecutor.SonarScannerEnd(solutionFilename);
        }

        private static void LoggerCallback(LogType logType, string data)
        {
            string prefix = null;

            switch (logType)
            {
                case CommandlineExecutor.LogType.Normal:
                    Console.ResetColor();
                    prefix = string.Empty;
                    break;
                case CommandlineExecutor.LogType.Info:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    prefix = "INFO: ";
                    break;
                case CommandlineExecutor.LogType.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    prefix = "ERROR: ";
                    break;
                case CommandlineExecutor.LogType.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    prefix = "WARN: ";
                    break;
                default:
                    break;
            }

            Console.WriteLine(prefix + data);
            Console.ResetColor();
        }
    }
}
