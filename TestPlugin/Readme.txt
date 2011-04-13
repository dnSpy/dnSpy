ILSpy uses MEF (Managed Extensibility Framework) for plugins.
Plugins must be placed in the same directory as ILSpy.exe, and must be called "*.Plugin.dll".

To write a plugin, you need to add a reference to ILSpy.exe and to System.ComponentModel.Composition.
Depending on what your plugin is doing, you might also need references to the other libraries shipping with ILSpy.

Plugins work by exporting types for certain extension points.
Here is a list of extension points:

Adding another language:

	[Export(typeof(Language))]
	public class CustomLanguage : Language
	
	This adds an additional language to the combobox in the toolbar.
	The language has to implement its own decompiler (all the way from IL), but it can also re-use
	the ICSharpCode.Decompiler library to decompile to C#, and then translate the C# code to the target language.

---

Adding an entry to the main menu:

	[ExportMainMenuCommand(Menu = "_File", MenuIcon = "Clear.png", Header = "_Clear List", MenuCategory = "Open", MenuOrder = 1.5)]
	public class UnloadAllAssembliesCommand : SimpleCommand
	
	Menu: menu into which the item is added
	MenuIcon: optional, icon to use for the menu item. Must be embedded as "Resource" (WPF-style resource) in the same assembly as the command type.
	Header: text on the menu item
	MenuCategory: optional, used for grouping related menu items together. A separator is added between different groups.
	MenuOrder: controls the order in which the items appear (items are sorted by this value)

	The command class must implement WPF's ICommand interface.

---

Adding an entry to the tool bar:

	[ExportToolbarCommand(ToolTip = "Clears the current assembly list", ToolbarIcon = "Clear.png", ToolbarCategory = "Open", ToolbarOrder = 1.5)]
	public class UnloadAllAssembliesCommand : SimpleCommand

	ToolTip: the tool tip
	ToolbarIcon: The icon. Must be embedded as "Resource" (WPF-style resource) in the same assembly as the command type.
	ToolbarCategory: optional, used for grouping related toolbar items together. A separator is added between different groups.
	ToolbarOrder: controls the order in which the items appear (items are sorted by this value)

	The command class must implement WPF's ICommand interface.

---

Adding an entry to the context menu:

	[ExportContextMenuEntry(Header = "_Save Assembly")]
	public class SaveAssembly : IContextMenuEntry
	
	Icon: optional, icon to use for the menu item. Must be embedded as "Resource" (WPF-style resource) in the same assembly as the command type.
	Header: text on the menu item
	Category: optional, used for grouping related menu items together. A separator is added between different groups.
	Order: controls the order in which the items appear (items are sorted by this value)
	
	Context menu entries must implement IContextMenuEntry, which defines 3 methods:
	bool IsVisible, bool IsEnabled, and void Execute.

---

Adding an option page:

	[ExportOptionPage("TestPlugin")]
	partial class CustomOptionPage : UserControl, IOptionPage
	
