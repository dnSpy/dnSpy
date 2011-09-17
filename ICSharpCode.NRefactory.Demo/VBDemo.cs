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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Parser;

namespace ICSharpCode.NRefactory.Demo
{
	/// <summary>
	/// Description of VBDemo.
	/// </summary>
	public partial class VBDemo : UserControl
	{
		public VBDemo()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
		}
		
		CompilationUnit compilationUnit;
		
		void CSharpParseButtonClick(object sender, EventArgs e)
		{
			var parser = new VBParser();
			compilationUnit = parser.Parse(new StringReader(codeView.Text));
			if (parser.HasErrors)
				MessageBox.Show(parser.Errors.ErrorOutput);
			treeView.Nodes.Clear();
			foreach (var element in compilationUnit.Children) {
				treeView.Nodes.Add(MakeTreeNode(element));
			}
			SelectCurrentNode(treeView.Nodes);
		}
		
		TreeNode MakeTreeNode(AstNode node)
		{
			TreeNode t = new TreeNode(GetNodeTitle(node));
			t.Tag = node;
			foreach (AstNode child in node.Children) {
				t.Nodes.Add(MakeTreeNode(child));
			}
			return t;
		}
		
		string GetNodeTitle(AstNode node)
		{
			StringBuilder b = new StringBuilder();
			b.Append(node.Role.ToString());
			b.Append(": ");
			b.Append(node.GetType().Name);
			bool hasProperties = false;
			foreach (PropertyInfo p in node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				if (p.Name == "NodeType" || p.Name == "IsNull")
					continue;
				if (p.PropertyType == typeof(string) || p.PropertyType.IsEnum || p.PropertyType == typeof(bool)) {
					if (!hasProperties) {
						hasProperties = true;
						b.Append(" (");
					} else {
						b.Append(", ");
					}
					b.Append(p.Name);
					b.Append(" = ");
					try {
						object val = p.GetValue(node, null);
						b.Append(val != null ? val.ToString() : "**null**");
					} catch (TargetInvocationException ex) {
						b.Append("**" + ex.InnerException.GetType().Name + "**");
					}
				}
			}
			if (hasProperties)
				b.Append(")");
			return b.ToString();
		}
		
		bool SelectCurrentNode(TreeNodeCollection c)
		{
			int selectionStart = codeView.SelectionStart;
			int selectionEnd = selectionStart + codeView.SelectionLength;
			foreach (TreeNode t in c) {
				AstNode node = t.Tag as AstNode;
				if (node != null
				    && selectionStart >= GetOffset(codeView, node.StartLocation)
				    && selectionEnd <= GetOffset(codeView, node.EndLocation))
				{
					if (selectionStart == selectionEnd
					    && (selectionStart == GetOffset(codeView, node.StartLocation)
					        || selectionStart == GetOffset(codeView, node.EndLocation)))
					{
						// caret is on border of this node; don't expand
						treeView.SelectedNode = t;
					} else {
						t.Expand();
						if (!SelectCurrentNode(t.Nodes))
							treeView.SelectedNode = t;
					}
					return true;
				}
			}
			return false;
		}
		
		void CSharpGenerateCodeButtonClick(object sender, EventArgs e)
		{
			StringWriter w = new StringWriter();
			OutputVisitor output = new OutputVisitor(w, new VBFormattingOptions());
			compilationUnit.AcceptVisitor(output, null);
			codeView.Text = w.ToString();
		}
		
		int GetOffset(TextBox textBox, TextLocation location)
		{
			return textBox.GetFirstCharIndexFromLine(location.Line - 1) + location.Column - 1;
		}
		
		void CSharpTreeViewAfterSelect(object sender, TreeViewEventArgs e)
		{
			AstNode node = e.Node.Tag as AstNode;
			if (node != null) {
				int startOffset = GetOffset(codeView, node.StartLocation);
				int endOffset = GetOffset(codeView, node.EndLocation);
				codeView.Select(startOffset, endOffset - startOffset);
			}
		}
	}
}
