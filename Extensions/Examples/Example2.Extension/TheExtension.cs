using System.Collections.Generic;
using dnSpy.Contracts.Extension;

// Each extension should export one class implementing IExtension

namespace Example2.Extension {
	[ExportExtension]
	sealed class TheExtension : IExtension {
		public IEnumerable<string> MergedResourceDictionaries {
			get {
				yield return "Themes/resourcedict.xaml";
			}
		}

		public ExtensionInfo ExtensionInfo => new ExtensionInfo {
			ShortDescription = "Example2 extension",
		};

		public void OnEvent(ExtensionEvent @event, object obj) {
			// We don't care about any events
		}
	}
}
