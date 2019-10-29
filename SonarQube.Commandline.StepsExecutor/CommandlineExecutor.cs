using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SonarQube.Commandline.StepsExecutor
{
    public static class CommandlineExecutor
    {
        private const string MsBuildPath = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe";
        private const string DotNetPath = @"C:\Program Files\dotnet\dotnet.exe";
        private const string VsTestConsolePath = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe";
        private const string CodeCoverageExePartialPath = @"\.nuget\packages\microsoft.codecoverage\15.9.0\build\netstandard1.0\CodeCoverage\CodeCoverage.exe";

        public static void RemoveTestResultsDirectories(string solutionFilename)
        {
            var projectDirectory = Path.GetDirectoryName(solutionFilename);
            var testResultsDirectories = Directory.GetDirectories(projectDirectory, "TestResults", SearchOption.AllDirectories);

            foreach (var testResultsDirectory in testResultsDirectories)
            {
                Directory.Delete(testResultsDirectory, true);
            }
        }

        public static void SonarScannerBegin(string solutionFilename, string solutionName)
        {
            var projectDirectory = Path.GetDirectoryName(solutionFilename);
            var commandlineArguments = $"begin /k:\"{solutionName}\" /d:sonar.cs.vstest.reportsPaths=\"{projectDirectory}\\**\\TestResults\\**\\*.trx\" /d:sonar.cs.vscoveragexml.reportsPaths=\"{projectDirectory}\\**\\TestResults\\**\\*.coveragexml";
            ExecuteCommandlineProcess(projectDirectory, "SonarScanner.MSBuild.exe", commandlineArguments);
        }

        public static void SonarScannerEnd(string solutionFilename)
        {            
            var projectDirectory = Path.GetDirectoryName(solutionFilename);
            ExecuteCommandlineProcess(projectDirectory, "SonarScanner.MSBuild.exe", "end");
        }

        public static void RunTestsUsingDotNet(string solutionFilename, string runsettingsFilename)
        {
            var projectDirectory = Path.GetDirectoryName(solutionFilename);
            var commandlineArguments = $"test \"{solutionFilename}\" --configuration Release --settings \"{ projectDirectory + "\\" + runsettingsFilename}\" --no-build --collect \"Code Coverage\" --logger \"trx\"";
            ExecuteCommandlineProcess(projectDirectory, DotNetPath, commandlineArguments);
        }

        /// <summary>
        /// Thi method supports running tests can caculating code coverage for .NET Framework, .NET Core as well as hybrid solutions
        /// The runsettings file (parameter) is typically called CodeCoverage.runsettings and is typically placed in the solution folder.
        /// </summary>
        /// <param name="solutionFilename">Path and file name of the solution</param>
        /// <param name="runsettingsfileName">This is a file with a .runsettings extension. This file is required/used when you want to indicate that when you're collecting code coverage you want to exclude test projects</param>
        /// <param name="testCaseFilter">If you have tests that you want/need to exclude then use can use the Priority attribute on the tests and use that as a filter here</param>
        public static void RunTestsUsingVsTest(string solutionFilename, string runsettingsfileName = null, string testCaseFilter = null)
        {
            var projectDirectory = Path.GetDirectoryName(solutionFilename);
            string commandlineArguments = $"--inIsolation --parallel --collect:\"Code Coverage\" --logger:\"trx\"";

            if (runsettingsfileName != null)
            {
                commandlineArguments += $" --settings:\"{projectDirectory + "\\" + runsettingsfileName}\"";
            }

            if (testCaseFilter != null)
            {
                commandlineArguments += $" --testCaseFilter:\"{testCaseFilter}\"";
            }

            var testAssemblies = GetTestProjectAssemblies(projectDirectory).ToList();

            var testFileNames = string.Join(" ", testAssemblies);


            ExecuteCommandlineProcess(projectDirectory, VsTestConsolePath, testFileNames + " " + commandlineArguments);
        }

        public static void BuildSolution(string solutionFilename)
        {
            var projectDirectory = Path.GetDirectoryName(solutionFilename);            
            var commandlineArguments = $"\"{solutionFilename}\" /m /nr:false /t:Clean;Restore;Rebuild /p:Configuration=Release";
            ExecuteCommandlineProcess(projectDirectory, MsBuildPath, commandlineArguments);
        }

        public static string GetSolutionName(string solutionFilename)
        {
            return Path.GetFileNameWithoutExtension(solutionFilename);
        }

        public static void ConvertCoverageFilesToXml(string solutionFilename)
        {
            var projectDirectory = Path.GetDirectoryName(solutionFilename);
            var coverageFiles = Directory.GetFiles(projectDirectory, "*.coverage", SearchOption.AllDirectories);            

            var uniqueProjects = new Dictionary<string, string>();

            foreach (var coverageFile in coverageFiles)
            {
                var projectName = ExtractProjectName(coverageFile);
                if (!uniqueProjects.ContainsKey(projectName))
                {
                    uniqueProjects.Add(projectName, coverageFile);
                }
            }


            string CodeCoverageExecutable = GetUserDirectory() + CodeCoverageExePartialPath;

            foreach (var coverageFile in uniqueProjects.Values)
            {

                var commandlineArguments = $"analyze /output:\"{coverageFile}xml\" \"{coverageFile}\"";
                ExecuteCommandlineProcess(projectDirectory, CodeCoverageExecutable, commandlineArguments);
            }
        }

        /// <summary>
        /// This method executes a Powershell script from the command line
        /// </summary>
        /// <param name="scriptPathAndFilename">The full path and file name of the Powershell script</param>
        /// <param name="scriptArguments">Any arguments the powershell script needs.</param>
        public static void ExecutePowershellScript(string scriptPathAndFilename, string scriptArguments)
        {
            var workingDirectory = Path.GetDirectoryName(scriptPathAndFilename);
            var commandlineArguments = $" -NoProfile -ExecutionPolicy unrestricted -file \"{scriptPathAndFilename}\" {scriptArguments}";
            ExecuteCommandlineProcess(workingDirectory, "powershell.exe", commandlineArguments);
        }

        private static void ExecuteCommandlineProcess(string workingDirectory, string fileName, string commandlinearguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                Arguments = commandlinearguments,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();                
            }
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private static string ExtractProjectName(string coverageFilePath)
        {
            return Directory.GetParent(coverageFilePath).Parent.Parent.Name;
        }

        private static string GetUserDirectory()
        {
            return Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Parent.FullName;
        }

        private static IEnumerable<string> GetTestProjectAssemblies(string projectDirectory)
        {
            var testProjects = DiscoverTestProjects(projectDirectory);

            foreach (var testProject in testProjects)
            {
                var testProjectDirectory = Path.GetDirectoryName(testProject);
                var testProjectFilename = Path.GetFileNameWithoutExtension(testProject);
                var testProjectAssemblies = Directory.EnumerateFiles(testProjectDirectory, testProjectFilename + ".*", SearchOption.AllDirectories);
                var testProjectAssembly = testProjectAssemblies.FirstOrDefault(f => f.Contains(".dll") || f.Contains(".exe"));
                if (testProjectAssembly != null)
                {
                    yield return "\"" + testProjectAssembly + "\"";
                }
            }
        }

        private static IEnumerable<string> DiscoverTestProjects(string projectDirectory)
        {
            var csProjectFiles = Directory.EnumerateFiles(projectDirectory, "*.csproj", SearchOption.AllDirectories);

            foreach (var csProjectFile in csProjectFiles)
            {
                var projectFileContents = File.ReadAllText(csProjectFile);
                if (projectFileContents.Contains("{3AC096D0-A1C2-E12C-1390-A8335801FDAB}"))
                {
                    yield return csProjectFile;
                }
            }
        }
    }
}
