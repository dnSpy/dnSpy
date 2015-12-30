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

using System;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Files;
using dnSpy.Shared.UI.MVVM;
using dnSpy.Shared.UI.Search;

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

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public ICommand PickAssemblyCommand {
			get { return new RelayCommand(a => PickAssembly(), a => IsAssemblyLinked); }
		}

		ResourceType Type {
			get { return type; }
			set {
				if (type != value) {
					type = value;
					OnPropertyChanged("Type");
					OnPropertyChanged("IsEmbedded");
					OnPropertyChanged("IsAssemblyLinked");
					OnPropertyChanged("IsLinked");
				}
			}
		}
		ResourceType type;

		public bool IsEmbedded {
			get { return Type == ResourceType.Embedded; }
		}

		public bool IsAssemblyLinked {
			get { return Type == ResourceType.AssemblyLinked; }
		}

		public bool IsLinked {
			get { return Type == ResourceType.Linked; }
		}

		internal static readonly EnumVM[] resourceVisibilityList = EnumVM.Create(typeof(ResourceVisibility));
		public EnumListVM ResourceVisibilityVM {
			get { return resourceVisibilityVM; }
		}
		readonly EnumListVM resourceVisibilityVM = new EnumListVM(resourceVisibilityList);

		public ManifestResourceAttributes Attributes {
			get {
				var mask = ManifestResourceAttributes.VisibilityMask;
				return (attrs & ~mask) |
					(ManifestResourceAttributes)((int)(ResourceVisibility)ResourceVisibilityVM.SelectedItem << 0);
			}
			set {
				if (attrs != value) {
					attrs = value;
					OnPropertyChanged("Attributes");
				}
			}
		}
		ManifestResourceAttributes attrs;

		public string Name {
			get { return name; }
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged("Name");
				}
			}
		}
		UTF8String name;

		public AssemblyRef Assembly {
			get { return assembly; }
			set {
				if (assembly != value) {
					assembly = value;
					OnPropertyChanged("Assembly");
					OnPropertyChanged("AssemblyFullName");
					HasErrorUpdated();
				}
			}
		}
		AssemblyRef assembly;

		public string AssemblyFullName {
			get { return Assembly == null ? "null" : Assembly.FullName; }
		}

		public HexStringVM FileHashValue {
			get { return fileHashValue; }
		}
		HexStringVM fileHashValue;

		public string FileName {
			get { return fileName; }
			set {
				if (fileName != value) {
					fileName = value;
					OnPropertyChanged("FileName");
				}
			}
		}
		UTF8String fileName;

		public bool FileContainsNoMetaData {
			get { return fileContainsNoMetaData; }
			set {
				if (fileContainsNoMetaData != value) {
					fileContainsNoMetaData = value;
					OnPropertyChanged("FileContainsNoMetaData");
				}
			}
		}
		bool fileContainsNoMetaData;

		readonly ModuleDef ownerModule;

		public ResourceVM(ResourceOptions options, ModuleDef ownerModule) {
			this.origOptions = options;
			this.ownerModule = ownerModule;

			this.fileHashValue = new HexStringVM(a => HasErrorUpdated());

			Reinitialize();
		}

		void PickAssembly() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var newAsm = dnlibTypePicker.GetDnlibType<IDnSpyFile>(dnSpy_AsmEditor_Resources.Pick_Assembly, new FlagsFileTreeNodeFilter(VisibleMembersFlags.AssemblyDef), null, ownerModule);
			if (newAsm != null && newAsm.AssemblyDef != null)
				Assembly = newAsm.AssemblyDef.ToAssemblyRef();
		}

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public ResourceOptions CreateResourceOptions() {
			return CopyTo(new ResourceOptions());
		}

		void InitializeFrom(ResourceOptions options) {
			this.Type = options.ResourceType;
			this.ResourceVisibilityVM.SelectedItem = (ResourceVisibility)((int)(options.Attributes & ManifestResourceAttributes.VisibilityMask) >> 0);
			this.Attributes = options.Attributes;
			this.Name = options.Name;
			this.Assembly = options.Assembly;
			if (options.File != null) {
				this.FileHashValue.Value = options.File.HashValue;
				this.FileName = options.File.Name ?? UTF8String.Empty;
				this.FileContainsNoMetaData = options.File.ContainsNoMetaData;
			}
			else {
				this.FileHashValue.Value = new byte[0];
				this.FileName = string.Empty;
				this.FileContainsNoMetaData = false;
			}
		}

		ResourceOptions CopyTo(ResourceOptions options) {
			options.ResourceType = this.Type;
			options.Attributes = this.Attributes;
			options.Name = this.Name;
			options.Assembly = this.Assembly;
			options.File = new FileDefUser(this.FileName,
					this.FileContainsNoMetaData ? FileAttributes.ContainsNoMetaData : FileAttributes.ContainsMetaData,
					this.FileHashValue.Value.ToArray());
			return options;
		}

		protected override string Verify(string columnName) {
			if (columnName == "AssemblyFullName") {
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
