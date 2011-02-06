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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Base class of all ILSpy tree nodes.
	/// </summary>
	abstract class ILSpyTreeNodeBase : SharpTreeNode
	{
		FilterSettings filterSettings;
		
		public FilterSettings FilterSettings {
			get { return filterSettings; }
			set {
				if (filterSettings != value) {
					filterSettings = value;
					OnFilterSettingsChanged();
				}
			}
		}
		
		public Language Language {
			get { return filterSettings.Language; }
		}
		
		public SharpTreeNodeCollection VisibleChildren {
			get { return base.Children; }
		}
		
		protected abstract void OnFilterSettingsChanged();
		
		public virtual FilterResult Filter(FilterSettings settings)
		{
			if (string.IsNullOrEmpty(settings.SearchTerm))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}
		
		public virtual void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
		}
	}
	
	enum FilterResult
	{
		/// <summary>
		/// Hides the node.
		/// </summary>
		Hidden,
		/// <summary>
		/// Shows the node (and resets the search term for child nodes).
		/// </summary>
		Match,
		/// <summary>
		/// Hides the node only if all children are hidden (and resets the search term for child nodes).
		/// </summary>
		MatchAndRecurse,
		/// <summary>
		/// Hides the node only if all children are hidden (doesn't reset the search term for child nodes).
		/// </summary>
		Recurse
	}
	
	/// <summary>
	/// Base class for ILSpy tree nodes.
	/// </summary>
	class ILSpyTreeNode<T> : ILSpyTreeNodeBase where T : ILSpyTreeNodeBase
	{
		public ILSpyTreeNode()
			: this(new ObservableCollection<T>())
		{
		}
		
		public ILSpyTreeNode(ObservableCollection<T> children)
		{
			if (children == null)
				throw new ArgumentNullException("children");
			this.allChildren = children;
			children.CollectionChanged += allChildren_CollectionChanged;
		}
		
		void allChildren_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var visibleChildren = this.VisibleChildren;
			
			switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					if (e.NewItems.Count == 1 && e.NewStartingIndex == allChildren.Count - 1) {
						T newChild = (T)e.NewItems[0];
						if (FilterChild(newChild))
							visibleChildren.Add(newChild);
						break;
					} else {
						goto default;
					}
				default:
					ResetChildren();
					break;
			}
		}
		
		void ResetChildren()
		{
			var visibleChildren = this.VisibleChildren;
			
			visibleChildren.Clear();
			foreach (T child in allChildren) {
				if (FilterChild(child))
					visibleChildren.Add(child);
			}
		}
		
		bool FilterChild(T child)
		{
			FilterResult r;
			if (this.FilterSettings == null)
				r = FilterResult.Match;
			else
				r = child.Filter(this.FilterSettings);
			switch (r) {
				case FilterResult.Hidden:
					return false;
				case FilterResult.Match:
					child.FilterSettings = StripSearchTerm(this.FilterSettings);
					return true;
				case FilterResult.Recurse:
					child.FilterSettings = this.FilterSettings;
					child.EnsureLazyChildren();
					return child.VisibleChildren.Count > 0;
				case FilterResult.MatchAndRecurse:
					child.FilterSettings = StripSearchTerm(this.FilterSettings);
					child.EnsureLazyChildren();
					return child.VisibleChildren.Count > 0;
				default:
					throw new InvalidEnumArgumentException();
			}
		}
		
		FilterSettings StripSearchTerm(FilterSettings filterSettings)
		{
			if (filterSettings == null)
				return null;
			if (!string.IsNullOrEmpty(filterSettings.SearchTerm)) {
				filterSettings = filterSettings.Clone();
				filterSettings.SearchTerm = null;
			}
			return filterSettings;
		}
		
		protected override void OnFilterSettingsChanged()
		{
			var visibleChildren = this.VisibleChildren;
			var allChildren = this.Children;
			int j = 0;
			for (int i = 0; i < allChildren.Count; i++) {
				T child = allChildren[i];
				if (j < visibleChildren.Count && visibleChildren[j] == child) {
					// it was visible before
					if (FilterChild(child)) {
						j++; // keep it visible
					} else {
						visibleChildren.RemoveAt(j); // hide it
					}
				} else {
					// it wasn't visible before
					if (FilterChild(child)) {
						// make it visible
						visibleChildren.Insert(j++, child);
					}
				}
			}
			RaisePropertyChanged("Text");
		}
		
		readonly ObservableCollection<T> allChildren;
		
		public new ObservableCollection<T> Children {
			get { return allChildren; }
		}
	}
}
