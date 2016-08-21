using System;
using System.ComponentModel.Composition;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

// Reads and writes the extension settings

namespace Example1.Extension {
	class MySettings : ViewModelBase {
		// overridden by the global settings class. Hooking the PropertyChanged event could be used too
		protected virtual void OnModified() {
		}

		public bool BoolOption1 {
			get { return boolOption1; }
			set {
				if (boolOption1 != value) {
					boolOption1 = value;
					OnPropertyChanged(nameof(BoolOption1));
					OnModified();
				}
			}
		}
		bool boolOption1 = true;

		public bool BoolOption2 {
			get { return boolOption2; }
			set {
				if (boolOption2 != value) {
					boolOption2 = value;
					OnPropertyChanged(nameof(BoolOption2));
					OnModified();
				}
			}
		}
		bool boolOption2 = false;

		public string StringOption3 {
			get { return stringOption3; }
			set {
				if (stringOption3 != value) {
					stringOption3 = value;
					OnPropertyChanged(nameof(StringOption3));
					OnModified();
				}
			}
		}
		string stringOption3 = string.Empty;

		public MySettings Clone() => CopyTo(new MySettings());

		public MySettings CopyTo(MySettings other) {
			other.BoolOption1 = this.BoolOption1;
			other.BoolOption2 = this.BoolOption2;
			other.StringOption3 = this.StringOption3;
			return other;
		}
	}

	// Export this class so it can be imported by other classes in this extension
	[Export(typeof(MySettings))]
	sealed class MySettingsImpl : MySettings {
		//TODO: Use your own guid
		static readonly Guid SETTINGS_GUID = new Guid("A308405D-0DF5-4C56-8B1E-8CE7BA6365E1");

		readonly ISettingsManager settingsManager;

		// Tell MEF to pass in the required ISettingsManager instance exported by dnSpy
		[ImportingConstructor]
		MySettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			// Read the settings from the file or use the default values if our settings haven't
			// been saved to it yet.

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			BoolOption1 = sect.Attribute<bool?>(nameof(BoolOption1)) ?? BoolOption1;
			BoolOption2 = sect.Attribute<bool?>(nameof(BoolOption2)) ?? BoolOption2;
			StringOption3 = sect.Attribute<string>(nameof(StringOption3)) ?? StringOption3;
			this.disableSave = false;
		}
		readonly bool disableSave;

		// Called by the base class
		protected override void OnModified() {
			if (disableSave)
				return;

			// Save the settings

			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(BoolOption1), BoolOption1);
			sect.Attribute(nameof(BoolOption2), BoolOption2);
			sect.Attribute(nameof(StringOption3), StringOption3);
		}
	}
}
