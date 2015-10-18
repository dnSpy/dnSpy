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
using System.Collections.Specialized;
using System.Linq;
using dnSpy.dntheme;
using dnSpy.Files;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer {
	public abstract class AnalyzerTreeNode : SharpTreeNode, IDisposable
	{
		private Language language;

		public Language Language
		{
			get { return language; }
			set
			{
				if (language != value) {
					language = value;
					foreach (var child in this.Children.OfType<AnalyzerTreeNode>())
						child.Language = value;
				}
			}
		}

		public override bool SingleClickExpandsChildren {
			get { return Options.DisplaySettingsPanel.CurrentDisplaySettings.SingleClickExpandsChildren; }
		}

		public sealed override object Text {
			get {
				var gen = UISyntaxHighlighter.CreateAnalyzerTreeView();
				Write(gen.TextOutput, Language);
				return gen.CreateObject();
			}
		}

		public string ToString(Language language)
		{
			var output = new PlainTextOutput();
			Write(output, language);
			return output.ToString();
		}

		public override string ToString()
		{
			return ToString(Language);
		}

		protected abstract void Write(ITextOutput output, Language language);

		public override System.Windows.Media.Brush Foreground {
			get { return Themes.Theme.GetColor(ColorType.NodePublic).InheritedColor.Foreground.GetBrush(null); }
		}

		public void RaiseUIPropsChanged()
		{
			RaisePropertyChanged("Icon");
			RaisePropertyChanged("ExpandedIcon");
			RaisePropertyChanged("ToolTip");
			RaisePropertyChanged("Text");
			RaisePropertyChanged("Foreground");
		}

		public override bool CanDelete()
		{
			return Parent != null && Parent.IsRoot;
		}

		public override void DeleteCore()
		{
			DisposeSelfAndChildren();
			Parent.Children.Remove(this);
		}

		public override void Delete()
		{
			DeleteCore();
		}

		protected override void OnChildrenChanged(NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null) {
				foreach (AnalyzerTreeNode a in e.NewItems.OfType<AnalyzerTreeNode>())
					a.Language = this.Language;
			}
			base.OnChildrenChanged(e);
		}
		
		/// <summary>
		/// Handles changes to the assembly list.
		/// </summary>
		public abstract bool HandleAssemblyListChanged(ICollection<DnSpyFile> removedAssemblies, ICollection<DnSpyFile> addedAssemblies);

		public abstract bool HandleModelUpdated(DnSpyFile asm);

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
		}

		public void DisposeSelfAndChildren()
		{
			foreach (var c in this.DescendantsAndSelf()) {
				var id = c as IDisposable;
				if (id != null)
					id.Dispose();
			}
		}
	}
}
