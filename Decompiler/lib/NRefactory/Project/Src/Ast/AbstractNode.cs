// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ICSharpCode.NRefactory.Ast
{
	public abstract class AbstractNode : INode
	{
		NodeCollection children;
		
		public INode Parent { get; set; }
		public Location StartLocation { get; set; }
		public Location EndLocation { get; set; }
		public Dictionary<string, object> UserData { get; set; }
		
		IList<INode> INode.Children {
			get {
				return children;
			}
		}
		
		public NodeCollection Children {
			get {
				return children;
			}
			set {
				Debug.Assert(value != null);
				children = value;
			}
		}
		
		public AbstractNode()
		{
			children = new NodeCollection();
			children.Added += delegate(object sender, NodeEventArgs e) { e.Node.Parent = this; };
			this.UserData = new Dictionary<string, object>();
		}
		
		public virtual void AddChild(INode childNode)
		{
			Debug.Assert(childNode != null);
			children.Add(childNode);
		}
		
		public abstract object AcceptVisitor(IAstVisitor visitor, object data);
		
		public virtual object AcceptChildren(IAstVisitor visitor, object data)
		{
			foreach (INode child in children) {
				Debug.Assert(child != null);
				child.AcceptVisitor(visitor, data);
			}
			return data;
		}
		
		public static string GetCollectionString<T>(IList<T> collection)
		{
			StringBuilder output = new StringBuilder();
			output.Append('{');
			
			if (collection != null) {
				IEnumerator en = collection.GetEnumerator();
				bool isFirst = true;
				while (en.MoveNext()) {
					if (!isFirst) {
						output.Append(", ");
					} else {
						isFirst = false;
					}
					output.Append(en.Current == null ? "<null>" : en.Current.ToString());
				}
			} else {
				return "null";
			}
			
			output.Append('}');
			return output.ToString();
		}
	}
}
