using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Compilation;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using VBin.Manager;

namespace VBin.AspNet
{
    public class AspNetVirtualSite
    {
        public string VirtualSiteName { get; private set; }

        public AspNetVirtualSite( string virtualSiteName )
        {
            if( virtualSiteName == null )
            {
                throw new ArgumentNullException( "virtualSiteName" );
            }

            VirtualSiteName = virtualSiteName;
        }

        public void Initialize()
        {
            var config = GetConfig();

            foreach( var asm in config.AspNetAssemblies )
            {
                string assemblyNameWithoutExtension = asm;

                if( Regex.IsMatch( asm, @"\.(exe|dll)$", RegexOptions.IgnoreCase ) )
                {
                    assemblyNameWithoutExtension = asm.Substring( 0, asm.Length - 4 );
                }

                var assembly = VBinManager.Resolver.GetAssembly( assemblyNameWithoutExtension );
                BuildManager.AddReferencedAssembly( assembly );
            }
        }

        private AspNetVirtualSiteConfig GetConfig()
        {
            var col = GetDatabase().GetCollection( "AspNetVirtualSites" );
            col.EnsureIndex( IndexKeys.Ascending( "Name" ), IndexOptions.SetUnique( true ) );
            var config = col.FindAs<AspNetVirtualSiteConfig>( Query.EQ( "Name", VirtualSiteName ) ).SetLimit( 1 ).FirstOrDefault();

            if( config == null )
            {
                throw new InvalidDataException( "Virtual config for site missing: " + VirtualSiteName );
            }

            return config;
        }

        private MongoDatabase GetDatabase()
        {
            string con = ConfigurationManager.AppSettings["MongoDB.Server"];
            var client = new MongoClient( con );
            var server = client.GetServer();
            return server.GetDatabase(  ConfigurationManager.AppSettings["VBinDatabase"] );
        }
    }
}