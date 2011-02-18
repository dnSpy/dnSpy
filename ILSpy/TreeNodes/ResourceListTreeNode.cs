// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;
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
	/// <summary>
	/// Lists the embedded resources in an assembly.
	/// </summary>
	sealed class ResourceListTreeNode : ILSpyTreeNode
	{
		readonly ModuleDefinition module;
		
		public ResourceListTreeNode(ModuleDefinition module)
		{
			this.LazyLoading = true;
			this.module = module;
		}
		
		public override object Text {
			get { return "Resources"; }
		}
		
		public override object Icon {
			get { return Images.Resource; }
		}
		
		protected override void LoadChildren()
		{
			foreach (Resource r in module.Resources)
				this.Children.Add(new ResourceTreeNode(r));
		}
		
		public override FilterResult Filter(FilterSettings settings)
		{
			if (string.IsNullOrEmpty(settings.SearchTerm))
				return FilterResult.MatchAndRecurse;
			else
				return FilterResult.Recurse;
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(EnsureLazyChildren));
			foreach (ILSpyTreeNode child in this.Children) {
				child.Decompile(language, output, options);
				output.WriteLine();
			}
		}
	}
	
	class ResourceTreeNode : ILSpyTreeNode
	{
		Resource r;
		
		public ResourceTreeNode(Resource r)
		{
			this.LazyLoading = true;
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
				smartOutput.AddButton(Images.Save, "Save", delegate { Save(); });
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
		
		public override bool Save()
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
		
		protected override void LoadChildren()
		{
			EmbeddedResource er = r as EmbeddedResource;
			if (er != null) {
				try {
					Stream s = er.GetResourceStream();
					if (er.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)) {
						ResourceSet set = new ResourceSet(s);
						foreach (DictionaryEntry entry in set.Cast<DictionaryEntry>().OrderBy(e => e.Key.ToString())) {
							if (entry.Value is Stream)
								Children.Add(new ResourceEntryNode(entry.Key.ToString(), (Stream)entry.Value));
						}
					}
				} catch (ArgumentException) {
				}
			}
		}
	}
}
