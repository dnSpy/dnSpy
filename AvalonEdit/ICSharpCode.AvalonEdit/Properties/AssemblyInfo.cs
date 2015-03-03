// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#region Using directives

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;

#endregion

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ICSharpCode.AvalonEdit")]
[assembly: AssemblyDescription("WPF-based extensible text editor")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: CLSCompliant(true)]

[assembly:InternalsVisibleTo("ICSharpCode.AvalonEdit.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100F1844BC8CBDC3779B0E5970A30D800668414128135F5D6CD274E726F7C84FBDBF74AD1AD0D9FBA9C0A6CC64C11D0F6A9EDBBE7B32B6F19D8F734E1C130814D40DF54FF9D063CE29BF7AF86B46A69F0E2B910991B52A2AE443648E199A09547E74663CBE1E72E89365034FF53B6A3CE281415CBE7E2DFB5E40E54667F35DC04CA")]

[assembly: ThemeInfo(
	ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
	//(used if a resource is not found in the page,
	// or application resource dictionaries)
	ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
	//(used if a resource is not found in the page,
	// app, or any theme specific resource dictionaries)
)]

[assembly: System.Runtime.InteropServices.ComVisible(false)]
[assembly: AssemblyCompany("ic#code")]
[assembly: AssemblyProduct("SharpDevelop")]
[assembly: AssemblyCopyright("2000-2014 AlphaSierraPapa for the SharpDevelop Team")]
[assembly: AssemblyVersion("5.0.1")]
[assembly: NeutralResourcesLanguage("en-US")]

[assembly: XmlnsPrefix("http://icsharpcode.net/sharpdevelop/avalonedit", "avalonedit")]

[assembly: XmlnsDefinition("http://icsharpcode.net/sharpdevelop/avalonedit", "ICSharpCode.AvalonEdit")]
[assembly: XmlnsDefinition("http://icsharpcode.net/sharpdevelop/avalonedit", "ICSharpCode.AvalonEdit.Editing")]
[assembly: XmlnsDefinition("http://icsharpcode.net/sharpdevelop/avalonedit", "ICSharpCode.AvalonEdit.Rendering")]
[assembly: XmlnsDefinition("http://icsharpcode.net/sharpdevelop/avalonedit", "ICSharpCode.AvalonEdit.Highlighting")]
[assembly: XmlnsDefinition("http://icsharpcode.net/sharpdevelop/avalonedit", "ICSharpCode.AvalonEdit.Search")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2243:AttributeStringLiteralsShouldParseCorrectly",
	Justification = "AssemblyInformationalVersion does not need to be a parsable version")]

namespace ICSharpCode.NRefactory.Editor {}
