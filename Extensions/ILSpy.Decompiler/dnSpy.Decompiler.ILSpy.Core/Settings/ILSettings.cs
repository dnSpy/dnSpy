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
using System.Threading;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Decompiler.ILSpy.Core.Settings {
	class ILSettings : ViewModelBase {
		protected virtual void OnModified() { }
		public event EventHandler SettingsVersionChanged;

		void OptionsChanged() {
			Interlocked.Increment(ref settingsVersion);
			OnModified();
			SettingsVersionChanged?.Invoke(this, EventArgs.Empty);
		}

		public int SettingsVersion => settingsVersion;
		volatile int settingsVersion;

		public bool ShowILComments {
			get => showILComments;
			set {
				if (showILComments != value) {
					showILComments = value;
					OnPropertyChanged(nameof(ShowILComments));
					OptionsChanged();
				}
			}
		}
		bool showILComments = false;

		public bool ShowXmlDocumentation {
			get => showXmlDocumentation;
			set {
				if (showXmlDocumentation != value) {
					showXmlDocumentation = value;
					OnPropertyChanged(nameof(ShowXmlDocumentation));
					OptionsChanged();
				}
			}
		}
		bool showXmlDocumentation = true;

		public bool ShowTokenAndRvaComments {
			get => showTokenAndRvaComments;
			set {
				if (showTokenAndRvaComments != value) {
					showTokenAndRvaComments = value;
					OnPropertyChanged(nameof(ShowTokenAndRvaComments));
					OptionsChanged();
				}
			}
		}
		bool showTokenAndRvaComments = true;

		public bool ShowILBytes {
			get => showILBytes;
			set {
				if (showILBytes != value) {
					showILBytes = value;
					OnPropertyChanged(nameof(ShowILBytes));
					OptionsChanged();
				}
			}
		}
		bool showILBytes = true;

		public bool SortMembers {
			get => sortMembers;
			set {
				if (sortMembers != value) {
					sortMembers = value;
					OnPropertyChanged(nameof(SortMembers));
					OptionsChanged();
				}
			}
		}
		bool sortMembers = false;

		public bool ShowPdbInfo {
			get => showPdbInfo;
			set {
				if (showPdbInfo != value) {
					showPdbInfo = value;
					OnPropertyChanged(nameof(ShowPdbInfo));
					OptionsChanged();
				}
			}
		}
		bool showPdbInfo = true;

		public int MaxStringLength {
			get => maxStringLength;
			set {
				if (maxStringLength != value) {
					maxStringLength = value;
					OnPropertyChanged(nameof(MaxStringLength));
					OptionsChanged();
				}
			}
		}
		int maxStringLength = ICSharpCode.Decompiler.DecompilerSettings.ConstMaxStringLength;

		public ILSettings Clone() => CopyTo(new ILSettings());

		public ILSettings CopyTo(ILSettings other) {
			other.ShowILComments = ShowILComments;
			other.ShowXmlDocumentation = ShowXmlDocumentation;
			other.ShowTokenAndRvaComments = ShowTokenAndRvaComments;
			other.ShowILBytes = ShowILBytes;
			other.SortMembers = SortMembers;
			other.ShowPdbInfo = ShowPdbInfo;
			other.MaxStringLength = MaxStringLength;
			return other;
		}

		public override bool Equals(object obj) {
			var other = obj as ILSettings;
			return other != null &&
				ShowILComments == other.ShowILComments &&
				ShowXmlDocumentation == other.ShowXmlDocumentation &&
				ShowTokenAndRvaComments == other.ShowTokenAndRvaComments &&
				ShowILBytes == other.ShowILBytes &&
				SortMembers == other.SortMembers &&
				ShowPdbInfo == other.ShowPdbInfo &&
				MaxStringLength == other.MaxStringLength;
		}

		public override int GetHashCode() {
			uint h = 0;

			if (ShowILComments) h ^= 0x80000000;
			if (ShowXmlDocumentation) h ^= 0x40000000;
			if (ShowTokenAndRvaComments) h ^= 0x20000000;
			if (ShowILBytes) h ^= 0x10000000;
			if (SortMembers) h ^= 0x08000000;
			if (ShowPdbInfo) h ^= 0x04000000;
			h ^= (uint)MaxStringLength;

			return (int)h;
		}
	}
}
