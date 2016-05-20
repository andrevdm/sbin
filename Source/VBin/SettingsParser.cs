using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

namespace VBin
{
    public static class SettingsParser
    {
        public static VBinSettings ParseArgs( string[] args )
        {
            if( args.Length < 1 )
            {
                throw new ArgumentOutOfRangeException( "args", "Too few arguments. Expecting startup name" );
           }

            long? version = Match<long?>( args[0], @"-v=(?<x>\d+)", "x", s => long.Parse( s ) );
            string exeName;
            string[] remainingArgs;

            //Config when explicit version is first arg
            if( version != null )
            {
                if( args.Length < 2 )
                {
                    throw new ArgumentOutOfRangeException( "args", "Too few arguments. Missing startup name" );
                }

                exeName = args[1];
                remainingArgs = args.Length > 2 ? args.Skip( 2 ).ToArray() : new string[] { };

                return new VBinSettings( exeName, remainingArgs, version );
            }

            //Config when explicit config used
            if( args[0] == "--cfg" )
            {
                var settingArgs = args.Skip( 1 ).TakeWhile( a => a != "--" ).ToArray();
                var rest = args.SkipWhile( a => a != "--" ).Skip( 1 ).ToList();

                //Split on k=v and remove leading '-' chars from the key
                var settings = settingArgs.Select( s =>
                {
                    var idx = s.IndexOf( "=", StringComparison.InvariantCultureIgnoreCase );
                    string[] kv = idx > 0
                        ? new[] {s.Substring( 0, idx ).Trim(), s.Substring( idx + 1 ).Trim()}
                        : new[] {s, "true"};

                    kv[0] = Regex.Replace( kv[0], @"^-+", "" );

                    return kv;
                } ).ToDictionary( k => k[0], v => v[1] );

                if( rest.Count == 0 )
                    throw new ArgumentOutOfRangeException( "args", "Too few arguments. Missing startup name" );

                string ver;
                if( settings.TryGetValue( "v", out ver ) )
                    version = long.Parse( ver );

                return new VBinSettings( rest.First(), rest.Skip( 1 ).ToArray(), version, settings );
            }

            //No explicit version config
            exeName = args[0];
            remainingArgs = args.Length > 1 ? args.Skip( 1 ).ToArray() : new string[] { };
            return new VBinSettings( exeName, remainingArgs );
        }

        private static T Match<T>( string text, string pattern, string groupName, Func<string, T> onMatch )
        {
            var match = Regex.Match( text, pattern, RegexOptions.IgnoreCase );
            return match.Success ? onMatch( match.Groups[groupName].Value ) : default( T );
        }

        public class VBinSettings
        {
            public Dictionary<string, string> Settings { get; }
            public string ExeName { get; }
            public string[] RemainingArgs { get; }
            public long? Version { get; }

            public VBinSettings( string exeName, string[] remainingArgs, long? version = null, IDictionary<string, string> settings = null )
            {
                ExeName = exeName;
                RemainingArgs = remainingArgs;
                Version = version;
                Settings = new Dictionary<string, string>( settings ?? new Dictionary<string, string>(), StringComparer.InvariantCultureIgnoreCase );
            }

            public string Get( string key, string defaultIfNull )
            {
                string v;
                return Settings.TryGetValue( key, out v ) ? v : defaultIfNull;
            }

            public string GetOrConfig( string key )
            {
                return Get( key, null ) ?? ConfigurationManager.AppSettings[key];
            }
        }
    }
}