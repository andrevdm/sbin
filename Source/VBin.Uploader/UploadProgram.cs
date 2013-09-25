using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using NDesk.Options;

namespace VBin.Uploader
{
    public class UploadProgram
    {
        static int Main( string[] args )
        {
            long version = -1;
            string host = "localhost";
            int port = 27017;
            bool showHelp = false;
            string filespec = null;
            string sourcePath = ".";
            bool isAspUpload = false;
            string aspnetSite = null;

            var os = new OptionSet
                         {
                             {
                                 "t|type=", "type of upload: standard/aspnet. Requires --aspSite=...",
                                 v => isAspUpload = v.Trim().ToLower() == "aspnet"
                             },

                             {
                                 "v|version=", "version",
                                 v => version = long.Parse( v.Trim() )
                             },

                             {
                                 "h|host=", "mongodb host",
                                 v => host = v.Trim()
                             },

                             {
                                 "p|port=", "mongodb port",
                                 v => port = int.Parse( v.Trim() )
                             },

                             {
                                 "fileSpec=", "regex file spec to match",
                                 v => filespec = v.Trim()
                             },

                             {
                                 "sourcePath=", "source path for the files to upload",
                                 v => sourcePath = v.Trim()
                             },

                             {
                                 "aspnetSite=", "ASP.NET site being uploaded to. Requires --type=aspnet",
                                 v => aspnetSite = v.Trim()
                             },

                             {
                                 "help", "show help",
                                 v => showHelp = true
                             },
                         };

            os.Parse( args );

            if( showHelp || (version <= 0) || (new[] { host, sourcePath }.Any( string.IsNullOrWhiteSpace )) )
            {
                Console.WriteLine();
                os.WriteOptionDescriptions( Console.Out );
                return 1;
            }

            if( isAspUpload && string.IsNullOrWhiteSpace( aspnetSite ) )
            {
                Console.WriteLine( "--aspnetSite option is required when --type=aspnet" );
                return 10;
            }

            if( !isAspUpload && !string.IsNullOrWhiteSpace( aspnetSite ) )
            {
                Console.WriteLine( "isAspUpload option is required when --type=aspnet" );
                return 10;
            }

            if( string.IsNullOrWhiteSpace( filespec ) )
            {
                filespec = !isAspUpload ? @"(?<!vshost)\.((exe)|(dll)|(pdb))$" : @"\.((gif)|(jpg)|(png)|(js)|(css)|(asmx))$";
            }

            string basePath = !isAspUpload ? version + "\\" : string.Format( "aspnet\\{0}\\{1}\\", aspnetSite, version );

            Console.WriteLine( "Uploading to " + basePath );
            Console.WriteLine( "   FileSpec {0}", filespec );
            Console.Title += " - " + basePath;

            string mongoConnection = "mongodb://" + host + ":" + port;

            var client = new MongoClient( mongoConnection );
            var svr = client.GetServer();
            var db = svr.GetDatabase( ConfigurationManager.AppSettings["VBinDatabase"] );
            var grid = db.GridFS;

            var fileSpecRegex = new Regex( filespec, RegexOptions.IgnoreCase );

            foreach( var file in Directory.GetFiles( sourcePath ) )
            {
                if( !fileSpecRegex.IsMatch( Path.GetFileName( file ) ) )
                {
                    continue;
                }

                using( var strm = File.OpenRead( file ) )
                {
                    string remoteFileName = Path.Combine( basePath, Path.GetFileName( file ) );

                    Console.WriteLine( "{0} -> {1}", file, remoteFileName );

                    grid.Delete( remoteFileName );
                    grid.Upload( strm, remoteFileName );
                }
            }

            return 0;
        }
    }
}
