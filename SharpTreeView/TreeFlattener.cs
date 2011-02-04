// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;

namespace ICSharpCode.TreeView
{
	class TreeFlattener
	{
		public TreeFlattener(SharpTreeNode root, bool includeRoot)
		{
			this.root = root;
			this.includeRoot = includeRoot;
			List = new ObservableCollection<SharpTreeNode>();
		}

		SharpTreeNode root;
		bool includeRoot;

		public ObservableCollection<SharpTreeNode> List { get; private set; }

		public void Start()
		{
			if (includeRoot) {
				Add(root);
			}
			else {
				root.Children.CollectionChanged += node_ChildrenChanged;
			}

			foreach (var node in root.ExpandedDescendants()) {
				Add(node);
			}
		}

		public void Stop()
		{
			while (List.Count > 0) {
				RemoveAt(0);
			}
		}

		void Add(SharpTreeNode node)
		{
			Insert(List.Count, node);
		}

		void Insert(int index, SharpTreeNode node)
		{
			List.Insert(index, node);
			node.PropertyChanged += node_PropertyChanged;
			if (node.IsExpanded) {
				node.Children.CollectionChanged += node_ChildrenChanged;
			}
		}

		void RemoveAt(int index)
		{
			var node = List[index];
			List.RemoveAt(index);
			node.PropertyChanged -= node_PropertyChanged;
			if (node.IsExpanded) {
				node.Children.CollectionChanged -= node_ChildrenChanged;
			}
		}

		void ClearDescendants(SharpTreeNode node)
		{
			var index = List.IndexOf(node);
			while (index + 1 < List.Count && List[index + 1].Level > node.Level) {
				RemoveAt(index + 1);
			}
		}

		void node_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsExpanded") {
				var node = sender as SharpTreeNode;

				if (node.IsExpanded) {
					var index = List.IndexOf(node);
					foreach (var childNode in node.ExpandedDescendants()) {
						Insert(++index, childNode);
					}
					node.Children.CollectionChanged += node_ChildrenChanged;
				}
				else {
					ClearDescendants(node);
					node.Children.CollectionChanged -= node_ChildrenChanged;
				}
			}
		}

		void Insert(SharpTreeNode parent, int index, SharpTreeNode node)
		{
			int finalIndex = 0;
			if (index > 0) {
				finalIndex = List.IndexOf(parent.Children[index - 1]) + 1;
				while (finalIndex < List.Count && List[finalIndex].Level > node.Level) {
					finalIndex++;
				}
			}
			else {
				finalIndex = List.IndexOf(parent) + 1;
			}
			Insert(finalIndex, node);
		}

		void RemoveAt(SharpTreeNode parent, int index, SharpTreeNode node)
		{
			var i = List.IndexOf(node);
			foreach (var child in node.ExpandedDescendantsAndSelf()) {
				RemoveAt(i);
			}			
		}

		void node_ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var collection = sender as SharpTreeNodeCollection;
			var parent = collection.Parent;
			var index = List.IndexOf(collection.Parent) + 1;

			switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					Insert(parent, e.NewStartingIndex, e.NewItems[0] as SharpTreeNode);
					break;
				case NotifyCollectionChangedAction.Remove:
					RemoveAt(parent, e.OldStartingIndex, e.OldItems[0] as SharpTreeNode);
					break;
				case NotifyCollectionChangedAction.Move:
					RemoveAt(parent, e.OldStartingIndex, e.OldItems[0] as SharpTreeNode);
					Insert(parent, e.NewStartingIndex, e.NewItems[0] as SharpTreeNode);
					break;
				case NotifyCollectionChangedAction.Replace:
					RemoveAt(parent, e.OldStartingIndex, e.OldItems[0] as SharpTreeNode);
					Insert(parent, e.NewStartingIndex, e.NewItems[0] as SharpTreeNode);
					break;
				case NotifyCollectionChangedAction.Reset:
					ClearDescendants(parent);
					break;
			}
		}
	}
}
