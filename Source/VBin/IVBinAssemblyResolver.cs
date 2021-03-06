﻿using System;
using System.Reflection;

namespace VBin
{
    /// <summary>
    /// Dont use this directly, rather use VBin.Mamanger.VBinManager which manages access to this class
    /// </summary>
    public interface IVBinAssemblyResolver
    {
        /// <summary>
        /// The current vbin version.
        /// -1 if not running from vbin
        /// </summary>
        long CurrentVersion { get; }
        
        /// <summary>
        /// Check if the current application is running from vbin
        /// </summary>
        bool IsRunningInVBin { get; }

        /// <summary>
        /// Name of the main assembly loaded by VBin
        /// </summary>
        string MainAssemblyName { get; }
        
        /// <summary>
        /// Initialise vbin. You should not need to call this method
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
        /// See <see cref="VBinMongoDbAssemblyResolver"/>
        /// </summary>
        /// <param name="domainName">Friendly name</param>
        /// <param name="setup">The AppDomainSetup</param>
        /// <param name="assemblyName">The name of the assembly to load (Assembly.FullName)</param>
        /// <param name="typeName">Type name to load (Type.FullName)</param>
        /// <returns></returns>
        Tuple<AppDomain, object> CreateAndUnwrapAppDomain( string domainName, AppDomainSetup setup, string assemblyName, string typeName );
    }
}