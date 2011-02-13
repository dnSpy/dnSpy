// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ICSharpCode.TreeView
{
	sealed class TreeFlattener : IList, INotifyCollectionChanged
	{
		readonly SharpTreeNode root;
		readonly bool includeRoot;
		readonly object syncRoot = new object();
		
		public TreeFlattener(SharpTreeNode root, bool includeRoot)
		{
			this.root = root;
			this.includeRoot = includeRoot;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;
		
		public void Start()
		{
			
		}
		
		public void Stop()
		{
			
		}
		
		public object this[int index] {
			get {
				if (index < 0 || index >= this.Count)
					throw new ArgumentOutOfRangeException();
				return SharpTreeNode.GetNodeByVisibleIndex(root, includeRoot ? index : index - 1);
			}
			set {
				throw new NotSupportedException();
			}
		}
		
		public int Count {
			get {
				return includeRoot ? root.totalListLength : root.totalListLength - 1;
			}
		}
		
		public int IndexOf(object item)
		{
			SharpTreeNode node = item as SharpTreeNode;
			if (node != null && node.AncestorsAndSelf().Last() == root) {
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
