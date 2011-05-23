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
		private string _xmlNamespace;
		private short _assemblyId;
		private string _clrNamespace;
		private static XmlPIMapping _default = new XmlPIMapping(PresentationNamespace, 0, String.Empty);

		public const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
		public const string PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
		public const string PresentationOptionsNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation/options";
		public const string McNamespace = "http://schemas.openxmlformats.org/markup-compatibility/2006";

		public XmlPIMapping(string xmlNamespace, short assemblyId, string clrNamespace)
		{
			_xmlNamespace = xmlNamespace;
			_assemblyId = assemblyId;
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
		/// Restituisce l'id dell'assembly
		/// </summary>
		public short AssemblyId
		{
			get { return _assemblyId; }
		}

		/// <summary>
		/// Restituisce il namespace clr
		/// </summary>
		public string ClrNamespace
		{
			get { return _clrNamespace; }
		}

		/// <summary>
		/// Restituisce il mapping di default di WPF
		/// </summary>
		public static XmlPIMapping Presentation
		{
			get { return _default; }
		}
	}
}