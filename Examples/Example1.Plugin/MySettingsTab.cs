using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings.Dialog;

// Adds an options dialog box tab showing settings saved in MySettings

namespace Example1.Plugin {
	// This instance gets called by dnSpy to create the tab each time the user opens the options dialog
	[Export(typeof(IAppSettingsTabCreator))]	// Tell MEF we're exporting this instance
	sealed class MyAppSettingsTabCreator : IAppSettingsTabCreator {
		readonly MySettings mySettings;

		// This constructor gets the single MySettingsImpl instance exported by MySettingsImpl in MySettings.cs
		[ImportingConstructor]
		MyAppSettingsTabCreator(MySettings mySettings) {
			this.mySettings = mySettings;
		}

		public IEnumerable<IAppSettingsTab> Create() {
			// We only create one tab
			yield return new MyAppSettingsTab(mySettings);
		}
	}

	sealed class MyAppSettingsTab : IAppSettingsTab {
		// The order of the tab, let's place it after the debugger tab
		public double Order {
			get { return AppSettingsConstants.ORDER_DEBUGGER_TAB_DISPLAY + 0.1; }
		}

		public string Title {
			get { return "MySettings"; }
		}

		// This is the content shown in the tab. It should be a WPF object (eg. a UserControl) or a
		// ViewModel with a DataTemplate defined in a resource dictionary.
		public object UIObject {
			get {
				if (uiObject == null) {
					uiObject = new MySettingsControl();
					uiObject.DataContext = newSettings;
				}
				return uiObject;
			}
		}
		MySettingsControl uiObject;

		readonly MySettings globalSettings;
		readonly MySettings newSettings;

		public MyAppSettingsTab(MySettings mySettings) {
			this.globalSettings = mySettings;
			this.newSettings = mySettings.Clone();
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			// Check if user canceled
			if (!saveSettings)
				return;

			// OK was pressed, save the settings
			newSettings.CopyTo(globalSettings);
		}
	}
}
