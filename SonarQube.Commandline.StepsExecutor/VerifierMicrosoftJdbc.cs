using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SonarQube.Commandline.StepsExecutor
{
    internal static class VerifierMicrosoftJdbc
    {
        public static string Verify(IEnumerable<string> environmentPaths)
        {
            var jdbcJreMessage = VerifyMsSqlJdbcJre(environmentPaths);
            var jdbcAuthMessage = VerifyMsSqlJdbcAuth(environmentPaths);

            string message = null;

            if (jdbcJreMessage != null)
            {
                message += jdbcJreMessage;
            }

            if (jdbcAuthMessage != null)
            {
                message += jdbcAuthMessage;
            }

            return message;

        }

        private static string VerifyMsSqlJdbcAuth(IEnumerable<string> environmentPaths)
        {
            foreach (var path in environmentPaths)
            {
                if (File.Exists(Path.Combine(path, "mssql-jdbc-6.2.2.jre7.jar")))
                {
                    return null;
                }
            }

            return "Microsoft JDBC Driver Verification: Could not find sqljdbc_auth.dll in any of the locations in the Environment \"Path\" variable";
        }

        private static object VerifyMsSqlJdbcJre(IEnumerable<string> environmentPaths)
        {
            var jre7Found = false;
            var jre8Found = false;

            foreach (var path in environmentPaths)
            {
                if (!jre7Found && File.Exists(Path.Combine(path, "mssql-jdbc-6.2.2.jre7.jar")))
                {
                    jre7Found = true;
                }

                if (!jre8Found && File.Exists(Path.Combine(path, "mssql-jdbc-6.2.2.jre8.jar")))
                {
                    jre8Found = true;
                }

                if (jre7Found && jre8Found)
                {
                    return null;
                }
            }

            return "Microsoft JDBC Driver Verification: Could not find mssql-jdbc-6.2.2.jre7.jar and/or mssql-jdbc-6.2.2.jre8.jar, in any of the locations in the Environment \"Path\" variable";
        }
    }
}
