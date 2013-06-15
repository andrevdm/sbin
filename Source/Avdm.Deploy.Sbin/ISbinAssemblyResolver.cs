using System;
using System.Reflection;

namespace Avdm.Deploy.Sbin
{
    public interface ISbinAssemblyResolver
    {
        /// <summary>
        /// The current sbin version.
        /// -1 if not running from sbin
        /// </summary>
        long CurrentVersion { get; }
        
        /// <summary>
        /// Check if the current application is running from sbin
        /// </summary>
        bool IsRunningInSbin { get; }
        
        /// <summary>
        /// Initialise sbin. You should not need to call this method
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="version"></param>
        /// <param name="exeName"></param>
        /// <param name="remainingArgs"></param>
        void Initialise( string basePath, long version, string exeName, string[] remainingArgs );

        /// <summary>
        /// Resolve an assembly by name
        /// </summary>
        /// <param name="assemblyname"></param>
        /// <returns></returns>
        Assembly GetAssembly( string assemblyname );

        /// <summary>
        /// Create a new AppDomain and return a type from it.
        /// This must be done from the resolver as the sin infrastructure must first be configured in the new domain.
        /// See <see cref="SbinMongoDbAssemblyResolver"/>
        /// </summary>
        /// <param name="domainName">Friendly name</param>
        /// <param name="setup">The AppDomainSetup</param>
        /// <param name="assemblyName">The name of the assembly to load (Assembly.FullName)</param>
        /// <param name="typeName">Type name to load (Type.FullName)</param>
        /// <returns></returns>
        Tuple<AppDomain, object> CreateAndUnwrapAppDomain( string domainName, AppDomainSetup setup, string assemblyName, string typeName );
    }
}