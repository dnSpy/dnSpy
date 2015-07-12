#region Using directives

using System;
using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#endregion

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ILSpy")]
[assembly: AssemblyDescription(".NET assembly inspector and decompiler")]
[assembly: AssemblyCompany("ic#code")]
[assembly: AssemblyProduct("ILSpy")]
[assembly: AssemblyCopyright("Copyright 2011-2014 AlphaSierraPapa for the SharpDevelop Team")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// This sets the default COM visibility of types in the assembly to invisible.
// If you need to expose a type to COM, use [ComVisible(true)] on that type.
[assembly: ComVisible(false)]

[assembly: AssemblyVersion(RevisionClass.Major + "." + RevisionClass.Minor + "." + RevisionClass.Build + "." + RevisionClass.Revision)]
[assembly: AssemblyInformationalVersion(RevisionClass.FullVersion + "-$INSERTSHORTCOMMITHASH$")]
[assembly: NeutralResourcesLanguage("en-US")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2243:AttributeStringLiteralsShouldParseCorrectly",
	Justification = "AssemblyInformationalVersion does not need to be a parsable version")]

[assembly: InternalsVisibleTo("ILSpy.AddIn, PublicKey=0024000004800000940000000602000000240000525341310004000001000100653c4a319be4f524972c3c5bba5fd243330f8e900287d9022d7821a63fd0086fd3801e3683dbe9897f2ecc44727023e9b40adcf180730af70c81c54476b3e5ba8b0f07f5132b2c3cc54347a2c1a9d64ebaaaf3cbffc1a18c427981e2a51d53d5ab02536b7550e732f795121c38a0abfdb38596353525d034baf9e6f1fd8ac4ac")]

internal static class RevisionClass
{
	public const string Major = "2";
	public const string Minor = "3";
	public const string Build = "1";
	public const string Revision = "$INSERTREVISION$";
	public const string VersionName = null;
	
	public const string FullVersion = Major + "." + Minor + "." + Build + ".$INSERTREVISION$$INSERTBRANCHPOSTFIX$$INSERTVERSIONNAMEPOSTFIX$";
}
