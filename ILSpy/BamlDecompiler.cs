// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Baml2006;
using System.Xaml;
using System.Xml;
using System.Xml.Linq;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace ICSharpCode.ILSpy.Baml
{
	/// <remarks>Caution: use in separate AppDomain only!</remarks>
	sealed class BamlDecompiler : MarshalByRefObject
	{
		public BamlDecompiler()
		{
		}
		
		abstract class XamlNode
		{
			public readonly List<XamlNode> Children = new List<XamlNode>();
			
			public abstract void WriteTo(XamlWriter writer);
		}

		[Conditional("DEBUG")]
		static void Log(string format, params object[] args)
		{
			//Debug.WriteLine(format, args);
		}
		
		sealed class XamlObjectNode : XamlNode
		{
			public readonly XamlType Type;
			
			public XamlObjectNode(XamlType type)
			{
				this.Type = type;
			}
			
			public override void WriteTo(XamlWriter writer)
			{
				Log("StartObject {0}", this.Type);
				writer.WriteStartObject(this.Type);
				Debug.Indent();
				foreach (XamlNode node in this.Children)
					node.WriteTo(writer);
				Debug.Unindent();
				Log("EndObject");
				writer.WriteEndObject();
			}
		}
		
		sealed class XamlGetObjectNode : XamlNode
		{
			public override void WriteTo(XamlWriter writer)
			{
				Log("GetObject");
				writer.WriteGetObject();
				Debug.Indent();
				foreach (XamlNode node in this.Children)
					node.WriteTo(writer);
				Debug.Unindent();
				Log("EndObject");
				writer.WriteEndObject();
			}
		}
		
		sealed class XamlMemberNode : XamlNode
		{
			public XamlMember Member;
			
			public XamlMemberNode(XamlMember member)
			{
				this.Member = member;
			}
			
			public override void WriteTo(XamlWriter writer)
			{
				Log("StartMember {0}", this.Member);
				writer.WriteStartMember(this.Member);
				Debug.Indent();
				foreach (XamlNode node in this.Children)
					node.WriteTo(writer);
				Debug.Unindent();
				Log("EndMember");
				writer.WriteEndMember();
			}
		}
		
		sealed class XamlValueNode : XamlNode
		{
			public readonly object Value;
			
			public XamlValueNode(object value)
			{
				this.Value = value;
			}
			
			public override void WriteTo(XamlWriter writer)
			{
				Log("Value {0}", this.Value);
				Debug.Assert(this.Children.Count == 0);
				// requires XamlReaderSettings.ValuesMustBeString = true to work properly
				writer.WriteValue(this.Value);
			}
		}
		
		sealed class XamlNamespaceDeclarationNode : XamlNode
		{
			public readonly NamespaceDeclaration Namespace;
			
			public XamlNamespaceDeclarationNode(NamespaceDeclaration @namespace)
			{
				this.Namespace = @namespace;
			}
			
			public override void WriteTo(XamlWriter writer)
			{
				Log("NamespaceDeclaration {0}", this.Namespace);
				Debug.Assert(this.Children.Count == 0);
				writer.WriteNamespace(this.Namespace);
			}
		}
		
		static List<XamlNode> Parse(XamlReader reader)
		{
			List<XamlNode> currentList = new List<XamlNode>();
			Stack<List<XamlNode>> stack = new Stack<List<XamlNode>>();
			while (reader.Read()) {
				switch (reader.NodeType) {
					case XamlNodeType.None:
						break;
					case XamlNodeType.StartObject:
						XamlObjectNode obj = new XamlObjectNode(reader.Type);
						currentList.Add(obj);
						stack.Push(currentList);
						currentList = obj.Children;
						break;
					case XamlNodeType.GetObject:
						XamlGetObjectNode getObject = new XamlGetObjectNode();
						currentList.Add(getObject);
						stack.Push(currentList);
						currentList = getObject.Children;
						break;
					case XamlNodeType.StartMember:
						XamlMemberNode member = new XamlMemberNode(reader.Member);
						currentList.Add(member);
						stack.Push(currentList);
						currentList = member.Children;
						break;
					case XamlNodeType.Value:
						currentList.Add(new XamlValueNode(reader.Value));
						break;
					case XamlNodeType.NamespaceDeclaration:
						currentList.Add(new XamlNamespaceDeclarationNode(reader.Namespace));
						break;
					case XamlNodeType.EndObject:
					case XamlNodeType.EndMember:
						currentList = stack.Pop();
						break;
					default:
						throw new InvalidOperationException("Invalid value for XamlNodeType");
				}
			}
			if (stack.Count != 0)
				throw new InvalidOperationException("Imbalanced stack");
			return currentList;
		}
		
		void AvoidContentProperties(XamlNode node)
		{
			foreach (XamlNode child in node.Children)
				AvoidContentProperties(child);
			

			XamlObjectNode obj = node as XamlObjectNode;
			if (obj != null) {
				// Visit all except for the last child:
				for (int i = 0; i < obj.Children.Count - 1; i++) {
					// Avoids using content property syntax for simple string values, if the content property is not the last member.
					// Without this, we cannot decompile &lt;GridViewColumn Header="Culture" DisplayMemberBinding="{Binding Culture}" /&gt;,
					// because the Header property is the content property, but there is no way to represent the Binding as an element.
					XamlMemberNode memberNode = obj.Children[i] as XamlMemberNode;
					if (memberNode != null && memberNode.Member == obj.Type.ContentProperty) {
						if (memberNode.Children.Count == 1 && memberNode.Children[0] is XamlValueNode) {
							// By creating a clone of the XamlMember, we prevent WPF from knowing that it's the content property.
							XamlMember member = memberNode.Member;
							memberNode.Member = new XamlMember(member.Name, member.DeclaringType, member.IsAttachable);
						}
					}
				}
				// We also need to avoid using content properties that have a markup extension as value, as the XamlXmlWriter would always expand those:
				for (int i = 0; i < obj.Children.Count; i++) {
					XamlMemberNode memberNode = obj.Children[i] as XamlMemberNode;
					if (memberNode != null && memberNode.Member == obj.Type.ContentProperty && memberNode.Children.Count == 1) {
						XamlObjectNode me = memberNode.Children[0] as XamlObjectNode;
						if (me != null && me.Type.IsMarkupExtension) {
							// By creating a clone of the XamlMember, we prevent WPF from knowing that it's the content property.
							XamlMember member = memberNode.Member;
							memberNode.Member = new XamlMember(member.Name, member.DeclaringType, member.IsAttachable);
						}
					}
				}
			}
		}

		/// <summary>
		/// It seems like BamlReader will always output 'x:Key' as last property. However, it must be specified as attribute in valid .xaml, so we move it to the front
		/// of the attribute list.
		/// </summary>
		void MoveXKeyToFront(XamlNode node)
		{
			foreach (XamlNode child in node.Children)
				MoveXKeyToFront(child);

			XamlObjectNode obj = node as XamlObjectNode;
			if (obj != null && obj.Children.Count > 0) {
				XamlMemberNode memberNode = obj.Children[obj.Children.Count - 1] as XamlMemberNode;
				if (memberNode != null && memberNode.Member == XamlLanguage.Key) {
					// move memberNode in front of the first member node:
					for (int i = 0; i < obj.Children.Count; i++) {
						if (obj.Children[i] is XamlMemberNode) {
							obj.Children.Insert(i, memberNode);
							obj.Children.RemoveAt(obj.Children.Count - 1);
							break;
						}
					}
				}
			}
		}
		
		public string DecompileBaml(MemoryStream bamlCode, string containingAssemblyFile)
		{
			bamlCode.Position = 0;
			TextWriter w = new StringWriter();
			
			Assembly assembly = Assembly.LoadFile(containingAssemblyFile);
			
			Baml2006Reader reader = new Baml2006Reader(bamlCode, new XamlReaderSettings() { ValuesMustBeString = true, LocalAssembly = assembly });
			var xamlDocument = Parse(reader);

			foreach (var xamlNode in xamlDocument) {
				AvoidContentProperties(xamlNode);
				MoveXKeyToFront(xamlNode);
			}
			
			XDocument doc = new XDocument();
			XamlXmlWriter writer = new XamlXmlWriter(doc.CreateWriter(), reader.SchemaContext, new XamlXmlWriterSettings { AssumeValidInput = true });
			foreach (var xamlNode in xamlDocument)
				xamlNode.WriteTo(writer);
			writer.Close();
			
			// Fix namespace references
			string suffixToRemove = ";assembly=" + assembly.GetName().Name;
			foreach (XAttribute attrib in doc.Root.Attributes()) {
				if (attrib.Name.Namespace == XNamespace.Xmlns) {
					if (attrib.Value.EndsWith(suffixToRemove, StringComparison.Ordinal)) {
						string newNamespace = attrib.Value.Substring(0, attrib.Value.Length - suffixToRemove.Length);
						ChangeXmlNamespace(doc, attrib.Value, newNamespace);
						attrib.Value = newNamespace;
					}
				}
			}

			return doc.ToString();
		}
		
		/// <summary>
		/// Changes all references from oldNamespace to newNamespace in the document.
		/// </summary>
		void ChangeXmlNamespace(XDocument doc, XNamespace oldNamespace, XNamespace newNamespace)
		{
			foreach (XElement e in doc.Descendants()) {
				if (e.Name.Namespace == oldNamespace)
					e.Name = newNamespace + e.Name.LocalName;
			}
		}
	}
	
	[Export(typeof(IResourceNodeFactory))]
	sealed class BamlResourceNodeFactory : IResourceNodeFactory
	{
		public ILSpyTreeNode CreateNode(Mono.Cecil.Resource resource)
		{
			return null;
		}
		
		public ILSpyTreeNode CreateNode(string key, Stream data)
		{
			if (key.EndsWith(".baml", StringComparison.OrdinalIgnoreCase))
				return new BamlResourceEntryNode(key, data);
			else
				return null;
		}
	}
	
	sealed class BamlResourceEntryNode : ResourceEntryNode
	{
		public BamlResourceEntryNode(string key, Stream data) : base(key, data)
		{
		}
		
		internal override bool View(DecompilerTextView textView)
		{
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			IHighlightingDefinition highlighting = null;
			
			textView.RunWithCancellation(
				token => Task.Factory.StartNew(
					() => {
						try {
							if (LoadBaml(output))
								highlighting = HighlightingManager.Instance.GetDefinitionByExtension(".xml");
						} catch (Exception ex) {
							output.Write(ex.ToString());
						}
						return output;
					}),
				t => textView.Show(t.Result, highlighting)
			);
			return true;
		}
		
		bool LoadBaml(AvalonEditTextOutput output)
		{
			var asm = this.Ancestors().OfType<AssemblyTreeNode>().FirstOrDefault().LoadedAssembly;
			
			AppDomain bamlDecompilerAppDomain = null;
			try {
				BamlDecompiler decompiler = CreateBamlDecompilerInAppDomain(ref bamlDecompilerAppDomain, asm.FileName);
				
				MemoryStream bamlStream = new MemoryStream();
				data.Position = 0;
				data.CopyTo(bamlStream);
				
				output.Write(decompiler.DecompileBaml(bamlStream, asm.FileName));
				return true;
			} finally {
				if (bamlDecompilerAppDomain != null)
					AppDomain.Unload(bamlDecompilerAppDomain);
			}
		}
		
		public static BamlDecompiler CreateBamlDecompilerInAppDomain(ref AppDomain appDomain, string assemblyFileName)
		{
			if (appDomain == null) {
				// Construct and initialize settings for a second AppDomain.
				AppDomainSetup bamlDecompilerAppDomainSetup = new AppDomainSetup();
				bamlDecompilerAppDomainSetup.ApplicationBase = "file:///" + Path.GetDirectoryName(assemblyFileName);
				bamlDecompilerAppDomainSetup.DisallowBindingRedirects = false;
				bamlDecompilerAppDomainSetup.DisallowCodeDownload = true;
				bamlDecompilerAppDomainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

				// Create the second AppDomain.
				appDomain = AppDomain.CreateDomain("BamlDecompiler AD", null, bamlDecompilerAppDomainSetup);
			}
			return (BamlDecompiler)appDomain.CreateInstanceFromAndUnwrap(typeof(BamlDecompiler).Assembly.Location, typeof(BamlDecompiler).FullName);
		}
	}
}