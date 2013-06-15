using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using NDesk.Options;

namespace Avdm.Deploy.Uploader
{
    public class UploadProgram
    {
        static void Main( string[] args )
        {
            long version = -1;
            string host = "localhost";
            int port = 27017;
            bool showHelp = false;
            string filespec = @"(?<!vshost)\.((exe)|(dll)|(pdb))$";
            string sourcePath = ".";

            var os = new OptionSet
                         {
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
                                 "help", "show help",
                                 v => showHelp = true
                             },
                         };

            os.Parse( args );

            if( showHelp || (version <= 0) || (new[] { host, filespec, sourcePath }.Any( string.IsNullOrWhiteSpace )) )
            {
                Console.WriteLine();
                os.WriteOptionDescriptions( Console.Out );
                return;
            }

            string basePath = version.ToString() + "\\";

            Console.WriteLine( "Uploading to " + basePath );
            Console.Title += " - " + basePath;


            string mongoConnection = "mongodb://" + host + ":" + port;

            var client = new MongoClient( mongoConnection );
            var svr = client.GetServer();
            var db = svr.GetDatabase( "sbin" );
            var grid = db.GridFS;

            var fileSpecRegex = new Regex( filespec, RegexOptions.IgnoreCase );

            foreach( var file in Directory.GetFiles( sourcePath ) )
            {
                if( !fileSpecRegex.IsMatch( file ) )
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

        }
    }
}
