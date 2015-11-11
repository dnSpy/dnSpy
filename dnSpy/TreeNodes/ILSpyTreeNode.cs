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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.dntheme;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes {
	/// <summary>
	/// Base class of all ILSpy tree nodes.
	/// </summary>
	public abstract class ILSpyTreeNode : SharpTreeNode {
		public static readonly RoutedUICommand TreeNodeActivatedEvent = new RoutedUICommand("TreeNodeActivated", "TreeNodeActivated", typeof(ILSpyTreeNode));
		FilterSettings filterSettings;
		bool childrenNeedFiltering;

		public sealed override void ActivateItem(RoutedEventArgs e) {
			ActivateItemInternal(e);
			if (!e.Handled) {
				var asmList = GetNode<DnSpyFileListTreeNode>(this);
				var inputElem = asmList == null ? null : asmList.OwnerTreeView as IInputElement;
				if (inputElem != null) {
					if (TreeNodeActivatedEvent.CanExecute(this, inputElem))
						TreeNodeActivatedEvent.Execute(this, inputElem);
				}
			}
		}

		protected virtual void ActivateItemInternal(RoutedEventArgs e) {
		}

		public override bool SingleClickExpandsChildren {
			get { return Options.DisplaySettingsPanel.CurrentDisplaySettings.SingleClickExpandsChildren; }
		}

		public FilterSettings FilterSettings {
			get { return filterSettings; }
			set {
				if (filterSettings != value) {
					filterSettings = value;
					OnFilterSettingsChanged();
				}
			}
		}

		object cacheText;
		public sealed override object Text {
			get {
				var gen = UISyntaxHighlighter.CreateTreeView();

				if (cacheText != null && !gen.IsSyntaxHighlighted)
					return cacheText;
				else
					cacheText = null;

				Write(gen.TextOutput, Language);

				var text = gen.CreateTextBlock(filterOutNewLines: true);
				if (!gen.IsSyntaxHighlighted)
					cacheText = text;
				return text;
			}
		}

		protected abstract void Write(ITextOutput output, Language language);

		public Language Language {
			get { return filterSettings != null ? filterSettings.Language : Languages.AllLanguages[0]; }
		}

		object cacheToolTip;
		public override object ToolTip {
			get {
				var gen = UISyntaxHighlighter.CreateTreeView();

				if (cacheToolTip != null && !gen.IsSyntaxHighlighted)
					return cacheToolTip;
				else
					cacheToolTip = null;

				Write(gen.TextOutput, Language);

				var text = gen.CreateTextBlock(filterOutNewLines: false);
				if (!gen.IsSyntaxHighlighted)
					cacheToolTip = text;
				return text;
			}
		}

		public override string ToString() {
			return ToString(Language);
		}

		public string ToString(Language language) {
			var output = new PlainTextOutput();
			Write(output, language);
			return output.ToString();
		}

		public virtual FilterResult Filter(FilterSettings settings) {
			if (string.IsNullOrEmpty(settings.SearchTerm))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}

		public abstract void Decompile(Language language, ITextOutput output, DecompilationOptions options);

		public virtual object GetViewObject(TextView.DecompilerTextView textView) {
			return null;
		}

		/// <summary>
		/// Used to implement special view logic for some items.
		/// This method is called on the main thread when only a single item is selected.
		/// If it returns false, normal decompilation is used to view the item.
		/// </summary>
		public virtual bool View(TextView.DecompilerTextView textView) {
			return false;
		}

		/// <summary>
		/// Used to implement special save logic for some items.
		/// This method is called on the main thread when only a single item is selected.
		/// If it returns false, normal decompilation is used to save the item.
		/// </summary>
		public virtual bool Save(TextView.DecompilerTextView textView) {
			return false;
		}

		protected override void OnChildrenChanged(NotifyCollectionChangedEventArgs e) {
			// Make sure to call the base before executing the other code. ApplyFilterToChild()
			// could result in an assembly resolve which could then add a new assembly to the
			// assembly list and trigger a new OnChildrenChanged(). This would then lead to an
			// exception.
			base.OnChildrenChanged(e);

			if (Parent == null)
				childrenNeedFiltering = true;
			else {
				if (e.NewItems != null) {
					if (IsHidden)
						ReapplyFilter();
					if (IsVisible) {
						foreach (ILSpyTreeNode node in e.NewItems)
							ApplyFilterToChild(node);
					}
					else {
						childrenNeedFiltering = true;
					}
				}

				if (IsVisible && Children.Count == 0)
					ReapplyFilter();
			}
		}

		void ReapplyFilter() {
			var parent = this.Parent as ILSpyTreeNode ?? this;
			parent.ApplyFilterToChild(this);
		}

		void ApplyFilterToChild(ILSpyTreeNode child) {
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
				if (child.childrenNeedFiltering && child.Children.Count > 0)
					child.EnsureChildrenFiltered();
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

		static FilterSettings StripSearchTerm(FilterSettings filterSettings) {
			if (filterSettings == null)
				return null;
			if (!string.IsNullOrEmpty(filterSettings.SearchTerm)) {
				filterSettings = filterSettings.Clone();
				filterSettings.SearchTerm = null;
			}
			return filterSettings;
		}

		protected virtual void OnFilterSettingsChanged() {
			RaiseUIPropsChanged();
			if (IsVisible) {
				foreach (ILSpyTreeNode node in this.Children.OfType<ILSpyTreeNode>())
					ApplyFilterToChild(node);
			}
			else {
				childrenNeedFiltering = true;
			}
		}

		protected override void OnIsVisibleChanged() {
			base.OnIsVisibleChanged();
			if (childrenNeedFiltering && Children.Count > 0)
				EnsureChildrenFiltered();
		}

		public void EnsureChildrenFiltered() {
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

		public virtual bool IsAutoLoaded {
			get { return false; }
		}

		public override System.Windows.Media.Brush Foreground {
			get {
				if (IsPublicAPI)
					if (IsAutoLoaded) {
						return Themes.Theme.GetColor(ColorType.NodeAutoLoaded).InheritedColor.Foreground.GetBrush(null);
					}
					else {
						return Themes.Theme.GetColor(ColorType.NodePublic).InheritedColor.Foreground.GetBrush(null);
					}
				else
					return Themes.Theme.GetColor(ColorType.NodeNotPublic).InheritedColor.Foreground.GetBrush(null);
			}
		}

		public abstract NodePathName NodePathName { get; }

		internal static ModuleDef GetModule(IList<SharpTreeNode> nodes) {
			if (nodes == null || nodes.Count < 1)
				return null;
			return GetModule(nodes[0]);
		}

		internal static ModuleDef GetModule(SharpTreeNode node) {
			var asmNode = GetNode<AssemblyTreeNode>(node);
			return asmNode == null ? null : asmNode.DnSpyFile.ModuleDef;
		}

		public static T GetNode<T>(SharpTreeNode node) where T : SharpTreeNode {
			while (node != null) {
				var foundNode = node as T;
				if (foundNode != null)
					return foundNode;
				node = node.Parent;
			}
			return null;
		}

		protected void GetStartIndex(SharpTreeNode node, out int start, out int end) {
			EnsureChildrenFiltered();

			if (!SortOnNodeType) {
				start = 0;
				end = Children.Count;
				return;
			}

			for (int i = 0; i < Children.Count; i++) {
				if (node.GetType() == Children[i].GetType()) {
					start = i;
					for (i++; i < Children.Count; i++) {
						if (node.GetType() != Children[i].GetType())
							break;
					}
					end = i;
					return;
				}
			}

			IList<Type> typeOrder = ChildTypeOrder;
			if (typeOrder != null) {
				int typeIndex = typeOrder.IndexOf(node.GetType());
				Debug.Assert(typeIndex >= 0);
				if (typeIndex >= 0) {
					Type prevType = null;
					for (int i = 0; i < Children.Count; i++) {
						var type = Children[i].GetType();
						if (prevType == type)
							continue;
						prevType = type;
						int index = typeOrder.IndexOf(type);
						Debug.Assert(index >= 0);
						if (typeIndex < index) {
							start = end = i;
							return;
						}
					}
				}
			}

			start = end = Children.Count;
		}

		protected virtual bool SortOnNodeType {
			get { return true; }
		}

		protected virtual Type[] ChildTypeOrder {
			get { return null; }
		}

		protected int GetNewChildIndex(SharpTreeNode node, Func<SharpTreeNode, SharpTreeNode, int> comparer) {
			int start, end;
			GetStartIndex(node, out start, out end);
			return GetNewChildIndex(start, end, node, comparer);
		}

		protected int GetNewChildIndex(int start, int end, SharpTreeNode node, Func<SharpTreeNode, SharpTreeNode, int> comparer) {
			for (int i = start; i < end; i++) {
				if (comparer(node, Children[i]) < 0)
					return i;
			}
			return end;
		}

		/// <summary>
		/// Gets the index where <paramref name="node"/> should be inserted or -1 if unknown
		/// </summary>
		/// <param name="node">New child node</param>
		/// <returns></returns>
		protected virtual int GetNewChildIndex(SharpTreeNode node) {
			return -1;
		}

		/// <summary>
		/// Add <paramref name="node"/> to the <see cref="Children"/> collection. The default
		/// implementation adds <paramref name="node"/> to the end but it could be added anywhere.
		/// </summary>
		/// <param name="node">Node to add</param>
		public void AddToChildren(SharpTreeNode node) {
			EnsureChildrenFiltered();
			int index = GetNewChildIndex(node);
			if (index < 0 || index > Children.Count)
				Children.Add(node);
			else
				Children.Insert(index, node);
		}

		public virtual void RaiseUIPropsChanged() {
			RaisePropertyChanged("Icon");
			RaisePropertyChanged("ExpandedIcon");
			RaisePropertyChanged("ToolTip");
			RaisePropertyChanged("Text");
			RaisePropertyChanged("Foreground");
		}

		/// <summary>
		/// Gets full node path name of the node's ancestors.
		/// </summary>
		public FullNodePathName CreateFullNodePathName() {
			var fullPath = new FullNodePathName();
			ILSpyTreeNode node = this;
			while (node.Parent != null) {
				fullPath.Names.Add(node.NodePathName);
				node = (ILSpyTreeNode)node.Parent;
			}
			fullPath.Names.Reverse();
			return fullPath;
		}
	}
}