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

using dnSpy.Contracts.MVVM;

namespace dnSpy.Decompiler.ILSpy.Core.Settings {
	class ILSettings : ViewModelBase {
		protected virtual void OnModified() { }

		public bool ShowILComments {
			get { return showILComments; }
			set {
				if (showILComments != value) {
					showILComments = value;
					OnPropertyChanged(nameof(ShowILComments));
					OnModified();
				}
			}
		}
		bool showILComments = false;

		public bool ShowXmlDocumentation {
			get { return showXmlDocumentation; }
			set {
				if (showXmlDocumentation != value) {
					showXmlDocumentation = value;
					OnPropertyChanged(nameof(ShowXmlDocumentation));
					OnModified();
				}
			}
		}
		bool showXmlDocumentation = true;

		public bool ShowTokenAndRvaComments {
			get { return showTokenAndRvaComments; }
			set {
				if (showTokenAndRvaComments != value) {
					showTokenAndRvaComments = value;
					OnPropertyChanged(nameof(ShowTokenAndRvaComments));
					OnModified();
				}
			}
		}
		bool showTokenAndRvaComments = true;

		public bool ShowILBytes {
			get { return showILBytes; }
			set {
				if (showILBytes != value) {
					showILBytes = value;
					OnPropertyChanged(nameof(ShowILBytes));
					OnModified();
				}
			}
		}
		bool showILBytes = true;

		public bool SortMembers {
			get { return sortMembers; }
			set {
				if (sortMembers != value) {
					sortMembers = value;
					OnPropertyChanged(nameof(SortMembers));
					OnModified();
				}
			}
		}
		bool sortMembers = true;

		public ILSettings Clone() => CopyTo(new ILSettings());

		public ILSettings CopyTo(ILSettings other) {
			other.ShowILComments = this.ShowILComments;
			other.ShowXmlDocumentation = this.ShowXmlDocumentation;
			other.ShowTokenAndRvaComments = this.ShowTokenAndRvaComments;
			other.ShowILBytes = this.ShowILBytes;
			other.SortMembers = this.SortMembers;
			return other;
		}

		public override bool Equals(object obj) {
			var other = obj as ILSettings;
			return other != null &&
				ShowILComments == other.ShowILComments &&
				ShowXmlDocumentation == other.ShowXmlDocumentation &&
				ShowTokenAndRvaComments == other.ShowTokenAndRvaComments &&
				ShowILBytes == other.ShowILBytes &&
				SortMembers == other.SortMembers;
		}

		public override int GetHashCode() {
			uint h = 0;

			if (ShowILComments) h ^= 0x80000000;
			if (ShowXmlDocumentation) h ^= 0x40000000;
			if (ShowTokenAndRvaComments) h ^= 0x20000000;
			if (ShowILBytes) h ^= 0x10000000;
			if (SortMembers) h ^= 0x08000000;

			return (int)h;
		}
	}
}
