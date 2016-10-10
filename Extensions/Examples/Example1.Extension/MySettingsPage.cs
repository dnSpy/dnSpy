using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings.Dialog;

// Adds an options dialog box page showing settings saved in MySettings

namespace Example1.Extension {
	// This instance gets called by dnSpy to create the page each time the user opens the options dialog
	[Export(typeof(IAppSettingsPageProvider))]	// Tell MEF we're exporting this instance
	sealed class MyAppSettingsPageProvider : IAppSettingsPageProvider {
		readonly MySettings mySettings;

		// This constructor gets the single MySettingsImpl instance exported by MySettingsImpl in MySettings.cs
		[ImportingConstructor]
		MyAppSettingsPageProvider(MySettings mySettings) {
			this.mySettings = mySettings;
		}

		public IEnumerable<IAppSettingsPage> Create() {
			// We only create one page
			yield return new MyAppSettingsPage(mySettings);
		}
	}

	sealed class MyAppSettingsPage : IAppSettingsPage {
		//TODO: Use your own GUID
		static readonly Guid THE_GUID = new Guid("AE905210-A789-4AE2-B83B-537515D9F435");

		// Guid of parent or Guid.Empty if it has none
		public Guid ParentGuid => Guid.Empty;

		// Unique guid of this settings page
		public Guid Guid => THE_GUID;

		// The order of the page, let's place it after the debugger page
		public double Order => AppSettingsConstants.ORDER_DEBUGGER + 0.1;

		public string Title => "MySettings";

		// An image that can be shown. You can return ImageReference.None if you don't want an image.
		// Let's return an image since no other settings page is currently using images.
		public ImageReference Icon => DsImages.Assembly;

		// This is the content shown in the page. It should be a WPF object (eg. a UserControl) or a
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

		public MyAppSettingsPage(MySettings mySettings) {
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
