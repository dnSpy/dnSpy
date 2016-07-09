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
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.PE;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.MVVM;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.Search;

namespace dnSpy.AsmEditor.Module {
	enum EntryPointType {
		None,
		Managed,
		Native,
	}

	sealed class ModuleOptionsVM : ViewModelBase {
		readonly ModuleOptions options;
		readonly ModuleOptions origOptions;

		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public ICommand PickManagedEntryPointCommand => new RelayCommand(a => PickManagedEntryPoint());
		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand GenerateNewMvidCommand => new RelayCommand(a => Mvid.Value = Guid.NewGuid());
		public ICommand GenerateNewEncIdCommand => new RelayCommand(a => EncId.Value = Guid.NewGuid());
		public ICommand GenerateNewEncBaseIdCommand => new RelayCommand(a => EncBaseId.Value = Guid.NewGuid());
		public NullableGuidVM Mvid { get; }
		public NullableGuidVM EncId { get; }
		public NullableGuidVM EncBaseId { get; }

		public string Name {
			get { return options.Name; }
			set {
				options.Name = value;
				OnPropertyChanged(nameof(Name));
			}
		}

		public EnumListVM ClrVersion { get; }
		public EnumListVM ModuleKind { get; }

		public Characteristics Characteristics {
			get { return options.Characteristics; }
			set {
				if (options.Characteristics != value) {
					options.Characteristics = value;
					OnPropertyChanged(nameof(Characteristics));
					OnPropertyChanged(nameof(RelocsStripped));
					OnPropertyChanged(nameof(ExecutableImage));
					OnPropertyChanged(nameof(LineNumsStripped));
					OnPropertyChanged(nameof(LocalSymsStripped));
					OnPropertyChanged(nameof(AggressiveWsTrim));
					OnPropertyChanged(nameof(LargeAddressAware));
					OnPropertyChanged(nameof(CharacteristicsReserved1));
					OnPropertyChanged(nameof(BytesReversedLo));
					OnPropertyChanged(nameof(Bit32Machine));
					OnPropertyChanged(nameof(DebugStripped));
					OnPropertyChanged(nameof(RemovableRunFromSwap));
					OnPropertyChanged(nameof(NetRunFromSwap));
					OnPropertyChanged(nameof(System));
					OnPropertyChanged(nameof(Dll));
					OnPropertyChanged(nameof(UpSystemOnly));
					OnPropertyChanged(nameof(BytesReversedHi));
				}
			}
		}

		public bool RelocsStripped {
			get { return GetFlagValue(dnlib.PE.Characteristics.RelocsStripped); }
			set { SetFlagValue(dnlib.PE.Characteristics.RelocsStripped, value); }
		}

		public bool ExecutableImage {
			get { return GetFlagValue(dnlib.PE.Characteristics.ExecutableImage); }
			set { SetFlagValue(dnlib.PE.Characteristics.ExecutableImage, value); }
		}

		public bool LineNumsStripped {
			get { return GetFlagValue(dnlib.PE.Characteristics.LineNumsStripped); }
			set { SetFlagValue(dnlib.PE.Characteristics.LineNumsStripped, value); }
		}

		public bool LocalSymsStripped {
			get { return GetFlagValue(dnlib.PE.Characteristics.LocalSymsStripped); }
			set { SetFlagValue(dnlib.PE.Characteristics.LocalSymsStripped, value); }
		}

		public bool AggressiveWsTrim {
			get { return GetFlagValue(dnlib.PE.Characteristics.AggressiveWsTrim); }
			set { SetFlagValue(dnlib.PE.Characteristics.AggressiveWsTrim, value); }
		}

		public bool LargeAddressAware {
			get { return GetFlagValue(dnlib.PE.Characteristics.LargeAddressAware); }
			set { SetFlagValue(dnlib.PE.Characteristics.LargeAddressAware, value); }
		}

		public bool CharacteristicsReserved1 {
			get { return GetFlagValue(dnlib.PE.Characteristics.Reserved1); }
			set { SetFlagValue(dnlib.PE.Characteristics.Reserved1, value); }
		}

