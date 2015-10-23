// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer {
	[Export(typeof(IPlugin))]
	sealed class InstructionCommandsLoader : IPlugin {
		void IPlugin.OnLoaded() {
			var cmd = new RoutedCommand();
			cmd.InputGestures.Add(new KeyGesture(Key.R, ModifierKeys.Control));
			MainWindow.Instance.CommandBindings.Add(new CommandBinding(cmd, AnalyzeExecuted, AnalyzeCanExecute));
		}

		void AnalyzeCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = AnalyzeContextMenuEntry.CanAnalyze(GetMemberRef());
		}

		void AnalyzeExecuted(object sender, ExecutedRoutedEventArgs e) {
			AnalyzeContextMenuEntry.Analyze(GetMemberRef());
		}

		static IMemberRef GetMemberRef() {
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView != null && textView.IsKeyboardFocusWithin) {
				var refSeg = textView.GetCurrentReferenceSegment();
				return refSeg == null ? null : refSeg.Reference as IMemberRef;
			}

			var treeView = MainWindow.Instance.treeView;
			if (treeView.IsKeyboardFocusWithin) {
				var node = treeView.SelectedItem as IMemberTreeNode;
				return node == null ? null : node.Member;
			}

			treeView = AnalyzerTreeView.Instance;
			if (treeView.IsKeyboardFocusWithin) {
				var node = treeView.SelectedItem as IMemberTreeNode;
				return node == null ? null : node.Member;
			}

			var listBox = SearchPane.Instance.listBox;
			if (listBox.IsKeyboardFocusWithin) {
				var res = listBox.SelectedItem as SearchResult;
				return res == null ? null : res.Member;
			}

			return null;
		}
	}

	[ExportContextMenuEntryAttribute(Header = "Analy_ze", Icon = "Search", Order = 900, Category = "Other", InputGestureText = "Ctrl+R")]
	internal sealed class AnalyzeContextMenuEntry : IContextMenuEntry {
		public bool IsVisible(ContextMenuEntryContext context) {
			return IsEnabled(context);
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			if (context.Element is AnalyzerTreeView && context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length > 0 && context.SelectedTreeNodes.All(n => n.Parent.IsRoot))
				return false;
			if (context.SelectedTreeNodes == null)
				return context.Reference != null && MainWindow.ResolveReference(context.Reference.Reference) != null;
			foreach (var node in context.SelectedTreeNodes) {
				var mr = node as IMemberTreeNode;
				if (mr != null && CanAnalyze(mr.Member))
					return true;
			}
			return false;
		}

		public void Execute(ContextMenuEntryContext context) {
			if (context.SelectedTreeNodes != null) {
				foreach (var node in context.SelectedTreeNodes) {
					var mr = node as IMemberTreeNode;
					if (mr != null)
						Analyze(mr.Member);
				}
			}
			else if (context.Reference != null && context.Reference.Reference is IMemberRef) {
				if (context.Reference.Reference is IMemberRef)
					Analyze((IMemberRef)context.Reference.Reference);
			}
		}

		public static bool CanAnalyze(IMemberRef member) {
			member = MainWindow.ResolveReference(member);
			return member is TypeDef ||
					member is FieldDef ||
					member is MethodDef ||
					AnalyzedPropertyTreeNode.CanShow(member) ||
					AnalyzedEventTreeNode.CanShow(member);
		}

		public static void Analyze(IMemberRef member) {
			var memberDef = MainWindow.ResolveReference(member) as IMemberDef;

			var type = memberDef as TypeDef;
			if (type != null)
				AnalyzerTreeView.Instance.ShowOrFocus(new AnalyzedTypeTreeNode(type));

			var field = memberDef as FieldDef;
			if (field != null)
				AnalyzerTreeView.Instance.ShowOrFocus(new AnalyzedFieldTreeNode(field));

			var method = memberDef as MethodDef;
			if (method != null)
				AnalyzerTreeView.Instance.ShowOrFocus(new AnalyzedMethodTreeNode(method));

			var propertyAnalyzer = AnalyzedPropertyTreeNode.TryCreateAnalyzer(member);
			if (propertyAnalyzer != null)
				AnalyzerTreeView.Instance.ShowOrFocus(propertyAnalyzer);

			var eventAnalyzer = AnalyzedEventTreeNode.TryCreateAnalyzer(member);
			if (eventAnalyzer != null)
				AnalyzerTreeView.Instance.ShowOrFocus(eventAnalyzer);
		}
	}
}
