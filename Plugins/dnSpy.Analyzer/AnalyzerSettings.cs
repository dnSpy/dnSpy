/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Settings;
using dnSpy.Shared.MVVM;

namespace dnSpy.Analyzer {
	interface IAnalyzerSettings : INotifyPropertyChanged {
		bool SyntaxHighlight { get; }
		bool ShowToken { get; }
		bool SingleClickExpandsChildren { get; }
		bool UseNewRenderer { get; }
	}

	class AnalyzerSettings : ViewModelBase, IAnalyzerSettings {
		protected virtual void OnModified() {
		}

		public bool SyntaxHighlight {
			get { return syntaxHighlight; }
			set {
				if (syntaxHighlight != value) {
					syntaxHighlight = value;
					OnPropertyChanged("SyntaxHighlight");
					OnModified();
				}
			}
		}
		bool syntaxHighlight = true;

		public bool ShowToken {
			get { return showToken; }
			set {
				if (showToken != value) {
					showToken = value;
					OnPropertyChanged("ShowToken");
					OnModified();
				}
			}
		}
		bool showToken = true;

		public bool SingleClickExpandsChildren {
			get { return singleClickExpandsChildren; }
			set {
				if (singleClickExpandsChildren != value) {
					singleClickExpandsChildren = value;
					OnPropertyChanged("SingleClickExpandsChildren");
					OnModified();
				}
			}
		}
		bool singleClickExpandsChildren = true;

		public bool UseNewRenderer {
			get { return useNewRenderer; }
			set {
				if (useNewRenderer != value) {
					useNewRenderer = value;
					OnPropertyChanged("UseNewRenderer");
					OnModified();
				}
			}
		}
		bool useNewRenderer = false;

		public AnalyzerSettings Clone() {
			return CopyTo(new AnalyzerSettings());
		}

		public AnalyzerSettings CopyTo(AnalyzerSettings other) {
			other.SyntaxHighlight = this.SyntaxHighlight;
			other.ShowToken = this.ShowToken;
			other.SingleClickExpandsChildren = this.SingleClickExpandsChildren;
			other.UseNewRenderer = this.UseNewRenderer;
			return other;
		}
	}

	[Export, Export(typeof(IAnalyzerSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class AnalyzerSettingsImpl : AnalyzerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("0A9208EC-CFAB-41C2-82C6-FCDA44A8E684");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		AnalyzerSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.SyntaxHighlight = sect.Attribute<bool?>("SyntaxHighlight") ?? this.SyntaxHighlight;
			this.ShowToken = sect.Attribute<bool?>("ShowToken") ?? this.ShowToken;
			this.SingleClickExpandsChildren = sect.Attribute<bool?>("SingleClickExpandsChildren") ?? this.SingleClickExpandsChildren;
			this.UseNewRenderer = sect.Attribute<bool?>("UseNewRenderer") ?? this.UseNewRenderer;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute("SyntaxHighlight", SyntaxHighlight);
			sect.Attribute("ShowToken", ShowToken);
			sect.Attribute("SingleClickExpandsChildren", SingleClickExpandsChildren);
			sect.Attribute("UseNewRenderer", UseNewRenderer);
		}
	}
}
