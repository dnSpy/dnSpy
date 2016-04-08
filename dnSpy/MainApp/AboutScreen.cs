/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Settings;
using dnSpy.Decompiler.Shared;
using dnSpy.Plugin;
using dnSpy.Properties;
using dnSpy.Shared.Decompiler;
using dnSpy.Shared.Menus;

namespace dnSpy.MainApp {
	[Export, ExportFileTabContentFactory(Order = double.MaxValue)]
	sealed class DecompileFileTabContentFactory : IFileTabContentFactory {
		readonly IAppWindow appWindow;
		readonly IPluginManager pluginManager;

		[ImportingConstructor]
		DecompileFileTabContentFactory(IAppWindow appWindow, IPluginManager pluginManager) {
			this.appWindow = appWindow;
			this.pluginManager = pluginManager;
		}

		public IFileTabContent Create(IFileTabContentFactoryContext context) {
			return null;
		}

		static readonly Guid GUID_SerializedContent = new Guid("1C931C0F-D968-4664-B22D-87287A226EEC");

		public IFileTabContent Deserialize(Guid guid, ISettingsSection section, IFileTabContentFactoryContext context) {
			if (guid == GUID_SerializedContent)
				return new AboutScreenFileTabContent(appWindow, pluginManager);
			return null;
		}

