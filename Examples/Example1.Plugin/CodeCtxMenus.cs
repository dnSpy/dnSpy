using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Shared.Menus;

// Adds menu items to the text editor context menu
// If you have many similar commands, it's better to create a base class and derive from
// MenuItemBase<TContext> instead of MenuItemBase, see TreeViewCtxMenus.cs for an example.

namespace Example1.Plugin {
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
		CommandLoader(IWpfCommandManager wpfCommandManager, MySettings mySettings) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_TEXTEDITOR_UICONTEXT);
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
		TextEditorCommand1(MySettings mySettings) {
			this.mySettings = mySettings;
		}

		public override bool IsChecked(IMenuItemContext context) {
			return mySettings.BoolOption1;
		}

		public override void Execute(IMenuItemContext context) {
			// Toggle value
			mySettings.BoolOption1 = !mySettings.BoolOption1;
		}

		public override bool IsVisible(IMenuItemContext context) {
			// Only show this in the text editor
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID);
		}
	}

	[ExportMenuItem(Header = "Option 2", Group = Constants.GROUP_TEXTEDITOR, Order = 10)]
	sealed class TextEditorCommand2 : MenuItemBase {
		readonly MySettings mySettings;

		[ImportingConstructor]
		TextEditorCommand2(MySettings mySettings) {
			this.mySettings = mySettings;
		}

		public override bool IsChecked(IMenuItemContext context) {
			return mySettings.BoolOption2;
		}

		public override void Execute(IMenuItemContext context) {
			// Toggle value
			mySettings.BoolOption2 = !mySettings.BoolOption2;
		}

		public override bool IsVisible(IMenuItemContext context) {
			// Only show this in the text editor
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID);
		}
	}

	[ExportMenuItem(Group = Constants.GROUP_TEXTEDITOR, Order = 20)]
	sealed class TextEditorCommand3 : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			var md = GetTokenObj(context);
			if (md != null)
				Clipboard.SetText(string.Format("{0:X8}", md.MDToken.Raw));
		}

		public override string GetHeader(IMenuItemContext context) {
			var md = GetTokenObj(context);
			if (md == null)
				return "Copy token";
			return string.Format("Copy token {0:X8}", md.MDToken.Raw);
		}

		IMDTokenProvider GetTokenObj(IMenuItemContext context) {
			// Only show this in the text editor
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
				return null;

			// All references in the text editor are stored in CodeReferences
			var codeRef = context.Find<CodeReference>();
			if (codeRef == null)
				return null;

			return codeRef.Reference as IMDTokenProvider;
		}

		public override bool IsVisible(IMenuItemContext context) {
			// Only show this in the text editor
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID);
		}

		public override bool IsEnabled(IMenuItemContext context) {
			return GetTokenObj(context) != null;
		}
	}

	[ExportMenuItem(Group = Constants.GROUP_TEXTEDITOR, Order = 30)]
	sealed class TextEditorCommand4 : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			var uiContext = GetUIContext(context);
			if (uiContext != null)
				Clipboard.SetText(string.Format("Line,col: {0},{1}", uiContext.Location.Line, uiContext.Location.Column));
		}

		public override string GetHeader(IMenuItemContext context) {
			var uiContext = GetUIContext(context);
			if (uiContext == null)
				return "Copy line and column";
			return string.Format("Copy line,col {0},{1}", uiContext.Location.Line, uiContext.Location.Column);
		}

		ITextEditorUIContext GetUIContext(IMenuItemContext context) {
			// Only show this in the text editor
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
				return null;

			return context.Find<ITextEditorUIContext>();
		}

		public override bool IsVisible(IMenuItemContext context) {
			// Only show this in the text editor
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID);
		}

		public override bool IsEnabled(IMenuItemContext context) {
			return GetUIContext(context) != null;
		}
	}
}
