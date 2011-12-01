// 
// MainWindow.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using ICSharpCode.NRefactory.CSharp;
using Gtk;
using System.IO;
using System.Text;
using System.Reflection;
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using Gdk;

namespace ICSharpCode.NRefactory.GtkDemo
{
	public partial class MainWindow : Gtk.Window
	{
		TreeStore store = new TreeStore (typeof (string), typeof (string), typeof (AstNode), typeof (Pixbuf));
		Dictionary<AstNode, TreeIter> iterDict = new Dictionary<AstNode, TreeIter> ();
		TextEditor editor = new TextEditor ();
		CompilationUnit unit;
		
		Pixbuf comment = new Pixbuf (typeof (MainWindow).Assembly, "comment.png");
		Pixbuf classPixbuf = new Pixbuf (typeof (MainWindow).Assembly, "class.png");
		Pixbuf tokenPixbuf = new Pixbuf (typeof (MainWindow).Assembly, "token.png");
		Pixbuf statementPixbuf = new Pixbuf (typeof (MainWindow).Assembly, "statement.png");
		Pixbuf expressionPixbuf = new Pixbuf (typeof (MainWindow).Assembly, "expression.png");
		Pixbuf namespacePixbuf = new Pixbuf (typeof (MainWindow).Assembly, "namespace.png");
		
		public MainWindow () : 
				base(Gtk.WindowType.Toplevel)
		{
			this.Build ();
			this.BorderWidth = 12;
			this.treeviewNodes.Model = store;
			var col =new TreeViewColumn ();
			col.Title ="Node";
			var pb = new CellRendererPixbuf ();
			col.PackStart (pb, false);
			col.AddAttribute (pb, "pixbuf", 3);
			
			var text = new CellRendererText ();
			col.PackStart (text, true);
			col.AddAttribute (text, "text", 0);
			
			this.treeviewNodes.AppendColumn (col);
			this.treeviewNodes.AppendColumn ("ResolveResult", new CellRendererText (), "text", 1);
			this.treeviewNodes.Selection.Changed += SelectionChanged;
//			this.treeviewNodes.HeadersVisible = false;
			this.scrolledwindow1.Child = editor;
			this.scrolledwindow1.Child.ShowAll ();
			this.editor.Document.MimeType = "text/x-csharp";
			this.editor.Options.FontName = "Mono 14";
			this.editor.Caret.PositionChanged += HandlePositionChanged;
			this.editor.Text = File.ReadAllText ("/Users/mike/work/NRefactory/ICSharpCode.NRefactory.GtkDemo/CSharpDemo.cs");
			buttonParse.Clicked += HandleClicked;
			buttonGenerate.Clicked += CSharpGenerateCodeButtonClick;
			HandleClicked (this, EventArgs.Empty);
		}

		void HandlePositionChanged (object sender, DocumentLocationEventArgs e)
		{
			var node = unit.GetNodeAt (editor.Caret.Line, editor.Caret.Column);
			if (node == null)
				return;
			TreeIter iter;
			if (!iterDict.TryGetValue (node, out iter))
				return;
			this.treeviewNodes.Selection.Changed -= SelectionChanged;
			treeviewNodes.Selection.SelectIter (iter);
			
			treeviewNodes.ScrollToCell (store.GetPath (iter), null, true, 0, 0);
			this.treeviewNodes.Selection.Changed += SelectionChanged;
		}

		void CSharpGenerateCodeButtonClick(object sender, EventArgs e)
		{
			var w = new StringWriter();
			var output = new CSharpOutputVisitor (w, new CSharpFormattingOptions());
			unit.AcceptVisitor (output, null);
			editor.Text = w.ToString();
		}
		
