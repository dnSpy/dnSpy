using System.Collections.Generic;
using dnSpy.Contracts.Plugin;

// Each plugin should export one class implementing IPlugin

namespace Example2.Plugin {
	[ExportPlugin]
	sealed class Plugin : IPlugin {
		public IEnumerable<string> MergedResourceDictionaries {
			get {
				yield return "Themes/resourcedict.xaml";
			}
		}

		public PluginInfo PluginInfo {
			get {
				return new PluginInfo {
					ShortDescription = "Example2 plugin",
				};
			}
		}

		public void OnEvent(PluginEvent @event, object obj) {
			// We don't care about any events
		}
	}
}