		public Guid? Serialize(IFileTabContent content, ISettingsSection section) {
			if (content is AboutScreenFileTabContent)
				return GUID_SerializedContent;
			return null;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_Menu", Group = MenuConstants.GROUP_APP_MENU_HELP_ABOUT, Order = 1000000)]
	sealed class AboutScreenMenuItem : MenuItemBase {
		readonly IFileTabManager fileTabManager;
		readonly IAppWindow appWindow;
		readonly IPluginManager pluginManager;

		[ImportingConstructor]
		AboutScreenMenuItem(IFileTabManager fileTabManager, IAppWindow appWindow, IPluginManager pluginManager) {
			this.fileTabManager = fileTabManager;
			this.appWindow = appWindow;
			this.pluginManager = pluginManager;
		}

		public override void Execute(IMenuItemContext context) {
			var tab = fileTabManager.GetOrCreateActiveTab();
			tab.Show(new AboutScreenFileTabContent(appWindow, pluginManager), null, null);
			fileTabManager.SetFocus(tab);
		}
	}

	sealed class AboutScreenFileTabContent : IFileTabContent {
		public IFileTab FileTab { get; set; }

		public IEnumerable<IFileTreeNodeData> Nodes {
			get { yield break; }
		}

		public string Title {
			get { return dnSpy_Resources.About_TabTitle; }
		}

		public object ToolTip {
			get { return null; }
		}

		readonly IAppWindow appWindow;
		readonly IPluginManager pluginManager;

		public AboutScreenFileTabContent(IAppWindow appWindow, IPluginManager pluginManager) {
			this.appWindow = appWindow;
			this.pluginManager = pluginManager;
		}

		public IFileTabContent Clone() {
			return new AboutScreenFileTabContent(appWindow, pluginManager);
		}

		public IFileTabUIContext CreateUIContext(IFileTabUIContextLocator locator) {
			return locator.Get<ITextEditorUIContext>();
		}

		public void OnHide() {
		}

		public void OnSelected() {
		}

		public void OnUnselected() {
		}

		public void OnShow(IShowContext ctx) {
			var uiCtx = (ITextEditorUIContext)ctx.UIContext;
			var output = new AvalonEditTextOutput();
			Write(output);
			uiCtx.SetOutput(output, null);
		}

		sealed class Info {
			public readonly Assembly Assembly;
			public readonly PluginInfo PluginInfo;

			string VersionString {
				get {
					try {
						var info = FileVersionInfo.GetVersionInfo(Assembly.Location);
						var fileVer = info.FileVersion;
						if (!string.IsNullOrEmpty(fileVer))
							return fileVer;
					}
					catch {
					}
					return Assembly.GetName().Version.ToString();
				}
			}

			public string Name {
				get {
					var s = Path.GetFileNameWithoutExtension(Assembly.Location);
					const string PLUGIN = ".Plugin";
					if (s.EndsWith(PLUGIN, StringComparison.OrdinalIgnoreCase))
						s = s.Substring(0, s.Length - PLUGIN.Length);
					return s;
				}
			}

			public string NameAndVersion {
				get {
					var name = Name;
					var verStr = VersionString;
					Debug.Assert(!string.IsNullOrEmpty(verStr));
					if (string.IsNullOrEmpty(verStr))
						return name;
					return $"{name} ({verStr})";
				}
			}

			public string Copyright {
				get {
					var c = PluginInfo.Copyright;
					if (!string.IsNullOrEmpty(c))
						return c;
					var attr = Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
					if (attr.Length == 0)
						return string.Empty;
					return ((AssemblyCopyrightAttribute)attr[0]).Copyright;
				}
			}

			public string ShortDescription {
				get {
					var s = PluginInfo.ShortDescription;
					if (!string.IsNullOrEmpty(s))
						return s;
					var attr = Assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
					if (attr.Length == 0)
						return string.Empty;
					return ((AssemblyDescriptionAttribute)attr[0]).Description;
				}
			}

			public Info(Assembly asm, PluginInfo info) {
				this.Assembly = asm;
				this.PluginInfo = info;
			}
		}

		void Write(AvalonEditTextOutput output) {
			output.WriteLine(string.Format("dnSpy {0}", appWindow.AssemblyInformationalVersion), TextTokenKind.Text);
			output.WriteLine();
			output.WriteLine(dnSpy_Resources.AboutScreen_LicenseInfo, TextTokenKind.Text);
			output.WriteLine();
			output.WriteLine(dnSpy_Resources.AboutScreen_LoadedFiles, TextTokenKind.Text);
			foreach (var info in GetInfos()) {
				output.WriteLine();
				WriteShortInfo(output, info.NameAndVersion);
				WriteShortInfo(output, info.Copyright);
				WriteShortInfo(output, info.ShortDescription);
			}
			output.WriteLine();
			WriteResourceFile(output, "dnSpy.CREDITS.txt");
		}

		void WriteResourceFile(AvalonEditTextOutput output, string name, bool addNewLine = true) {
			if (addNewLine)
				output.WriteLine();
			using (var stream = GetType().Assembly.GetManifestResourceStream(name))
			using (var streamReader = new StreamReader(stream, Encoding.UTF8)) {
				for (;;) {
					var line = streamReader.ReadLine();
					if (line == null)
						break;
					output.WriteLine(line, TextTokenKind.Text);
				}
			}
		}

		void WriteShortInfo(AvalonEditTextOutput output, string s) {
			if (string.IsNullOrEmpty(s))
				return;
			const int MAX_SHORT_LEN = 128;
			if (s.Length > MAX_SHORT_LEN)
				s = s.Substring(0, MAX_SHORT_LEN) + "[...]";
			output.WriteLine(string.Format("\t{0}", s), TextTokenKind.Text);
		}

		List<Info> GetInfos() {
			var infos = new List<Info>();

			infos.Add(new Info(GetType().Assembly, CreateDnSpyInfo()));

			var toPlugin = new Dictionary<Assembly, IPlugin>();
			foreach (var plugin in pluginManager.Plugins)
				toPlugin[plugin.GetType().Assembly] = plugin;

			// Show the plugins in random order
			var random = new Random();
			foreach (var x in pluginManager.LoadedPlugins.OrderBy(a => random.Next())) {
				PluginInfo pluginInfo;
				IPlugin plugin;
				if (toPlugin.TryGetValue(x.Assembly, out plugin))
					pluginInfo = plugin.PluginInfo;
				else
					pluginInfo = new PluginInfo();

				infos.Add(new Info(x.Assembly, pluginInfo));
			}

			return infos;
		}

		static PluginInfo CreateDnSpyInfo() {
			return new PluginInfo();
		}
	}
}
