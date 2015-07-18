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
using System.Collections.Generic;
using System.IO;
using System.Text;
using dnlib.DotNet;
using dnlib.IO;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Utils;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.TreeNodes
{
	public abstract class ResourceTreeNode : ILSpyTreeNode, IResourceNode
	{
		protected Resource r;

		protected ResourceTreeNode(Resource r)
		{
			this.r = r;
		}
		
		public Resource Resource {
			get { return r; }
			internal set { r = value; }
		}
		
		protected sealed override void Write(ITextOutput output, Language language)
		{
			WriteFileName(output, r.Name);
		}

		public static void WriteFileName(ITextOutput output, string name)
		{
			name = UIUtils.CleanUpName(name);
			var s = name.Replace('\\', '/');
			var parts = s.Split('/');
			int slashIndex = 0;
			for (int i = 0; i < parts.Length - 1; i++) {
				output.Write(parts[i], TextTokenType.DirectoryPart);
				slashIndex += parts[i].Length;
				output.Write(name[slashIndex], TextTokenType.Text);
				slashIndex++;
			}
			var fn = parts[parts.Length - 1];
			int index = fn.LastIndexOf('.');
			if (index < 0)
				output.Write(fn, TextTokenType.FileNameNoExtension);
			else {
				string ext = fn.Substring(index + 1);
				fn = fn.Substring(0, index);
				output.Write(fn, TextTokenType.FileNameNoExtension);
				output.Write(".", TextTokenType.Text);
				output.Write(ext, TextTokenType.FileExtension);
			}
		}
		
		public sealed override object Icon {
			get { return ResourceUtils.GetIcon(IconName, BackgroundType.TreeNode); }
		}

		public virtual string IconName {
			get { return ResourceUtils.GetIconName(r.Name); }
		}

		public string Name {
			get { return r.Name; }
		}
		
		public override bool IsPublicAPI {
			get { return IsPublicAPIInternal(r); }
		}

		internal static bool IsPublicAPIInternal(Resource r)
		{
			return (r.Attributes & ManifestResourceAttributes.VisibilityMask) != ManifestResourceAttributes.Private;
		}

		public uint RVA {
			get {
				FileOffset fo;
				var module = GetModuleOffset(out fo);
				if (module == null)
					return 0;

				return (uint)module.MetaData.PEImage.ToRVA(fo);
			}
		}

		public long FileOffset {
			get {
				FileOffset fo;
				GetModuleOffset(out fo);
				return (long)fo;
			}
		}

		ModuleDefMD GetModuleOffset(out FileOffset fileOffset)
		{
			fileOffset = 0;

			var er = r as EmbeddedResource;
			if (er == null)
				return null;

			var module = GetModule(this) as ModuleDefMD;
			if (module == null)
				return null;

			fileOffset = er.Data.FileOffset;
			return module;
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			var res = settings.Filter.GetFilterResult(this);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
		}

		protected void Save()
		{
			AsmEditor.Resources.SaveResources.Save(new IResourceNode[] { this }, false, ResourceDataType.Deserialized);
		}
		
		public sealed override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			Decompile(language, output);
		}

		public virtual void Decompile(Language language, ITextOutput output)
		{
			language.WriteComment(output, string.Empty);
			if (Options.DecompilerSettingsPanel.CurrentDecompilerSettings.ShowTokenAndRvaComments) {
				long fo = FileOffset;
				if (fo != 0)
					output.Write(string.Format("0x{0:X8}: ", fo), TextTokenType.Comment);
			}
			output.WriteDefinition(UIUtils.CleanUpName(Name), this, TextTokenType.Comment);
			string extra = null;
			if (r.ResourceType == ResourceType.AssemblyLinked)
				extra = ((AssemblyLinkedResource)r).Assembly.FullName;
			else if (r.ResourceType == ResourceType.Linked) {
				var file = ((LinkedResource)r).File;
				extra = string.Format("{0}, {1}, {2}", file.Name, file.ContainsNoMetaData ? "ContainsNoMetaData" : "ContainsMetaData", AsmEditor.NumberVMUtils.ByteArrayToString(file.HashValue));
			}
			output.Write(string.Format(" ({0}{1}, {2})", extra == null ? string.Empty : string.Format("{0}, ", extra), r.ResourceType, r.Attributes), TextTokenType.Comment);
			output.WriteLine();
		}

		internal static bool View(ILSpyTreeNode node, DecompilerTextView textView, Stream stream, string name)
		{
			if (stream == null || stream.Length >= DecompilerTextView.DefaultOutputLengthLimit)
				return false;

			stream.Position = 0;
			FileType type = GuessFileType.DetectFileType(stream);
			if (type == FileType.Binary)
				return false;

			stream.Position = 0;
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			output.Write(FileReader.OpenStream(stream, Encoding.UTF8).ReadToEnd(), TextTokenType.Text);
			string ext;
			if (type == FileType.Xml)
				ext = ".xml";
			else {
				try {
					ext = Path.GetExtension(DecompilerTextView.CleanUpName(name));
				}
				catch (ArgumentException) {
					ext = ".txt";
				}
			}
			textView.ShowNode(output, node, HighlightingManager.Instance.GetDefinitionByExtension(ext));
			return true;
		}

		internal static string GetStringContents(Stream stream)
		{
			if (stream == null)
				return null;

			stream.Position = 0;
			if (GuessFileType.DetectFileType(stream) == FileType.Binary)
				return null;

			stream.Position = 0;
			return FileReader.OpenStream(stream, Encoding.UTF8).ReadToEnd();
		}

		public virtual string GetStringContents()
		{
			return null;
		}

		public virtual void RegenerateEmbeddedResource()
		{
			throw new NotSupportedException();
		}

		public IEnumerable<ResourceData> GetResourceData(ResourceDataType type)
		{
			switch (type) {
			case ResourceDataType.Deserialized:	return GetDeserialized();
			case ResourceDataType.Serialized:	return GetSerialized();
			default: throw new InvalidOperationException();
			}
		}

		protected virtual IEnumerable<ResourceData> GetDeserialized()
		{
			return GetSerialized();
		}

		protected virtual IEnumerable<ResourceData> GetSerialized()
		{
			var er = r as EmbeddedResource;
			if (er != null)
				yield return new ResourceData(r.Name, () => new MemoryStream(er.GetResourceData()));
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("rst", UIUtils.CleanUpName(r.Name) + " - " + r.ResourceType.ToString()); }
		}
	}
}
