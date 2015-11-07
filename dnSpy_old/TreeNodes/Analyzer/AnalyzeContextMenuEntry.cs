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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.Menus;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer {
	[Export(typeof(IPlugin))]
	sealed class InstructionCommandsLoader : IPlugin {
		void IPlugin.EarlyInit() {
		}

		void IPlugin.OnLoaded() {
			var cmd = new RoutedCommand();
			cmd.InputGestures.Add(new KeyGesture(Key.R, ModifierKeys.Control));
			MainWindow.Instance.CommandBindings.Add(new CommandBinding(cmd, AnalyzeExecuted, AnalyzeCanExecute));
		}

		void AnalyzeCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = AnalyzeCommand.CanAnalyze(GetMemberRef());
		}

		void AnalyzeExecuted(object sender, ExecutedRoutedEventArgs e) {
			AnalyzeCommand.Analyze(GetMemberRef());
		}

		static IMemberRef GetMemberRef() {
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView != null && textView.IsKeyboardFocusWithin) {
				var refSeg = textView.GetCurrentReferenceSegment();
				return refSeg == null ? null : refSeg.Reference as IMemberRef;
			}

			var treeView = MainWindow.Instance.TreeView;
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

	static class AnalyzeCommand {
		[ExportMenuItem(Header = "Analy_ze", Icon = "Search", InputGestureText = "Ctrl+R", Group = MenuConstants.GROUP_CTX_FILES_OTHER, Order = 0)]
		sealed class FilesCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) {
				return GetMemberRefs(context).Any();
			}

			IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) {
				return GetMemberRefs(context, MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID, false);
			}

			internal static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context, string guid, bool checkRoot) {
				if (context.CreatorObject.Guid != new Guid(guid))
					yield break;
				var nodes = context.FindByType<SharpTreeNode[]>();
				if (nodes == null)
					yield break;

				if (checkRoot && nodes.All(a => a.Parent.IsRoot))
					yield break;

				foreach (var node in nodes) {
					var mr = node as IMemberTreeNode;
					if (mr != null && CanAnalyze(mr.Member))
						yield return mr.Member;
				}
			}

			public override void Execute(IMenuItemContext context) {
				Analyze(GetMemberRefs(context));
			}
		}

		[ExportMenuItem(Header = "Analy_ze", Icon = "Search", InputGestureText = "Ctrl+R", Group = MenuConstants.GROUP_CTX_ANALYZER_OTHER, Order = 0)]
		sealed class AnalyzerCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) {
				return GetMemberRefs(context).Any();
			}

			IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) {
				return FilesCommand.GetMemberRefs(context, MenuConstants.GUIDOBJ_ANALYZER_GUID, true);
			}

			public override void Execute(IMenuItemContext context) {
				Analyze(GetMemberRefs(context));
			}
		}

		[ExportMenuItem(Header = "Analy_ze", Icon = "Search", InputGestureText = "Ctrl+R", Group = MenuConstants.GROUP_CTX_CODE_OTHER, Order = 0)]
		sealed class CodeCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) {
				return GetMemberRefs(context).Any();
			}

			static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) {
				return GetMemberRefs(context, MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID);
			}

			internal static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					yield break;

				var @ref = context.FindByType<CodeReferenceSegment>();
				if (@ref != null) {
					var mr = @ref.Reference as IMemberRef;
					if (mr != null)
						yield return mr;
				}
			}

			public override void Execute(IMenuItemContext context) {
				Analyze(GetMemberRefs(context));
			}
		}

		[ExportMenuItem(Header = "Analy_ze", Icon = "Search", InputGestureText = "Ctrl+R", Group = MenuConstants.GROUP_CTX_SEARCH_OTHER, Order = 0)]
		sealed class SearchCommand : MenuItemBase {
			static IEnumerable<IMemberRef> GetMemberRefs(IMenuItemContext context) {
				return CodeCommand.GetMemberRefs(context, MenuConstants.GUIDOBJ_SEARCH_GUID);
			}

			public override bool IsVisible(IMenuItemContext context) {
				return GetMemberRefs(context).Any();
			}

			public override void Execute(IMenuItemContext context) {
				Analyze(GetMemberRefs(context));
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

		static void Analyze(IEnumerable<IMemberRef> mrs) {
			foreach (var mr in mrs)
				Analyze(mr);
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
