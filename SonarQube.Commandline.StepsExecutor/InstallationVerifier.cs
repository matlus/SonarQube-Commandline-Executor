using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SonarQube.Commandline.StepsExecutor
{
    internal static class InstallationVerifier
    {
        public static event EventHandler EnvironmentPathChanged;

        private static void OnEnvironmentPathChanged(EventArgs e)
        {
            EventHandler handler = EnvironmentPathChanged;
            handler?.Invoke(null, e);
        }

        public static void Verify(string msBuildPath, string dotNetPath, string vsTestConsolePath, string codeCoverageExecutablePath)
        {
            var errorrMessages = new StringBuilder();

            errorrMessages.Append(VerifyMsBuildPath(msBuildPath));
            errorrMessages.Append(VerifyDotNetPath(dotNetPath));
            errorrMessages.Append(VerifyVsTestConsolePath(vsTestConsolePath));
            errorrMessages.Append(VerifyCodeCoverageExecutablePath(codeCoverageExecutablePath));
            errorrMessages = VerifyPaths(errorrMessages);

            if (errorrMessages.Length > 0)
            {
                throw new InstallationVerificationException(errorrMessages.ToString());
            }
        }

        private static StringBuilder VerifyPaths(StringBuilder errorMessages)
        {
            var environmentVariables = (Hashtable)Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

            errorMessages = VerifyJavaHome(environmentVariables, errorMessages);
            errorMessages = VerifyEnvironmentPaths(environmentVariables, errorMessages);

            return errorMessages;
        }

        private static StringBuilder VerifyEnvironmentPaths(Hashtable environmentVariables, StringBuilder errorMessages)
        {
            var environmentPath = (string)environmentVariables["Path"];
            var environmentPaths = environmentPath.Split(new char[] { ';' });

            errorMessages.Append(VerifySonarScannerMsBuildIsInPath(environmentPaths));
            errorMessages.Append(VerifierMicrosoftJdbc.Verify(environmentPaths));

            return errorMessages;
        }

        private static string VerifySonarScannerMsBuildIsInPath(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                if (File.Exists(Path.Combine(path, "SonarScanner.MSBuild.exe")))
                {
                    return null;
                }
            }

            return "SonarScanner.MSBuild Verification: Could not find SonarScanner.MSBuild.exe, in any of the locations in the Environment \"Path\" variable";
        }

        private static StringBuilder VerifyJavaHome(Hashtable environmentVariables, StringBuilder errorMessages)
        {
            var javaHome = "JAVA_HOME";
            var javaExecutable = "bin\\Java.exe";

            if (environmentVariables.Contains(javaHome))
            {
                var javaHomePath = (string)environmentVariables[javaHome];
                var pathMissing = GetDirectoryAndFileExistance(javaHomePath, javaExecutable);

                switch (pathMissing)
                {
                    case PathMissing.None:
                        break;
                    case PathMissing.Directory:
                        errorMessages.AppendLine("Java Home Path Verification: The Directory: " + javaHome + " does not Exist");
                        break;
                    case PathMissing.File:
                        errorMessages.AppendLine("Java Home Path Verification: The " + javaExecutable + " was not found in the Path: " + javaHome);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                errorMessages.AppendLine("Java Home Path Verification: The " + javaHome + " Environment variable has not be set up");
            }

            return errorMessages;
        }

        private static PathMissing GetDirectoryAndFileExistance(string directory, string specificFileName)
        {
            if (Directory.Exists(directory))
            {
                if (!File.Exists(Path.Combine(directory, specificFileName)))
                {
                    return PathMissing.File;
                }
            }
            else
            {
                return PathMissing.Directory;
            }

            return PathMissing.None;
        }

        private static string VerifyCodeCoverageExecutablePath(string codeCoverageExecutablePath)
        {
            if (!File.Exists(codeCoverageExecutablePath))
            {
                return Environment.NewLine + "Code Coverage Executable Path Verification: The Code Coverage Executable was not found at: " + codeCoverageExecutablePath;
            }

            return null;
        }

        private static string VerifyVsTestConsolePath(string vsTestConsolePath)
        {
            if (!File.Exists(vsTestConsolePath))
            {
                return Environment.NewLine + "Vs Test Console Path Verification: The Vs Test Console Executable was not found at: " + vsTestConsolePath;
            }

            return null;
        }

        private static string VerifyDotNetPath(string dotNetPath)
        {
            if (!File.Exists(dotNetPath))
            {
                return Environment.NewLine + "Dot Net Executable Path Verification: The Dot Net Executable was not found at: " + dotNetPath;
            }

            return null;
        }

        private static string VerifyMsBuildPath(string msBuildPath)
        {
            if (!File.Exists(msBuildPath))
            {
                return Environment.NewLine + "MS Build Executable Path Verification: The MS Build Executable was not found at: " + msBuildPath;
            }

            return null;
        }
    }

    internal enum PathMissing { None, Directory, File }
}
