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

using System.Text;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.PE;
using dnlib.W32Resources;
using dnSpy.Files;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.AsmEditor.SaveModule {
	sealed class SaveModuleOptionsVM : SaveOptionsVM {
		public override SaveOptionsType Type {
			get { return SaveOptionsType.Module; }
		}

		public ModuleDef Module {
			get { return module; }
		}
		readonly ModuleDef module;

		public override IUndoObject UndoObject {
			get { return UndoCommandManager.Instance.GetUndoObject(dnSpyFile); }
		}
		readonly DnSpyFile dnSpyFile;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public bool CanSaveMixedModeModule {
			get { return module is ModuleDefMD; }
		}

		public bool IsMixedModeModule {
			get { return CanSaveMixedModeModule && !module.IsILOnly; }
		}

		public bool UseMixedMode {
			get { return useMixedMode; }
			set {
				if (useMixedMode != value) {
					useMixedMode = value;
					OnPropertyChanged("UseMixedMode");
					ReinitializeModuleWriterOptions();
				}
			}
		}
		bool useMixedMode;

		public bool CanWritePdb {
			get { return module.PdbState != null; }
		}

		public bool WritePdb {
			get { return writePdb; }
			set {
				if (writePdb != value) {
					writePdb = value;
					OnPropertyChanged("WritePdb");
				}
			}
		}
		bool writePdb;

		public bool ShareMethodBodies {
			get { return shareMethodBodies; }
			set {
				if (value != shareMethodBodies) {
					shareMethodBodies = value;
					OnPropertyChanged("ShareMethodBodies");
				}
			}
		}
		bool shareMethodBodies;

		public bool AddCheckSum {
			get { return addCheckSum; }
			set {
				if (value != addCheckSum) {
					addCheckSum = value;
					OnPropertyChanged("AddCheckSum");
				}
			}
		}
		bool addCheckSum;

		public Win32Resources Win32Resources {
			get { return win32Resources; }
			set {
				if (win32Resources != value) {
					win32Resources = value;
					OnPropertyChanged("Win32Resources");
				}
			}
		}
		Win32Resources win32Resources;

		public bool KeepExtraPEData {
			get { return keepExtraPEData; }
			set {
				if (keepExtraPEData != value) {
					keepExtraPEData = value;
					OnPropertyChanged("KeepExtraPEData");
				}
			}
		}
		bool keepExtraPEData;

		public bool KeepWin32Resources {
			get { return keepWin32Resources; }
			set {
				if (keepWin32Resources != value) {
					keepWin32Resources = value;
					OnPropertyChanged("KeepWin32Resources");
				}
			}
		}
		bool keepWin32Resources;

		internal static readonly EnumVM[] moduleKindList = EnumVM.Create(typeof(dnlib.DotNet.ModuleKind));

		public EnumListVM ModuleKind {
			get { return moduleKindVM; }
		}
		readonly EnumListVM moduleKindVM;

		public string Extension {
			get {
				switch ((dnlib.DotNet.ModuleKind)moduleKindVM.SelectedItem) {
				case dnlib.DotNet.ModuleKind.Console:
				case dnlib.DotNet.ModuleKind.Windows:
				default:
					return "exe";
				case dnlib.DotNet.ModuleKind.Dll:
					return "dll";
				case dnlib.DotNet.ModuleKind.NetModule:
					return "netmodule";
				}
			}
		}

		public PEHeadersOptionsVM PEHeadersOptions {
			get { return peHeadersOptions; }
		}
		readonly PEHeadersOptionsVM peHeadersOptions;

		public Cor20HeaderOptionsVM Cor20HeaderOptions {
			get { return cor20HeaderOptions; }
		}
		readonly Cor20HeaderOptionsVM cor20HeaderOptions;

		public MetaDataOptionsVM MetaDataOptions {
			get { return metaDataOptions; }
		}
		readonly MetaDataOptionsVM metaDataOptions;

		public SaveModuleOptionsVM(DnSpyFile dnSpyFile) {
			this.dnSpyFile = dnSpyFile;
			this.module = dnSpyFile.ModuleDef;
			this.peHeadersOptions = new PEHeadersOptionsVM(module.Machine, GetSubsystem(module.Kind));
			this.cor20HeaderOptions = new Cor20HeaderOptionsVM();
			this.metaDataOptions = new MetaDataOptionsVM();

			this.peHeadersOptions.PropertyChanged += (s, e) => HasErrorUpdated();
			this.cor20HeaderOptions.PropertyChanged += (s, e) => HasErrorUpdated();
			this.metaDataOptions.PropertyChanged += (s, e) => HasErrorUpdated();

			moduleKindVM = new EnumListVM(moduleKindList, (a, b) => {
				OnPropertyChanged("Extension");
				PEHeadersOptions.Subsystem.SelectedItem = GetSubsystem((dnlib.DotNet.ModuleKind)ModuleKind.SelectedItem);
				PEHeadersOptions.Characteristics = CharacteristicsHelper.GetCharacteristics(PEHeadersOptions.Characteristics ?? 0, (dnlib.DotNet.ModuleKind)ModuleKind.SelectedItem);
			});

			Reinitialize();
		}

		static Subsystem GetSubsystem(ModuleKind moduleKind) {
			if (moduleKind == dnlib.DotNet.ModuleKind.Windows)
				return Subsystem.WindowsGui;
			return Subsystem.WindowsCui;
		}

		void Reinitialize() {
			FileName = module.Location;
			if (UseMixedMode == IsMixedModeModule)
				ReinitializeModuleWriterOptions();
			else
				UseMixedMode = IsMixedModeModule;
		}

		void ReinitializeModuleWriterOptions() {
			if (UseMixedMode)
				InitializeFrom(new NativeModuleWriterOptions((ModuleDefMD)module));
			else
				InitializeFrom(new ModuleWriterOptions(module));
			WritePdb = CanWritePdb;
		}

		public ModuleWriterOptionsBase CreateWriterOptions() {
			if (UseMixedMode) {
				var options = new NativeModuleWriterOptions((ModuleDefMD)module);
				CopyTo(options);
				options.KeepExtraPEData = KeepExtraPEData;
				options.KeepWin32Resources = KeepWin32Resources;
				return options;
			}
			else {
				var options = new ModuleWriterOptions();
				CopyTo(options);
				if (module.ManagedEntryPoint != null || module.NativeEntryPoint == 0)
					options.Cor20HeaderOptions.Flags &= ~ComImageFlags.NativeEntryPoint;
				return options;
			}
		}

		void CopyTo(ModuleWriterOptionsBase options) {
			peHeadersOptions.CopyTo(options.PEHeadersOptions);
			cor20HeaderOptions.CopyTo(options.Cor20HeaderOptions);
			metaDataOptions.CopyTo(options.MetaDataOptions);

			options.WritePdb = WritePdb;
			options.ShareMethodBodies = ShareMethodBodies;
			options.AddCheckSum = AddCheckSum;
			options.Win32Resources = Win32Resources;
			options.ModuleKind = (dnlib.DotNet.ModuleKind)moduleKindVM.SelectedItem;
		}

		public SaveModuleOptionsVM Clone() {
			return CopyTo(new SaveModuleOptionsVM(dnSpyFile));
		}

		public SaveModuleOptionsVM CopyTo(SaveModuleOptionsVM other) {
			other.FileName = FileName;
			other.UseMixedMode = UseMixedMode;
			other.InitializeFrom(CreateWriterOptions());
			return other;
		}

		public void InitializeFrom(ModuleWriterOptionsBase options) {
			if (options is ModuleWriterOptions)
				InitializeFrom((ModuleWriterOptions)options);
			else
				InitializeFrom((NativeModuleWriterOptions)options);
		}

		public void InitializeFrom(ModuleWriterOptions options) {
			InitializeFromInternal((ModuleWriterOptionsBase)options);
			KeepExtraPEData = false;
			KeepWin32Resources = false;
		}

		public void InitializeFrom(NativeModuleWriterOptions options) {
			InitializeFromInternal((ModuleWriterOptionsBase)options);
			KeepExtraPEData = options.KeepExtraPEData;
			KeepWin32Resources = options.KeepWin32Resources;
		}

		void InitializeFromInternal(ModuleWriterOptionsBase options) {
			// Writing to it triggers a write to Subsystem so write it first
			moduleKindVM.SelectedItem = options.ModuleKind;

			peHeadersOptions.InitializeFrom(options.PEHeadersOptions);
			cor20HeaderOptions.InitializeFrom(options.Cor20HeaderOptions);
			metaDataOptions.InitializeFrom(options.MetaDataOptions);

			WritePdb = options.WritePdb;
			ShareMethodBodies = options.ShareMethodBodies;
			AddCheckSum = options.AddCheckSum;
			Win32Resources = options.Win32Resources;

			// Writing to Machine and ModuleKind triggers code that updates Characteristics
			peHeadersOptions.Characteristics = options.PEHeadersOptions.Characteristics;
		}

		protected override string GetExtension(string filename) {
			return Extension;
		}

		public override bool HasError {
			get {
				return base.HasError ||
						peHeadersOptions.HasError ||
						cor20HeaderOptions.HasError ||
						metaDataOptions.HasError;
			}
		}
	}

	sealed class PEHeadersOptionsVM : ViewModelBase {
		readonly Machine defaultMachine;
		readonly Subsystem defaultSubsystem;

		public PEHeadersOptionsVM(Machine defaultMachine, Subsystem defaultSubsystem) {
			this.defaultMachine = defaultMachine;
			this.defaultSubsystem = defaultSubsystem;
			this.machineVM = new EnumListVM(machineList, (a, b) => {
				Characteristics = CharacteristicsHelper.GetCharacteristics(Characteristics ?? 0, (dnlib.PE.Machine)Machine.SelectedItem);
			});
			this.timeDateStamp = new NullableUInt32VM(a => HasErrorUpdated());
			this.pointerToSymbolTable = new NullableUInt32VM(a => HasErrorUpdated());
			this.numberOfSymbols = new NullableUInt32VM(a => HasErrorUpdated());
			this.majorLinkerVersion = new NullableByteVM(a => HasErrorUpdated());
			this.minorLinkerVersion = new NullableByteVM(a => HasErrorUpdated());
			this.imageBase = new NullableUInt64VM(a => HasErrorUpdated());
			this.sectionAlignment = new NullableUInt32VM(a => HasErrorUpdated());
			this.fileAlignment = new NullableUInt32VM(a => HasErrorUpdated());
			this.majorOperatingSystemVersion = new NullableUInt16VM(a => HasErrorUpdated());
			this.minorOperatingSystemVersion = new NullableUInt16VM(a => HasErrorUpdated());
			this.majorImageVersion = new NullableUInt16VM(a => HasErrorUpdated());
			this.minorImageVersion = new NullableUInt16VM(a => HasErrorUpdated());
			this.majorSubsystemVersion = new NullableUInt16VM(a => HasErrorUpdated());
			this.minorSubsystemVersion = new NullableUInt16VM(a => HasErrorUpdated());
			this.win32VersionValue = new NullableUInt32VM(a => HasErrorUpdated());
			this.sizeOfStackReserve = new NullableUInt64VM(a => HasErrorUpdated());
			this.sizeOfStackCommit = new NullableUInt64VM(a => HasErrorUpdated());
			this.sizeOfHeapReserve = new NullableUInt64VM(a => HasErrorUpdated());
			this.sizeOfHeapCommit = new NullableUInt64VM(a => HasErrorUpdated());
			this.loaderFlags = new NullableUInt32VM(a => HasErrorUpdated());
			this.numberOfRvaAndSizes = new NullableUInt32VM(a => HasErrorUpdated());
		}

		public EnumListVM Machine {
			get { return machineVM; }
		}
		internal static readonly EnumVM[] machineList = EnumVM.Create(typeof(dnlib.PE.Machine), dnlib.PE.Machine.I386, dnlib.PE.Machine.AMD64, dnlib.PE.Machine.IA64, dnlib.PE.Machine.ARMNT, dnlib.PE.Machine.ARM64);
		readonly EnumListVM machineVM;

		public NullableUInt32VM TimeDateStamp {
			get { return timeDateStamp; }
		}
		NullableUInt32VM timeDateStamp;

		public NullableUInt32VM PointerToSymbolTable {
			get { return pointerToSymbolTable; }
		}
		NullableUInt32VM pointerToSymbolTable;

		public NullableUInt32VM NumberOfSymbols {
			get { return numberOfSymbols; }
		}
		NullableUInt32VM numberOfSymbols;

		public Characteristics? Characteristics {
			get { return characteristics; }
			set {
				if (characteristics != value) {
					characteristics = value;
					OnPropertyChanged("Characteristics");
					OnPropertyChanged("RelocsStripped");
					OnPropertyChanged("ExecutableImage");
					OnPropertyChanged("LineNumsStripped");
					OnPropertyChanged("LocalSymsStripped");
					OnPropertyChanged("AggressiveWsTrim");
					OnPropertyChanged("LargeAddressAware");
					OnPropertyChanged("CharacteristicsReserved1");
					OnPropertyChanged("BytesReversedLo");
					OnPropertyChanged("Bit32Machine");
					OnPropertyChanged("DebugStripped");
					OnPropertyChanged("RemovableRunFromSwap");
					OnPropertyChanged("NetRunFromSwap");
					OnPropertyChanged("System");
					OnPropertyChanged("Dll");
					OnPropertyChanged("UpSystemOnly");
					OnPropertyChanged("BytesReversedHi");
				}
			}
		}
		Characteristics? characteristics;

		public bool? RelocsStripped {
			get { return GetFlagValue(dnlib.PE.Characteristics.RelocsStripped); }
			set { SetFlagValue(dnlib.PE.Characteristics.RelocsStripped, value); }
		}

		public bool? ExecutableImage {
			get { return GetFlagValue(dnlib.PE.Characteristics.ExecutableImage); }
			set { SetFlagValue(dnlib.PE.Characteristics.ExecutableImage, value); }
		}

		public bool? LineNumsStripped {
			get { return GetFlagValue(dnlib.PE.Characteristics.LineNumsStripped); }
			set { SetFlagValue(dnlib.PE.Characteristics.LineNumsStripped, value); }
		}

		public bool? LocalSymsStripped {
			get { return GetFlagValue(dnlib.PE.Characteristics.LocalSymsStripped); }
			set { SetFlagValue(dnlib.PE.Characteristics.LocalSymsStripped, value); }
		}

		public bool? AggressiveWsTrim {
			get { return GetFlagValue(dnlib.PE.Characteristics.AggressiveWsTrim); }
			set { SetFlagValue(dnlib.PE.Characteristics.AggressiveWsTrim, value); }
		}

		public bool? LargeAddressAware {
			get { return GetFlagValue(dnlib.PE.Characteristics.LargeAddressAware); }
			set { SetFlagValue(dnlib.PE.Characteristics.LargeAddressAware, value); }
		}

		public bool? CharacteristicsReserved1 {
			get { return GetFlagValue(dnlib.PE.Characteristics.Reserved1); }
			set { SetFlagValue(dnlib.PE.Characteristics.Reserved1, value); }
		}

		public bool? BytesReversedLo {
			get { return GetFlagValue(dnlib.PE.Characteristics.BytesReversedLo); }
			set { SetFlagValue(dnlib.PE.Characteristics.BytesReversedLo, value); }
		}

		public bool? Bit32Machine {
			get { return GetFlagValue(dnlib.PE.Characteristics._32BitMachine); }
			set { SetFlagValue(dnlib.PE.Characteristics._32BitMachine, value); }
		}

		public bool? DebugStripped {
			get { return GetFlagValue(dnlib.PE.Characteristics.DebugStripped); }
			set { SetFlagValue(dnlib.PE.Characteristics.DebugStripped, value); }
		}

		public bool? RemovableRunFromSwap {
			get { return GetFlagValue(dnlib.PE.Characteristics.RemovableRunFromSwap); }
			set { SetFlagValue(dnlib.PE.Characteristics.RemovableRunFromSwap, value); }
		}

		public bool? NetRunFromSwap {
			get { return GetFlagValue(dnlib.PE.Characteristics.NetRunFromSwap); }
			set { SetFlagValue(dnlib.PE.Characteristics.NetRunFromSwap, value); }
		}

		public bool? System {
			get { return GetFlagValue(dnlib.PE.Characteristics.System); }
			set { SetFlagValue(dnlib.PE.Characteristics.System, value); }
		}

		public bool? Dll {
			get { return GetFlagValue(dnlib.PE.Characteristics.Dll); }
			set { SetFlagValue(dnlib.PE.Characteristics.Dll, value); }
		}

		public bool? UpSystemOnly {
			get { return GetFlagValue(dnlib.PE.Characteristics.UpSystemOnly); }
			set { SetFlagValue(dnlib.PE.Characteristics.UpSystemOnly, value); }
		}

		public bool? BytesReversedHi {
			get { return GetFlagValue(dnlib.PE.Characteristics.BytesReversedHi); }
			set { SetFlagValue(dnlib.PE.Characteristics.BytesReversedHi, value); }
		}

		bool? GetFlagValue(Characteristics flag) {
			return Characteristics == null ? (bool?)null : (Characteristics.Value & flag) != 0;
		}

		void SetFlagValue(Characteristics flag, bool? value) {
			if (Characteristics == null)
				Characteristics = 0;
			if (value ?? false)
				Characteristics |= flag;
			else
				Characteristics &= ~flag;
		}

		public NullableByteVM MajorLinkerVersion {
			get { return majorLinkerVersion; }
		}
		NullableByteVM majorLinkerVersion;

		public NullableByteVM MinorLinkerVersion {
			get { return minorLinkerVersion; }
		}
		NullableByteVM minorLinkerVersion;

		public NullableUInt64VM ImageBase {
			get { return imageBase; }
		}
		NullableUInt64VM imageBase;

		public NullableUInt32VM SectionAlignment {
			get { return sectionAlignment; }
		}
		NullableUInt32VM sectionAlignment;

		public NullableUInt32VM FileAlignment {
			get { return fileAlignment; }
		}
		NullableUInt32VM fileAlignment;

		public NullableUInt16VM MajorOperatingSystemVersion {
			get { return majorOperatingSystemVersion; }
		}
		NullableUInt16VM majorOperatingSystemVersion;

		public NullableUInt16VM MinorOperatingSystemVersion {
			get { return minorOperatingSystemVersion; }
		}
		NullableUInt16VM minorOperatingSystemVersion;

		public NullableUInt16VM MajorImageVersion {
			get { return majorImageVersion; }
		}
		NullableUInt16VM majorImageVersion;

		public NullableUInt16VM MinorImageVersion {
			get { return minorImageVersion; }
		}
		NullableUInt16VM minorImageVersion;

		public NullableUInt16VM MajorSubsystemVersion {
			get { return majorSubsystemVersion; }
		}
		NullableUInt16VM majorSubsystemVersion;

		public NullableUInt16VM MinorSubsystemVersion {
			get { return minorSubsystemVersion; }
		}
		NullableUInt16VM minorSubsystemVersion;

		public NullableUInt32VM Win32VersionValue {
			get { return win32VersionValue; }
		}
		NullableUInt32VM win32VersionValue;

		public EnumListVM Subsystem {
			get { return subsystemVM; }
		}
		static readonly EnumVM[] subsystemList = EnumVM.Create(typeof(dnlib.PE.Subsystem), dnlib.PE.Subsystem.WindowsGui, dnlib.PE.Subsystem.WindowsCui);
		readonly EnumListVM subsystemVM = new EnumListVM(subsystemList);

		public DllCharacteristics? DllCharacteristics {
			get { return dllCharacteristics; }
			set {
				if (dllCharacteristics != value) {
					dllCharacteristics = value;
					OnPropertyChanged("DllCharacteristics");
					OnPropertyChanged("DllCharacteristicsReserved1");
					OnPropertyChanged("DllCharacteristicsReserved2");
					OnPropertyChanged("DllCharacteristicsReserved3");
					OnPropertyChanged("DllCharacteristicsReserved4");
					OnPropertyChanged("DllCharacteristicsReserved5");
					OnPropertyChanged("HighEntropyVA");
					OnPropertyChanged("DynamicBase");
					OnPropertyChanged("ForceIntegrity");
					OnPropertyChanged("NxCompat");
					OnPropertyChanged("NoIsolation");
					OnPropertyChanged("NoSeh");
					OnPropertyChanged("NoBind");
					OnPropertyChanged("AppContainer");
					OnPropertyChanged("WdmDriver");
					OnPropertyChanged("GuardCf");
					OnPropertyChanged("TerminalServerAware");
				}
			}
		}
		DllCharacteristics? dllCharacteristics;

		public bool? DllCharacteristicsReserved1 {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.Reserved1); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.Reserved1, value); }
		}

		public bool? DllCharacteristicsReserved2 {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.Reserved2); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.Reserved2, value); }
		}

		public bool? DllCharacteristicsReserved3 {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.Reserved3); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.Reserved3, value); }
		}

		public bool? DllCharacteristicsReserved4 {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.Reserved4); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.Reserved4, value); }
		}

		public bool? DllCharacteristicsReserved5 {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.Reserved5); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.Reserved5, value); }
		}

		public bool? HighEntropyVA {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.HighEntropyVA); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.HighEntropyVA, value); }
		}

		public bool? DynamicBase {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.DynamicBase); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.DynamicBase, value); }
		}

		public bool? ForceIntegrity {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.ForceIntegrity); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.ForceIntegrity, value); }
		}

		public bool? NxCompat {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.NxCompat); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.NxCompat, value); }
		}

		public bool? NoIsolation {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.NoIsolation); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.NoIsolation, value); }
		}

		public bool? NoSeh {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.NoSeh); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.NoSeh, value); }
		}

		public bool? NoBind {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.NoBind); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.NoBind, value); }
		}

		public bool? AppContainer {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.AppContainer); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.AppContainer, value); }
		}

		public bool? WdmDriver {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.WdmDriver); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.WdmDriver, value); }
		}

		public bool? GuardCf {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.GuardCf); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.GuardCf, value); }
		}

		public bool? TerminalServerAware {
			get { return GetFlagValue(dnlib.PE.DllCharacteristics.TerminalServerAware); }
			set { SetFlagValue(dnlib.PE.DllCharacteristics.TerminalServerAware, value); }
		}

		bool? GetFlagValue(DllCharacteristics flag) {
			return DllCharacteristics == null ? (bool?)null : (DllCharacteristics.Value & flag) != 0;
		}

		void SetFlagValue(DllCharacteristics flag, bool? value) {
			if (DllCharacteristics == null)
				DllCharacteristics = 0;
			if (value ?? false)
				DllCharacteristics |= flag;
			else
				DllCharacteristics &= ~flag;
		}

		public NullableUInt64VM SizeOfStackReserve {
			get { return sizeOfStackReserve; }
		}
		NullableUInt64VM sizeOfStackReserve;

		public NullableUInt64VM SizeOfStackCommit {
			get { return sizeOfStackCommit; }
		}
		NullableUInt64VM sizeOfStackCommit;

		public NullableUInt64VM SizeOfHeapReserve {
			get { return sizeOfHeapReserve; }
		}
		NullableUInt64VM sizeOfHeapReserve;

		public NullableUInt64VM SizeOfHeapCommit {
			get { return sizeOfHeapCommit; }
		}
		NullableUInt64VM sizeOfHeapCommit;

		public NullableUInt32VM LoaderFlags {
			get { return loaderFlags; }
		}
		NullableUInt32VM loaderFlags;

		public NullableUInt32VM NumberOfRvaAndSizes {
			get { return numberOfRvaAndSizes; }
		}
		NullableUInt32VM numberOfRvaAndSizes;

		public void CopyTo(PEHeadersOptions options) {
			options.Machine = (dnlib.PE.Machine)machineVM.SelectedItem;
			options.TimeDateStamp = TimeDateStamp.Value;
			options.PointerToSymbolTable = PointerToSymbolTable.Value;
			options.NumberOfSymbols = NumberOfSymbols.Value;
			options.Characteristics = Characteristics;
			options.MajorLinkerVersion = MajorLinkerVersion.Value;
			options.MinorLinkerVersion = MinorLinkerVersion.Value;
			options.ImageBase = ImageBase.Value;
			options.SectionAlignment = SectionAlignment.Value;
			options.FileAlignment = FileAlignment.Value;
			options.MajorOperatingSystemVersion = MajorOperatingSystemVersion.Value;
			options.MinorOperatingSystemVersion = MinorOperatingSystemVersion.Value;
			options.MajorImageVersion = MajorImageVersion.Value;
			options.MinorImageVersion = MinorImageVersion.Value;
			options.MajorSubsystemVersion = MajorSubsystemVersion.Value;
			options.MinorSubsystemVersion = MinorSubsystemVersion.Value;
			options.Win32VersionValue = Win32VersionValue.Value;
			options.Subsystem = (dnlib.PE.Subsystem)Subsystem.SelectedItem;
			options.DllCharacteristics = DllCharacteristics;
			options.SizeOfStackReserve = SizeOfStackReserve.Value;
			options.SizeOfStackCommit = SizeOfStackCommit.Value;
			options.SizeOfHeapReserve = SizeOfHeapReserve.Value;
			options.SizeOfHeapCommit = SizeOfHeapCommit.Value;
			options.LoaderFlags = LoaderFlags.Value;
			options.NumberOfRvaAndSizes = NumberOfRvaAndSizes.Value;
		}

		public void InitializeFrom(PEHeadersOptions options) {
			machineVM.SelectedItem = options.Machine ?? defaultMachine;
			TimeDateStamp.Value = options.TimeDateStamp;
			PointerToSymbolTable.Value = options.PointerToSymbolTable;
			NumberOfSymbols.Value = options.NumberOfSymbols;
			Characteristics = options.Characteristics;
			MajorLinkerVersion.Value = options.MajorLinkerVersion;
			MinorLinkerVersion.Value = options.MinorLinkerVersion;
			ImageBase.Value = options.ImageBase;
			SectionAlignment.Value = options.SectionAlignment;
			FileAlignment.Value = options.FileAlignment;
			MajorOperatingSystemVersion.Value = options.MajorOperatingSystemVersion;
			MinorOperatingSystemVersion.Value = options.MinorOperatingSystemVersion;
			MajorImageVersion.Value = options.MajorImageVersion;
			MinorImageVersion.Value = options.MinorImageVersion;
			MajorSubsystemVersion.Value = options.MajorSubsystemVersion;
			MinorSubsystemVersion.Value = options.MinorSubsystemVersion;
			Win32VersionValue.Value = options.Win32VersionValue;
			Subsystem.SelectedItem = options.Subsystem ?? defaultSubsystem;
			DllCharacteristics = options.DllCharacteristics;
			SizeOfStackReserve.Value = options.SizeOfStackReserve;
			SizeOfStackCommit.Value = options.SizeOfStackCommit;
			SizeOfHeapReserve.Value = options.SizeOfHeapReserve;
			SizeOfHeapCommit.Value = options.SizeOfHeapCommit;
			LoaderFlags.Value = options.LoaderFlags;
			NumberOfRvaAndSizes.Value = options.NumberOfRvaAndSizes;
		}

		public override bool HasError {
			get {
				return timeDateStamp.HasError ||
					pointerToSymbolTable.HasError ||
					numberOfSymbols.HasError ||
					majorLinkerVersion.HasError ||
					minorLinkerVersion.HasError ||
					imageBase.HasError ||
					sectionAlignment.HasError ||
					fileAlignment.HasError ||
					majorOperatingSystemVersion.HasError ||
					minorOperatingSystemVersion.HasError ||
					majorImageVersion.HasError ||
					minorImageVersion.HasError ||
					majorSubsystemVersion.HasError ||
					minorSubsystemVersion.HasError ||
					win32VersionValue.HasError ||
					sizeOfStackReserve.HasError ||
					sizeOfStackCommit.HasError ||
					sizeOfHeapReserve.HasError ||
					sizeOfHeapCommit.HasError ||
					loaderFlags.HasError ||
					numberOfRvaAndSizes.HasError;
			}
		}
	}

	sealed class Cor20HeaderOptionsVM : ViewModelBase {
		public Cor20HeaderOptionsVM() {
			this.majorRuntimeVersion = new NullableUInt16VM(a => HasErrorUpdated());
			this.minorRuntimeVersion = new NullableUInt16VM(a => HasErrorUpdated());
			this.entryPoint = new NullableUInt32VM(a => HasErrorUpdated());
		}

		public NullableUInt16VM MajorRuntimeVersion {
			get { return majorRuntimeVersion; }
		}
		NullableUInt16VM majorRuntimeVersion;

		public NullableUInt16VM MinorRuntimeVersion {
			get { return minorRuntimeVersion; }
		}
		NullableUInt16VM minorRuntimeVersion;

		public ComImageFlags? Flags {
			get { return flags; }
			set {
				if (flags != value) {
					flags = value;
					OnPropertyChanged("Flags");
					OnPropertyChanged("ILOnly");
					OnPropertyChanged("Bit32Required");
					OnPropertyChanged("ILLibrary");
					OnPropertyChanged("StrongNameSigned");
					OnPropertyChanged("TrackDebugData");
					OnPropertyChanged("Bit32Preferred");
				}
			}
		}
		ComImageFlags? flags;

		public bool? ILOnly {
			get { return GetFlagValue(ComImageFlags.ILOnly); }
			set { SetFlagValue(ComImageFlags.ILOnly, value); }
		}

		public bool? Bit32Required {
			get { return GetFlagValue(ComImageFlags._32BitRequired); }
			set { SetFlagValue(ComImageFlags._32BitRequired, value); }
		}

		public bool? ILLibrary {
			get { return GetFlagValue(ComImageFlags.ILLibrary); }
			set { SetFlagValue(ComImageFlags.ILLibrary, value); }
		}

		public bool? StrongNameSigned {
			get { return GetFlagValue(ComImageFlags.StrongNameSigned); }
			set { SetFlagValue(ComImageFlags.StrongNameSigned, value); }
		}

		public bool? TrackDebugData {
			get { return GetFlagValue(ComImageFlags.TrackDebugData); }
			set { SetFlagValue(ComImageFlags.TrackDebugData, value); }
		}

		public bool? Bit32Preferred {
			get { return GetFlagValue(ComImageFlags._32BitPreferred); }
			set { SetFlagValue(ComImageFlags._32BitPreferred, value); }
		}

		bool? GetFlagValue(ComImageFlags flag) {
			return Flags == null ? (bool?)null : (Flags.Value & flag) != 0;
		}

		void SetFlagValue(ComImageFlags flag, bool? value) {
			if (Flags == null)
				Flags = 0;
			if (value ?? false)
				Flags |= flag;
			else
				Flags &= ~flag;
		}

		public NullableUInt32VM EntryPoint {
			get { return entryPoint; }
		}
		NullableUInt32VM entryPoint;

		public void CopyTo(Cor20HeaderOptions options) {
			options.MajorRuntimeVersion = MajorRuntimeVersion.Value;
			options.MinorRuntimeVersion = MinorRuntimeVersion.Value;
			options.Flags = Flags;
			options.EntryPoint = EntryPoint.Value;
		}

		public void InitializeFrom(Cor20HeaderOptions options) {
			MajorRuntimeVersion.Value = options.MajorRuntimeVersion;
			MinorRuntimeVersion.Value = options.MinorRuntimeVersion;
			Flags = options.Flags;
			EntryPoint.Value = options.EntryPoint;
		}

		public override bool HasError {
			get {
				return majorRuntimeVersion.HasError ||
					minorRuntimeVersion.HasError ||
					entryPoint.HasError;
			}
		}
	}

	sealed class MetaDataOptionsVM : ViewModelBase {
		public MetaDataOptionsVM() {
			this.metaDataHeaderOptions = new MetaDataHeaderOptionsVM();
			this.tablesHeapOptions = new TablesHeapOptionsVM();

			this.metaDataHeaderOptions.PropertyChanged += (s, e) => HasErrorUpdated();
			this.tablesHeapOptions.PropertyChanged += (s, e) => HasErrorUpdated();
		}

		public MetaDataHeaderOptionsVM MetaDataHeaderOptions {
			get { return metaDataHeaderOptions; }
		}
		MetaDataHeaderOptionsVM metaDataHeaderOptions;

		public TablesHeapOptionsVM TablesHeapOptions {
			get { return tablesHeapOptions; }
		}
		TablesHeapOptionsVM tablesHeapOptions;

		public MetaDataFlags Flags {
			get { return flags; }
			set {
				if (flags != value) {
					flags = value;
					OnPropertyChanged("Flags");
				}
			}
		}
		MetaDataFlags flags;

		public bool PreserveTypeRefRids {
			get { return GetFlagValue(MetaDataFlags.PreserveTypeRefRids); }
			set { SetFlagValue(MetaDataFlags.PreserveTypeRefRids, value, "PreserveRids", "PreserveTypeRefRids"); }
		}

		public bool PreserveTypeDefRids {
			get { return GetFlagValue(MetaDataFlags.PreserveTypeDefRids); }
			set { SetFlagValue(MetaDataFlags.PreserveTypeDefRids, value, "PreserveRids", "PreserveTypeDefRids"); }
		}

		public bool PreserveFieldRids {
			get { return GetFlagValue(MetaDataFlags.PreserveFieldRids); }
			set { SetFlagValue(MetaDataFlags.PreserveFieldRids, value, "PreserveRids", "PreserveFieldRids"); }
		}

		public bool PreserveMethodRids {
			get { return GetFlagValue(MetaDataFlags.PreserveMethodRids); }
			set { SetFlagValue(MetaDataFlags.PreserveMethodRids, value, "PreserveRids", "PreserveMethodRids"); }
		}

		public bool PreserveParamRids {
			get { return GetFlagValue(MetaDataFlags.PreserveParamRids); }
			set { SetFlagValue(MetaDataFlags.PreserveParamRids, value, "PreserveRids", "PreserveParamRids"); }
		}

		public bool PreserveMemberRefRids {
			get { return GetFlagValue(MetaDataFlags.PreserveMemberRefRids); }
			set { SetFlagValue(MetaDataFlags.PreserveMemberRefRids, value, "PreserveRids", "PreserveMemberRefRids"); }
		}

		public bool PreserveStandAloneSigRids {
			get { return GetFlagValue(MetaDataFlags.PreserveStandAloneSigRids); }
			set { SetFlagValue(MetaDataFlags.PreserveStandAloneSigRids, value, "PreserveRids", "PreserveStandAloneSigRids"); }
		}

		public bool PreserveEventRids {
			get { return GetFlagValue(MetaDataFlags.PreserveEventRids); }
			set { SetFlagValue(MetaDataFlags.PreserveEventRids, value, "PreserveRids", "PreserveEventRids"); }
		}

		public bool PreservePropertyRids {
			get { return GetFlagValue(MetaDataFlags.PreservePropertyRids); }
			set { SetFlagValue(MetaDataFlags.PreservePropertyRids, value, "PreserveRids", "PreservePropertyRids"); }
		}

		public bool PreserveTypeSpecRids {
			get { return GetFlagValue(MetaDataFlags.PreserveTypeSpecRids); }
			set { SetFlagValue(MetaDataFlags.PreserveTypeSpecRids, value, "PreserveRids", "PreserveTypeSpecRids"); }
		}

		public bool PreserveMethodSpecRids {
			get { return GetFlagValue(MetaDataFlags.PreserveMethodSpecRids); }
			set { SetFlagValue(MetaDataFlags.PreserveMethodSpecRids, value, "PreserveRids", "PreserveMethodSpecRids"); }
		}

		public bool? PreserveRids {
			get {
				var val = Flags & MetaDataFlags.PreserveRids;
				if (val == MetaDataFlags.PreserveRids)
					return true;
				if (val == 0)
					return false;
				return null;
			}
			set {
				if (value != null && value != PreserveRids) {
					if (value.Value)
						Flags |= MetaDataFlags.PreserveRids;
					else
						Flags &= ~MetaDataFlags.PreserveRids;
					OnPropertyChanged("PreserveRids");
					OnPropertyChanged("PreserveTypeRefRids");
					OnPropertyChanged("PreserveTypeDefRids");
					OnPropertyChanged("PreserveFieldRids");
					OnPropertyChanged("PreserveMethodRids");
					OnPropertyChanged("PreserveParamRids");
					OnPropertyChanged("PreserveMemberRefRids");
					OnPropertyChanged("PreserveStandAloneSigRids");
					OnPropertyChanged("PreserveEventRids");
					OnPropertyChanged("PreservePropertyRids");
					OnPropertyChanged("PreserveTypeSpecRids");
					OnPropertyChanged("PreserveMethodSpecRids");
				}
			}
		}

		public bool PreserveStringsOffsets {
			get { return GetFlagValue(MetaDataFlags.PreserveStringsOffsets); }
			set { SetFlagValue(MetaDataFlags.PreserveStringsOffsets, value, "PreserveStringsOffsets"); }
		}

		public bool PreserveUSOffsets {
			get { return GetFlagValue(MetaDataFlags.PreserveUSOffsets); }
			set { SetFlagValue(MetaDataFlags.PreserveUSOffsets, value, "PreserveUSOffsets"); }
		}

		public bool PreserveBlobOffsets {
			get { return GetFlagValue(MetaDataFlags.PreserveBlobOffsets); }
			set { SetFlagValue(MetaDataFlags.PreserveBlobOffsets, value, "PreserveBlobOffsets"); }
		}

		public bool PreserveExtraSignatureData {
			get { return GetFlagValue(MetaDataFlags.PreserveExtraSignatureData); }
			set { SetFlagValue(MetaDataFlags.PreserveExtraSignatureData, value, "PreserveExtraSignatureData"); }
		}

		public bool KeepOldMaxStack {
			get { return GetFlagValue(MetaDataFlags.KeepOldMaxStack); }
			set { SetFlagValue(MetaDataFlags.KeepOldMaxStack, value, "KeepOldMaxStack"); }
		}

		public bool AlwaysCreateGuidHeap {
			get { return GetFlagValue(MetaDataFlags.AlwaysCreateGuidHeap); }
			set { SetFlagValue(MetaDataFlags.AlwaysCreateGuidHeap, value, "AlwaysCreateGuidHeap"); }
		}

		public bool AlwaysCreateStringsHeap {
			get { return GetFlagValue(MetaDataFlags.AlwaysCreateStringsHeap); }
			set { SetFlagValue(MetaDataFlags.AlwaysCreateStringsHeap, value, "AlwaysCreateStringsHeap"); }
		}

		public bool AlwaysCreateUSHeap {
			get { return GetFlagValue(MetaDataFlags.AlwaysCreateUSHeap); }
			set { SetFlagValue(MetaDataFlags.AlwaysCreateUSHeap, value, "AlwaysCreateUSHeap"); }
		}

		public bool AlwaysCreateBlobHeap {
			get { return GetFlagValue(MetaDataFlags.AlwaysCreateBlobHeap); }
			set { SetFlagValue(MetaDataFlags.AlwaysCreateBlobHeap, value, "AlwaysCreateBlobHeap"); }
		}

		bool GetFlagValue(MetaDataFlags flag) {
			return (Flags & flag) != 0;
		}

		void SetFlagValue(MetaDataFlags flag, bool value, string prop1, string prop2 = null) {
			bool origValue = (Flags & flag) != 0;
			if (origValue == value)
				return;

			if (value)
				Flags |= flag;
			else
				Flags &= ~flag;

			OnPropertyChanged(prop1);
			if (prop2 != null)
				OnPropertyChanged(prop2);
		}

		public void CopyTo(MetaDataOptions options) {
			MetaDataHeaderOptions.CopyTo(options.MetaDataHeaderOptions);
			TablesHeapOptions.CopyTo(options.TablesHeapOptions);
			options.Flags = Flags;
		}

		public void InitializeFrom(MetaDataOptions options) {
			MetaDataHeaderOptions.InitializeFrom(options.MetaDataHeaderOptions);
			TablesHeapOptions.InitializeFrom(options.TablesHeapOptions);
			Flags = options.Flags;
			OnFlagsChanged();
		}

		void OnFlagsChanged() {
			OnPropertyChanged("PreserveTypeRefRids");
			OnPropertyChanged("PreserveTypeDefRids");
			OnPropertyChanged("PreserveFieldRids");
			OnPropertyChanged("PreserveMethodRids");
			OnPropertyChanged("PreserveParamRids");
			OnPropertyChanged("PreserveMemberRefRids");
			OnPropertyChanged("PreserveStandAloneSigRids");
			OnPropertyChanged("PreserveEventRids");
			OnPropertyChanged("PreservePropertyRids");
			OnPropertyChanged("PreserveTypeSpecRids");
			OnPropertyChanged("PreserveMethodSpecRids");
			OnPropertyChanged("PreserveRids");
			OnPropertyChanged("PreserveStringsOffsets");
			OnPropertyChanged("PreserveUSOffsets");
			OnPropertyChanged("PreserveBlobOffsets");
			OnPropertyChanged("PreserveExtraSignatureData");
			OnPropertyChanged("KeepOldMaxStack");
			OnPropertyChanged("AlwaysCreateGuidHeap");
			OnPropertyChanged("AlwaysCreateStringsHeap");
			OnPropertyChanged("AlwaysCreateUSHeap");
			OnPropertyChanged("AlwaysCreateBlobHeap");
		}

		public override bool HasError {
			get {
				return metaDataHeaderOptions.HasError ||
					tablesHeapOptions.HasError;
			}
		}
	}

	sealed class MetaDataHeaderOptionsVM : ViewModelBase {
		public MetaDataHeaderOptionsVM() {
			this.signature = new NullableUInt32VM(a => HasErrorUpdated());
			this.majorVersion = new NullableUInt16VM(a => HasErrorUpdated());
			this.minorVersion = new NullableUInt16VM(a => HasErrorUpdated());
			this.reserved1 = new NullableUInt32VM(a => HasErrorUpdated());
			this.storageFlags = new NullableByteVM(a => HasErrorUpdated());
			this.reserved2 = new NullableByteVM(a => HasErrorUpdated());
		}

		public NullableUInt32VM Signature {
			get { return signature; }
		}
		NullableUInt32VM signature;

		public NullableUInt16VM MajorVersion {
			get { return majorVersion; }
		}
		NullableUInt16VM majorVersion;

		public NullableUInt16VM MinorVersion {
			get { return minorVersion; }
		}
		NullableUInt16VM minorVersion;

		public NullableUInt32VM Reserved1 {
			get { return reserved1; }
		}
		NullableUInt32VM reserved1;

		public string VersionString {
			get { return versionString; }
			set {
				versionString = value;
				OnPropertyChanged("VersionString");
				HasErrorUpdated();
			}
		}
		string versionString;

		public NullableByteVM StorageFlags {
			get { return storageFlags; }
		}
		NullableByteVM storageFlags;

		public NullableByteVM Reserved2 {
			get { return reserved2; }
		}
		NullableByteVM reserved2;

		public void CopyTo(MetaDataHeaderOptions options) {
			options.Signature = Signature.Value;
			options.MajorVersion = MajorVersion.Value;
			options.MinorVersion = MinorVersion.Value;
			options.Reserved1 = Reserved1.Value;
			options.VersionString = string.IsNullOrEmpty(VersionString) ? null : VersionString;
			options.StorageFlags = (StorageFlags?)StorageFlags.Value;
			options.Reserved2 = Reserved2.Value;
		}

		public void InitializeFrom(MetaDataHeaderOptions options) {
			Signature.Value = options.Signature;
			MajorVersion.Value = options.MajorVersion;
			MinorVersion.Value = options.MinorVersion;
			Reserved1.Value = options.Reserved1;
			VersionString = options.VersionString;
			StorageFlags.Value = (byte?)options.StorageFlags;
			Reserved2.Value = options.Reserved2;
		}

		protected override string Verify(string columnName) {
			if (columnName == "VersionString")
				return ValidateVersionString(versionString);

			return string.Empty;
		}

		public override bool HasError {
			get {
				if (!string.IsNullOrEmpty(Verify("VersionString")))
					return true;

				return signature.HasError ||
					majorVersion.HasError ||
					minorVersion.HasError ||
					reserved1.HasError ||
					storageFlags.HasError ||
					reserved2.HasError;
			}
		}

		internal static string ValidateVersionString(string versionString) {
			var bytes = Encoding.UTF8.GetBytes(versionString + "\0");
			if (bytes.Length > 256)
				return "Version string is too long";

			return string.Empty;
		}
	}

	sealed class TablesHeapOptionsVM : ViewModelBase {
		public TablesHeapOptionsVM() {
			this.reserved1 = new NullableUInt32VM(a => HasErrorUpdated());
			this.majorVersion = new NullableByteVM(a => HasErrorUpdated());
			this.minorVersion = new NullableByteVM(a => HasErrorUpdated());
			this.extraData = new NullableUInt32VM(a => HasErrorUpdated());
		}

		public NullableUInt32VM Reserved1 {
			get { return reserved1; }
		}
		NullableUInt32VM reserved1;

		public NullableByteVM MajorVersion {
			get { return majorVersion; }
		}
		NullableByteVM majorVersion;

		public NullableByteVM MinorVersion {
			get { return minorVersion; }
		}
		NullableByteVM minorVersion;

		public bool? UseENC {
			get { return useENC; }
			set {
				useENC = value;
				OnPropertyChanged("UseENC");
			}
		}
		bool? useENC;

		public NullableUInt32VM ExtraData {
			get { return extraData; }
		}
		NullableUInt32VM extraData;

		public bool? HasDeletedRows {
			get { return hasDeletedRows; }
			set {
				hasDeletedRows = value;
				OnPropertyChanged("HasDeletedRows");
			}
		}
		bool? hasDeletedRows;

		public void CopyTo(TablesHeapOptions options) {
			options.Reserved1 = Reserved1.Value;
			options.MajorVersion = MajorVersion.Value;
			options.MinorVersion = MinorVersion.Value;
			options.UseENC = UseENC;
			options.ExtraData = ExtraData.Value;
			options.HasDeletedRows = HasDeletedRows;
		}

		public void InitializeFrom(TablesHeapOptions options) {
			Reserved1.Value = options.Reserved1;
			MajorVersion.Value = options.MajorVersion;
			MinorVersion.Value = options.MinorVersion;
			UseENC = options.UseENC;
			ExtraData.Value = options.ExtraData;
			HasDeletedRows = options.HasDeletedRows;
		}

		public override bool HasError {
			get {
				return reserved1.HasError ||
					majorVersion.HasError ||
					minorVersion.HasError ||
					extraData.HasError;
			}
		}
	}
}
