using System;
using System.Collections.Generic;
using System.Text;

namespace SonarQube.Commandline.StepsExecutor
{


    [Serializable]
    public class InstallationVerificationException : Exception
    {
        public InstallationVerificationException() { }
        public InstallationVerificationException(string message) : base(message) { }
        public InstallationVerificationException(string message, Exception inner) : base(message, inner) { }
        protected InstallationVerificationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
