﻿using System;
using System.Reflection;

namespace VBin.Manager
{
    public class SystemAssemblyResolver : IVBinAssemblyResolver
    {
        public bool IsRunningInVBin
        {
            get { return false; }
        }
        
        public long CurrentVersion 
        {
            get { return -1; }
        }

        public string MainAssemblyName
        {
            get
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                return entryAssembly != null ? entryAssembly.GetName().Name : "";
            }
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
