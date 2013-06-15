using System;

namespace Avdm.Deploy.Sbin
{
    /// <summary>
    /// Helper class for creating new app domains while running from sbin.
    /// In the new app domain the sbin AssemblyResolve event wont have been configured. So any
    /// attempt to load a type will only look on the disk for the assembly and thus fail.
    /// This class is used by ISbinAssemblyResolver.CreateAndUnwrapAppDomain to firstly setup sbin
    /// and then return the type that the user requested
    /// </summary>
    public class AppDomainCreationHelper : MarshalByRefObject
    {
        private static ISbinAssemblyResolver g_resolver;

        public AppDomainCreationHelper( long version )
        {
            g_resolver = SbinProgram.Initialise( new string[] { "-v=" + version, "." } );
        }

        /// <summary>
        /// Create an instance of the type specified in the new app domain
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public object Create( string assembly, string typeName )
        {
            var idx = assembly.IndexOf( "," );

            if( idx > 0 )
            {
                assembly = assembly.Substring( 0, idx );
            }

            var asm = g_resolver.GetAssembly( assembly );
            var type = asm.GetType( typeName, true );
            var obj = Activator.CreateInstance( type );
            return obj;
        }
    }
}