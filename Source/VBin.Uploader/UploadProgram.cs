using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
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
            bool checkBeforeUpload = false;
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
                                 "m|md5", "Check if file exists using mongo's MD5 hash before uploading a new one",
                                 v => checkBeforeUpload = true
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

            Console.WriteLine();

			foreach( var file in Directory.GetFiles( sourcePath ) )
            {
                if( !fileSpecRegex.IsMatch( Path.GetFileName( file ) ) )
                {
                    continue;
                }

                using( var strm = File.OpenRead( file ) )
                {
					string remoteFileName = basePath + Path.GetFileName( file );

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine( "{0} -> {1}", file, remoteFileName );
                    Console.ResetColor();

                    if( checkBeforeUpload )
                    {
                        var f = grid.FindOne( Query.EQ( "filename", remoteFileName ) );

                        if( f != null )
                        {
                            string hash;

                            using( var md5 = MD5.Create() )
                            {
                                using( var fstrm = File.OpenRead( file ) )
                                {
                                    hash = BitConverter.ToString( md5.ComputeHash( fstrm ) ).Replace( "-", "" );
                                }
                            }

                            if( string.Compare( f.MD5, hash, StringComparison.InvariantCultureIgnoreCase ) == 0 )
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine( "   File already up to date - skipping" );                 
                                Console.WriteLine();
                                Console.ResetColor();
                                continue;
                            }
                        }
                    }

                    grid.Delete( remoteFileName );
                    grid.Upload( strm, remoteFileName );
                }
            }

            Console.WriteLine();
            return 0;
        }
    }
}
