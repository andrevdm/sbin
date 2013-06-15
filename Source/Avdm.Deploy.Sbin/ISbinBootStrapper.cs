using System;
using System.Collections.Generic;
using System.Reflection;

namespace Avdm.Deploy.Sbin
{
    public interface ISbinBootStrapper
    {
        void Initialise();
        void SetVersion( long version, string basePath, string exeName );
        Assembly CurrentDomainAssemblyResolve( object sender, ResolveEventArgs args );
        ISbinAssemblyResolver CreateResolver( string[] remainingArgs );
        IEnumerable<MachineVersion> GetVersions();
    }
}