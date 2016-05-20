using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace VBin
{
    /// <summary>
    /// VBin stub
    /// </summary>
    /// <remarks>
    /// NB dont add any external references
    /// 
    /// Usage e.g. /// e.g. 
    ///    explicitly select version: vbin.exe SomeApp.exe -v=1 param1 param2
    ///    or  get version from config  : vbin.exe SomeApp.exe param1 param2
    /// 
    /// The config is stored in the vbin DB in the vbinConfig collection. 
    /// It looks like this
    ///    {
    ///           "Key" : "machineVersions",
    ///           "Value" : [
    ///                   {
    ///                           "Machine" : ".*",
    ///                           "Version" : 1
    ///                   },
    ///                   {
    ///                           "Machine" : "localhost",
    ///                           "Version" : 2
    ///                   }
    ///           ]
    ///    }
    /// </remarks>
    public class VBinProgram
    {
        private static string g_basePath;
        private static long g_version;
        private static string g_exeName;
        private static IVBinAssemblyResolver g_resolver;
        private static string[] g_remainingArgs;
        private static IVBinBootStrapper g_bootStrapper;
        private static SettingsParser.VBinSettings g_settings;

        public IVBinAssemblyResolver AssemblyResolver
        {
            get { return g_resolver; }
        }

        public static IVBinAssemblyResolver Initialise( string[] args )
        {
            try
            {
                g_settings = SettingsParser.ParseArgs( args );

                string bootStrapperTypeName = GetOrConfig( "VBinBootStrapperType" ) ?? "VBin.MongoBootstrapper,vbin";

                if( string.IsNullOrWhiteSpace( bootStrapperTypeName ) )
                {
                    throw new ArgumentException( "Missing VBinBootStrapperType config value" );
                }

                Type bootStrapperType = Type.GetType( bootStrapperTypeName, true );
                g_bootStrapper = (IVBinBootStrapper)Activator.CreateInstance( bootStrapperType );

                g_bootStrapper.Initialise();
                GetVersionPaths();
                g_bootStrapper.SetVersion( g_version, g_basePath, g_exeName );

                //Give the bootstrapper the opportunity to resolve assemblies first
                AppDomain.CurrentDomain.AssemblyResolve += g_bootStrapper.CurrentDomainAssemblyResolve;

                g_resolver = g_bootStrapper.CreateResolver( g_remainingArgs );
                return g_resolver;
            }
            catch( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( ex );
                Console.ResetColor();
            }

            return null;
        }

        public static void Main( string[] args )
        {
            try
            {
                Initialise( args );

                string path = g_exeName;

                if( !path.Contains( "." ) )
                {
                    throw new ArgumentException( "exe name must contain an extension", "args" );
                }

                Assembly asm = g_resolver.GetAssembly( Path.GetFileNameWithoutExtension( g_exeName ) );

                if( asm == null )
                {
                    throw new FileNotFoundException( "Can't find assembly " + g_exeName );
                }

                var mainMethod = asm.EntryPoint;

                if( mainMethod == null )
                {
                    throw new InvalidProgramException( "No entry point found in " + g_exeName );
                }

                if( GetOrConfig( "VBin.Debug" ) == "true" )
                {
                    Console.WriteLine( "entryPoint = {0}", mainMethod );
                }

                mainMethod.Invoke( null, mainMethod.GetParameters().Length > 0 ? new object[] { g_remainingArgs } : null );
            }
            catch( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( ex );
                Console.ResetColor();
            }
        }

        private static void GetVersionPaths()
        {
            g_exeName = g_settings.ExeName;
            g_remainingArgs = g_settings.RemainingArgs;

            if( g_settings.Version == null )
            {
                g_version = 1;

                var versions = g_bootStrapper.GetVersions();

                foreach( var ver in versions )
                {
                    if( ver.MachineRegex.IsMatch( Environment.MachineName ) )
                    {
                        g_version = ver.Version;
                        break;
                    }
                }
            }
            else
            {
                g_version = g_settings.Version.Value;
            }

            g_basePath = g_version + "\\";
        }

        public static string GetOrConfig( string key ) => g_settings.GetOrConfig( key );
    }
}