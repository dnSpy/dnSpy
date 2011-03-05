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

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.Baml
{
	/// <remarks>Caution: use in separate AppDomain only!</remarks>
	sealed class BamlDecompiler : MarshalByRefObject
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