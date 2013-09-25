using System;
using System.Configuration;

namespace VBin.AspNet
{
    /// <summary>
    /// From http://shazwazza.com/post/developing-a-plugin-framework-in-aspnet-with-medium-trust
    /// </summary>
    public static class AspNetInitializer
    {
        public static AspNetVirtualSite VirtualSite { get; private set; }

        public static void Initialize()
        {
            var virtualSiteName = ConfigurationManager.AppSettings["VBinVirtualSiteName"];

            if( string.IsNullOrWhiteSpace( virtualSiteName ) )
            {
                throw new ArgumentNullException( "virtualSiteName" );
            }

            //Start VBin
            VBinProgram.Initialise( new[] {virtualSiteName + ".exe"} );

            //Create the virtual site
            VirtualSite = new AspNetVirtualSite( ConfigurationManager.AppSettings["VBinVirtualSiteName"] );
            VirtualSite.Initialize();
        }
    }
}
