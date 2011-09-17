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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.Demo
{
	/// <summary>
	/// Description of CSDemo.
	/// </summary>
	public partial class CSDemo : UserControl
	{
		public CSDemo()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			if (LicenseManager.UsageMode != LicenseUsageMode.Designtime) {
				csharpCodeTextBox.SelectAll();
				CSharpParseButtonClick(null, null);
				resolveButton.UseWaitCursor = true;
				ThreadPool.QueueUserWorkItem(
					delegate {
						builtInLibs.Value.ToString();
						BeginInvoke(new Action(delegate { resolveButton.UseWaitCursor = false; }));
					});
			}
		}
		
		CompilationUnit compilationUnit;
		
		void CSharpParseButtonClick(object sender, EventArgs e)
		{
			CSharpParser parser = new CSharpParser();
			compilationUnit = parser.Parse(new StringReader(csharpCodeTextBox.Text));
			csharpTreeView.Nodes.Clear();
			foreach (var element in compilationUnit.Children) {
				csharpTreeView.Nodes.Add(MakeTreeNode(element));
			}
			SelectCurrentNode(csharpTreeView.Nodes);
			resolveButton.Enabled = true;
			findReferencesButton.Enabled = true;
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
			int selectionStart = csharpCodeTextBox.SelectionStart;
			int selectionEnd = selectionStart + csharpCodeTextBox.SelectionLength;
			foreach (TreeNode t in c) {
				AstNode node = t.Tag as AstNode;
				if (node != null
				    && selectionStart >= GetOffset(csharpCodeTextBox, node.StartLocation)
				    && selectionEnd <= GetOffset(csharpCodeTextBox, node.EndLocation))
				{
					if (selectionStart == selectionEnd
					    && (selectionStart == GetOffset(csharpCodeTextBox, node.StartLocation)
					        || selectionStart == GetOffset(csharpCodeTextBox, node.EndLocation)))
					{
						// caret is on border of this node; don't expand
						csharpTreeView.SelectedNode = t;
					} else {
						t.Expand();
						if (!SelectCurrentNode(t.Nodes))
							csharpTreeView.SelectedNode = t;
					}
					return true;
				}
			}
			return false;
		}
		
		void CSharpGenerateCodeButtonClick(object sender, EventArgs e)
		{
			StringWriter w = new StringWriter();
			CSharpOutputVisitor output = new CSharpOutputVisitor(w, new CSharpFormattingOptions());
			compilationUnit.AcceptVisitor(output, null);
			csharpCodeTextBox.Text = w.ToString();
		}
		
		int GetOffset(TextBox textBox, TextLocation location)
		{
			return textBox.GetFirstCharIndexFromLine(location.Line - 1) + location.Column - 1;
		}
		
		void CSharpTreeViewAfterSelect(object sender, TreeViewEventArgs e)
		{
			AstNode node = e.Node.Tag as AstNode;
			if (node != null) {
				if (node.StartLocation.IsEmpty || node.EndLocation.IsEmpty) {
					csharpCodeTextBox.DeselectAll();
				} else {
					int startOffset = GetOffset(csharpCodeTextBox, node.StartLocation);
					int endOffset = GetOffset(csharpCodeTextBox, node.EndLocation);
					csharpCodeTextBox.Select(startOffset, endOffset - startOffset);
				}
			}
		}
		
		Lazy<IList<IProjectContent>> builtInLibs = new Lazy<IList<IProjectContent>>(
			delegate {
				Assembly[] assemblies = {
					typeof(object).Assembly, // mscorlib
					typeof(Uri).Assembly, // System.dll
					typeof(System.Linq.Enumerable).Assembly, // System.Core.dll
//					typeof(System.Xml.XmlDocument).Assembly, // System.Xml.dll
//					typeof(System.Drawing.Bitmap).Assembly, // System.Drawing.dll
//					typeof(Form).Assembly, // System.Windows.Forms.dll
					typeof(ICSharpCode.NRefactory.TypeSystem.IProjectContent).Assembly,
				};
				IProjectContent[] projectContents = new IProjectContent[assemblies.Length];
				Stopwatch total = Stopwatch.StartNew();
				Parallel.For(
					0, assemblies.Length,
					delegate (int i) {
						Stopwatch w = Stopwatch.StartNew();
						CecilLoader loader = new CecilLoader();
						projectContents[i] = loader.LoadAssemblyFile(assemblies[i].Location);
						Debug.WriteLine(Path.GetFileName(assemblies[i].Location) + ": " + w.Elapsed);
					});
				Debug.WriteLine("Total: " + total.Elapsed);
				return projectContents;
			});
		
		void ResolveButtonClick(object sender, EventArgs e)
		{
			SimpleProjectContent project = new SimpleProjectContent();
			var parsedFile = new TypeSystemConvertVisitor(project, "dummy.cs").Convert(compilationUnit);
			project.UpdateProjectContent(null, parsedFile);
			
			List<ITypeResolveContext> projects = new List<ITypeResolveContext>();
			projects.Add(project);
			projects.AddRange(builtInLibs.Value);
			
			using (var context = new CompositeTypeResolveContext(projects).Synchronize()) {
				CSharpResolver resolver = new CSharpResolver(context);
				
				IResolveVisitorNavigator navigator = null;
				if (csharpTreeView.SelectedNode != null) {
					navigator = new NodeListResolveVisitorNavigator(new[] { (AstNode)csharpTreeView.SelectedNode.Tag });
				}
				ResolveVisitor visitor = new ResolveVisitor(resolver, parsedFile, navigator);
				visitor.Scan(compilationUnit);
				csharpTreeView.BeginUpdate();
				ShowResolveResultsInTree(csharpTreeView.Nodes, visitor);
				csharpTreeView.EndUpdate();
			}
		}
		
		void ShowResolveResultsInTree(TreeNodeCollection c, ResolveVisitor v)
		{
			foreach (TreeNode t in c) {
				AstNode node = t.Tag as AstNode;
				if (node != null) {
					ResolveResult rr = v.GetResolveResultIfResolved(node);
					if (rr != null)
						t.Text = GetNodeTitle(node) + " " + rr.ToString();
					else
						t.Text = GetNodeTitle(node);
				}
				ShowResolveResultsInTree(t.Nodes, v);
			}
		}
		
		void CSharpCodeTextBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.A) {
				e.Handled = true;
				csharpCodeTextBox.SelectAll();
			}
		}
		
		void CsharpCodeTextBoxTextChanged(object sender, EventArgs e)
		{
			resolveButton.Enabled = false;
			findReferencesButton.Enabled = false;
		}
		
		void FindReferencesButtonClick(object sender, EventArgs e)
		{
			if (csharpTreeView.SelectedNode == null)
				return;
			
			SimpleProjectContent project = new SimpleProjectContent();
			var parsedFile = new TypeSystemConvertVisitor(project, "dummy.cs").Convert(compilationUnit);
			project.UpdateProjectContent(null, parsedFile);
			
			List<ITypeResolveContext> projects = new List<ITypeResolveContext>();
			projects.Add(project);
			projects.AddRange(builtInLibs.Value);
			
			using (var context = new CompositeTypeResolveContext(projects).Synchronize()) {
				CSharpResolver resolver = new CSharpResolver(context);
				
				AstNode node = (AstNode)csharpTreeView.SelectedNode.Tag;
				IResolveVisitorNavigator navigator = new NodeListResolveVisitorNavigator(new[] { node });
				ResolveVisitor visitor = new ResolveVisitor(resolver, parsedFile, navigator);
				visitor.Scan(compilationUnit);
				IEntity entity;
				MemberResolveResult mrr = visitor.GetResolveResult(node) as MemberResolveResult;
				TypeResolveResult trr = visitor.GetResolveResult(node) as TypeResolveResult;
				if (mrr != null) {
					entity = mrr.Member;
				} else if (trr != null) {
					entity = trr.Type.GetDefinition();
				} else {
					return;
				}
				
				FindReferences fr = new FindReferences();
				int referenceCount = 0;
				FoundReferenceCallback callback = delegate(AstNode matchNode, ResolveResult result) {
					referenceCount++;
				};
				
				var searchScopes = fr.GetSearchScopes(entity);
				navigator = new CompositeResolveVisitorNavigator(searchScopes.Select(s => s.GetNavigator(callback)).ToArray());
				visitor = new ResolveVisitor(resolver, parsedFile, navigator);
				visitor.Scan(compilationUnit);
				
				csharpTreeView.BeginUpdate();
				ShowResolveResultsInTree(csharpTreeView.Nodes, visitor);
				csharpTreeView.EndUpdate();
				
				MessageBox.Show("Found " + referenceCount + " references to " + entity.FullName);
			}
		}
	}
}
