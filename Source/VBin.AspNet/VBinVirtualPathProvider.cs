using System.Configuration;
using System.IO;
using System.Security.Permissions;
using System.Web;
using System.Web.Hosting;
using MongoDB.Driver;
using VBin.Manager;

namespace VBin.AspNet
{
    [AspNetHostingPermission( SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Medium )]
    [AspNetHostingPermission( SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.High )]
    public class VBinVirtualPathProvider : VirtualPathProvider
    {
        private string FormatMongoFileName( string virtualPath )
        {
            if( HttpContext.Current.Request.ApplicationPath != null )
            {
                if( virtualPath.StartsWith( HttpContext.Current.Request.ApplicationPath ) )
                {
                    virtualPath = virtualPath.Substring( HttpContext.Current.Request.ApplicationPath.Length );
                }
            }

            if( virtualPath.StartsWith( "/" ) )
            {
                virtualPath = virtualPath.Substring( 1 );
            }

            if( virtualPath.StartsWith( "~" ) )
            {
                virtualPath = virtualPath.Substring( 1 );
            }

            return string.Format( "aspnet\\{0}\\{1}\\{2}", AspNetInitializer.VirtualSite.VirtualSiteName, VBinManager.Resolver.CurrentVersion, virtualPath.Replace( "/", "\\" ) );
        }

        public override bool FileExists( string virtualPath )
        {
            string mongoFileName = FormatMongoFileName( virtualPath );

            var gridFs = GetDatabase().GridFS;
            return gridFs.Exists( mongoFileName );
        }

        public override VirtualFile GetFile( string virtualPath )
        {
            string mongoFileName = FormatMongoFileName( virtualPath );
            return new VBinVirtualFile( virtualPath, this, mongoFileName );
        }

        public Stream GetFileStream( string mongoFileName )
        {
            var strm = GetDatabase().GridFS.OpenRead( mongoFileName );
            return strm;
        }

        private MongoDatabase GetDatabase()
        {
            string con = ConfigurationManager.AppSettings["MongoDB.Server"];
            var client = new MongoClient( con );
            var server = client.GetServer();
            return server.GetDatabase( ConfigurationManager.AppSettings["VBinDatabase"] );
        }
    }
}
