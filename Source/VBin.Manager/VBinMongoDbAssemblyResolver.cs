using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using StructureMap;

namespace VBin.Manager
{
    /// <summary>
    /// Implements IVBinAssemblyResolver
    /// Loads requested types for the current version from MonogDB
    /// </summary>
    public class VBinMongoDbAssemblyResolver : IVBinAssemblyResolver
    {
        private string m_basePath;
        private string m_assemblyName;
        private IMongoDatabase m_db;
        private MongoGridFS m_grid;
        private readonly ConcurrentDictionary<string, Assembly> m_assemblies = new ConcurrentDictionary<string, Assembly>( StringComparer.InvariantCultureIgnoreCase );
        private readonly object m_syncAsmLoad = new object();
        private long m_version = -1;
        private bool m_runningInVBin = false;

        public string MainAssemblyName { get; private set; }

        public long CurrentVersion{ get { return m_version; } }

        public bool IsRunningInVBin { get { return m_runningInVBin; } }

        public void Initialise( string basePath, long version, string exeName, string[] remainingArgs )
        {
            Console.WriteLine( "AssemblyResolver {0}, basePath={1}, v={2}, exe={3} args={4}", GetType().Assembly.GetName().Version, basePath, version, exeName, string.Join( ",", remainingArgs ?? new string[] { } ) );

            m_basePath = basePath;
            m_assemblyName = exeName;
            MainAssemblyName = m_assemblyName;
            m_version = version;
            m_runningInVBin = true;

            var serverConnectionString = ConfigurationManager.AppSettings["MongoDB.Server"];

            if( string.IsNullOrWhiteSpace( serverConnectionString ) )
            {
                throw new ArgumentException( "Config value MongoDB.Server is missing" );
            }

            var dbName = ConfigurationManager.AppSettings["VBinDatabase"];

            if( string.IsNullOrWhiteSpace( dbName ) )
            {
                throw new ArgumentException( "Config value VBinDatabase is missing" );
            }

            var client = new MongoClient( serverConnectionString );
            m_db = client.GetDatabase( dbName );

            m_grid = new MongoGridFS( client.GetServer(), dbName, MongoGridFSSettings.Defaults );

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;
            Initialise();
        }

        //This must be in a seperate method
        private void Initialise()
        {
            ObjectFactory.Configure( x => x.For<IVBinAssemblyResolver>().Singleton().Use( () => this ) );
            m_assemblies[GetType().Name] = GetType().Assembly; //TODO test caching of this assembly
        }

        private Assembly CurrentDomainAssemblyResolve( object sender, ResolveEventArgs args )
        {
            var assemblyname = args.Name.Split( ',' )[0];

            return GetAssembly( assemblyname );
        }

        public Tuple<byte[], byte[]> GetAssemblyBytes( string assemblyname )
        {
            string gridFileName = FormatGridFileName( assemblyname );

            if( m_grid.Exists( Query.Matches( "filename", new BsonRegularExpression( new Regex( "^" + Regex.Escape( gridFileName + ".dll" ) + "$", RegexOptions.IgnoreCase ) ) ) ) )
            {
                gridFileName = gridFileName + ".dll";
            }
            else
            {
                if( m_grid.Exists( Query.Matches( "filename", new BsonRegularExpression( new Regex( "^" + Regex.Escape( gridFileName + ".exe" ) + "$", RegexOptions.IgnoreCase ) ) ) ) )
                {
                    gridFileName = gridFileName + ".exe";
                }
                else
                {
                    Console.WriteLine( "Assembly not found {0}", assemblyname );
                    return null;
                }
            }

            byte[] asmBytes = ReadFile( gridFileName );
            byte[] pdbBytes = null;

            if( m_grid.Exists( Path.ChangeExtension( gridFileName, "pdb" ) ) )
            {
                pdbBytes = ReadFile( Path.ChangeExtension( gridFileName, "pdb" ) );
            }

            return new Tuple<byte[], byte[]>( asmBytes, pdbBytes );
        }

        public Assembly GetAssembly( string assemblyname )
        {
            Assembly asm;

            if( m_assemblies.TryGetValue( assemblyname, out asm ) )
            {
                return asm;
            }

            lock( m_syncAsmLoad )
            {
                var bytes = GetAssemblyBytes( assemblyname );

                if( bytes == null )
                {
                    Console.WriteLine( "No matching assembly found - {0}", assemblyname );

                    if( ConfigurationManager.AppSettings["Vbin.ThrowOnAsmNotFound"] == "true" )
                    {
                        throw new FileNotFoundException( "No matching assembly found", assemblyname );
                    }

                    return null;
                }

                byte[] asmBytes = bytes.Item1;
                byte[] pdbBytes = bytes.Item2;

                var assembly = Assembly.Load( asmBytes, pdbBytes );
                m_assemblies[assemblyname] = assembly;

                return assembly;
            }
        }

        /// <summary>
        /// Create a new AppDomain, setup vbin and return the requested type
        /// 
        /// In the new app domain the vbin AssemblyResolve event wont have been configured. So any
        /// attempt to load a type will only look on the disk for the assembly and thus fail.
        /// This method will setup vbin in the new AppDomain and then return the type that the user requested
        /// </summary>
        public Tuple<AppDomain, object> CreateAndUnwrapAppDomain( string domainName, AppDomainSetup setup, string assemblyName, string typeName )
        {
            var domain = AppDomain.CreateDomain( domainName, null, setup );

            var helper = (AppDomainCreationHelper)domain.CreateInstanceAndUnwrap(
                typeof(AppDomainCreationHelper).Assembly.FullName,
                typeof(AppDomainCreationHelper).FullName,
                false,
                BindingFlags.CreateInstance,
                null,
                new object[] {m_version},
                null,
                null );

            var obj = helper.Create( assemblyName, typeName );
            return new Tuple<AppDomain, object>( domain, obj );
        }

        private byte[] ReadFile( string fileName )
        {
            var found = m_grid.FindOne( Query.Matches( "filename", new BsonRegularExpression( new Regex( "^" + Regex.Escape( fileName ) + "$", RegexOptions.IgnoreCase ) ) ) );

            byte[] bytes;
            using( var g = found.OpenRead() )
            {
                bytes = new byte[g.Length];
                g.Read( bytes, 0, bytes.Length );
            }

            return bytes;
        }

        private string FormatGridFileName( string assemblyname )
        {
            string path = Path.Combine( m_basePath, assemblyname );
            return path;
        }
    }
}
