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
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.Demo
{
	/// <summary>
	/// Description of SemanticTreeDialog.
	/// </summary>
	public partial class SemanticTreeDialog : Form
	{
		public SemanticTreeDialog(ResolveResult rr)
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			var rootNode = MakeObjectNode("Resolve() = ", rr);
			rootNode.Expand();
			treeView.Nodes.Add(rootNode);
		}
		
		TreeNode MakeObjectNode(string prefix, object obj)
		{
			if (obj == null)
				return new TreeNode(prefix + "null");
			if (obj is ResolveResult) {
				TreeNode t = new TreeNode(prefix + obj.GetType().Name);
				t.Tag = obj;
				foreach (PropertyInfo p in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
					if (p.Name == "ConstantValue" && !((ResolveResult)obj).IsCompileTimeConstant)
						continue;
					TreeNode child = MakePropertyNode(p.Name, p.PropertyType, p.GetValue(obj, null));
					if (child != null)
						t.Nodes.Add(child);
				}
				foreach (FieldInfo p in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) {
					TreeNode child = MakePropertyNode(p.Name, p.FieldType, p.GetValue(obj));
					if (child != null)
						t.Nodes.Add(child);
				}
				return t;
			} else {
				return new TreeNode(prefix + obj.ToString());
			}
		}
		
		TreeNode MakePropertyNode(string propertyName, Type propertyType, object propertyValue)
		{
			if (propertyName == "IsError" && (propertyValue as bool?) == false)
				return null;
			if (propertyName == "IsCompileTimeConstant" && (propertyValue as bool?) == false)
				return null;
			if (propertyValue == null) {
				return new TreeNode(propertyName + " = null");
			}
			if (propertyType.GetInterface("System.Collections.IEnumerable") != null && propertyType != typeof(string)) {
				var collection = ((IEnumerable)propertyValue).Cast<object>().ToList();
				var node = new TreeNode(propertyName + " = Collection with " + collection.Count + " elements");
				foreach (object element in collection) {
					node.Nodes.Add(MakeObjectNode("", element));
				}
				return node;
			}
			return MakeObjectNode(propertyName + " = ", propertyValue);
		}
	}
}
