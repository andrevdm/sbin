using System;
using System.IO;
using System.Web.Hosting;

namespace VBin.AspNet
{
    public class VBinVirtualFile : VirtualFile
    {
        private readonly VBinVirtualPathProvider m_provider;
        private readonly string m_mongoFileName;

        public VBinVirtualFile( string virtualPath, VBinVirtualPathProvider provider, string mongoFileName )
            : base( virtualPath )
        {
            if( provider == null )
            {
                throw new ArgumentNullException( "provider" );
            }

            if( mongoFileName == null )
            {
                throw new ArgumentNullException( "mongoFileName" );
            }

            m_provider = provider;
            m_mongoFileName = mongoFileName;
        }

        public override Stream Open()
        {
            var strm = m_provider.GetFileStream( m_mongoFileName );
            return strm;
        }
    }
}