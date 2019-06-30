using System.Collections.Generic;
using dnSpy.Contracts.Extension;

// Each extension should export one class implementing IExtension

namespace Example1.Extension {
	[ExportExtension]
	sealed class TheExtension : IExtension {
		public IEnumerable<string> MergedResourceDictionaries {
			get {
				// We don't have any extra resource dictionaries
				yield break;
			}
		}

		public ExtensionInfo ExtensionInfo => new ExtensionInfo {
			ShortDescription = "Example1 extension",
		};

		public void OnEvent(ExtensionEvent @event, object? obj) {
			// We don't care about any events
		}
	}
}
