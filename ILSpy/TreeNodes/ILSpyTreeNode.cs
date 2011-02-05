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

namespace ICSharpCode.ILSpy
{
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
	
	public enum FilterResult
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
			this.Children = children;
			children.CollectionChanged += children_CollectionChanged;
		}
		
		void children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					if (e.NewItems.Count == 1 && e.NewStartingIndex == this.Children.Count - 1) {
						FilterChild((T)e.NewItems[0]);
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
			base.Children.Clear();
			foreach (T child in this.Children) {
				FilterChild(child);
			}
		}
		
		void FilterChild(T child)
		{
			FilterResult r;
			if (this.FilterSettings == null)
				r = FilterResult.Match;
			else
				r = child.Filter(this.FilterSettings);
			switch (r) {
				case FilterResult.Hidden:
					// don't add to base.Children
					break;
				case FilterResult.Match:
					child.FilterSettings = StripSearchTerm(this.FilterSettings);
					base.Children.Add(child);
					break;
				case FilterResult.Recurse:
					child.FilterSettings = this.FilterSettings;
					child.EnsureLazyChildren();
					if (child.VisibleChildren.Count > 0)
						base.Children.Add(child);
					break;
				case FilterResult.MatchAndRecurse:
					child.FilterSettings = StripSearchTerm(this.FilterSettings);
					child.EnsureLazyChildren();
					if (child.VisibleChildren.Count > 0)
						base.Children.Add(child);
					break;
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
			ResetChildren();
		}
		
		public new readonly ObservableCollection<T> Children;
	}
	
	class ILSpyTreeNode : ILSpyTreeNode<ILSpyTreeNodeBase> {}
}
