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
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	public abstract class AnalyzerTreeNode : SharpTreeNode
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

		public override bool CanDelete()
		{
			return Parent != null && Parent.IsRoot;
		}

		public override void DeleteCore()
		{
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
		public abstract bool HandleAssemblyListChanged(ICollection<LoadedAssembly> removedAssemblies, ICollection<LoadedAssembly> addedAssemblies);
	}
}
