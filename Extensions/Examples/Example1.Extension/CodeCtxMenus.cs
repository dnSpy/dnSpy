using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Menus;
using Microsoft.VisualStudio.Text;

// Adds menu items to the text editor context menu
// If you have many similar commands, it's better to create a base class and derive from
// MenuItemBase<TContext> instead of MenuItemBase, see TreeViewCtxMenus.cs for an example.

namespace Example1.Extension {
	static class Constants {
		//TODO: Use your own guids
		// The first number is the order of the group, and the guid is the guid of the group,
		// see eg. dnSpy.Contracts.Menus.MenuConstants.GROUP_CTX_CODE_HEX etc
		public const string GROUP_TEXTEDITOR = "20000,3567EC95-E68E-44CE-932C-98A686FDCACF";
		public const string GROUP_TREEVIEW = "20000,77ACC18E-D8EB-483B-8D93-3581574B8891";
	}

	// This gets loaded by dnSpy and is used to add the Ctrl+Alt+Q command
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		static readonly RoutedCommand Option1Command = new RoutedCommand("Option1Command", typeof(CommandLoader));

		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, MySettings mySettings) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENTVIEWER_UICONTEXT);
			// This command will be added to all text editors
			cmds.Add(Option1Command,
				(s, e) => mySettings.BoolOption1 = !mySettings.BoolOption1,
				(s, e) => e.CanExecute = true,
				ModifierKeys.Control | ModifierKeys.Alt, Key.Q);
		}
	}

	[ExportMenuItem(Header = "Option 1", InputGestureText = "Ctrl+Alt+Q", Group = Constants.GROUP_TEXTEDITOR, Order = 0)]
	sealed class TextEditorCommand1 : MenuItemBase {
		readonly MySettings mySettings;

		[ImportingConstructor]
		TextEditorCommand1(MySettings mySettings) => this.mySettings = mySettings;

		public override bool IsChecked(IMenuItemContext context) => mySettings.BoolOption1;
		public override void Execute(IMenuItemContext context) => mySettings.BoolOption1 = !mySettings.BoolOption1;

		// Only show this in the document viewer
		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID);
	}

	[ExportMenuItem(Header = "Option 2", Group = Constants.GROUP_TEXTEDITOR, Order = 10)]
	sealed class TextEditorCommand2 : MenuItemBase {
		readonly MySettings mySettings;

		[ImportingConstructor]
		TextEditorCommand2(MySettings mySettings) => this.mySettings = mySettings;

		public override bool IsChecked(IMenuItemContext context) => mySettings.BoolOption2;
		public override void Execute(IMenuItemContext context) => mySettings.BoolOption2 = !mySettings.BoolOption2;

		// Only show this in the document viewer
		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID);
	}

	[ExportMenuItem(Group = Constants.GROUP_TEXTEDITOR, Order = 20)]
	sealed class TextEditorCommand3 : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			var md = GetTokenObj(context);
			if (!(md is null)) {
				try {
					Clipboard.SetText($"{md.MDToken.Raw:X8}");
				}
				catch (ExternalException) { }
			}
		}

		public override string? GetHeader(IMenuItemContext context) {
			var md = GetTokenObj(context);
			if (md is null)
				return "Copy token";
			return $"Copy token {md.MDToken.Raw:X8}";
		}

		IMDTokenProvider? GetTokenObj(IMenuItemContext context) {
			// Only show this in the document viewer
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return null;

			// All references in the text editor are stored in TextReferences
			var textRef = context.Find<TextReference>();
			if (textRef is null)
				return null;

			return textRef.Reference as IMDTokenProvider;
		}

		// Only show this in the document viewer
		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID);
		public override bool IsEnabled(IMenuItemContext context) => !(GetTokenObj(context) is null);
	}

	[ExportMenuItem(Group = Constants.GROUP_TEXTEDITOR, Order = 30)]
	sealed class TextEditorCommand4 : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			var documentViewer = GetDocumentViewer(context);
			if (!(documentViewer is null)) {
				try {
					var lineColumn = GetLineColumn(documentViewer.Caret.Position.VirtualBufferPosition);
					Clipboard.SetText($"Line,col: {lineColumn.Line + 1},{lineColumn.Column + 1}");
				}
				catch (ExternalException) { }
			}
		}

		public override string? GetHeader(IMenuItemContext context) {
			var documentViewer = GetDocumentViewer(context);
			if (documentViewer is null)
				return "Copy line and column";
			var lineColumn = GetLineColumn(documentViewer.Caret.Position.VirtualBufferPosition);
			return $"Copy line,col {lineColumn.Line + 1},{lineColumn.Column + 1}";
		}

		LineColumn GetLineColumn(VirtualSnapshotPoint point) {
			var line = point.Position.GetContainingLine();
			int column = point.Position - line.Start + point.VirtualSpaces;
			return new LineColumn(line.LineNumber, column);
		}

		struct LineColumn {
			public int Line { get; }
			public int Column { get; }
			public LineColumn(int line, int column) {
				Line = line;
				Column = column;
			}
		}

		IDocumentViewer? GetDocumentViewer(IMenuItemContext context) {
			// Only show this in the document viewer
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return null;

			return context.Find<IDocumentViewer>();
		}

		// Only show this in the document viewer
		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID);
		public override bool IsEnabled(IMenuItemContext context) => !(GetDocumentViewer(context) is null);
	}
}
