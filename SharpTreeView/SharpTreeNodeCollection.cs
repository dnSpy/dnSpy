// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ICSharpCode.TreeView
{
	public class SharpTreeNodeCollection : ObservableCollection<SharpTreeNode>
	{
		public SharpTreeNodeCollection(SharpTreeNode parent)
		{
			Parent = parent;
		}

		public SharpTreeNode Parent { get; private set; }

		protected override void InsertItem(int index, SharpTreeNode node)
		{
			node.Parent = Parent;
			base.InsertItem(index, node);
		}

		protected override void RemoveItem(int index)
		{
			var node = this[index];
			node.Parent = null;
			base.RemoveItem(index);
		}

		protected override void ClearItems()
		{
			/*foreach (var node in this) {
				node.Parent = null;
			}
			base.ClearItems();*/
			
			// workaround for bug (reproducable when using ILSpy search filter)
			while (Count > 0)
				RemoveAt(Count - 1);
		}

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			base.OnCollectionChanged(e);
			Parent.OnChildrenChanged(e);
		}
	}
}
