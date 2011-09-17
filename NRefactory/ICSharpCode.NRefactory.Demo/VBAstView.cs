// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections;
using System.Windows.Forms;
using System.Reflection;

using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB;

namespace ICSharpCode.NRefactory.Demo
{
	public partial class VBAstView
	{
		CompilationUnit unit;
		
		public CompilationUnit Unit {
			get {
				return unit;
			}
			set {
				if (value != null) {
					unit = value;
					UpdateTree();
				}
			}
		}
		
		void UpdateTree()
		{
			tree.Nodes.Clear();
			tree.Nodes.Add(new CollectionNode("CompilationUnit", unit.Children));
			tree.SelectedNode = tree.Nodes[0];
		}
		
		public VBAstView()
		{
			InitializeComponent();
		}
		
//		public void DeleteSelectedNode()
//		{
//			if (tree.SelectedNode is ElementNode) {
//				INode element = (tree.SelectedNode as ElementNode).element;
//				if (tree.SelectedNode.Parent is CollectionNode) {
//					if (MessageBox.Show("Remove selected node from parent collection?", "Remove node", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
//					    == DialogResult.Yes)
//					{
//						IList col = (tree.SelectedNode.Parent as CollectionNode).collection;
//						col.Remove(element);
//						(tree.SelectedNode.Parent as CollectionNode).Update();
//					}
//				} else if (tree.SelectedNode.Parent is ElementNode) {
//					if (MessageBox.Show("Set selected property to null?", "Remove node", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
//					    == DialogResult.Yes)
//					{
//						// get parent element
//						element = (tree.SelectedNode.Parent as ElementNode).element;
//						string propertyName = (string)tree.SelectedNode.Tag;
//						element.GetType().GetProperty(propertyName).SetValue(element, null, null);
//						(tree.SelectedNode.Parent as ElementNode).Update();
//					}
//				}
//			} else if (tree.SelectedNode is CollectionNode) {
//				if (MessageBox.Show("Remove all elements from selected collection?", "Clear collection", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
//				    == DialogResult.Yes)
//				{
//					IList col = (tree.SelectedNode as CollectionNode).collection;
//					col.Clear();
//					(tree.SelectedNode as CollectionNode).Update();
//				}
//			}
//		}
//		
//		public void EditSelectedNode()
//		{
//			TreeNode node = tree.SelectedNode;
//			while (!(node is ElementNode)) {
//				if (node == null) {
//					return;
//				}
//				node = node.Parent;
//			}
//			INode element = ((ElementNode)node).element;
//			using (VBEditDialog dlg = new VBEditDialog(element)) {
//				dlg.ShowDialog();
//			}
//			((ElementNode)node).Update();
//		}
//		
//		public void ApplyTransformation(IAstVisitor visitor)
//		{
//			if (tree.SelectedNode == tree.Nodes[0]) {
//				unit.AcceptVisitor(visitor, null);
//				UpdateTree();
//			} else {
//				string name = visitor.GetType().Name;
//				ElementNode elementNode = tree.SelectedNode as ElementNode;
//				CollectionNode collectionNode = tree.SelectedNode as CollectionNode;
//				if (elementNode != null) {
//					if (MessageBox.Show(("Apply " + name + " to selected element '" + elementNode.Text + "'?"),
//					                    "Apply transformation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
//					    == DialogResult.Yes)
//					{
//						elementNode.element.AcceptVisitor(visitor, null);
//						elementNode.Update();
//					}
//				} else if (collectionNode != null) {
//					if (MessageBox.Show(("Apply " + name + " to all elements in selected collection '" + collectionNode.Text + "'?"),
//					                    "Apply transformation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
//					    == DialogResult.Yes)
//					{
//						foreach (TreeNode subNode in collectionNode.Nodes) {
//							if (subNode is ElementNode) {
//								(subNode as ElementNode).element.AcceptVisitor(visitor, null);
//							}
//						}
//						collectionNode.Update();
//					}
//				}
//			}
//		}
//		
		static TreeNode CreateNode(object child)
		{
			if (child == null) {
				return new TreeNode("*null reference*");
			} else if (child is INode) {
				return new ElementNode(child as INode);
			} else {
				return new TreeNode(child.ToString());
			}
		}
		
		class CollectionNode : TreeNode
		{
			internal IList collection;
			string baseName;
			
			public CollectionNode(string text, IList children) : base(text)
			{
				this.baseName = text;
				this.collection = children;
				Update();
			}
			
			public void Update()
			{
				if (collection.Count == 0) {
					Text = baseName + " (empty collection)";
				} else if (collection.Count == 1) {
					Text = baseName + " (collection with 1 element)";
				} else {
					Text = baseName + " (collection with " + collection.Count + " elements)";
				}
				Nodes.Clear();
				foreach (object child in collection) {
					Nodes.Add(CreateNode(child));
				}
				Expand();
			}
		}
		
		class ElementNode : TreeNode
		{
			internal AstNode element;
			
			public ElementNode(AstNode node)
			{
				this.element = node;
				Update();
			}
			
			public void Update()
			{
				Nodes.Clear();
				Type type = element.GetType();
				Text = type.Name;
				if (Tag != null) { // HACK: after editing property element
					Text = Tag.ToString() + " = " + Text;
				}
				if (!(element is INullable && (element as INullable).IsNull)) {
					AddProperties(type, element);
					if (element.Children.Count > 0) {
						Nodes.Add(new CollectionNode("Children", element.Children));
					}
				}
			}
			
			void AddProperties(Type type, AstNode node)
			{
				if (type == typeof(AbstractNode))
					return;
				foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
					if (pi.DeclaringType != type) // don't add derived properties
						continue;
					if (pi.Name == "IsNull")
						continue;
					object value = pi.GetValue(node, null);
					if (value is IList) {
						Nodes.Add(new CollectionNode(pi.Name, (IList)value));
					} else if (value is string) {
						Text += " " + pi.Name + "='" + value + "'";
					} else {
						TreeNode treeNode = CreateNode(value);
						treeNode.Text = pi.Name + " = " + treeNode.Text;
						treeNode.Tag = pi.Name;
						Nodes.Add(treeNode);
					}
				}
				AddProperties(type.BaseType, node);
			}
		}
		
		void TreeKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.Delete) {
				DeleteSelectedNode();
			} else if (e.KeyData == Keys.Space || e.KeyData == Keys.Enter) {
				EditSelectedNode();
			}
		}
	}
}
