/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Files.TreeView {
	class FileTreeViewSettings : ViewModelBase, IFileTreeViewSettings {
		protected virtual void OnModified() {
		}

		public bool SyntaxHighlight {
			get { return syntaxHighlightFileTreeView; }
			set {
				if (syntaxHighlightFileTreeView != value) {
					syntaxHighlightFileTreeView = value;
					OnPropertyChanged("SyntaxHighlight");
					OnModified();
				}
			}
		}
		bool syntaxHighlightFileTreeView = true;

		public bool SingleClickExpandsTreeViewChildren {
			get { return singleClickExpandsTreeViewChildren; }
			set {
				if (singleClickExpandsTreeViewChildren != value) {
					singleClickExpandsTreeViewChildren = value;
					OnPropertyChanged("SingleClickExpandsTreeViewChildren");
					OnModified();
				}
			}
		}
		bool singleClickExpandsTreeViewChildren = true;

		public bool ShowAssemblyVersion {
			get { return showAssemblyVersion; }
			set {
				if (showAssemblyVersion != value) {
					showAssemblyVersion = value;
					OnPropertyChanged("ShowAssemblyVersion");
					OnModified();
				}
			}
		}
		bool showAssemblyVersion = true;

		public bool ShowAssemblyPublicKeyToken {
			get { return showAssemblyPublicKeyToken; }
			set {
				if (showAssemblyPublicKeyToken != value) {
					showAssemblyPublicKeyToken = value;
					OnPropertyChanged("ShowAssemblyPublicKeyToken");
					OnModified();
				}
			}
		}
		bool showAssemblyPublicKeyToken = false;

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
	}

	[Export, Export(typeof(IFileTreeViewSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class FileTreeViewSettingsImpl : FileTreeViewSettings {
		const string SETTINGS_NAME = "3E04ABE0-FD5E-4938-B40C-F86AA0FA377D";

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		FileTreeViewSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_NAME);
			this.SyntaxHighlight = sect.Attribute<bool?>("SyntaxHighlight") ?? this.SyntaxHighlight;
			this.SingleClickExpandsTreeViewChildren = sect.Attribute<bool?>("SingleClickExpandsTreeViewChildren") ?? this.SingleClickExpandsTreeViewChildren;
			this.ShowAssemblyVersion = sect.Attribute<bool?>("ShowAssemblyVersion") ?? this.ShowAssemblyVersion;
			this.ShowAssemblyPublicKeyToken = sect.Attribute<bool?>("ShowAssemblyPublicKeyToken") ?? this.ShowAssemblyPublicKeyToken;
			this.ShowToken = sect.Attribute<bool?>("ShowToken") ?? this.ShowToken;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_NAME);
			sect.Attribute("SyntaxHighlight", SyntaxHighlight);
			sect.Attribute("SingleClickExpandsTreeViewChildren", SingleClickExpandsTreeViewChildren);
			sect.Attribute("ShowAssemblyVersion", ShowAssemblyVersion);
			sect.Attribute("ShowAssemblyPublicKeyToken", ShowAssemblyPublicKeyToken);
			sect.Attribute("ShowToken", ShowToken);
		}
	}
}
