// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.Xaml
{
	[Export(typeof(IResourceNodeFactory))]
	sealed class XamlResourceNodeFactory : IResourceNodeFactory
	{
		public ILSpyTreeNode CreateNode(Mono.Cecil.Resource resource)
		{
			return null;
		}
		
		public ILSpyTreeNode CreateNode(string key, Stream data)
		{
			if (key.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
				return new XamlResourceEntryNode(key, data);
			else
				return null;
		}
	}
	
	sealed class XamlResourceEntryNode : ResourceEntryNode
	{
		string xaml;
		
		public XamlResourceEntryNode(string key, Stream data) : base(key, data)
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
							// cache read XAML because stream will be closed after first read
							if (xaml == null) {
								using (var reader = new StreamReader(Data)) {
									xaml = reader.ReadToEnd();
								}
							}
							output.Write(xaml);
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
	}
}
