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
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using Gdk;
using System.Threading;
using System.Diagnostics;

namespace ICSharpCode.NRefactory.GtkDemo
{
	public partial class MainWindow : Gtk.Window
	{
		TreeStore store = new TreeStore (typeof (string), typeof (string), typeof (AstNode), typeof (Pixbuf));
		Dictionary<AstNode, TreeIter> iterDict = new Dictionary<AstNode, TreeIter> ();
//		TextEditor editor = new TextEditor ();
		SyntaxTree unit;
		
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
			this.textview1.ModifyFont (Pango.FontDescription.FromString ("Mono 14"));
			this.textview1.MoveCursor += HandleMoveCursor;
			string path = System.IO.Path.Combine (System.IO.Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location), "CSharpDemo.cs");
			this.textview1.Buffer.Text = File.ReadAllText (path);
			buttonParse.Clicked += HandleClicked;
			buttonGenerate.Clicked += CSharpGenerateCodeButtonClick;
			HandleClicked (this, EventArgs.Empty);
		}
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			Application.Quit ();
		}
		
		void HandleMoveCursor (object o, MoveCursorArgs args)
		{
			int cp = textview1.Buffer.CursorPosition;
			var textIter = textview1.Buffer.GetIterAtOffset (cp);
			var node = unit.GetNodeAt (textIter.Line + 1, textIter.LineOffset + 1);
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
			this.textview1.Buffer.Text = unit.GetText();
		}
		
		void SelectionChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			
			if (!this.treeviewNodes.Selection.GetSelected (out iter))
				return;
			var node = store.GetValue (iter, 2) as AstNode;
			if (node == null)
				return;
			this.textview1.MoveCursor -= HandleMoveCursor;
			var textIter = this.textview1.Buffer.GetIterAtLineOffset (node.StartLocation.Line - 1, node.StartLocation.Column - 1);
			this.textview1.ScrollToIter (textIter, 0, false, 0, 0);
			this.textview1.Buffer.PlaceCursor (textIter);
			this.textview1.Buffer.SelectRange (textIter, this.textview1.Buffer.GetIterAtLineOffset (node.EndLocation.Line -1, node.EndLocation.Column - 1));
			this.textview1.MoveCursor += HandleMoveCursor;
		}


		public void ShowUnit (SyntaxTree unit, CSharpAstResolver visitor)
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
			if (child is EntityDeclaration)
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
		
		public void AddChildren (AstNode node, CSharpAstResolver visitor, TreeIter iter)
		{
			if (node == null)
				return;
			iterDict [node] = iter;
			foreach (var child in node.Children) {
				ResolveResult result = null;
				try {
					if (child is Expression)
						result = visitor.Resolve (child, CancellationToken.None);
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
			var unit = parser.Parse (textview1.Buffer.Text, "dummy.cs");
			
			var unresolvedFile = unit.ToTypeSystem();
			
			IProjectContent project = new CSharpProjectContent ();
			project = project.AddOrUpdateFiles (unresolvedFile);
			project = project.AddAssemblyReferences (builtInLibs.Value);
			
			
			CSharpAstResolver resolver = new CSharpAstResolver(project.CreateCompilation (), unit, unresolvedFile);
			ShowUnit (unit, resolver);
			
		}
		
		Lazy<IList<IUnresolvedAssembly>> builtInLibs = new Lazy<IList<IUnresolvedAssembly>>(
			delegate {
				Assembly[] assemblies =  new Assembly[] {
					typeof(object).Assembly, // mscorlib
					typeof(Uri).Assembly, // System.dll
					typeof(System.Linq.Enumerable).Assembly, // System.Core.dll
//					typeof(System.Xml.XmlDocument).Assembly, // System.Xml.dll
//					typeof(System.Drawing.Bitmap).Assembly, // System.Drawing.dll
//					typeof(Form).Assembly, // System.Windows.Forms.dll
					typeof(ICSharpCode.NRefactory.TypeSystem.IProjectContent).Assembly,
				};
				IUnresolvedAssembly[] projectContents = new IUnresolvedAssembly[assemblies.Length];
				Parallel.For(
					0, assemblies.Length,
					delegate (int i) {
						Stopwatch w = Stopwatch.StartNew();
						CecilLoader loader = new CecilLoader();
						projectContents[i] = loader.LoadAssemblyFile(assemblies[i].Location);
					});
				return projectContents;
			});
	}
}

