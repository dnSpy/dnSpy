using System;
using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("dnSpy")]
[assembly: AssemblyDescription(".NET assembly editor, decompiler and debugger")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("dnSpy")]
[assembly: AssemblyCopyright("Copyright 2011-2015 many people. See README.txt")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("$DNSPY_INSERTVERSION$")]
[assembly: AssemblyInformationalVersion("$DNSPY_INSERTVERSION$")]
[assembly: NeutralResourcesLanguage("en-US")]

#if false
internal static class RevisionClass
{
	public const string Major = "2";
	public const string Minor = "3";
	public const string Build = "1";
	public const string Revision = "$INSERTREVISION$";
	public const string VersionName = null;
	
	public const string FullVersion = Major + "." + Minor + "." + Build + ".$INSERTREVISION$$INSERTBRANCHPOSTFIX$$INSERTVERSIONNAMEPOSTFIX$";
}
#endif
