using System.Web.Hosting;

namespace VBin.AspNet.VirtualWebSite.App_Code
{
    public static class AppStart
    {
        public static void AppInitialize()
        {
            HostingEnvironment.RegisterVirtualPathProvider( new VBinVirtualPathProvider() );
        }
    }
}