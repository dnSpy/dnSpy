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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Base class of all ILSpy tree nodes.
	/// </summary>
	public abstract class ILSpyTreeNode : SharpTreeNode
	{
		FilterSettings filterSettings;
		bool childrenNeedFiltering;

		public FilterSettings FilterSettings
		{
			get { return filterSettings; }
			set
			{
				if (filterSettings != value) {
					filterSettings = value;
					OnFilterSettingsChanged();
				}
			}
		}

		public Language Language
		{
			get { return filterSettings != null ? filterSettings.Language : Languages.AllLanguages[0]; }
		}

		public virtual FilterResult Filter(FilterSettings settings)
		{
			if (string.IsNullOrEmpty(settings.SearchTerm))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}

		protected static object HighlightSearchMatch(string text, string suffix = null)
		{
			// TODO: implement highlighting the search match
			return text + suffix;
		}

		public abstract void Decompile(Language language, ITextOutput output, DecompilationOptions options);

		/// <summary>
		/// Used to implement special view logic for some items.
		/// This method is called on the main thread when only a single item is selected.
		/// If it returns false, normal decompilation is used to view the item.
		/// </summary>
		public virtual bool View(TextView.DecompilerTextView textView)
		{
			return false;
		}

		/// <summary>
		/// Used to implement special save logic for some items.
		/// This method is called on the main thread when only a single item is selected.
		/// If it returns false, normal decompilation is used to save the item.
		/// </summary>
		public virtual bool Save(TextView.DecompilerTextView textView)
		{
			return false;
		}

		protected override void OnChildrenChanged(NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null) {
				if (IsVisible) {
					foreach (ILSpyTreeNode node in e.NewItems)
						ApplyFilterToChild(node);
				} else {
					childrenNeedFiltering = true;
				}
			}
			base.OnChildrenChanged(e);
		}

		void ApplyFilterToChild(ILSpyTreeNode child)
		{
			FilterResult r;
			if (this.FilterSettings == null)
				r = FilterResult.Match;
			else
				r = child.Filter(this.FilterSettings);
			switch (r) {
				case FilterResult.Hidden:
					child.IsHidden = true;
					break;
				case FilterResult.Match:
					child.FilterSettings = StripSearchTerm(this.FilterSettings);
					child.IsHidden = false;
					break;
				case FilterResult.Recurse:
					child.FilterSettings = this.FilterSettings;
					child.EnsureChildrenFiltered();
					child.IsHidden = child.Children.All(c => c.IsHidden);
					break;
				case FilterResult.MatchAndRecurse:
					child.FilterSettings = StripSearchTerm(this.FilterSettings);
					child.EnsureChildrenFiltered();
					child.IsHidden = child.Children.All(c => c.IsHidden);
					break;
				default:
					throw new InvalidEnumArgumentException();
			}
		}

		static FilterSettings StripSearchTerm(FilterSettings filterSettings)
		{
			if (filterSettings == null)
				return null;
			if (!string.IsNullOrEmpty(filterSettings.SearchTerm)) {
				filterSettings = filterSettings.Clone();
				filterSettings.SearchTerm = null;
			}
			return filterSettings;
		}

		protected virtual void OnFilterSettingsChanged()
		{
			RaisePropertyChanged("Text");
			if (IsVisible) {
				foreach (ILSpyTreeNode node in this.Children.OfType<ILSpyTreeNode>())
					ApplyFilterToChild(node);
			} else {
				childrenNeedFiltering = true;
			}
		}

		protected override void OnIsVisibleChanged()
		{
			base.OnIsVisibleChanged();
			EnsureChildrenFiltered();
		}

		void EnsureChildrenFiltered()
		{
			EnsureLazyChildren();
			if (childrenNeedFiltering) {
				childrenNeedFiltering = false;
				foreach (ILSpyTreeNode node in this.Children.OfType<ILSpyTreeNode>())
					ApplyFilterToChild(node);
			}
		}
		
		public virtual bool IsPublicAPI {
			get { return true; }
		}

		public virtual bool IsAutoLoaded
		{
			get { return false; }
		}
		
		public override System.Windows.Media.Brush Foreground {
			get {
				if (IsPublicAPI)
					if (IsAutoLoaded) {
						// HACK: should not be hard coded?
						return System.Windows.Media.Brushes.SteelBlue;
					}
					else {
						return base.Foreground;
					}
				else
					return System.Windows.SystemColors.GrayTextBrush;
			}
		}
	}
}