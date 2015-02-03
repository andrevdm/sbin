using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using VBin.AspNet.VirtualWebSite;

[assembly: PreApplicationStartMethod( typeof( Initializer ), "Initialize" )]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle( "VBin.AspNet.VirtualWebSite" )]
[assembly: AssemblyDescription( "" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "" )]
[assembly: AssemblyProduct( "VBin.AspNet.VirtualWebSite" )]
[assembly: AssemblyCopyright( "Copyright © Andre Van Der Merwe 2013-2014" )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid( "97076ad4-a178-4218-9617-0f66862e39bc" )]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion( "1.5.1.*" )]
[assembly: AssemblyFileVersion( "1.0.0.0" )]
