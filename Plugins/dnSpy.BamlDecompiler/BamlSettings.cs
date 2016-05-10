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
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Shared.MVVM;

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
		bool disassembleBaml;

		public BamlSettings Clone() => CopyTo(new BamlSettings());

		public BamlSettings CopyTo(BamlSettings other) {
			other.DisassembleBaml = this.DisassembleBaml;
			return other;
		}
	}

	[Export]
	sealed class BamlSettingsImpl : BamlSettings {
		static readonly Guid SETTINGS_GUID = new Guid("D9809EB3-1605-4E05-A84F-6EE241FAAD6C");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		BamlSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.DisassembleBaml = sect.Attribute<bool?>(nameof(DisassembleBaml)) ?? this.DisassembleBaml;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(DisassembleBaml), DisassembleBaml);
		}
	}

	// This is disabled for now since it only contains one option that can be modified in the text
	// editor by using the context menu. Enable again when more options are added.
	// [Export(typeof(IAppSettingsTabCreator))]
	sealed class BamlSettingsTabCreator : IAppSettingsTabCreator {
		readonly BamlSettingsImpl bamlSettings;

		[ImportingConstructor]
		BamlSettingsTabCreator(BamlSettingsImpl bamlSettings) {
			this.bamlSettings = bamlSettings;
		}

		public IEnumerable<IAppSettingsTab> Create() {
			yield return new BamlAppSettingsTab(bamlSettings);
		}
	}

	sealed class BamlAppSettingsTab : IAppSettingsTab {
		public double Order => AppSettingsConstants.ORDER_BAML_TAB_DISPLAY;
		public string Title => dnSpy_BamlDecompiler_Resources.BamlOptionDlgTab;
		public object UIObject => bamlSettings;

		readonly BamlSettingsImpl _global_settings;
		readonly BamlSettings bamlSettings;

		public BamlAppSettingsTab(BamlSettingsImpl _global_settings) {
			this._global_settings = _global_settings;
			this.bamlSettings = _global_settings.Clone();
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			if (!saveSettings)
				return;
			bamlSettings.CopyTo(_global_settings);
		}
	}

	[ExportAutoLoaded]
	sealed class BamlRefresher : IAutoLoaded {
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		BamlRefresher(BamlSettingsImpl bamlSettings, IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
			bamlSettings.PropertyChanged += BamlSettings_PropertyChanged;
		}

		void BamlSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(BamlSettings.DisassembleBaml))
				fileTabManager.Refresh<BamlResourceElementNode>();
		}
	}
}