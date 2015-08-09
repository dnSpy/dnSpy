// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Xml.Linq;
using dnSpy.AsmEditor;
using ICSharpCode.ILSpy.Debugger;

namespace ICSharpCode.ILSpy.Options {
	[ExportOptionPage(Title = "Debugger", Order = 4)]
	sealed class DebuggerSettingsPanelCreator : IOptionPageCreator {
		public OptionPage Create() {
			return new DebuggerSettingsPanel();
		}
	}

	partial class DebuggerSettingsPanel : OptionPage {
		public DebuggerSettings Settings {
			get { return settings; }
		}
		DebuggerSettings settings;

		public override void Load(ILSpySettings settings) {
			var s = DebuggerSettings.Instance;
			s.Load(settings);
			this.settings = s;
		}

		public override RefreshFlags Save(XElement root) {
			this.settings.Save(root);
			return RefreshFlags.None;
		}
	}
}