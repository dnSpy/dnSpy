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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text;
using dnSpy.Documents.Tabs.DocViewer;
using dnSpy.Extension;
using dnSpy.Properties;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.MainApp {
	[ExportDocumentTabContentFactory(Order = double.MaxValue)]
	sealed class AboutScreenDocumentTabContentFactory : IDocumentTabContentFactory {
		readonly IDocumentViewerContentFactoryProvider documentViewerContentFactoryProvider;
		readonly IAppWindow appWindow;
		readonly IExtensionService extensionService;
		readonly IContentType aboutContentType;

		[ImportingConstructor]
		AboutScreenDocumentTabContentFactory(IDocumentViewerContentFactoryProvider documentViewerContentFactoryProvider, IAppWindow appWindow, IExtensionService extensionService, IContentTypeRegistryService contentTypeRegistryService) {
			this.documentViewerContentFactoryProvider = documentViewerContentFactoryProvider;
			this.appWindow = appWindow;
			this.extensionService = extensionService;
			this.aboutContentType = contentTypeRegistryService.GetContentType(ContentTypes.AboutDnSpy);
		}

		public IDocumentTabContent Create(IDocumentTabContentFactoryContext context) => null;

		static readonly Guid GUID_SerializedContent = new Guid("1C931C0F-D968-4664-B22D-87287A226EEC");

		public IDocumentTabContent Deserialize(Guid guid, ISettingsSection section, IDocumentTabContentFactoryContext context) {
			if (guid == GUID_SerializedContent)
				return new AboutScreenDocumentTabContent(documentViewerContentFactoryProvider, appWindow, extensionService, aboutContentType);
			return null;
		}

		public Guid? Serialize(IDocumentTabContent content, ISettingsSection section) {
			if (content is AboutScreenDocumentTabContent)
				return GUID_SerializedContent;
			return null;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_Menu", Group = MenuConstants.GROUP_APP_MENU_HELP_ABOUT, Order = 1000000)]
	sealed class AboutScreenMenuItem : MenuItemBase {
		readonly IDocumentViewerContentFactoryProvider documentViewerContentFactoryProvider;
		readonly IDocumentTabService documentTabService;
		readonly IAppWindow appWindow;
		readonly IExtensionService extensionService;
		readonly IContentType aboutContentType;

		[ImportingConstructor]
		AboutScreenMenuItem(IDocumentViewerContentFactoryProvider documentViewerContentFactoryProvider, IDocumentTabService documentTabService, IAppWindow appWindow, IExtensionService extensionService, IContentTypeRegistryService contentTypeRegistryService) {
			this.documentViewerContentFactoryProvider = documentViewerContentFactoryProvider;
			this.documentTabService = documentTabService;
			this.appWindow = appWindow;
			this.extensionService = extensionService;
			this.aboutContentType = contentTypeRegistryService.GetContentType(ContentTypes.AboutDnSpy);
		}

		public override void Execute(IMenuItemContext context) {
			var tab = documentTabService.GetOrCreateActiveTab();
			tab.Show(new AboutScreenDocumentTabContent(documentViewerContentFactoryProvider, appWindow, extensionService, aboutContentType), null, null);
			documentTabService.SetFocus(tab);
		}
	}

	sealed class AboutScreenDocumentTabContent : IDocumentTabContent {
		public IDocumentTab DocumentTab { get; set; }

		public IEnumerable<IDocumentTreeNodeData> Nodes {
			get { yield break; }
		}

		public string Title => dnSpy_Resources.About_TabTitle;
		public object ToolTip => null;

		readonly IAppWindow appWindow;
		readonly IExtensionService extensionService;
		readonly IContentType aboutContentType;
		readonly IDocumentViewerContentFactoryProvider documentViewerContentFactoryProvider;

		public AboutScreenDocumentTabContent(IDocumentViewerContentFactoryProvider documentViewerContentFactoryProvider, IAppWindow appWindow, IExtensionService extensionService, IContentType aboutContentType) {
			this.documentViewerContentFactoryProvider = documentViewerContentFactoryProvider;
			this.appWindow = appWindow;
			this.extensionService = extensionService;
			this.aboutContentType = aboutContentType;
		}

		public IDocumentTabContent Clone() => new AboutScreenDocumentTabContent(documentViewerContentFactoryProvider, appWindow, extensionService, aboutContentType);
		public IDocumentTabUIContext CreateUIContext(IDocumentTabUIContextLocator locator) => locator.Get<IDocumentViewer>();
		public void OnHide() { }
		public void OnSelected() { }
		public void OnUnselected() { }

		public void OnShow(IShowContext ctx) {
			var documentViewer = (IDocumentViewer)ctx.UIContext;
			var contentFactory = documentViewerContentFactoryProvider.Create();
			Write(contentFactory.Output);
			documentViewer.SetContent(contentFactory.CreateContent(documentViewer), aboutContentType);
		}

		sealed class Info {
			public readonly Assembly Assembly;
			public readonly ExtensionInfo ExtensionInfo;

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
					const string EXTENSION = ".x";
					if (s.EndsWith(EXTENSION, StringComparison.OrdinalIgnoreCase))
						s = s.Substring(0, s.Length - EXTENSION.Length);
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
					var c = ExtensionInfo.Copyright;
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
					var s = ExtensionInfo.ShortDescription;
					if (!string.IsNullOrEmpty(s))
						return s;
					var attr = Assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
					if (attr.Length == 0)
						return string.Empty;
					return ((AssemblyDescriptionAttribute)attr[0]).Description;
				}
			}

			public Info(Assembly asm, ExtensionInfo info) {
				this.Assembly = asm;
				this.ExtensionInfo = info;
			}
		}

		void Write(IDecompilerOutput output) {
			output.WriteLine(string.Format("dnSpy {0}", appWindow.AssemblyInformationalVersion), BoxedTextColor.Text);
			output.WriteLine();
			output.WriteLine(dnSpy_Resources.AboutScreen_LicenseInfo, BoxedTextColor.Text);
			output.WriteLine();
			output.WriteLine(dnSpy_Resources.AboutScreen_LoadedFiles, BoxedTextColor.Text);
			foreach (var info in GetInfos()) {
				output.WriteLine();
				WriteShortInfo(output, info.NameAndVersion);
				WriteShortInfo(output, info.Copyright);
				WriteShortInfo(output, info.ShortDescription);
			}
			output.WriteLine();
			WriteResourceFile(output, "dnSpy.LicenseInfo.CREDITS.txt");
		}

		void WriteResourceFile(IDecompilerOutput output, string name, bool addNewLine = true) {
			if (addNewLine)
				output.WriteLine();
			using (var stream = GetType().Assembly.GetManifestResourceStream(name))
			using (var streamReader = new StreamReader(stream, Encoding.UTF8)) {
				for (;;) {
					var line = streamReader.ReadLine();
					if (line == null)
						break;
					output.WriteLine(line, BoxedTextColor.Text);
				}
			}
		}

		void WriteShortInfo(IDecompilerOutput output, string s) {
			if (string.IsNullOrEmpty(s))
				return;
			const int MAX_SHORT_LEN = 128;
			if (s.Length > MAX_SHORT_LEN)
				s = s.Substring(0, MAX_SHORT_LEN) + "[...]";
			output.WriteLine(string.Format("\t{0}", s), BoxedTextColor.Text);
		}

		List<Info> GetInfos() {
			var infos = new List<Info>();

			infos.Add(new Info(GetType().Assembly, CreateDnSpyInfo()));

			var toExtension = new Dictionary<Assembly, IExtension>();
			foreach (var extension in extensionService.Extensions)
				toExtension[extension.GetType().Assembly] = extension;

			// Show the extensions in random order
			var random = new Random();
			foreach (var x in extensionService.LoadedExtensions.OrderBy(a => random.Next())) {
				ExtensionInfo extensionInfo;
				IExtension extension;
				if (toExtension.TryGetValue(x.Assembly, out extension))
					extensionInfo = extension.ExtensionInfo;
				else
					extensionInfo = new ExtensionInfo();

				infos.Add(new Info(x.Assembly, extensionInfo));
			}

			return infos;
		}

		static ExtensionInfo CreateDnSpyInfo() => new ExtensionInfo();
	}
}
