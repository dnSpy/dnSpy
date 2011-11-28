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
using System.IO;
using System.Text;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Utils;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;
using Microsoft.Win32;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// This is the default resource entry tree node, which is used if no specific
	/// <see cref="IResourceNodeFactory"/> exists for the given resource type. 
	/// </summary>
	public class ResourceTreeNode : ILSpyTreeNode
	{
		readonly Resource r;
		
		public ResourceTreeNode(Resource r)
		{
			if (r == null)
				throw new ArgumentNullException("r");
			this.r = r;
		}
		
		public Resource Resource {
			get { return r; }
		}
		
		public override object Text {
			get { return r.Name; }
		}
		
		public override object Icon {
			get { return Images.Resource; }
		}
		
		public override FilterResult Filter(FilterSettings settings)
		{
			if (!settings.ShowInternalApi && (r.Attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Private)
				return FilterResult.Hidden;
			if (settings.SearchTermMatches(r.Name))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, string.Format("{0} ({1}, {2})", r.Name, r.ResourceType, r.Attributes));
			
			ISmartTextOutput smartOutput = output as ISmartTextOutput;
			if (smartOutput != null && r is EmbeddedResource) {
				smartOutput.AddButton(Images.Save, "Save", delegate { Save(null); });
				output.WriteLine();
			}
		}
		
		public override bool View(DecompilerTextView textView)
		{
			EmbeddedResource er = r as EmbeddedResource;
			if (er != null) {
				Stream s = er.GetResourceStream();
				if (s != null && s.Length < DecompilerTextView.DefaultOutputLengthLimit) {
					s.Position = 0;
					FileType type = GuessFileType.DetectFileType(s);
					if (type != FileType.Binary) {
						s.Position = 0;
						AvalonEditTextOutput output = new AvalonEditTextOutput();
						output.Write(FileReader.OpenStream(s, Encoding.UTF8).ReadToEnd());
						string ext;
						if (type == FileType.Xml)
							ext = ".xml";
						else
							ext = Path.GetExtension(DecompilerTextView.CleanUpName(er.Name));
						textView.ShowNode(output, this, HighlightingManager.Instance.GetDefinitionByExtension(ext));
						return true;
					}
				}
			}
			return false;
		}
		
		public override bool Save(DecompilerTextView textView)
		{
			EmbeddedResource er = r as EmbeddedResource;
			if (er != null) {
				SaveFileDialog dlg = new SaveFileDialog();
				dlg.FileName = DecompilerTextView.CleanUpName(er.Name);
				if (dlg.ShowDialog() == true) {
					Stream s = er.GetResourceStream();
					s.Position = 0;
					using (var fs = dlg.OpenFile()) {
						s.CopyTo(fs);
					}
				}
				return true;
			}
			return false;
		}
		
		public static ILSpyTreeNode Create(Resource resource)
		{
			ILSpyTreeNode result = null;
			foreach (var factory in App.CompositionContainer.GetExportedValues<IResourceNodeFactory>()) {
				result = factory.CreateNode(resource);
				if (result != null)
					break;
			}
			return result ?? new ResourceTreeNode(resource);
		}
	}
}
