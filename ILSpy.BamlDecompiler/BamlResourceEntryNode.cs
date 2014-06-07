// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;
using Ricciolo.StylesExplorer.MarkupReflection;

namespace ILSpy.BamlDecompiler
{
	public sealed class BamlResourceEntryNode : ResourceEntryNode
	{
		public BamlResourceEntryNode(string key, Stream data) : base(key, data)
		{
		}
		
		public override bool View(DecompilerTextView textView)
		{
			IHighlightingDefinition highlighting = null;
			
			textView.RunWithCancellation(
				token => Task.Factory.StartNew(
					() => {
						AvalonEditTextOutput output = new AvalonEditTextOutput();
						try {
							if (LoadBaml(output))
								highlighting = HighlightingManager.Instance.GetDefinitionByExtension(".xml");
						} catch (Exception ex) {
							output.Write(ex.ToString());
						}
						return output;
					}, token))
				.Then(output => textView.ShowNode(output, this, highlighting))
				.HandleExceptions();
			return true;
		}
		
		bool LoadBaml(AvalonEditTextOutput output)
		{
			var asm = this.Ancestors().OfType<AssemblyTreeNode>().FirstOrDefault().LoadedAssembly;
			Data.Position = 0;
			XDocument xamlDocument = LoadIntoDocument(asm.GetAssemblyResolver(), asm.AssemblyDefinition, Data);
			output.Write(xamlDocument.ToString());
			return true;
		}

		internal static XDocument LoadIntoDocument(IAssemblyResolver resolver, AssemblyDefinition asm, Stream stream)
		{
			XDocument xamlDocument;
			using (XmlBamlReader reader = new XmlBamlReader(stream, new CecilTypeResolver(resolver, asm)))
				xamlDocument = XDocument.Load(reader);
			ConvertConnectionIds(xamlDocument, asm);
			ConvertToEmptyElements(xamlDocument.Root);
			MoveNamespacesToRoot(xamlDocument);
			return xamlDocument;
		}

		static void ConvertConnectionIds(XDocument xamlDocument, AssemblyDefinition asm)
		{
			var attr = xamlDocument.Root.Attribute(XName.Get("Class", XmlBamlReader.XWPFNamespace));
			if (attr != null) {
				string fullTypeName = attr.Value;
				var mappings = new ConnectMethodDecompiler(asm).DecompileEventMappings(fullTypeName);
				RemoveConnectionIds(xamlDocument.Root, mappings);
			}
		}

		static void MoveNamespacesToRoot(XDocument xamlDocument)
		{
			var additionalXmlns = new List<XAttribute> {
				new XAttribute("xmlns", XmlBamlReader.DefaultWPFNamespace),
				new XAttribute(XName.Get("x", XNamespace.Xmlns.NamespaceName), XmlBamlReader.XWPFNamespace)
			};
			
			foreach (var element in xamlDocument.Root.DescendantsAndSelf()) {
				if (element.Name.NamespaceName != XmlBamlReader.DefaultWPFNamespace && !additionalXmlns.Any(ka => ka.Value == element.Name.NamespaceName)) {
					string newPrefix = new string(element.Name.LocalName.Where(c => char.IsUpper(c)).ToArray()).ToLowerInvariant();
					int current = additionalXmlns.Count(ka => ka.Name.Namespace == XNamespace.Xmlns && ka.Name.LocalName.TrimEnd(ch => char.IsNumber(ch)) == newPrefix);
					if (current > 0)
						newPrefix += (current + 1).ToString();
					XName defaultXmlns = XName.Get(newPrefix, XNamespace.Xmlns.NamespaceName);
					if (element.Name.NamespaceName != XmlBamlReader.DefaultWPFNamespace)
						additionalXmlns.Add(new XAttribute(defaultXmlns, element.Name.NamespaceName));
				}
			}
			
			foreach (var xmlns in additionalXmlns.Except(xamlDocument.Root.Attributes())) {
				xamlDocument.Root.Add(xmlns);
			}
		}
		
		static void ConvertToEmptyElements(XElement element)
		{
			foreach (var el in element.Elements()) {
				if (!el.IsEmpty && !el.HasElements && el.Value == "") {
					el.RemoveNodes();
					continue;
				}
				ConvertToEmptyElements(el);
			}
		}
		
		static void RemoveConnectionIds(XElement element, Dictionary<int, EventRegistration[]> eventMappings)
		{
			foreach (var child in element.Elements())
				RemoveConnectionIds(child, eventMappings);
			
			var removableAttrs = new List<XAttribute>();
			var addableAttrs = new List<XAttribute>();
			foreach (var attr in element.Attributes(XName.Get("ConnectionId", XmlBamlReader.XWPFNamespace))) {
				int id;
				if (int.TryParse(attr.Value, out id) && eventMappings.ContainsKey(id)) {
					var map = eventMappings[id];
					foreach (var entry in map) {
						string xmlns = ""; // TODO : implement xmlns resolver!
						if (entry.IsAttached) {
							addableAttrs.Add(new XAttribute(xmlns + entry.AttachSourceType.Name + "." + entry.EventName, entry.MethodName));
						} else {
							addableAttrs.Add(new XAttribute(xmlns + entry.EventName, entry.MethodName));
						}
					}
					removableAttrs.Add(attr);
				}
			}
			foreach (var attr in removableAttrs)
				attr.Remove();
			element.Add(addableAttrs);
		}
	}
}