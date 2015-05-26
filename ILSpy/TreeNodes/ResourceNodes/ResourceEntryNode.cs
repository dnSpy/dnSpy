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
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Entry in a .resources file
	/// </summary>
	public class ResourceEntryNode : ILSpyTreeNode
	{
		private readonly string key;
		private readonly Stream data;

		public override object Text
		{
			get { return ToString(Language); }
		}

		public override string ToString(Language language)
		{
			return CleanUpName(this.key);
		}

		public override object Icon
		{
			get { return GetIcon((string)Text, BackgroundType.TreeNode); }
		}

		internal static BitmapSource GetIcon(string name, BackgroundType bgType)
		{
			var rsrcName = "Resource";
			// Don't use Path.GetExtension() since it can throw
			int index = name.LastIndexOf('.');
			if (index >= 0) {
				var ext = name.Substring(index + 1).ToLowerInvariant();
				if (ext == "cs")
					rsrcName = "CSharpFile";
				else if (ext == "vb")
					rsrcName = "VisualBasicFile";
				else if (ext == "txt")
					rsrcName = "TextFile";
				else if (ext == "xaml" || ext == "baml")
					rsrcName = "XamlFile";
				else if (ext == "xml")
					rsrcName = "XmlFile";
				else if (ext == "xsd")
					rsrcName = "XsdFile";
				else if (ext == "xslt")
					rsrcName = "XsltFile";
				else if (ext == "png" || ext == "gif" || ext == "bmp" || ext == "jpg" || ext == "ico" || ext == "cur")
					rsrcName = "ImageFile";
			}
			return ImageCache.Instance.GetImage(rsrcName, bgType);
		}

		protected Stream Data
		{
			get { return data; }
		}


		public ResourceEntryNode(string key, Stream data)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (data == null)
				throw new ArgumentNullException("data");
			this.key = key;
			this.data = data;
		}

		public static ILSpyTreeNode Create(string key, object data)
		{
			ILSpyTreeNode result = null;
			foreach (var factory in App.CompositionContainer.GetExportedValues<IResourceNodeFactory>()) {
				result = factory.CreateNode(key, data);
				if (result != null)
					return result;
			}
			var streamData = data as Stream;
			if(streamData !=null)
				result =  new ResourceEntryNode(key, data as Stream);

			return result;
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, string.Format("{0} = {1}", key, data));
		}

		public override bool Save(DecompilerTextView textView)
		{
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.FileName = Path.GetFileName(DecompilerTextView.CleanUpName(key));
			if (dlg.ShowDialog() == true) {
				data.Position = 0;
				using (var fs = dlg.OpenFile()) {
					data.CopyTo(fs);
				}
			}
			return true;
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			var res = settings.Filter.GetFilterResult(this);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("rse", key); }
		}
	}
}
