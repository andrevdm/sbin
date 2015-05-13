using System;
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
        private static long? g_version;
        private static string g_exeName;
        private static IVBinAssemblyResolver g_resolver;
        private static string[] g_remainingArgs;
        private static IVBinBootStrapper g_bootStrapper;

        public IVBinAssemblyResolver AssemblyResolver
        {
            get { return g_resolver; }
        }

        public static IVBinAssemblyResolver Initialise( string[] args )
        {
            try
            {
                string bootStrapperTypeName = ConfigurationManager.AppSettings["VBinBootStrapperType"];

                if( string.IsNullOrWhiteSpace( bootStrapperTypeName ) )
                {
                    throw new ArgumentException( "Missing VBinBootStrapperType config value" );
                }

                Type bootStrapperType = Type.GetType( bootStrapperTypeName, true );
                g_bootStrapper = (IVBinBootStrapper)Activator.CreateInstance( bootStrapperType );

                g_bootStrapper.Initialise();
                GetVersionPaths( args );
                g_bootStrapper.SetVersion( g_version.Value, g_basePath, g_exeName );

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

				if( ConfigurationManager.AppSettings ["VBin.Debug"] == "true" ) 
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

        private static void GetVersionPaths( string[] args )
        {
            if( args.Length < 1 )
            {
                throw new ArgumentOutOfRangeException( "args", "Too few arguments. Expecting startup name" );
            }

            g_version = Match<long?>( args[0], @"-v=(?<x>\d+)", "x", s => long.Parse( s ) );

            if( g_version != null )
            {
                if( args.Length < 2 )
                {
                    throw new ArgumentOutOfRangeException( "args", "Too few arguments. Missing startup name" );
                }

                g_exeName = args[1];
                g_remainingArgs = args.Length > 2 ? args.Skip( 2 ).ToArray() : new string[] { };
            }
            else
            {
                g_exeName = args[0];
                g_remainingArgs = args.Length > 1 ? args.Skip( 1 ).ToArray() : new string[] { };

                g_version = 1;

                var versions = g_bootStrapper.GetVersions();

                foreach( var ver in versions )
                {
                    if( ver.MachineRegex.IsMatch( Environment.MachineName ) )
                    {
                        g_version = ver.Version;
                    }
                }
            }

            g_basePath = g_version + "\\";
        }

        private static T Match<T>( string text, string pattern, string groupName, Func<string, T> onMatch )
        {
            var match = Regex.Match( text, pattern, RegexOptions.IgnoreCase );
            return match.Success ? onMatch( match.Groups[groupName].Value ) : default( T );
        }
    }
}