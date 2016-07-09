using dnSpy.Contracts.App;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.Menus;

// Adds a new "_Plugin" menu and several commands.
// Adds a command to the View menu

namespace Example1.Plugin {
	static class MainMenuConstants {
		//TODO: Use your own guids
		public const string APP_MENU_PLUGIN = "4E6829A6-AEA0-4803-9344-D19BF0A74DA1";
		public const string GROUP_PLUGIN_MENU1 = "0,73BEBC37-387A-4004-8076-A1A90A17611B";
		public const string GROUP_PLUGIN_MENU2 = "10,C21B8B99-A2E4-474F-B4BC-4CF348ECBD0A";
	}

	// Create the Plugin menu and place it right after the Debug menu
	[ExportMenu(OwnerGuid = MenuConstants.APP_MENU_GUID, Guid = MainMenuConstants.APP_MENU_PLUGIN, Order = MenuConstants.ORDER_APP_MENU_DEBUG + 0.1, Header = "_Plugin")]
	sealed class DebugMenu : IMenu {
	}

	[ExportMenuItem(OwnerGuid = MainMenuConstants.APP_MENU_PLUGIN, Header = "Command #1", Group = MainMenuConstants.GROUP_PLUGIN_MENU1, Order = 0)]
	sealed class PluginCommand1 : MenuItemBase {
		public override void Execute(IMenuItemContext context) => MsgBox.Instance.Show("Command #1");
	}

	[ExportMenuItem(OwnerGuid = MainMenuConstants.APP_MENU_PLUGIN, Header = "Command #2", Group = MainMenuConstants.GROUP_PLUGIN_MENU1, Order = 10)]
	sealed class PluginCommand2 : MenuItemBase {
		public override void Execute(IMenuItemContext context) => MsgBox.Instance.Show("Command #2");
	}

	[ExportMenuItem(OwnerGuid = MainMenuConstants.APP_MENU_PLUGIN, Header = "Command #3", Group = MainMenuConstants.GROUP_PLUGIN_MENU2, Order = 0)]
	sealed class PluginCommand3 : MenuItemBase {
		public override void Execute(IMenuItemContext context) => MsgBox.Instance.Show("Command #3");
	}

	[ExportMenuItem(OwnerGuid = MainMenuConstants.APP_MENU_PLUGIN, Header = "Command #4", Group = MainMenuConstants.GROUP_PLUGIN_MENU2, Order = 10)]
	sealed class PluginCommand4 : MenuItemBase {
		public override void Execute(IMenuItemContext context) => MsgBox.Instance.Show("Command #4");
	}

	[ExportMenuItem(OwnerGuid = MainMenuConstants.APP_MENU_PLUGIN, Header = "Command #5", Group = MainMenuConstants.GROUP_PLUGIN_MENU2, Order = 20)]
	sealed class PluginCommand5 : MenuItemBase {
		public override void Execute(IMenuItemContext context) => MsgBox.Instance.Show("Command #5");
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "Command #1", Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 1000)]
	sealed class ViewCommand1 : MenuItemBase {
		public override void Execute(IMenuItemContext context) => MsgBox.Instance.Show("View Command #1");
	}
}
