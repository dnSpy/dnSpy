using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings.Dialog;

// This adds an option to the "Misc" tab. If you don't have enough options to warrant a new tab,
// this can be used to add a few of them to the Misc tab.

namespace Example1.Plugin {
	// This class adds a new option to the Misc tab
	[ExportSimpleAppOptionProvider(Guid = AppSettingsConstants.GUID_DYNTAB_MISC)]
	sealed class MiscOptionProvider : ISimpleAppOptionProvider {
		readonly MySettings mySettings;

		// This constructor gets the single MySettingsImpl instance exported by MySettingsImpl in MySettings.cs
		[ImportingConstructor]
		MiscOptionProvider(MySettings mySettings) {
			this.mySettings = mySettings;
		}

		public IEnumerable<ISimpleAppOption> Create() {
			// Create a textbox showing our StringOption3 value. Classes that can be used:
			//	SimpleAppOptionCheckBox
			//	SimpleAppOptionButton
			//	SimpleAppOptionTextBox
			//	SimpleAppOptionUserContent<T>
			yield return new SimpleAppOptionTextBox(mySettings.StringOption3, (saveSettings, appRefreshSettings, newValue) => {
				// this is false if the user canceled
				if (saveSettings)
					mySettings.StringOption3 = newValue;
			}) {
				Order = AppSettingsConstants.ORDER_MISC_USENEWRENDERER + 1,
				Text = "Some String",
			};
		}
	}
}
