// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Utils;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;
using Microsoft.Win32;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	public class ResourceTreeNode : ILSpyTreeNode
	{
		Resource r;
		
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
		
		internal override bool View(DecompilerTextView textView)
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
						textView.Show(output, HighlightingManager.Instance.GetDefinitionByExtension(ext));
						return true;
					}
				}
			}
			return false;
		}
		
		public override bool Save(TextView.DecompilerTextView textView)
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
	
	/// <summary>
	/// This interface allows plugins to create custom nodes for resources.
	/// </summary>
	public interface IResourceNodeFactory
	{
		ILSpyTreeNode CreateNode(Resource resource);
		ILSpyTreeNode CreateNode(string key, Stream data);
	}
	
	[Export(typeof(IResourceNodeFactory))]
	sealed class ResourcesFileTreeNodeFactory : IResourceNodeFactory
	{
		public ILSpyTreeNode CreateNode(Resource resource)
		{
			EmbeddedResource er = resource as EmbeddedResource;
			if (er != null && er.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)) {
				return new ResourcesFileTreeNode(er);
			}
			return null;
		}
		
		public ILSpyTreeNode CreateNode(string key, Stream data)
		{
			return null;
		}
	}
	
	sealed class ResourcesFileTreeNode : ResourceTreeNode
	{
		public ResourcesFileTreeNode(EmbeddedResource er) : base(er)
		{
			this.LazyLoading = true;
		}

		public override object Icon
		{
			get { return Images.ResourceResourcesFile; }
		}

		protected override void LoadChildren()
		{
			EmbeddedResource er = this.Resource as EmbeddedResource;
			if (er != null) {
				Stream s = er.GetResourceStream();
				s.Position = 0;
				ResourceReader reader;
				try {
					reader = new ResourceReader(s);
				} catch (ArgumentException) {
					return;
				}
				foreach (DictionaryEntry entry in reader.Cast<DictionaryEntry>().OrderBy(e => e.Key.ToString())) {
					if (entry.Value is Stream)
						Children.Add(ResourceEntryNode.Create(entry.Key.ToString(), (Stream)entry.Value));
				}
			}
		}
	}
}
