/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.BamlDecompiler.Properties;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.BamlDecompiler {
	class BamlSettings : ViewModelBase {
		protected virtual void OnModified() { }

		public bool DisassembleBaml {
			get { return disassembleBaml; }
			set {
				if (disassembleBaml != value) {
					disassembleBaml = value;
					OnPropertyChanged(nameof(DisassembleBaml));
					OnModified();
				}
			}
		}
		bool disassembleBaml = false;

		public bool UseTabs {
			get { return useTabs; }
			set {
				if (useTabs != value) {
					useTabs = value;
					OnPropertyChanged(nameof(UseTabs));
					OnModified();
				}
			}
		}
		bool useTabs = true;

		public bool NewLineOnAttributes {
			get { return newLineOnAttributes; }
			set {
				if (newLineOnAttributes != value) {
					newLineOnAttributes = value;
					OnPropertyChanged(nameof(NewLineOnAttributes));
					OnModified();
				}
			}
		}
		bool newLineOnAttributes = true;

		public BamlSettings Clone() => CopyTo(new BamlSettings());

		public BamlSettings CopyTo(BamlSettings other) {
			other.DisassembleBaml = this.DisassembleBaml;
			other.UseTabs = this.UseTabs;
			other.NewLineOnAttributes = this.NewLineOnAttributes;
			return other;
		}
	}

	[Export]
	sealed class BamlSettingsImpl : BamlSettings {
		static readonly Guid SETTINGS_GUID = new Guid("D9809EB3-1605-4E05-A84F-6EE241FAAD6C");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		BamlSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			this.disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			this.DisassembleBaml = sect.Attribute<bool?>(nameof(DisassembleBaml)) ?? this.DisassembleBaml;
			this.UseTabs = sect.Attribute<bool?>(nameof(UseTabs)) ?? this.UseTabs;
			this.NewLineOnAttributes = sect.Attribute<bool?>(nameof(NewLineOnAttributes)) ?? this.NewLineOnAttributes;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(DisassembleBaml), DisassembleBaml);
			sect.Attribute(nameof(UseTabs), UseTabs);
			sect.Attribute(nameof(NewLineOnAttributes), NewLineOnAttributes);
		}
	}

	[Export(typeof(IAppSettingsPageProvider))]
	sealed class BamlSettingsPageProvider : IAppSettingsPageProvider {
		readonly BamlSettingsImpl bamlSettings;

		[ImportingConstructor]
		BamlSettingsPageProvider(BamlSettingsImpl bamlSettings) {
			this.bamlSettings = bamlSettings;
		}

		public IEnumerable<AppSettingsPage> Create() {
			yield return new BamlAppSettingsPage(bamlSettings);
		}
	}

	sealed class BamlAppSettingsPage : AppSettingsPage {
		public override Guid Guid => new Guid("DF5D8216-35D9-4E25-8BDF-817D4CA90C17");
		public override double Order => AppSettingsConstants.ORDER_BAML;
		public override string Title => dnSpy_BamlDecompiler_Resources.BamlOptionDlgTab;
		public override object UIObject => bamlSettings;

		readonly BamlSettingsImpl _global_settings;
		readonly BamlSettings bamlSettings;

		public BamlAppSettingsPage(BamlSettingsImpl _global_settings) {
			this._global_settings = _global_settings;
			this.bamlSettings = _global_settings.Clone();
		}

		public override void OnApply() => bamlSettings.CopyTo(_global_settings);
	}

	[ExportAutoLoaded]
	sealed class BamlRefresher : IAutoLoaded {
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		BamlRefresher(BamlSettingsImpl bamlSettings, IDocumentTabService documentTabService) {
			this.documentTabService = documentTabService;
			bamlSettings.PropertyChanged += BamlSettings_PropertyChanged;
		}

		void BamlSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case nameof(BamlSettings.DisassembleBaml):
			case nameof(BamlSettings.UseTabs):
			case nameof(BamlSettings.NewLineOnAttributes):
				documentTabService.Refresh<BamlResourceElementNode>();
				break;
			}
		}
	}
}
