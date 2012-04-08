// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	/// <summary>
	/// Rappresenta la mappatura tra namespace XML e namespace CLR con relativo assembly
	/// </summary>
	public class XmlPIMapping
	{
		string _xmlNamespace;
		string assemblyName;
		string _clrNamespace;

		public const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
		public const string PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
		public const string PresentationOptionsNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation/options";
		public const string McNamespace = "http://schemas.openxmlformats.org/markup-compatibility/2006";

		public XmlPIMapping(string xmlNamespace, string assembly, string clrNamespace)
		{
			_xmlNamespace = xmlNamespace;
			assemblyName = assembly;
			_clrNamespace = clrNamespace;
		}

		/// <summary>
		/// Restituisce o imposta il namespace XML
		/// </summary>
		public string XmlNamespace
		{
			get { return _xmlNamespace; }
			set { _xmlNamespace = value;}
		}

		/// <summary>
		/// Name of the assembly.
		/// </summary>
		public string Assembly {
			get { return assemblyName; }
		}

		/// <summary>
		/// Restituisce il namespace clr
		/// </summary>
		public string ClrNamespace
		{
			get { return _clrNamespace; }
		}
		
		public static XmlPIMapping GetPresentationMapping(Func<short, string> assemblyResolve)
		{
			return new XmlPIMapping(PresentationNamespace, assemblyResolve(0), string.Empty);
		}
	}
}