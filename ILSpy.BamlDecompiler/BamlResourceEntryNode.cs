// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
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
				t => textView.ShowNode(t.Result, this, highlighting)
			);
			return true;
		}
		
		bool LoadBaml(AvalonEditTextOutput output)
		{
			var asm = this.Ancestors().OfType<AssemblyTreeNode>().FirstOrDefault().LoadedAssembly;
			MemoryStream bamlStream = new MemoryStream();
			Data.Position = 0;
			Data.CopyTo(bamlStream);
			bamlStream.Position = 0;
			
			XDocument xamlDocument;
			using (XmlBamlReader reader = new XmlBamlReader(bamlStream))
				xamlDocument = XDocument.Load(reader);
			
			output.Write(xamlDocument.ToString());
			return true;
		}
	}
}