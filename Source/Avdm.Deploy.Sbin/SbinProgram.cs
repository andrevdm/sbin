using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Avdm.Deploy.Sbin
{
    /// <summary>
    /// Sbin stub
    /// </summary>
    /// <remarks>
    /// NB dont add any external references
    /// 
    /// Usage e.g. /// e.g. 
    ///    explicitly select version: sbin.exe SomeApp.exe -v=1 param1 param2
    ///    or  get version from config  : sbin.exe SomeApp.exe param1 param2
    /// 
    /// The config is stored in the sbin DB in the sbinConfig collection. 
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
    public class SbinProgram
    {
        private static string g_basePath;
        private static long? g_version;
        private static string g_exeName;
        private static ISbinAssemblyResolver g_resolver;
        private static string[] g_remainingArgs;
        private static ISbinBootStrapper g_bootStrapper;

        public ISbinAssemblyResolver AssemblyResolver
        {
            get { return g_resolver; }
        }

        public static ISbinAssemblyResolver Initialise( string[] args )
        {
            try
            {
                string bootStrapperTypeName = ConfigurationManager.AppSettings["SbinBootStrapperType"];
                Type bootStrapperType = Type.GetType( bootStrapperTypeName, true );
                g_bootStrapper = (ISbinBootStrapper)Activator.CreateInstance( bootStrapperType );

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
                
                Assembly asm = g_resolver.GetAssembly( Path.GetFileNameWithoutExtension( g_exeName ) );

                if( asm == null )
                {
                    throw new Exception( "Can't find assembly " + g_exeName );
                }

                if( asm == null )
                {
                    throw new Exception( "Can't find assembly " + g_exeName );
                }

                var mainMethod = (from type in asm.GetTypes()
                                  from t in type.GetMethods( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static )
                                  where t.Name == "Main"
                                  select t).FirstOrDefault();

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