		void SelectionChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			
			if (!this.treeviewNodes.Selection.GetSelected (out iter))
				return;
			var node = store.GetValue (iter, 2) as AstNode;
			if (node == null)
				return;
			this.editor.Caret.PositionChanged -= HandlePositionChanged;
			this.editor.SetCaretTo (node.StartLocation.Line, node.StartLocation.Column);
			this.editor.SetSelection (node.StartLocation.Line, node.StartLocation.Column, node.EndLocation.Line, node.EndLocation.Column);
			this.editor.Caret.PositionChanged += HandlePositionChanged;
		}
		
		public void ShowUnit (CompilationUnit unit, ResolveVisitor visitor)
		{
			this.unit = unit;
			store.Clear ();
			iterDict.Clear ();
			if (unit == null)
				return;
			var iter = store.AppendValues (GetNodeTitle (unit), "", unit, GetIcon (unit));
			AddChildren (unit, visitor, iter);
			treeviewNodes.ExpandAll ();
		}

		public Pixbuf GetIcon (AstNode child)
		{
			if (child is Comment)
				return comment;
			if (child is PreProcessorDirective)
				return comment;
			if (child is AttributedNode)
				return classPixbuf;
			if (child is CSharpTokenNode)
				return tokenPixbuf;
			if (child is Identifier)
				return tokenPixbuf;
			if (child is Statement)
				return statementPixbuf;
			if (child is Expression)
				return expressionPixbuf;
			if (child is UsingDeclaration)
				return namespacePixbuf;
			if (child is NamespaceDeclaration)
				return namespacePixbuf;
			
			return null;
		}
		
		public void AddChildren (AstNode node, ResolveVisitor visitor, TreeIter iter)
		{
			if (node == null)
				return;
			iterDict [node] = iter;
			foreach (var child in node.Children) {
				ResolveResult result = null;
				try {
					if (child is Expression)
						result = visitor.GetResolveResult (child);
				} catch (Exception){
					result = null;
				}
				
				var childIter = store.AppendValues (iter, GetNodeTitle (child), result != null ? result.ToString () : "", child, GetIcon (child));
				AddChildren (child, visitor, childIter);
			}
		}
		
		string GetNodeTitle(AstNode node)
		{
			var b = new StringBuilder();
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
//			b.Append(" Start " + node.StartLocation);
//			b.Append(" End " + node.EndLocation);
			return b.ToString();
		}
		
		void HandleClicked (object sender, EventArgs e)
		{
			var parser = new CSharpParser ();
			var unit = parser.Parse (editor.Text);
			
			var project = new SimpleProjectContent();
			var parsedFile = new TypeSystemConvertVisitor(project, "dummy.cs").Convert (unit);
			project.UpdateProjectContent(null, parsedFile);
			
			var projects = new List<ITypeResolveContext>();
			projects.Add(project);
			projects.AddRange(builtInLibs.Value);
			
			using (var context = new CompositeTypeResolveContext(projects).Synchronize()) {
				var resolver = new CSharpResolver(context);
				
				IResolveVisitorNavigator navigator = null;
//				if (csharpTreeView.SelectedNode != null) {
//					navigator = new NodeListResolveVisitorNavigator(new[] { (AstNode)csharpTreeView.SelectedNode.Tag });
//				}
				
				var visitor = new ResolveVisitor (resolver, parsedFile, navigator);
				visitor.Scan(unit);
				ShowUnit (unit, visitor);
			}
			
		}
		
		Lazy<IList<IProjectContent>> builtInLibs = new Lazy<IList<IProjectContent>>(
			delegate {
				Assembly[] assemblies = new Assembly[] { // Compiler error ?
					typeof(object).Assembly, // mscorlib
					typeof(Uri).Assembly, // System.dll
					typeof(System.Linq.Enumerable).Assembly,
					typeof(ICSharpCode.NRefactory.TypeSystem.IProjectContent).Assembly
				};
				IProjectContent[] projectContents = new IProjectContent[assemblies.Length];
				Parallel.For(
					0, assemblies.Length,
					delegate (int i) {
						CecilLoader loader = new CecilLoader();
						projectContents[i] = loader.LoadAssemblyFile(assemblies[i].Location);
					});
				return projectContents;
			});
		}
}

