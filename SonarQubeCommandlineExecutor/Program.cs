using SonarQube.Commandline.StepsExecutor;
using System;

namespace SonarQubeCommandlineExecutor
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var solutionFilename = @"C:\Users\c102116\Source\Repos\MovieService\MovieService.sln";
            var solutionName = CommandlineExecutor.GetSolutionName(solutionFilename);

            CommandlineExecutor.RemoveTestResultsDirectories(solutionFilename);
            CommandlineExecutor.SonarScannerBegin(solutionFilename, solutionName);
            CommandlineExecutor.BuildSolution(solutionFilename);
            CommandlineExecutor.RunTestsUsingVsTest(solutionFilename, "CodeCoverage.runsettings", "Priority != -1");            
            CommandlineExecutor.ConvertCoverageFilesToXml(solutionFilename);
            CommandlineExecutor.SonarScannerEnd(solutionFilename);
        }
    }
}
