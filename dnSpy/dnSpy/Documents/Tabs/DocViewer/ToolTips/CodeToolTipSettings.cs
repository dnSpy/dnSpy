/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

namespace dnSpy.Documents.Tabs.DocViewer.ToolTips {
	interface ICodeToolTipSettings : INotifyPropertyChanged {
		bool SyntaxHighlight { get; }
	}

	class CodeToolTipSettings : ViewModelBase, ICodeToolTipSettings {
		protected virtual void OnModified() { }

		public bool SyntaxHighlight {
			get => syntaxHighlight;
			set {
				if (syntaxHighlight != value) {
					syntaxHighlight = value;
					OnPropertyChanged(nameof(SyntaxHighlight));
					OnModified();
				}
			}
		}
		bool syntaxHighlight = true;
	}

	[Export(typeof(ICodeToolTipSettings))]
	sealed class CodeToolTipSettingsImpl : CodeToolTipSettings {
		static readonly Guid SETTINGS_GUID = new Guid("6AA691D6-C3B8-4823-87EC-DC2E9134CB3E");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		CodeToolTipSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			SyntaxHighlight = sect.Attribute<bool?>(nameof(SyntaxHighlight)) ?? SyntaxHighlight;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
		}
	}
}
