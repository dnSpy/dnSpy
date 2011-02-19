// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Reflection;
using System.Windows.Baml2006;
using System.Xaml;
using System.Xml;

namespace ICSharpCode.ILSpy
{
	/// <remarks>Caution: use in separate AppDomain only!</remarks>
	public class BamlDecompiler : MarshalByRefObject
	{
		public BamlDecompiler()
		{
		}
		
		public string DecompileBaml(MemoryStream bamlCode, string containingAssemblyFile)
		{
			bamlCode.Position = 0;
			TextWriter w = new StringWriter();
			
			Assembly assembly = Assembly.LoadFile(containingAssemblyFile);
			
			Baml2006Reader reader = new Baml2006Reader(bamlCode, new XamlReaderSettings() { ValuesMustBeString = true, LocalAssembly = assembly });
			XamlXmlWriter writer = new XamlXmlWriter(new XmlTextWriter(w) { Formatting = Formatting.Indented }, reader.SchemaContext);
			while (reader.Read()) {
				switch (reader.NodeType) {
					case XamlNodeType.None:

						break;
					case XamlNodeType.StartObject:
						writer.WriteStartObject(reader.Type);
						break;
					case XamlNodeType.GetObject:
						writer.WriteGetObject();
						break;
					case XamlNodeType.EndObject:
						writer.WriteEndObject();
						break;
					case XamlNodeType.StartMember:
						writer.WriteStartMember(reader.Member);
						break;
					case XamlNodeType.EndMember:
						writer.WriteEndMember();
						break;
					case XamlNodeType.Value:
						// requires XamlReaderSettings.ValuesMustBeString = true to work properly
						writer.WriteValue(reader.Value);
						break;
					case XamlNodeType.NamespaceDeclaration:
						writer.WriteNamespace(reader.Namespace);
						break;
					default:
						throw new Exception("Invalid value for XamlNodeType");
				}
			}
			return w.ToString();
		}
	}
}