		public bool BytesReversedLo {
			get { return GetFlagValue(dnlib.PE.Characteristics.BytesReversedLo); }
			set { SetFlagValue(dnlib.PE.Characteristics.BytesReversedLo, value); }
		}

		public bool Bit32Machine {
			get { return GetFlagValue(dnlib.PE.Characteristics._32BitMachine); }
			set { SetFlagValue(dnlib.PE.Characteristics._32BitMachine, value); }
		}

		public bool DebugStripped {
			get { return GetFlagValue(dnlib.PE.Characteristics.DebugStripped); }
			set { SetFlagValue(dnlib.PE.Characteristics.DebugStripped, value); }
		}

		public bool RemovableRunFromSwap {
			get { return GetFlagValue(dnlib.PE.Characteristics.RemovableRunFromSwap); }
			set { SetFlagValue(dnlib.PE.Characteristics.RemovableRunFromSwap, value); }
		}

		public bool NetRunFromSwap {
			get { return GetFlagValue(dnlib.PE.Characteristics.NetRunFromSwap); }
			set { SetFlagValue(dnlib.PE.Characteristics.NetRunFromSwap, value); }
		}

		public bool System {
			get { return GetFlagValue(dnlib.PE.Characteristics.System); }
			set { SetFlagValue(dnlib.PE.Characteristics.System, value); }
		}

		public bool Dll {
			get { return GetFlagValue(dnlib.PE.Characteristics.Dll); }
			set { SetFlagValue(dnlib.PE.Characteristics.Dll, value); }
		}

		public bool UpSystemOnly {
			get { return GetFlagValue(dnlib.PE.Characteristics.UpSystemOnly); }
			set { SetFlagValue(dnlib.PE.Characteristics.UpSystemOnly, value); }
		}

		public bool BytesReversedHi {
			get { return GetFlagValue(dnlib.PE.Characteristics.BytesReversedHi); }
			set { SetFlagValue(dnlib.PE.Characteristics.BytesReversedHi, value); }
		}

		bool GetFlagValue(Characteristics flag) => (Characteristics & flag) != 0;

		void SetFlagValue(Characteristics flag, bool value) {
			if (value)
				Characteristics |= flag;
			else
				Characteristics &= ~flag;
		}

		public DllCharacteristics DllCharacteristics {
			get { return options.DllCharacteristics; }
			set {
				if (options.DllCharacteristics != value) {
					options.DllCharacteristics = value;
					OnPropertyChanged(nameof(DllCharacteristics));
					OnPropertyChanged(nameof(DllCharacteristicsReserved1));
					OnPropertyChanged(nameof(DllCharacteristicsReserved2));
					OnPropertyChanged(nameof(DllCharacteristicsReserved3));
					OnPropertyChanged(nameof(DllCharacteristicsReserved4));
					OnPropertyChanged(nameof(DllCharacteristicsReserved5));
					OnPropertyChanged(nameof(HighEntropyVA));
					OnPropertyChanged(nameof(DynamicBase));
					OnPropertyChanged(nameof(ForceIntegrity));
					OnPropertyChanged(nameof(NxCompat));
					OnPropertyChanged(nameof(NoIsolation));
					OnPropertyChanged(nameof(NoSeh));
					OnPropertyChanged(nameof(NoBind));
					OnPropertyChanged(nameof(AppContainer));
					OnPropertyChanged(nameof(WdmDriver));
					OnPropertyChanged(nameof(GuardCf));
					OnPropertyChanged(nameof(TerminalServerAware));
				}
			}
		}

