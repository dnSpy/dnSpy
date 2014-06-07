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
using System.Threading.Tasks;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.Xaml
{
	[Export(typeof(IResourceNodeFactory))]
	sealed class XmlResourceNodeFactory : IResourceNodeFactory
	{
		private readonly static string[] xmlFileExtensions = { ".xml", ".xsd", ".xslt" };

		public ILSpyTreeNode CreateNode(Resource resource)
		{
			EmbeddedResource er = resource as EmbeddedResource;
			if (er != null)
				return CreateNode(er.Name, er.GetResourceStream());
			return null;
		}
		
		public ILSpyTreeNode CreateNode(string key, object data)
		{
			if (!(data is Stream))
			    return null;
			foreach (string fileExt in xmlFileExtensions)
			{
				if (key.EndsWith(fileExt, StringComparison.OrdinalIgnoreCase))
					return new XmlResourceEntryNode(key, (Stream)data);
			}
			return null;
		}
	}
	
	sealed class XmlResourceEntryNode : ResourceEntryNode
	{
		string xml;
		
		public XmlResourceEntryNode(string key, Stream data)
			: base(key, data)
		{
		}
		
		public override object Icon
		{
			get
			{
				string text = (string)Text;
				if (text.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
					return Images.ResourceXml;
				else if (text.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase))
					return Images.ResourceXsd;
				else if (text.EndsWith(".xslt", StringComparison.OrdinalIgnoreCase))
					return Images.ResourceXslt;
				else
					return Images.Resource;
			}
		}

		public override bool View(DecompilerTextView textView)
		{
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			IHighlightingDefinition highlighting = null;
			
			textView.RunWithCancellation(
				token => Task.Factory.StartNew(
					() => {
						try {
							// cache read XAML because stream will be closed after first read
							if (xml == null) {
								using (var reader = new StreamReader(Data)) {
									xml = reader.ReadToEnd();
								}
							}
							output.Write(xml);
							highlighting = HighlightingManager.Instance.GetDefinitionByExtension(".xml");
						}
						catch (Exception ex) {
							output.Write(ex.ToString());
						}
						return output;
					}, token)
			).Then(t => textView.ShowNode(t, this, highlighting)).HandleExceptions();
			return true;
		}
	}
}
