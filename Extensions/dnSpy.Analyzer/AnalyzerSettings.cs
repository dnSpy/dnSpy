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

namespace dnSpy.Analyzer {
	interface IAnalyzerSettings : INotifyPropertyChanged {
		bool SyntaxHighlight { get; }
		bool ShowToken { get; }
		bool SingleClickExpandsChildren { get; }
	}

	class AnalyzerSettings : ViewModelBase, IAnalyzerSettings {
		public bool SyntaxHighlight {
			get => syntaxHighlight;
			set {
				if (syntaxHighlight != value) {
					syntaxHighlight = value;
					OnPropertyChanged(nameof(SyntaxHighlight));
				}
			}
		}
		bool syntaxHighlight = true;

		public bool ShowToken {
			get => showToken;
			set {
				if (showToken != value) {
					showToken = value;
					OnPropertyChanged(nameof(ShowToken));
				}
			}
		}
		bool showToken = true;

		public bool SingleClickExpandsChildren {
			get => singleClickExpandsChildren;
			set {
				if (singleClickExpandsChildren != value) {
					singleClickExpandsChildren = value;
					OnPropertyChanged(nameof(SingleClickExpandsChildren));
				}
			}
		}
		bool singleClickExpandsChildren = true;

		public AnalyzerSettings Clone() => CopyTo(new AnalyzerSettings());

		public AnalyzerSettings CopyTo(AnalyzerSettings other) {
			other.SyntaxHighlight = SyntaxHighlight;
			other.ShowToken = ShowToken;
			other.SingleClickExpandsChildren = SingleClickExpandsChildren;
			return other;
		}
	}

	[Export, Export(typeof(IAnalyzerSettings))]
	sealed class AnalyzerSettingsImpl : AnalyzerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("0A9208EC-CFAB-41C2-82C6-FCDA44A8E684");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		AnalyzerSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			SyntaxHighlight = sect.Attribute<bool?>(nameof(SyntaxHighlight)) ?? SyntaxHighlight;
			ShowToken = sect.Attribute<bool?>(nameof(ShowToken)) ?? ShowToken;
			SingleClickExpandsChildren = sect.Attribute<bool?>(nameof(SingleClickExpandsChildren)) ?? SingleClickExpandsChildren;
			PropertyChanged += AnalyzerSettingsImpl_PropertyChanged;
		}

		void AnalyzerSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
			sect.Attribute(nameof(ShowToken), ShowToken);
			sect.Attribute(nameof(SingleClickExpandsChildren), SingleClickExpandsChildren);
		}
	}
}
