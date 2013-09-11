using System;
using System.Collections.Generic;
using System.Reflection;

namespace VBin
{
    public interface IVBinBootStrapper
    {
        void Initialise();
        void SetVersion( long version, string basePath, string exeName );
        Assembly CurrentDomainAssemblyResolve( object sender, ResolveEventArgs args );
        IVBinAssemblyResolver CreateResolver( string[] remainingArgs );
        IEnumerable<MachineVersion> GetVersions();
    }
}