using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Configuration;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Avdm.Deploy.Sbin
{
    public class MongoBootstrapper : ISbinBootStrapper
    {
        private Assembly m_mongoBsonAssembly;
        private Assembly m_mongoDriverAssembly;
        private AppDomain m_domain;
        private IStubMongoHelper m_mongoHelper;
        private byte[] m_asmBytes;
        private byte[] m_pdbBytes;
        private string m_basePath;
        private string m_exeName;
        private long m_version;
        private static Func<string, dynamic> g_createFileNameQuery;

        public void Initialise()
        {
            if( string.IsNullOrWhiteSpace( ConfigurationManager.AppSettings["MongoDB.Server"] ) )
            {
                throw new ArgumentNullException( "MongoDB.Server connection string not specified" );
            }

            m_domain = CreateHelperAppDomain();
            m_mongoHelper = GetMongoHelper( m_domain );
        }

        public void SetVersion( long version, string basePath, string exeName )
        {
            m_version = version;
            m_basePath = basePath;
            m_exeName = exeName;

            m_mongoBsonAssembly = Assembly.Load( m_mongoHelper.GetAssemby( Path.Combine( basePath, "MongoDB.Bson.dll" ) ), null );
            m_mongoDriverAssembly = Assembly.Load( m_mongoHelper.GetAssemby( Path.Combine( basePath, "MongoDB.Driver.dll" ) ), null );

            m_asmBytes = m_mongoHelper.GetAssemby( Path.Combine( basePath, @"Avdm.Deploy.Manager.dll" ) );
            m_pdbBytes = null;

            if( m_asmBytes == null )
            {
                throw new InvalidOperationException( Path.Combine( basePath, @"Avdm.Deploy.Manager.dll" ) + " not found" );
            }

            AppDomain.Unload( m_domain );
        }

        public Assembly CurrentDomainAssemblyResolve( object sender, ResolveEventArgs args )
        {
            var assemblyname = args.Name.Split( ',' )[0];

            if( assemblyname == "MongoDB.Bson" )
            {
                return m_mongoBsonAssembly;
            }

            if( assemblyname == "MongoDB.Driver" )
            {
                return m_mongoDriverAssembly;
            }

            return null;
        }

        public ISbinAssemblyResolver CreateResolver( string[] remainingArgs )
        {
            var deployManagerAsm = Assembly.Load( m_asmBytes, m_pdbBytes );
            var resolverType = deployManagerAsm.GetType( "Avdm.Deploy.Manager.SbinMongoDbAssemblyResolver", true );
            var resolver = (ISbinAssemblyResolver)Activator.CreateInstance( resolverType );
            resolver.Initialise( m_basePath, m_version, m_exeName, remainingArgs );

            return resolver;
        }

        public IEnumerable<MachineVersion> GetVersions()
        {
            return m_mongoHelper.GetVersions();
        }

        private static IStubMongoHelper GetMongoHelper( AppDomain domain )
        {
            object o = domain.CreateInstanceAndUnwrap( typeof( StubMongoHelper ).Assembly.FullName, typeof( StubMongoHelper ).FullName );
            var mongoHelper = (IStubMongoHelper)o;
            return mongoHelper;
        }

        private static AppDomain CreateHelperAppDomain()
        {
            var setup = new AppDomainSetup();
            setup.ApplicationName = "StubLoader";
            setup.ApplicationBase = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );

            AppDomain domain = AppDomain.CreateDomain( "StubLoaderDomain", null, setup );
            return domain;
        }

        private interface IStubMongoHelper
        {
            byte[] GetAssemby( string path );
            List<MachineVersion> GetVersions();
        }

        /// <summary>
        /// Loads the mongo DLLs to get the main assemblies in a separate appdomain
        /// This allows the main application to use a different version of the MongoDB 
        ///  drivers from the rest of the app. 
        /// So there are no external dependencies for sbin.exe
        /// </summary>
        private class StubMongoHelper : MarshalByRefObject, IStubMongoHelper
        {
            private readonly dynamic m_svr;
            private readonly dynamic m_deployDb;
            private readonly dynamic m_grid;
            private readonly Assembly m_mongoBsonAssembly;
            private readonly Assembly m_mongoDriverAssembly;
            private List<MachineVersion> m_versions;

            public StubMongoHelper()
            {
                var mongoConnectionString = ConfigurationManager.AppSettings["MongoDB.Server"];

                if( string.IsNullOrWhiteSpace( mongoConnectionString ) )
                {
                    throw new ArgumentNullException( "MongoDB.Server connection string not specified" );
                }

                byte[] mongoBsonBytes = GetManifestResourceBytes( typeof( SbinProgram ).Namespace + ".Resources.MongoDB.Bson.dll" );
                byte[] mongoDriverBytes = GetManifestResourceBytes( typeof( SbinProgram ).Namespace + ".Resources.MongoDB.Driver.dll" );

                m_mongoBsonAssembly = Assembly.Load( mongoBsonBytes );
                m_mongoDriverAssembly = Assembly.Load( mongoDriverBytes );
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainAssemblyResolve;

                m_svr = m_mongoDriverAssembly.GetType( "MongoDB.Driver.MongoServer" ).InvokeMember(
                    "Create",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                    null,
                    null,
                    new object[] { mongoConnectionString } );

                m_deployDb = m_svr.GetDatabase( "sbin" );
                m_grid = m_deployDb.GridFS;

                var queryType = m_mongoDriverAssembly.GetType( "MongoDB.Driver.Builders.Query" );
                var queryMatchesMethod = queryType.GetMethod( "Matches", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod );

                Func<Regex, object> createBsonRegex = r =>
                    {
                        dynamic bsonRegex = Activator.CreateInstance( queryMatchesMethod.GetParameters()[1].ParameterType, r );
                        return bsonRegex;
                    };

                g_createFileNameQuery = fileName =>
                    {
                        var regex = createBsonRegex( new Regex( Regex.Escape( fileName ), RegexOptions.IgnoreCase ) );

                        dynamic query = queryType.InvokeMember(
                            "Matches",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod,
                            null,
                            null,
                            new[] { "filename", regex } );

                        return query;
                    };
            }

            /// <summary>
            /// Expecting config of the format
            /// {
            ///   "Key" : "machineVersions",
            ///   "Value" : [{
            ///       "Machine" : "someMachineName",
            ///       "Version" : 2
            ///     }, {
            ///       "Machine" : ".*",
            ///       "Version" : 1
            ///     }]
            /// }
            /// </summary>
            /// <returns></returns>
            public List<MachineVersion> GetVersions()
            {
                if( m_versions != null )
                {
                    return m_versions;
                }

                var queryType = m_mongoDriverAssembly.GetType( "MongoDB.Driver.Builders.Query" );

                dynamic sbinConfig = m_deployDb["sbinConfig"];
                var eqMethod = queryType.GetMethod( "EQ", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod );
                
                var bsonValueType = eqMethod.GetParameters()[1].ParameterType;
                var machineVersionName = bsonValueType.InvokeMember( "Create", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, new object[] { "machineVersions" } );

                var eq = eqMethod.Invoke( null, new object[] { "Key", machineVersionName } );

                MethodInfo findOneAsMethodType = ((Type)sbinConfig.GetType()).GetMethods().First( m => m.Name == "FindOneAs" && m.IsGenericMethod && m.GetParameters().Length == 1 );
                var findAsMethod = findOneAsMethodType.MakeGenericMethod( new[] { typeof( MachineVersions ) } );
                var versions = (MachineVersions)findAsMethod.Invoke( sbinConfig, new[] { eq } );
                m_versions = versions != null ? versions.Value : new List<MachineVersion>();
                return m_versions;
            }

            public byte[] GetAssemby( string path )
            {
                return ReadMongoFile( path );
            }

            private byte[] ReadMongoFile( string fileName )
            {
                var found = m_grid.FindOne( g_createFileNameQuery( fileName ) );

                byte[] bytes;
                using( var g = found.OpenRead() )
                {
                    bytes = new byte[g.Length];
                    g.Read( bytes, 0, bytes.Length );
                }

                return bytes;
            }

            private byte[] GetManifestResourceBytes( string name )
            {
                using( var strm = GetType().Assembly.GetManifestResourceStream( name ) )
                {
                    var bytes = new byte[strm.Length];
                    strm.Read( bytes, 0, (int)strm.Length );
                    return bytes;
                }
            }

            private Assembly CurrentDomainAssemblyResolve( object sender, ResolveEventArgs args )
            {
                var assemblyname = args.Name.Split( ',' )[0];

                if( assemblyname == "MongoDB.Bson" )
                {
                    return m_mongoBsonAssembly;
                }

                if( assemblyname == "MongoDB.Driver" )
                {
                    return m_mongoDriverAssembly;
                }

                return null;
            }
        }
    }
}