		public bool DllCharacteristicsReserved1 {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.Reserved1); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.Reserved1, value); }
		}

		public bool DllCharacteristicsReserved2 {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.Reserved2); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.Reserved2, value); }
		}

		public bool DllCharacteristicsReserved3 {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.Reserved3); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.Reserved3, value); }
		}

		public bool DllCharacteristicsReserved4 {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.Reserved4); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.Reserved4, value); }
		}

		public bool DllCharacteristicsReserved5 {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.Reserved5); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.Reserved5, value); }
		}

		public bool HighEntropyVA {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.HighEntropyVA); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.HighEntropyVA, value); }
		}

		public bool DynamicBase {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.DynamicBase); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.DynamicBase, value); }
		}

		public bool ForceIntegrity {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.ForceIntegrity); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.ForceIntegrity, value); }
		}

		public bool NxCompat {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.NxCompat); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.NxCompat, value); }
		}

		public bool NoIsolation {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.NoIsolation); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.NoIsolation, value); }
		}

		public bool NoSeh {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.NoSeh); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.NoSeh, value); }
		}

		public bool NoBind {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.NoBind); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.NoBind, value); }
		}

		public bool AppContainer {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.AppContainer); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.AppContainer, value); }
		}

		public bool WdmDriver {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.WdmDriver); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.WdmDriver, value); }
		}

		public bool GuardCf {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.GuardCf); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.GuardCf, value); }
		}

		public bool TerminalServerAware {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.TerminalServerAware); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.TerminalServerAware, value); }
		}

		bool GetFlagValue(DllCharacteristics flag) => (DllCharacteristics & flag) != 0;

		void SetFlagValue(DllCharacteristics flag, bool value) {
			if (value)
				DllCharacteristics |= flag;
			else
				DllCharacteristics &= ~flag;
		}

		public string RuntimeVersion {
			get { return options.RuntimeVersion; }
			set {
				options.RuntimeVersion = value;
				OnPropertyChanged(nameof(RuntimeVersion));
				UpdateClrVersion();
				HasErrorUpdated();
			}
		}

		public EnumListVM Machine { get; }

		public ComImageFlags Cor20HeaderFlags {
			get { return options.Cor20HeaderFlags; }
			set {
				if (options.Cor20HeaderFlags != value) {
					options.Cor20HeaderFlags = value;
					OnPropertyChanged(nameof(Cor20HeaderFlags));
					OnPropertyChanged(nameof(ILOnly));
					OnPropertyChanged(nameof(Bit32Required));
					OnPropertyChanged(nameof(ILLibrary));
					OnPropertyChanged(nameof(StrongNameSigned));
					OnPropertyChanged(nameof(TrackDebugData));
					OnPropertyChanged(nameof(Bit32Preferred));
				}
			}
		}

		public bool ILOnly {
			get { return GetFlagValue(ComImageFlags.ILOnly); }
			set { SetFlagValue(ComImageFlags.ILOnly, value); }
		}

		public bool Bit32Required {
			get { return GetFlagValue(ComImageFlags._32BitRequired); }
			set { SetFlagValue(ComImageFlags._32BitRequired, value); }
		}

		public bool ILLibrary {
			get { return GetFlagValue(ComImageFlags.ILLibrary); }
			set { SetFlagValue(ComImageFlags.ILLibrary, value); }
		}

		public bool StrongNameSigned {
			get { return GetFlagValue(ComImageFlags.StrongNameSigned); }
			set { SetFlagValue(ComImageFlags.StrongNameSigned, value); }
		}

		public bool TrackDebugData {
			get { return GetFlagValue(ComImageFlags.TrackDebugData); }
			set { SetFlagValue(ComImageFlags.TrackDebugData, value); }
		}

		public bool Bit32Preferred {
			get { return GetFlagValue(ComImageFlags._32BitPreferred); }
			set { SetFlagValue(ComImageFlags._32BitPreferred, value); }
		}

		bool GetFlagValue(ComImageFlags flag) => (Cor20HeaderFlags & flag) != 0;

		void SetFlagValue(ComImageFlags flag, bool value) {
			if (value)
				Cor20HeaderFlags |= flag;
			else
				Cor20HeaderFlags &= ~flag;
		}

		public NullableUInt32VM Cor20HeaderRuntimeVersion { get; }
		public NullableUInt16VM TablesHeaderVersion { get; }

		public EntryPointType EntryPointEnum {
			get { return entryPointEnum; }
			set {
				if (entryPointEnum != value) {
					entryPointEnum = value;
					OnPropertyChanged(nameof(EntryPointEnum));
					if (entryPointEnum == EntryPointType.Native)
						Cor20HeaderFlags |= ComImageFlags.NativeEntryPoint;
					else
						Cor20HeaderFlags &= ~ComImageFlags.NativeEntryPoint;
				}
			}
		}
		EntryPointType entryPointEnum;

		public IManagedEntryPoint ManagedEntryPoint {
			get { return managedEntryPoint; }
			set {
				if (managedEntryPoint != value) {
					managedEntryPoint = value;
					OnPropertyChanged(nameof(ManagedEntryPoint));
					OnPropertyChanged(nameof(EntryPointName));
					OnPropertyChanged(nameof(EntryPointNameToolTip));
				}
			}
		}
		IManagedEntryPoint managedEntryPoint;

		public string EntryPointName => GetEntryPointString(80);
		public string EntryPointNameToolTip => ManagedEntryPoint == null ? null : GetEntryPointString(500);

		string GetEntryPointString(int maxChars) {
			var ep = ManagedEntryPoint;
			if (ep == null)
				return string.Empty;
			string s;
			var method = ep as MethodDef;
			if (method != null) {
				var declType = method.DeclaringType;
				if (declType != null)
					s = string.Format("{0} ({1})", method.Name, declType.FullName);
				else
					s = method.Name;
			}
			else {
				//TODO: Support EP in other module
				s = string.Empty;
			}
			if (s.Length > maxChars)
				s = s.Substring(0, maxChars) + "...";
			return s;
		}

		public UInt32VM NativeEntryPointRva { get; }
		public CustomAttributesVM CustomAttributesVM { get; }

		readonly ModuleDef module;

		public ModuleOptionsVM(ModuleDef module, ModuleOptions options, ILanguageManager languageManager) {
			this.module = module;
			this.options = new ModuleOptions();
			this.origOptions = options;
			ModuleKind = new EnumListVM(SaveModule.SaveModuleOptionsVM.moduleKindList, (a, b) => {
				Characteristics = SaveModule.CharacteristicsHelper.GetCharacteristics(Characteristics, (dnlib.DotNet.ModuleKind)ModuleKind.SelectedItem);
			});
			this.Machine = new EnumListVM(SaveModule.PEHeadersOptionsVM.machineList, (a, b) => {
				Characteristics = SaveModule.CharacteristicsHelper.GetCharacteristics(Characteristics, (dnlib.PE.Machine)Machine.SelectedItem);
			});
			Mvid = new NullableGuidVM(a => HasErrorUpdated());
			EncId = new NullableGuidVM(a => HasErrorUpdated());
			EncBaseId = new NullableGuidVM(a => HasErrorUpdated());
			ClrVersion = new EnumListVM(NetModuleOptionsVM.clrVersionList, (a, b) => OnClrVersionChanged());
			ClrVersion.Items.Add(new EnumVM(Module.ClrVersion.Unknown, dnSpy_AsmEditor_Resources.Unknown));
			ClrVersion.SelectedItem = Module.ClrVersion.Unknown;
			Cor20HeaderRuntimeVersion = new NullableUInt32VM(a => { HasErrorUpdated(); UpdateClrVersion(); });
			TablesHeaderVersion = new NullableUInt16VM(a => { HasErrorUpdated(); UpdateClrVersion(); });
			NativeEntryPointRva = new UInt32VM(a => HasErrorUpdated());
			CustomAttributesVM = new CustomAttributesVM(module, languageManager);
			Reinitialize();
		}

		void OnClrVersionChanged() {
			var clrVersion = (Module.ClrVersion)ClrVersion.SelectedItem;
			var clrValues = ClrVersionValues.GetValues(clrVersion);
			if (clrValues == null)
				return;

			if (Cor20HeaderRuntimeVersion != null)
				Cor20HeaderRuntimeVersion.Value = clrValues.Cor20HeaderRuntimeVersion;
			if (TablesHeaderVersion != null)
				TablesHeaderVersion.Value = clrValues.TablesHeaderVersion;
			RuntimeVersion = clrValues.RuntimeVersion;
		}

		void UpdateClrVersion() {
			ClrVersion clrVersion = Module.ClrVersion.Unknown;
			if (Cor20HeaderRuntimeVersion != null && !Cor20HeaderRuntimeVersion.HasError && Cor20HeaderRuntimeVersion.Value != null &&
				TablesHeaderVersion != null && !TablesHeaderVersion.HasError && TablesHeaderVersion.Value != null) {
				var clrValues = ClrVersionValues.Find(Cor20HeaderRuntimeVersion.Value.Value, TablesHeaderVersion.Value.Value, RuntimeVersion);
				if (clrValues != null)
					clrVersion = clrValues.ClrVersion;
			}
			if (ClrVersion != null)
				ClrVersion.SelectedItem = clrVersion;
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public ModuleOptions CreateModuleOptions() => CopyTo(new ModuleOptions());

		void InitializeFrom(ModuleOptions options) {
			Mvid.Value = options.Mvid;
			EncId.Value = options.EncId;
			EncBaseId.Value = options.EncBaseId;
			Name = options.Name;
			ModuleKind.SelectedItem = options.Kind;
			DllCharacteristics = options.DllCharacteristics;
			RuntimeVersion = options.RuntimeVersion;
			Machine.SelectedItem = options.Machine;
			Cor20HeaderFlags = options.Cor20HeaderFlags;
			Cor20HeaderRuntimeVersion.Value = options.Cor20HeaderRuntimeVersion;
			TablesHeaderVersion.Value = options.TablesHeaderVersion;

			ManagedEntryPoint = options.ManagedEntryPoint;
			NativeEntryPointRva.Value = (uint)options.NativeEntryPoint;
			if (options.ManagedEntryPoint != null)
				EntryPointEnum = EntryPointType.Managed;
			else if (options.NativeEntryPoint != 0)
				EntryPointEnum = EntryPointType.Native;
			else
				EntryPointEnum = EntryPointType.None;

			// Writing to Machine and ModuleKind triggers code that updates Characteristics so write
			// this property last.
			Characteristics = options.Characteristics;

			CustomAttributesVM.InitializeFrom(options.CustomAttributes);
		}

		ModuleOptions CopyTo(ModuleOptions options) {
			options.Mvid = Mvid.Value;
			options.EncId = EncId.Value;
			options.EncBaseId = EncBaseId.Value;
			options.Name = Name;
			options.Kind = (ModuleKind)ModuleKind.SelectedItem;
			options.Characteristics = Characteristics;
			options.DllCharacteristics = DllCharacteristics;
			options.RuntimeVersion = RuntimeVersion;
			options.Machine = (dnlib.PE.Machine)Machine.SelectedItem;
			options.Cor20HeaderFlags = Cor20HeaderFlags;
			options.Cor20HeaderRuntimeVersion = Cor20HeaderRuntimeVersion.Value;
			options.TablesHeaderVersion = TablesHeaderVersion.Value;

			if (EntryPointEnum == EntryPointType.None) {
				options.ManagedEntryPoint = null;
				options.NativeEntryPoint = 0;
			}
			else if (EntryPointEnum == EntryPointType.Managed) {
				options.ManagedEntryPoint = ManagedEntryPoint;
				options.NativeEntryPoint = 0;
			}
			else if (EntryPointEnum == EntryPointType.Native) {
				options.ManagedEntryPoint = null;
				options.NativeEntryPoint = (RVA)NativeEntryPointRva.Value;
			}
			else
				throw new InvalidOperationException();

			options.CustomAttributes.Clear();
			options.CustomAttributes.AddRange(CustomAttributesVM.Collection.Select(a => a.CreateCustomAttributeOptions().Create()));

			return options;
		}

		void PickManagedEntryPoint() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var ep = dnlibTypePicker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_EntryPoint, new EntryPointFileTreeNodeFilter(module), ManagedEntryPoint, module);
			if (ep != null) {
				ManagedEntryPoint = ep;
				EntryPointEnum = EntryPointType.Managed;
			}
		}

		protected override string Verify(string columnName) {
			if (columnName == nameof(RuntimeVersion))
				return SaveModule.MetaDataHeaderOptionsVM.ValidateVersionString(options.RuntimeVersion);

			return string.Empty;
		}

		public override bool HasError {
			get {
				if (!string.IsNullOrEmpty(Verify(nameof(RuntimeVersion))))
					return true;
				return Mvid.HasError ||
					EncId.HasError ||
					EncBaseId.HasError ||
					Cor20HeaderRuntimeVersion.HasError ||
					TablesHeaderVersion.HasError ||
					NativeEntryPointRva.HasError;
			}
		}
	}
}
