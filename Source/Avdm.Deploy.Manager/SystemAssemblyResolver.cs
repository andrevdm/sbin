using System;
using System.Reflection;
using Avdm.Deploy.Sbin;

namespace Avdm.Deploy.Manager
{
    public class SystemAssemblyResolver : ISbinAssemblyResolver
    {
        public bool IsRunningInSbin
        {
            get { return false; }
        }
        
        public long CurrentVersion 
        {
            get { return -1; }
        }

        public void Initialise( string basePath, long version, string exeName, string[] remainingArgs )
        {
        }

        public Assembly GetAssembly( string assemblyname )
        {
            return Assembly.Load( assemblyname );
        }

        /// <summary>
        /// Create an AppDomain 
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="setup"></param>
        /// <param name="assemblyName"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public Tuple<AppDomain, object> CreateAndUnwrapAppDomain( string domainName, AppDomainSetup setup, string assemblyName, string typeName )
        {
            var domain = AppDomain.CreateDomain( domainName, null, setup );
            var obj = domain.CreateInstanceAndUnwrap( assemblyName, typeName );
            return new Tuple<AppDomain, object>( domain, obj );
        }
    }
}
