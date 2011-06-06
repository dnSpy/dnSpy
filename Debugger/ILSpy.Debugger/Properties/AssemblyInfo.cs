#region Using directives

using System;
using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;

#endregion

[assembly: AssemblyTitle("ILspy.Debugger")]
[assembly: AssemblyDescription("ILSpy debugger engine")]
[assembly: AssemblyCompany("ic#code")]
[assembly: AssemblyProduct("ILSpy")]
[assembly: AssemblyCopyright("Copyright 2011 AlphaSierraPara for the SharpDevelop Team")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// This sets the default COM visibility of types in the assembly to invisible.
// If you need to expose a type to COM, use [ComVisible(true)] on that type.
[assembly: ComVisible(false)]

[assembly: AssemblyVersion("0.1.0.208")]
[assembly: AssemblyInformationalVersion("0.1.0.208-Debugger-alpha-5eadd1d9")]
[assembly: NeutralResourcesLanguage("en-US")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2243:AttributeStringLiteralsShouldParseCorrectly",
    Justification = "AssemblyInformationalVersion does not need to be a parsable version")]
