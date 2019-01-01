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
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Search;

namespace dnSpy.AsmEditor.Resources {
	enum ResourceVisibility {
		Public	= (int)ManifestResourceAttributes.Public >> 0,
		Private	= (int)ManifestResourceAttributes.Private >> 0,
	}

	sealed class ResourceVM : ViewModelBase {
		readonly ResourceOptions origOptions;

		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand PickAssemblyCommand => new RelayCommand(a => PickAssembly(), a => IsAssemblyLinked);

		ResourceType Type {
			get => type;
			set {
				if (type != value) {
					type = value;
					OnPropertyChanged(nameof(Type));
					OnPropertyChanged(nameof(IsEmbedded));
					OnPropertyChanged(nameof(IsAssemblyLinked));
					OnPropertyChanged(nameof(IsLinked));
				}
			}
		}
		ResourceType type;

		public bool IsEmbedded => Type == ResourceType.Embedded;
		public bool IsAssemblyLinked => Type == ResourceType.AssemblyLinked;
		public bool IsLinked => Type == ResourceType.Linked;

		internal static readonly EnumVM[] resourceVisibilityList = EnumVM.Create(typeof(ResourceVisibility));
		public EnumListVM ResourceVisibilityVM { get; } = new EnumListVM(resourceVisibilityList);

		public ManifestResourceAttributes Attributes {
			get {
				var mask = ManifestResourceAttributes.VisibilityMask;
				return (attrs & ~mask) |
					(ManifestResourceAttributes)((int)(ResourceVisibility)ResourceVisibilityVM.SelectedItem << 0);
			}
			set {
				if (attrs != value) {
					attrs = value;
					OnPropertyChanged(nameof(Attributes));
				}
			}
		}
		ManifestResourceAttributes attrs;

		public string Name {
			get => name;
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged(nameof(Name));
				}
			}
		}
		UTF8String name;

		public AssemblyRef Assembly {
			get => assembly;
			set {
				if (assembly != value) {
					assembly = value;
					OnPropertyChanged(nameof(Assembly));
					OnPropertyChanged(nameof(AssemblyFullName));
					HasErrorUpdated();
				}
			}
		}
		AssemblyRef assembly;

		public string AssemblyFullName => Assembly == null ? "null" : Assembly.FullName;
		public HexStringVM FileHashValue { get; }

		public string FileName {
			get => fileName;
			set {
				if (fileName != value) {
					fileName = value;
					OnPropertyChanged(nameof(FileName));
				}
			}
		}
		UTF8String fileName;

		public bool FileContainsNoMetadata {
			get => fileContainsNoMetadata;
			set {
				if (fileContainsNoMetadata != value) {
					fileContainsNoMetadata = value;
					OnPropertyChanged(nameof(FileContainsNoMetadata));
				}
			}
		}
		bool fileContainsNoMetadata;

		readonly ModuleDef ownerModule;

		public ResourceVM(ResourceOptions options, ModuleDef ownerModule) {
			origOptions = options;
			this.ownerModule = ownerModule;

			FileHashValue = new HexStringVM(a => HasErrorUpdated());

			Reinitialize();
		}

		void PickAssembly() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var newAsm = dnlibTypePicker.GetDnlibType<IDsDocument>(dnSpy_AsmEditor_Resources.Pick_Assembly, new FlagsDocumentTreeNodeFilter(VisibleMembersFlags.AssemblyDef), null, ownerModule);
			if (newAsm != null && newAsm.AssemblyDef != null)
				Assembly = newAsm.AssemblyDef.ToAssemblyRef();
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public ResourceOptions CreateResourceOptions() => CopyTo(new ResourceOptions());

		void InitializeFrom(ResourceOptions options) {
			Type = options.ResourceType;
			ResourceVisibilityVM.SelectedItem = (ResourceVisibility)((int)(options.Attributes & ManifestResourceAttributes.VisibilityMask) >> 0);
			Attributes = options.Attributes;
			Name = options.Name;
			Assembly = options.Assembly;
			if (options.File != null) {
				FileHashValue.Value = options.File.HashValue;
				FileName = options.File.Name ?? UTF8String.Empty;
				FileContainsNoMetadata = options.File.ContainsNoMetadata;
			}
			else {
				FileHashValue.Value = Array.Empty<byte>();
				FileName = string.Empty;
				FileContainsNoMetadata = false;
			}
		}

		ResourceOptions CopyTo(ResourceOptions options) {
			options.ResourceType = Type;
			options.Attributes = Attributes;
			options.Name = Name;
			options.Assembly = Assembly;
			options.File = new FileDefUser(FileName,
					FileContainsNoMetadata ? FileAttributes.ContainsNoMetadata : FileAttributes.ContainsMetadata,
					FileHashValue.Value.ToArray());
			return options;
		}

		protected override string Verify(string columnName) {
			if (columnName == nameof(AssemblyFullName)) {
				if (Assembly == null)
					return dnSpy_AsmEditor_Resources.Error_AssemblyFieldMustNotBeEmpty;
				return string.Empty;
			}
			return string.Empty;
		}

		public override bool HasError {
			get {
				return
					(IsAssemblyLinked && Assembly == null) ||
					(IsLinked && FileHashValue.HasError);
			}
		}
	}
}
