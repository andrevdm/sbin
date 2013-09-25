using System;
using System.Reflection;
using StructureMap;

namespace VBin.Manager
{
    /// <summary>
    /// Proxy for the IVBinAssemblyResolver class.
    /// Used to make it simpler to switch between using VBin and not using VBin
    /// </summary>
    public class VBinManager : IVBinAssemblyResolver
    {
        private readonly IVBinAssemblyResolver m_resolver;

        public VBinManager()
        {
            m_resolver = ObjectFactory.TryGetInstance<IVBinAssemblyResolver>() ?? new SystemAssemblyResolver();
        }

        public long CurrentVersion
        {
            get { return m_resolver.CurrentVersion; }
        }

        public bool IsRunningInVBin
        {
            get { return m_resolver.IsRunningInVBin; }
        }

        public string MainAssemblyName
        {
            get { return m_resolver.MainAssemblyName; }
        }

        public void Initialise( string basePath, long version, string exeName, string[] remainingArgs )
        {
            m_resolver.Initialise( basePath, version, exeName, remainingArgs );
        }

        public Assembly GetAssembly( string assemblyname )
        {
            return m_resolver.GetAssembly( assemblyname );
        }

        public Tuple<AppDomain, object> CreateAndUnwrapAppDomain( string domainName, AppDomainSetup setup, string assemblyName, string typeName )
        {
            return m_resolver.CreateAndUnwrapAppDomain( domainName, setup, assemblyName, typeName );
        }

        public static IVBinAssemblyResolver Resolver
        {
            get
            {
                return new VBinManager();
            }
        }
    }
}
