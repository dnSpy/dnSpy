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
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Assembly {
	enum AsmProcArch {
		None = (int)((uint)AssemblyAttributes.PA_None >> (int)AssemblyAttributes.PA_Shift),
		MSIL = (int)((uint)AssemblyAttributes.PA_MSIL >> (int)AssemblyAttributes.PA_Shift),
		x86 = (int)((uint)AssemblyAttributes.PA_x86 >> (int)AssemblyAttributes.PA_Shift),
		IA64 = (int)((uint)AssemblyAttributes.PA_IA64 >> (int)AssemblyAttributes.PA_Shift),
		AMD64 = (int)((uint)AssemblyAttributes.PA_AMD64 >> (int)AssemblyAttributes.PA_Shift),
		ARM = (int)((uint)AssemblyAttributes.PA_ARM >> (int)AssemblyAttributes.PA_Shift),
		Reserved = 6,
		NoPlatform = (int)((uint)AssemblyAttributes.PA_NoPlatform >> (int)AssemblyAttributes.PA_Shift),
	}

	enum AsmContType {
		Default = (int)((uint)AssemblyAttributes.ContentType_Default >> 9),
		WindowsRuntime = (int)((uint)AssemblyAttributes.ContentType_WindowsRuntime >> 9),
	}

	sealed class AssemblyOptionsVM : ViewModelBase {
		readonly AssemblyOptions origOptions;

		public IOpenPublicKeyFile OpenPublicKeyFile {
			set => openPublicKeyFile = value;
		}
		IOpenPublicKeyFile? openPublicKeyFile;

		public ICommand OpenPublicKeyFileCommand => new RelayCommand(a => OnOpenPublicKeyFile());
		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());

		public bool CanShowClrVersion {
			get => canShowClrVersion;
			set {
				if (canShowClrVersion != value) {
					canShowClrVersion = value;
					OnPropertyChanged(nameof(CanShowClrVersion));
				}
			}
		}
		bool canShowClrVersion;

		public EnumListVM ClrVersion => clrVersionVM;
		readonly EnumListVM clrVersionVM = new EnumListVM(Module.NetModuleOptionsVM.clrVersionList);

		static readonly EnumVM[] hashAlgorithmList = EnumVM.Create(typeof(AssemblyHashAlgorithm));
		public EnumListVM HashAlgorithm => hashAlgorithmVM;
		readonly EnumListVM hashAlgorithmVM;

		public UInt16VM VersionMajor { get; }
		public UInt16VM VersionMinor { get; }
		public UInt16VM VersionBuild { get; }
		public UInt16VM VersionRevision { get; }

		public AssemblyAttributes Flags {
			get => (flags & ~(AssemblyAttributes.PA_Mask | AssemblyAttributes.ContentType_Mask) |
					((AssemblyAttributes)((uint)(AsmProcArch)ProcessArchitecture.SelectedItem! << (int)AssemblyAttributes.PA_Shift) & AssemblyAttributes.PA_Mask) |
					((AssemblyAttributes)((uint)(AsmContType)ContentType.SelectedItem! << 9) & AssemblyAttributes.ContentType_Mask));
			set {
				if (flags != value) {
					flags = value;
					OnPropertyChanged(nameof(AssemblyFullName));
					OnPropertyChanged(nameof(Flags));
					OnPropertyChanged(nameof(FlagsPublicKey));
					OnPropertyChanged(nameof(PA_Specified));
					OnPropertyChanged(nameof(Retargetable));
					OnPropertyChanged(nameof(EnableJITcompileTracking));
					OnPropertyChanged(nameof(DisableJITcompileOptimizer));
					ProcessArchitecture.SelectedItem = (AsmProcArch)((uint)(flags & AssemblyAttributes.PA_Mask) >> (int)AssemblyAttributes.PA_Shift);
					ContentType.SelectedItem = (AsmContType)((uint)(flags & AssemblyAttributes.ContentType_Mask) >> 9);
				}
			}
		}
		AssemblyAttributes flags;

		public bool FlagsPublicKey {
			get => GetFlagValue(AssemblyAttributes.PublicKey);
			set => SetFlagValue(AssemblyAttributes.PublicKey, value);
		}

		public bool PA_Specified {
			get => GetFlagValue(AssemblyAttributes.PA_Specified);
			set => SetFlagValue(AssemblyAttributes.PA_Specified, value);
		}

		public bool Retargetable {
			get => GetFlagValue(AssemblyAttributes.Retargetable);
			set => SetFlagValue(AssemblyAttributes.Retargetable, value);
		}

		public bool EnableJITcompileTracking {
			get => GetFlagValue(AssemblyAttributes.EnableJITcompileTracking);
			set => SetFlagValue(AssemblyAttributes.EnableJITcompileTracking, value);
		}

		public bool DisableJITcompileOptimizer {
			get => GetFlagValue(AssemblyAttributes.DisableJITcompileOptimizer);
			set => SetFlagValue(AssemblyAttributes.DisableJITcompileOptimizer, value);
		}

		bool GetFlagValue(AssemblyAttributes flag) => (Flags & flag) != 0;

		void SetFlagValue(AssemblyAttributes flag, bool value) {
			if (value)
				Flags |= flag;
			else
				Flags &= ~flag;
		}

		internal static readonly EnumVM[] processArchList = EnumVM.Create(typeof(AsmProcArch));
		public EnumListVM ProcessArchitecture => processArchitectureVM;
		readonly EnumListVM processArchitectureVM = new EnumListVM(processArchList);

		internal static readonly EnumVM[] contentTypeList = EnumVM.Create(typeof(AsmContType));
		public EnumListVM ContentType => contentTypeVM;
		readonly EnumListVM contentTypeVM;

		public HexStringVM PublicKey { get; }

		public string? Name {
			get => name;
			set {
				if (name != value) {
					name = value;
					OnPropertyChanged(nameof(Name));
					OnPropertyChanged(nameof(AssemblyFullName));
				}
			}
		}
		UTF8String? name;

		public string? Culture {
			get => culture;
			set {
				if (culture != value) {
					culture = value;
					OnPropertyChanged(nameof(Culture));
					OnPropertyChanged(nameof(AssemblyFullName));
				}
			}
		}
		string? culture;

		public string AssemblyFullName {
			get {
				var asm = new AssemblyNameInfo();
				asm.HashAlgId = (AssemblyHashAlgorithm)HashAlgorithm.SelectedItem!;
				int major = VersionMajor.HasError ? 0 : VersionMajor.Value;
				int minor = VersionMinor.HasError ? 0 : VersionMinor.Value;
				int build = VersionBuild.HasError ? 0 : VersionBuild.Value;
				int revision = VersionRevision.HasError ? 0 : VersionRevision.Value;
				asm.Version = new Version(major, minor, build, revision);
				asm.Attributes = Flags;
				asm.PublicKeyOrToken = new PublicKey(PublicKey.HasError ? Array.Empty<byte>() : PublicKey.Value.ToArray());
				asm.Name = Name;
				asm.Culture = Culture;
				return asm.ToString();
			}
		}

		public CustomAttributesVM CustomAttributesVM { get; }
		public DeclSecuritiesVM DeclSecuritiesVM { get; }

		readonly ModuleDef ownerModule;

		public AssemblyOptionsVM(AssemblyOptions options, ModuleDef ownerModule, IDecompilerService decompilerService) {
			this.ownerModule = ownerModule;
			origOptions = options;
			hashAlgorithmVM = new EnumListVM(hashAlgorithmList, (a, b) => OnPropertyChanged(nameof(AssemblyFullName)));
			contentTypeVM = new EnumListVM(contentTypeList, (a, b) => OnPropertyChanged(nameof(AssemblyFullName)));
			VersionMajor = new UInt16VM(a => { HasErrorUpdated(); OnPropertyChanged(nameof(AssemblyFullName)); }, true);
			VersionMinor = new UInt16VM(a => { HasErrorUpdated(); OnPropertyChanged(nameof(AssemblyFullName)); }, true);
			VersionBuild = new UInt16VM(a => { HasErrorUpdated(); OnPropertyChanged(nameof(AssemblyFullName)); }, true);
			VersionRevision = new UInt16VM(a => { HasErrorUpdated(); OnPropertyChanged(nameof(AssemblyFullName)); }, true);
			PublicKey = new HexStringVM(a => { HasErrorUpdated(); OnPropertyChanged(nameof(AssemblyFullName)); UpdatePublicKeyFlag(); }) { UppercaseHex = false };
			CustomAttributesVM = new CustomAttributesVM(ownerModule, decompilerService);
			DeclSecuritiesVM = new DeclSecuritiesVM(ownerModule, decompilerService, null, null);
			Reinitialize();
		}

		void UpdatePublicKeyFlag() => FlagsPublicKey = !PublicKey.IsNull;
		void Reinitialize() => InitializeFrom(origOptions);
		public AssemblyOptions CreateAssemblyOptions() => CopyTo(new AssemblyOptions());

		void InitializeFrom(AssemblyOptions options) {
			PublicKey.Value = options.PublicKey.Data;
			HashAlgorithm.SelectedItem = options.HashAlgorithm;
			VersionMajor.Value = checked((ushort)options.Version.Major);
			VersionMinor.Value = checked((ushort)options.Version.Minor);
			VersionBuild.Value = checked((ushort)options.Version.Build);
			VersionRevision.Value = checked((ushort)options.Version.Revision);
			Flags = options.Attributes;
			ProcessArchitecture.SelectedItem = (AsmProcArch)((uint)(options.Attributes & AssemblyAttributes.PA_Mask) >> (int)AssemblyAttributes.PA_Shift);
			ContentType.SelectedItem = (AsmContType)((uint)(options.Attributes & AssemblyAttributes.ContentType_Mask) >> 9);
			Name = options.Name;
			Culture = options.Culture;
			ClrVersion.SelectedItem = options.ClrVersion;
			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
			DeclSecuritiesVM.InitializeFrom(options.DeclSecurities);
		}

		AssemblyOptions CopyTo(AssemblyOptions options) {
			options.HashAlgorithm = (AssemblyHashAlgorithm)HashAlgorithm.SelectedItem!;
			options.Version = new Version(VersionMajor.Value, VersionMinor.Value, VersionBuild.Value, VersionRevision.Value);
			options.Attributes = Flags;
			options.PublicKey = new PublicKey(PublicKey.Value.ToArray());
			options.Name = Name;
			options.Culture = Culture;
			options.ClrVersion = (Module.ClrVersion)ClrVersion.SelectedItem!;
			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));
			options.DeclSecurities.Clear();
			options.DeclSecurities.AddRange(DeclSecuritiesVM.Collection.Select(a => a.CreateDeclSecurityOptions().Create(ownerModule)));
			return options;
		}

		void OnOpenPublicKeyFile() {
			if (openPublicKeyFile is null)
				throw new InvalidOperationException();
			var newPublicKey = openPublicKeyFile.Open();
			if (newPublicKey is null)
				return;
			PublicKey.Value = newPublicKey.Data;
		}

		public override bool HasError {
			get {
				return VersionMajor.HasError ||
					VersionMinor.HasError ||
					VersionBuild.HasError ||
					VersionRevision.HasError ||
					PublicKey.HasError;
			}
		}
	}
}
