// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ICSharpCode.TreeView
{
	sealed class TreeFlattener : IList, INotifyCollectionChanged
	{
		/// <summary>
		/// The root node of the flat list tree.
		/// Tjis is not necessarily the root of the model!
		/// </summary>
		internal SharpTreeNode root;
		readonly bool includeRoot;
		readonly object syncRoot = new object();
		
		public TreeFlattener(SharpTreeNode modelRoot, bool includeRoot)
		{
			this.root = modelRoot;
			while (root.listParent != null)
				root = root.listParent;
			root.treeFlattener = this;
			this.includeRoot = includeRoot;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;
		
		public void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (CollectionChanged != null)
				CollectionChanged(this, e);
		}
		
		public void NodesInserted(int index, IEnumerable<SharpTreeNode> nodes)
		{
			if (!includeRoot) index--;
			foreach (SharpTreeNode node in nodes) {
				RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node, index++));
			}
		}
		
		public void NodesRemoved(int index, IEnumerable<SharpTreeNode> nodes)
		{
			if (!includeRoot) index--;
			foreach (SharpTreeNode node in nodes) {
				RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, node, index));
			}
		}
		
		public void Stop()
		{
			Debug.Assert(root.treeFlattener == this);
			root.treeFlattener = null;
		}
		
		public object this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException();
				return SharpTreeNode.GetNodeByVisibleIndex(root, includeRoot ? index : index + 1);
			}
			set {
				throw new NotSupportedException();
			}
		}
		
		public int Count {
			get {
				return includeRoot ? root.GetTotalListLength() : root.GetTotalListLength() - 1;
			}
		}
		
		public int IndexOf(object item)
		{
			SharpTreeNode node = item as SharpTreeNode;
			if (node != null && node.IsVisible && node.GetListRoot() == root) {
				if (includeRoot)
					return SharpTreeNode.GetVisibleIndexForNode(node);
				else
					return SharpTreeNode.GetVisibleIndexForNode(node) - 1;
			} else {
				return -1;
			}
		}
		
		bool IList.IsReadOnly {
			get { return true; }
		}
		
		bool IList.IsFixedSize {
			get { return false; }
		}
		
		bool ICollection.IsSynchronized {
			get { return false; }
		}
		
		object ICollection.SyncRoot {
			get {
				return syncRoot;
			}
		}
		
		void IList.Insert(int index, object item)
		{
			throw new NotSupportedException();
		}
		
		void IList.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}
		
		int IList.Add(object item)
		{
			throw new NotSupportedException();
		}
		
		void IList.Clear()
		{
			throw new NotSupportedException();
		}
		
		public bool Contains(object item)
		{
			return IndexOf(item) >= 0;
		}
		
		public void CopyTo(Array array, int arrayIndex)
		{
			foreach (object item in this)
				array.SetValue(item, arrayIndex++);
		}
		
		void IList.Remove(object item)
		{
			throw new NotSupportedException();
		}
		
		public IEnumerator GetEnumerator()
		{
			for (int i = 0; i < this.Count; i++) {
				yield return this[i];
			}
		}
	}
}
