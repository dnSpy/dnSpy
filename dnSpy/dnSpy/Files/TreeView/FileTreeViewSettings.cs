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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.MVVM;

namespace dnSpy.Files.TreeView {
	class FileTreeViewSettings : ViewModelBase, IFileTreeViewSettings {
		protected virtual void OnModified() { }

		public bool SyntaxHighlight {
			get { return syntaxHighlightFileTreeView; }
			set {
				if (syntaxHighlightFileTreeView != value) {
					syntaxHighlightFileTreeView = value;
					OnPropertyChanged(nameof(SyntaxHighlight));
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
					OnPropertyChanged(nameof(SingleClickExpandsTreeViewChildren));
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
					OnPropertyChanged(nameof(ShowAssemblyVersion));
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
					OnPropertyChanged(nameof(ShowAssemblyPublicKeyToken));
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
					OnPropertyChanged(nameof(ShowToken));
					OnModified();
				}
			}
		}
		bool showToken = true;

		public bool DeserializeResources {
			get { return deserializeResources; }
			set {
				if (deserializeResources != value) {
					deserializeResources = value;
					OnPropertyChanged(nameof(DeserializeResources));
					OnModified();
				}
			}
		}
		bool deserializeResources = true;

		MemberKind[] memberKinds = new MemberKind[5] {
			MemberKind.Methods,
			MemberKind.Properties,
			MemberKind.Events,
			MemberKind.Fields,
			MemberKind.NestedTypes,
		};

		public MemberKind MemberKind0 {
			get { return memberKinds[0]; }
			set { SetMemberKind(0, value); }
		}

		public MemberKind MemberKind1 {
			get { return memberKinds[1]; }
			set { SetMemberKind(1, value); }
		}

		public MemberKind MemberKind2 {
			get { return memberKinds[2]; }
			set { SetMemberKind(2, value); }
		}

		public MemberKind MemberKind3 {
			get { return memberKinds[3]; }
			set { SetMemberKind(3, value); }
		}

		public MemberKind MemberKind4 {
			get { return memberKinds[4]; }
			set { SetMemberKind(4, value); }
		}

		void SetMemberKind(int index, MemberKind newValue) {
			if (memberKinds[index] == newValue)
				return;

			int otherIndex = Array.IndexOf(memberKinds, newValue);
			Debug.Assert(otherIndex >= 0);
			if (otherIndex >= 0) {
				memberKinds[otherIndex] = memberKinds[index];
				memberKinds[index] = newValue;

				OnPropertyChanged(string.Format(MemberKind_format, otherIndex));
			}
			OnPropertyChanged(string.Format(MemberKind_format, index));
			OnModified();
		}
		static string MemberKind_format = nameof(MemberKind0).Substring(0, nameof(MemberKind0).Length - 1) + "{0}";

		public FileTreeViewSettings Clone() => CopyTo(new FileTreeViewSettings());

		public FileTreeViewSettings CopyTo(FileTreeViewSettings other) {
			other.SyntaxHighlight = this.SyntaxHighlight;
			other.SingleClickExpandsTreeViewChildren = this.SingleClickExpandsTreeViewChildren;
			other.ShowAssemblyVersion = this.ShowAssemblyVersion;
			other.ShowAssemblyPublicKeyToken = this.ShowAssemblyPublicKeyToken;
			other.ShowToken = this.ShowToken;
			other.DeserializeResources = this.DeserializeResources;
			other.MemberKind0 = this.MemberKind0;
			other.MemberKind1 = this.MemberKind1;
			other.MemberKind2 = this.MemberKind2;
			other.MemberKind3 = this.MemberKind3;
			other.MemberKind4 = this.MemberKind4;
			return other;
		}
	}

	[Export, Export(typeof(IFileTreeViewSettings))]
	sealed class FileTreeViewSettingsImpl : FileTreeViewSettings {
		static readonly Guid SETTINGS_GUID = new Guid("3E04ABE0-FD5E-4938-B40C-F86AA0FA377D");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		FileTreeViewSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.SyntaxHighlight = sect.Attribute<bool?>(nameof(SyntaxHighlight)) ?? this.SyntaxHighlight;
			this.SingleClickExpandsTreeViewChildren = sect.Attribute<bool?>(nameof(SingleClickExpandsTreeViewChildren)) ?? this.SingleClickExpandsTreeViewChildren;
			this.ShowAssemblyVersion = sect.Attribute<bool?>(nameof(ShowAssemblyVersion)) ?? this.ShowAssemblyVersion;
			this.ShowAssemblyPublicKeyToken = sect.Attribute<bool?>(nameof(ShowAssemblyPublicKeyToken)) ?? this.ShowAssemblyPublicKeyToken;
			this.ShowToken = sect.Attribute<bool?>(nameof(ShowToken)) ?? this.ShowToken;
			this.DeserializeResources = sect.Attribute<bool?>(nameof(DeserializeResources)) ?? this.DeserializeResources;
			this.MemberKind0 = sect.Attribute<MemberKind?>(nameof(MemberKind0)) ?? this.MemberKind0;
			this.MemberKind1 = sect.Attribute<MemberKind?>(nameof(MemberKind1)) ?? this.MemberKind1;
			this.MemberKind2 = sect.Attribute<MemberKind?>(nameof(MemberKind2)) ?? this.MemberKind2;
			this.MemberKind3 = sect.Attribute<MemberKind?>(nameof(MemberKind3)) ?? this.MemberKind3;
			this.MemberKind4 = sect.Attribute<MemberKind?>(nameof(MemberKind4)) ?? this.MemberKind4;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
			sect.Attribute(nameof(SingleClickExpandsTreeViewChildren), SingleClickExpandsTreeViewChildren);
			sect.Attribute(nameof(ShowAssemblyVersion), ShowAssemblyVersion);
			sect.Attribute(nameof(ShowAssemblyPublicKeyToken), ShowAssemblyPublicKeyToken);
			sect.Attribute(nameof(ShowToken), ShowToken);
			sect.Attribute(nameof(DeserializeResources), DeserializeResources);
			sect.Attribute(nameof(MemberKind0), MemberKind0);
			sect.Attribute(nameof(MemberKind1), MemberKind1);
			sect.Attribute(nameof(MemberKind2), MemberKind2);
			sect.Attribute(nameof(MemberKind3), MemberKind3);
			sect.Attribute(nameof(MemberKind4), MemberKind4);
		}
	}
}
