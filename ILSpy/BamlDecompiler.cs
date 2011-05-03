// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Baml2006;
using System.Xaml;
using System.Xaml.Schema;
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
			Debug.WriteLine(format, args);
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
		
		AssemblyResolver asmResolver;
		
		Assembly AssemblyResolve(object sender, ResolveEventArgs args)
		{
			string path = asmResolver.FindAssembly(args.Name);
			
			if (path == null)
				return null;
			
			return Assembly.LoadFile(path);
		}
		
		public string DecompileBaml(MemoryStream bamlCode, string containingAssemblyFile, ConnectMethodDecompiler connectMethodDecompiler, AssemblyResolver asmResolver)
		{
			this.asmResolver = asmResolver;
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
			
			bamlCode.Position = 0;
			TextWriter w = new StringWriter();
			
			Assembly assembly = Assembly.LoadFile(containingAssemblyFile);
			
			Baml2006Reader reader = new Baml2006Reader(bamlCode, new XamlReaderSettings() { ValuesMustBeString = true, LocalAssembly = assembly });
			var xamlDocument = Parse(reader);
			
			string bamlTypeName = xamlDocument.OfType<XamlObjectNode>().First().Type.UnderlyingType.FullName;
			
			var eventMappings = connectMethodDecompiler.DecompileEventMappings(bamlTypeName);

			foreach (var xamlNode in xamlDocument) {
				RemoveConnectionIds(xamlNode, eventMappings, reader.SchemaContext);
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
		
		void RemoveConnectionIds(XamlNode node, Dictionary<int, EventRegistration[]> eventMappings, XamlSchemaContext context)
		{
			foreach (XamlNode child in node.Children)
				RemoveConnectionIds(child, eventMappings, context);
			
			XamlObjectNode obj = node as XamlObjectNode;
			if (obj != null && obj.Children.Count > 0) {
				var removableNodes = new List<XamlMemberNode>();
				var addableNodes = new List<XamlMemberNode>();
				foreach (XamlMemberNode memberNode in obj.Children.OfType<XamlMemberNode>()) {
					if (memberNode.Member == XamlLanguage.ConnectionId && memberNode.Children.Single() is XamlValueNode) {
						var value = memberNode.Children.Single() as XamlValueNode;
						int id;
						if (value.Value is string && int.TryParse(value.Value as string, out id) && eventMappings.ContainsKey(id)) {
							var map = eventMappings[id];
							foreach (var entry in map) {
								if (entry.IsAttached) {
									var type = context.GetXamlType(Type.GetType(entry.AttachSourceType));
									var member = new XamlMemberNode(new XamlMember(entry.EventName, type, true));
									member.Children.Add(new XamlValueNode(entry.MethodName));
									addableNodes.Add(member);
								} else {
									var member = new XamlMemberNode(obj.Type.GetMember(entry.EventName));
									member.Children.Add(new XamlValueNode(entry.MethodName));
									addableNodes.Add(member);
								}
							}
							removableNodes.Add(memberNode);
						}
					}
				}
				foreach (var rnode in removableNodes)
					node.Children.Remove(rnode);
				node.Children.InsertRange(node.Children.Count > 1 ? node.Children.Count - 1 : 0, addableNodes);
			}
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
				Data.Position = 0;
				Data.CopyTo(bamlStream);
				
				output.Write(decompiler.DecompileBaml(bamlStream, asm.FileName, new ConnectMethodDecompiler(asm), new AssemblyResolver(asm)));
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
//				bamlDecompilerAppDomainSetup.ApplicationBase = "file:///" + Path.GetDirectoryName(assemblyFileName);
				bamlDecompilerAppDomainSetup.DisallowBindingRedirects = false;
				bamlDecompilerAppDomainSetup.DisallowCodeDownload = true;
				bamlDecompilerAppDomainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

				// Create the second AppDomain.
				appDomain = AppDomain.CreateDomain("BamlDecompiler AD", null, bamlDecompilerAppDomainSetup);
			}
			return (BamlDecompiler)appDomain.CreateInstanceAndUnwrap(typeof(BamlDecompiler).Assembly.FullName, typeof(BamlDecompiler).FullName);
		}
	}
}