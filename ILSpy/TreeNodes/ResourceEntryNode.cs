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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;
using Microsoft.Win32;
using Mono.Cecil;

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
			get { return key.ToString(); }
		}

		public override object Icon
		{
			get { return Images.Resource; }
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

		public static ILSpyTreeNode Create(string key, Stream data)
		{
			ILSpyTreeNode result = null;
			foreach (var factory in App.CompositionContainer.GetExportedValues<IResourceNodeFactory>()) {
				result = factory.CreateNode(key, data);
				if (result != null)
					break;
			}
			return result ?? new ResourceEntryNode(key, data);
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
	}

	[Export(typeof(IResourceNodeFactory))]
	sealed class ImageResourceNodeFactory : IResourceNodeFactory
	{
		static readonly string[] imageFileExtensions = { ".png", ".gif", ".bmp", ".jpg", ".ico" };

		public ILSpyTreeNode CreateNode(Mono.Cecil.Resource resource)
		{
			EmbeddedResource er = resource as EmbeddedResource;
			if (er != null) {
				return CreateNode(er.Name, er.GetResourceStream());
			}
			return null;
		}

		public ILSpyTreeNode CreateNode(string key, Stream data)
		{
			foreach (string fileExt in imageFileExtensions) {
				if (key.EndsWith(fileExt, StringComparison.OrdinalIgnoreCase))
					return new ImageResourceEntryNode(key, data);
			}
			return null;
		}
	}

	sealed class ImageResourceEntryNode : ResourceEntryNode
	{
		public ImageResourceEntryNode(string key, Stream data)
			: base(key, data)
		{
		}

		public override object Icon
		{
			get { return Images.ResourceImage; }
		}

		internal override bool View(DecompilerTextView textView)
		{
			try {
				AvalonEditTextOutput output = new AvalonEditTextOutput();
				Data.Position = 0;
				BitmapImage image = new BitmapImage();
				image.BeginInit();
				image.StreamSource = Data;
				image.EndInit();
				output.AddUIElement(() => new Image { Source = image });
				output.WriteLine();
				output.AddButton(Images.Save, "Save", delegate { Save(null); });
				textView.Show(output, null);
				return true;
			}
			catch (Exception) {
				return false;
			}
		}
	}
}
