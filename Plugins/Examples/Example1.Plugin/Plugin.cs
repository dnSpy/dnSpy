using System.Collections.Generic;
using dnSpy.Contracts.Plugin;

// Each plugin should export one class implementing IPlugin

namespace Example1.Plugin {
	[ExportPlugin]
	sealed class Plugin : IPlugin {
		public IEnumerable<string> MergedResourceDictionaries {
			get {
				// We don't have any extra resource dictionaries
				yield break;
			}
		}

		public PluginInfo PluginInfo {
			get {
				return new PluginInfo {
					ShortDescription = "Example1 plugin",
				};
			}
		}

		public void OnEvent(PluginEvent @event, object obj) {
			// We don't care about any events
		}
	}
}
