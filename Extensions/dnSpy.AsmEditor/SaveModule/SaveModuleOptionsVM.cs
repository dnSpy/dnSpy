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

using System.Text;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.PE;
using dnlib.W32Resources;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.SaveModule {
	sealed class SaveModuleOptionsVM : SaveOptionsVM {
		public override SaveOptionsType Type => SaveOptionsType.Module;
		public ModuleDef Module { get; }
		public override object UndoDocument => document;
		readonly IDsDocument document;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public bool CanSaveMixedModeModule => Module is ModuleDefMD;
		public bool IsMixedModeModule => CanSaveMixedModeModule && !Module.IsILOnly;
		public bool CanPreserveOtherMetadataStreams => Module is ModuleDefMD;

		public bool UseMixedMode {
			get => useMixedMode;
			set {
				if (useMixedMode != value) {
					useMixedMode = value;
					OnPropertyChanged(nameof(UseMixedMode));
					ReinitializeModuleWriterOptions();
				}
			}
		}
		bool useMixedMode;

		public bool CanWritePdb => Module.PdbState is not null;

		public bool WritePdb {
			get => writePdb;
			set {
				if (writePdb != value) {
					writePdb = value;
					OnPropertyChanged(nameof(WritePdb));
				}
			}
		}
		bool writePdb;

		public bool ShareMethodBodies {
			get => shareMethodBodies;
			set {
				if (value != shareMethodBodies) {
					shareMethodBodies = value;
					OnPropertyChanged(nameof(ShareMethodBodies));
				}
			}
		}
		bool shareMethodBodies;

		public bool AddCheckSum {
			get => addCheckSum;
			set {
				if (value != addCheckSum) {
					addCheckSum = value;
					OnPropertyChanged(nameof(AddCheckSum));
				}
			}
		}
		bool addCheckSum;

		public Win32Resources? Win32Resources {
			get => win32Resources;
			set {
				if (win32Resources != value) {
					win32Resources = value;
					OnPropertyChanged(nameof(Win32Resources));
				}
			}
		}
		Win32Resources? win32Resources;

		public bool KeepExtraPEData {
			get => keepExtraPEData;
			set {
				if (keepExtraPEData != value) {
					keepExtraPEData = value;
					OnPropertyChanged(nameof(KeepExtraPEData));
				}
			}
		}
		bool keepExtraPEData;

		public bool KeepWin32Resources {
			get => keepWin32Resources;
			set {
				if (keepWin32Resources != value) {
					keepWin32Resources = value;
					OnPropertyChanged(nameof(KeepWin32Resources));
				}
			}
		}
		bool keepWin32Resources;

		internal static readonly EnumVM[] moduleKindList = EnumVM.Create(typeof(ModuleKind));

		public EnumListVM ModuleKind { get; }

		public string Extension {
			get {
				switch ((ModuleKind)ModuleKind.SelectedItem!) {
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

		public PEHeadersOptionsVM PEHeadersOptions { get; }
		public Cor20HeaderOptionsVM Cor20HeaderOptions { get; }
		public MetadataOptionsVM MetadataOptions { get; }

		public SaveModuleOptionsVM(IDsDocument document) {
			this.document = document;
			Module = document.ModuleDef!;
			PEHeadersOptions = new PEHeadersOptionsVM(Module.Machine, GetSubsystem(Module.Kind));
			Cor20HeaderOptions = new Cor20HeaderOptionsVM();
			MetadataOptions = new MetadataOptionsVM(Module);

			PEHeadersOptions.PropertyChanged += (s, e) => HasErrorUpdated();
			Cor20HeaderOptions.PropertyChanged += (s, e) => HasErrorUpdated();
			MetadataOptions.PropertyChanged += (s, e) => HasErrorUpdated();

			ModuleKind = new EnumListVM(moduleKindList, (a, b) => {
				OnPropertyChanged(nameof(Extension));
				PEHeadersOptions.Subsystem.SelectedItem = GetSubsystem((ModuleKind)ModuleKind!.SelectedItem!);
				PEHeadersOptions.Characteristics = CharacteristicsHelper.GetCharacteristics(PEHeadersOptions.Characteristics ?? 0, (ModuleKind)ModuleKind.SelectedItem!);
			});

			Reinitialize();
		}

		static Subsystem GetSubsystem(ModuleKind moduleKind) {
			if (moduleKind == dnlib.DotNet.ModuleKind.Windows)
				return Subsystem.WindowsGui;
			return Subsystem.WindowsCui;
		}

		void Reinitialize() {
			FileName = Module.Location;
			OriginalFileName = Module.Location;
			if (UseMixedMode == IsMixedModeModule)
				ReinitializeModuleWriterOptions();
			else
				UseMixedMode = IsMixedModeModule;
		}

		void ReinitializeModuleWriterOptions() {
			if (UseMixedMode)
				InitializeFrom(new NativeModuleWriterOptions((ModuleDefMD)Module, optimizeImageSize: true));
			else
				InitializeFrom(new ModuleWriterOptions(Module));
			WritePdb = CanWritePdb;
		}

		public ModuleWriterOptionsBase CreateWriterOptions() {
			if (UseMixedMode) {
				var options = new NativeModuleWriterOptions((ModuleDefMD)Module, optimizeImageSize: true);
				CopyTo(options);
				options.KeepExtraPEData = KeepExtraPEData;
				options.KeepWin32Resources = KeepWin32Resources;
				return AddOtherOptions(options);
			}
			else {
				var options = new ModuleWriterOptions(Module);
				CopyTo(options);
				if (Module.ManagedEntryPoint is not null || Module.NativeEntryPoint == 0)
					options.Cor20HeaderOptions.Flags &= ~ComImageFlags.NativeEntryPoint;
				return AddOtherOptions(options);
			}
		}

		ModuleWriterOptionsBase AddOtherOptions(ModuleWriterOptionsBase options) {
			if (MetadataOptions.PreserveOtherMetadataStreams)
				options.MetadataOptions.PreserveHeapOrder(Module, addCustomHeaps: true);
			return options;
		}

		void CopyTo(ModuleWriterOptionsBase options) {
			PEHeadersOptions.CopyTo(options.PEHeadersOptions);
			Cor20HeaderOptions.CopyTo(options.Cor20HeaderOptions);
			MetadataOptions.CopyTo(options.MetadataOptions);

			options.WritePdb = WritePdb;
			options.ShareMethodBodies = ShareMethodBodies;
			options.AddCheckSum = AddCheckSum;
			options.Win32Resources = Win32Resources;
			options.ModuleKind = (ModuleKind)ModuleKind.SelectedItem!;
		}

		public SaveModuleOptionsVM Clone() => CopyTo(new SaveModuleOptionsVM(document));

		public SaveModuleOptionsVM CopyTo(SaveModuleOptionsVM other) {
			other.FileName = FileName;
			other.OriginalFileName = OriginalFileName;
			other.UseMixedMode = UseMixedMode;
			other.MetadataOptions.PreserveOtherMetadataStreams = MetadataOptions.PreserveOtherMetadataStreams;
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
			ModuleKind.SelectedItem = options.ModuleKind;

			PEHeadersOptions.InitializeFrom(options.PEHeadersOptions);
			Cor20HeaderOptions.InitializeFrom(options.Cor20HeaderOptions);
			MetadataOptions.InitializeFrom(options.MetadataOptions);

			WritePdb = options.WritePdb;
			ShareMethodBodies = options.ShareMethodBodies;
			AddCheckSum = options.AddCheckSum;
			Win32Resources = options.Win32Resources;

			// Writing to Machine and ModuleKind triggers code that updates Characteristics
			PEHeadersOptions.Characteristics = options.PEHeadersOptions.Characteristics;
		}

		protected override string GetExtension(string filename) => Extension;

		public override bool HasError {
			get {
				return base.HasError ||
						PEHeadersOptions.HasError ||
						Cor20HeaderOptions.HasError ||
						MetadataOptions.HasError;
			}
		}
	}

	sealed class PEHeadersOptionsVM : ViewModelBase {
		readonly Machine defaultMachine;
		readonly Subsystem defaultSubsystem;

		public PEHeadersOptionsVM(Machine defaultMachine, Subsystem defaultSubsystem) {
			this.defaultMachine = defaultMachine;
			this.defaultSubsystem = defaultSubsystem;
			Machine = new EnumListVM(machineList, (a, b) => {
				Characteristics = CharacteristicsHelper.GetCharacteristics(Characteristics ?? 0, (dnlib.PE.Machine)Machine!.SelectedItem!);
			});
			TimeDateStamp = new NullableUInt32VM(a => HasErrorUpdated());
			PointerToSymbolTable = new NullableUInt32VM(a => HasErrorUpdated());
			NumberOfSymbols = new NullableUInt32VM(a => HasErrorUpdated());
			MajorLinkerVersion = new NullableByteVM(a => HasErrorUpdated());
			MinorLinkerVersion = new NullableByteVM(a => HasErrorUpdated());
			ImageBase = new NullableUInt64VM(a => HasErrorUpdated());
			SectionAlignment = new NullableUInt32VM(a => HasErrorUpdated());
			FileAlignment = new NullableUInt32VM(a => HasErrorUpdated());
			MajorOperatingSystemVersion = new NullableUInt16VM(a => HasErrorUpdated());
			MinorOperatingSystemVersion = new NullableUInt16VM(a => HasErrorUpdated());
			MajorImageVersion = new NullableUInt16VM(a => HasErrorUpdated());
			MinorImageVersion = new NullableUInt16VM(a => HasErrorUpdated());
			MajorSubsystemVersion = new NullableUInt16VM(a => HasErrorUpdated());
			MinorSubsystemVersion = new NullableUInt16VM(a => HasErrorUpdated());
			Win32VersionValue = new NullableUInt32VM(a => HasErrorUpdated());
			SizeOfStackReserve = new NullableUInt64VM(a => HasErrorUpdated());
			SizeOfStackCommit = new NullableUInt64VM(a => HasErrorUpdated());
			SizeOfHeapReserve = new NullableUInt64VM(a => HasErrorUpdated());
			SizeOfHeapCommit = new NullableUInt64VM(a => HasErrorUpdated());
			LoaderFlags = new NullableUInt32VM(a => HasErrorUpdated());
			NumberOfRvaAndSizes = new NullableUInt32VM(a => HasErrorUpdated());
		}

		internal static readonly EnumVM[] machineList = EnumVM.Create(typeof(dnlib.PE.Machine), dnlib.PE.Machine.I386, dnlib.PE.Machine.AMD64, dnlib.PE.Machine.IA64, dnlib.PE.Machine.ARMNT, dnlib.PE.Machine.ARM64);
		public EnumListVM Machine { get; }
		public NullableUInt32VM TimeDateStamp { get; }
		public NullableUInt32VM PointerToSymbolTable { get; }
		public NullableUInt32VM NumberOfSymbols { get; }

		public Characteristics? Characteristics {
			get => characteristics;
			set {
				if (characteristics != value) {
					characteristics = value;
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
		Characteristics? characteristics;

		public bool? RelocsStripped {
			get => GetFlagValue(dnlib.PE.Characteristics.RelocsStripped);
			set => SetFlagValue(dnlib.PE.Characteristics.RelocsStripped, value);
		}

		public bool? ExecutableImage {
			get => GetFlagValue(dnlib.PE.Characteristics.ExecutableImage);
			set => SetFlagValue(dnlib.PE.Characteristics.ExecutableImage, value);
		}

		public bool? LineNumsStripped {
			get => GetFlagValue(dnlib.PE.Characteristics.LineNumsStripped);
			set => SetFlagValue(dnlib.PE.Characteristics.LineNumsStripped, value);
		}

		public bool? LocalSymsStripped {
			get => GetFlagValue(dnlib.PE.Characteristics.LocalSymsStripped);
			set => SetFlagValue(dnlib.PE.Characteristics.LocalSymsStripped, value);
		}

		public bool? AggressiveWsTrim {
			get => GetFlagValue(dnlib.PE.Characteristics.AggressiveWsTrim);
			set => SetFlagValue(dnlib.PE.Characteristics.AggressiveWsTrim, value);
		}

		public bool? LargeAddressAware {
			get => GetFlagValue(dnlib.PE.Characteristics.LargeAddressAware);
			set => SetFlagValue(dnlib.PE.Characteristics.LargeAddressAware, value);
		}

		public bool? CharacteristicsReserved1 {
			get => GetFlagValue(dnlib.PE.Characteristics.Reserved1);
			set => SetFlagValue(dnlib.PE.Characteristics.Reserved1, value);
		}

		public bool? BytesReversedLo {
			get => GetFlagValue(dnlib.PE.Characteristics.BytesReversedLo);
			set => SetFlagValue(dnlib.PE.Characteristics.BytesReversedLo, value);
		}

		public bool? Bit32Machine {
			get => GetFlagValue(dnlib.PE.Characteristics.Bit32Machine);
			set => SetFlagValue(dnlib.PE.Characteristics.Bit32Machine, value);
		}

		public bool? DebugStripped {
			get => GetFlagValue(dnlib.PE.Characteristics.DebugStripped);
			set => SetFlagValue(dnlib.PE.Characteristics.DebugStripped, value);
		}

		public bool? RemovableRunFromSwap {
			get => GetFlagValue(dnlib.PE.Characteristics.RemovableRunFromSwap);
			set => SetFlagValue(dnlib.PE.Characteristics.RemovableRunFromSwap, value);
		}

		public bool? NetRunFromSwap {
			get => GetFlagValue(dnlib.PE.Characteristics.NetRunFromSwap);
			set => SetFlagValue(dnlib.PE.Characteristics.NetRunFromSwap, value);
		}

		public bool? System {
			get => GetFlagValue(dnlib.PE.Characteristics.System);
			set => SetFlagValue(dnlib.PE.Characteristics.System, value);
		}

		public bool? Dll {
			get => GetFlagValue(dnlib.PE.Characteristics.Dll);
			set => SetFlagValue(dnlib.PE.Characteristics.Dll, value);
		}

		public bool? UpSystemOnly {
			get => GetFlagValue(dnlib.PE.Characteristics.UpSystemOnly);
			set => SetFlagValue(dnlib.PE.Characteristics.UpSystemOnly, value);
		}

		public bool? BytesReversedHi {
			get => GetFlagValue(dnlib.PE.Characteristics.BytesReversedHi);
			set => SetFlagValue(dnlib.PE.Characteristics.BytesReversedHi, value);
		}

		bool? GetFlagValue(Characteristics flag) => Characteristics is null ? (bool?)null : (Characteristics.Value & flag) != 0;

		void SetFlagValue(Characteristics flag, bool? value) {
			if (Characteristics is null)
				Characteristics = 0;
			if (value ?? false)
				Characteristics |= flag;
			else
				Characteristics &= ~flag;
		}

		public NullableByteVM MajorLinkerVersion { get; }
		public NullableByteVM MinorLinkerVersion { get; }
		public NullableUInt64VM ImageBase { get; }
		public NullableUInt32VM SectionAlignment { get; }
		public NullableUInt32VM FileAlignment { get; }
		public NullableUInt16VM MajorOperatingSystemVersion { get; }
		public NullableUInt16VM MinorOperatingSystemVersion { get; }
		public NullableUInt16VM MajorImageVersion { get; }
		public NullableUInt16VM MinorImageVersion { get; }
		public NullableUInt16VM MajorSubsystemVersion { get; }
		public NullableUInt16VM MinorSubsystemVersion { get; }
		public NullableUInt32VM Win32VersionValue { get; }

		static readonly EnumVM[] subsystemList = EnumVM.Create(typeof(dnlib.PE.Subsystem), dnlib.PE.Subsystem.WindowsGui, dnlib.PE.Subsystem.WindowsCui);
		public EnumListVM Subsystem { get; } = new EnumListVM(subsystemList);

		public DllCharacteristics? DllCharacteristics {
			get => dllCharacteristics;
			set {
				if (dllCharacteristics != value) {
					dllCharacteristics = value;
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
		DllCharacteristics? dllCharacteristics;

		public bool? DllCharacteristicsReserved1 {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.Reserved1);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.Reserved1, value);
		}

		public bool? DllCharacteristicsReserved2 {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.Reserved2);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.Reserved2, value);
		}

		public bool? DllCharacteristicsReserved3 {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.Reserved3);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.Reserved3, value);
		}

		public bool? DllCharacteristicsReserved4 {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.Reserved4);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.Reserved4, value);
		}

		public bool? DllCharacteristicsReserved5 {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.Reserved5);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.Reserved5, value);
		}

		public bool? HighEntropyVA {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.HighEntropyVA);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.HighEntropyVA, value);
		}

		public bool? DynamicBase {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.DynamicBase);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.DynamicBase, value);
		}

		public bool? ForceIntegrity {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.ForceIntegrity);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.ForceIntegrity, value);
		}

		public bool? NxCompat {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.NxCompat);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.NxCompat, value);
		}

		public bool? NoIsolation {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.NoIsolation);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.NoIsolation, value);
		}

		public bool? NoSeh {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.NoSeh);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.NoSeh, value);
		}

		public bool? NoBind {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.NoBind);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.NoBind, value);
		}

		public bool? AppContainer {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.AppContainer);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.AppContainer, value);
		}

		public bool? WdmDriver {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.WdmDriver);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.WdmDriver, value);
		}

		public bool? GuardCf {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.GuardCf);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.GuardCf, value);
		}

		public bool? TerminalServerAware {
			get => GetFlagValue(dnlib.PE.DllCharacteristics.TerminalServerAware);
			set => SetFlagValue(dnlib.PE.DllCharacteristics.TerminalServerAware, value);
		}

		bool? GetFlagValue(DllCharacteristics flag) => DllCharacteristics is null ? (bool?)null : (DllCharacteristics.Value & flag) != 0;

		void SetFlagValue(DllCharacteristics flag, bool? value) {
			if (DllCharacteristics is null)
				DllCharacteristics = 0;
			if (value ?? false)
				DllCharacteristics |= flag;
			else
				DllCharacteristics &= ~flag;
		}

		public NullableUInt64VM SizeOfStackReserve { get; }
		public NullableUInt64VM SizeOfStackCommit { get; }
		public NullableUInt64VM SizeOfHeapReserve { get; }
		public NullableUInt64VM SizeOfHeapCommit { get; }
		public NullableUInt32VM LoaderFlags { get; }
		public NullableUInt32VM NumberOfRvaAndSizes { get; }

		public void CopyTo(PEHeadersOptions options) {
			options.Machine = (dnlib.PE.Machine)Machine.SelectedItem!;
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
			options.Subsystem = (dnlib.PE.Subsystem)Subsystem.SelectedItem!;
			options.DllCharacteristics = DllCharacteristics;
			options.SizeOfStackReserve = SizeOfStackReserve.Value;
			options.SizeOfStackCommit = SizeOfStackCommit.Value;
			options.SizeOfHeapReserve = SizeOfHeapReserve.Value;
			options.SizeOfHeapCommit = SizeOfHeapCommit.Value;
			options.LoaderFlags = LoaderFlags.Value;
			options.NumberOfRvaAndSizes = NumberOfRvaAndSizes.Value;
		}

		public void InitializeFrom(PEHeadersOptions options) {
			Machine.SelectedItem = options.Machine ?? defaultMachine;
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
				return TimeDateStamp.HasError ||
					PointerToSymbolTable.HasError ||
					NumberOfSymbols.HasError ||
					MajorLinkerVersion.HasError ||
					MinorLinkerVersion.HasError ||
					ImageBase.HasError ||
					SectionAlignment.HasError ||
					FileAlignment.HasError ||
					MajorOperatingSystemVersion.HasError ||
					MinorOperatingSystemVersion.HasError ||
					MajorImageVersion.HasError ||
					MinorImageVersion.HasError ||
					MajorSubsystemVersion.HasError ||
					MinorSubsystemVersion.HasError ||
					Win32VersionValue.HasError ||
					SizeOfStackReserve.HasError ||
					SizeOfStackCommit.HasError ||
					SizeOfHeapReserve.HasError ||
					SizeOfHeapCommit.HasError ||
					LoaderFlags.HasError ||
					NumberOfRvaAndSizes.HasError;
			}
		}
	}

	sealed class Cor20HeaderOptionsVM : ViewModelBase {
		public Cor20HeaderOptionsVM() {
			MajorRuntimeVersion = new NullableUInt16VM(a => HasErrorUpdated());
			MinorRuntimeVersion = new NullableUInt16VM(a => HasErrorUpdated());
			EntryPoint = new NullableUInt32VM(a => HasErrorUpdated());
		}

		public NullableUInt16VM MajorRuntimeVersion { get; }
		public NullableUInt16VM MinorRuntimeVersion { get; }

		public ComImageFlags? Flags {
			get => flags;
			set {
				if (flags != value) {
					flags = value;
					OnPropertyChanged(nameof(Flags));
					OnPropertyChanged(nameof(ILOnly));
					OnPropertyChanged(nameof(Bit32Required));
					OnPropertyChanged(nameof(ILLibrary));
					OnPropertyChanged(nameof(StrongNameSigned));
					OnPropertyChanged(nameof(TrackDebugData));
					OnPropertyChanged(nameof(Bit32Preferred));
				}
			}
		}
		ComImageFlags? flags;

		public bool? ILOnly {
			get => GetFlagValue(ComImageFlags.ILOnly);
			set => SetFlagValue(ComImageFlags.ILOnly, value);
		}

		public bool? Bit32Required {
			get => GetFlagValue(ComImageFlags.Bit32Required);
			set => SetFlagValue(ComImageFlags.Bit32Required, value);
		}

		public bool? ILLibrary {
			get => GetFlagValue(ComImageFlags.ILLibrary);
			set => SetFlagValue(ComImageFlags.ILLibrary, value);
		}

		public bool? StrongNameSigned {
			get => GetFlagValue(ComImageFlags.StrongNameSigned);
			set => SetFlagValue(ComImageFlags.StrongNameSigned, value);
		}

		public bool? TrackDebugData {
			get => GetFlagValue(ComImageFlags.TrackDebugData);
			set => SetFlagValue(ComImageFlags.TrackDebugData, value);
		}

		public bool? Bit32Preferred {
			get => GetFlagValue(ComImageFlags.Bit32Preferred);
			set => SetFlagValue(ComImageFlags.Bit32Preferred, value);
		}

		bool? GetFlagValue(ComImageFlags flag) => Flags is null ? (bool?)null : (Flags.Value & flag) != 0;

		void SetFlagValue(ComImageFlags flag, bool? value) {
			if (Flags is null)
				Flags = 0;
			if (value ?? false)
				Flags |= flag;
			else
				Flags &= ~flag;
		}

		public NullableUInt32VM EntryPoint { get; }

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
				return MajorRuntimeVersion.HasError ||
					MinorRuntimeVersion.HasError ||
					EntryPoint.HasError;
			}
		}
	}

	sealed class MetadataOptionsVM : ViewModelBase {
		readonly ModuleDef module;

		public MetadataOptionsVM(ModuleDef module) {
			this.module = module;
			MetadataHeaderOptions = new MetadataHeaderOptionsVM();
			TablesHeapOptions = new TablesHeapOptionsVM();

			MetadataHeaderOptions.PropertyChanged += (s, e) => HasErrorUpdated();
			TablesHeapOptions.PropertyChanged += (s, e) => HasErrorUpdated();
		}

		public MetadataHeaderOptionsVM MetadataHeaderOptions { get; }
		public TablesHeapOptionsVM TablesHeapOptions { get; }

		public MetadataFlags Flags {
			get => flags;
			set {
				if (flags != value) {
					flags = value;
					OnPropertyChanged(nameof(Flags));
				}
			}
		}
		MetadataFlags flags;

		public bool PreserveTypeRefRids {
			get => GetFlagValue(MetadataFlags.PreserveTypeRefRids);
			set => SetFlagValue(MetadataFlags.PreserveTypeRefRids, value, nameof(PreserveRids), nameof(PreserveTypeRefRids));
		}

		public bool PreserveTypeDefRids {
			get => GetFlagValue(MetadataFlags.PreserveTypeDefRids);
			set => SetFlagValue(MetadataFlags.PreserveTypeDefRids, value, nameof(PreserveRids), nameof(PreserveTypeDefRids));
		}

		public bool PreserveFieldRids {
			get => GetFlagValue(MetadataFlags.PreserveFieldRids);
			set => SetFlagValue(MetadataFlags.PreserveFieldRids, value, nameof(PreserveRids), nameof(PreserveFieldRids));
		}

		public bool PreserveMethodRids {
			get => GetFlagValue(MetadataFlags.PreserveMethodRids);
			set => SetFlagValue(MetadataFlags.PreserveMethodRids, value, nameof(PreserveRids), nameof(PreserveMethodRids));
		}

		public bool PreserveParamRids {
			get => GetFlagValue(MetadataFlags.PreserveParamRids);
			set => SetFlagValue(MetadataFlags.PreserveParamRids, value, nameof(PreserveRids), nameof(PreserveParamRids));
		}

		public bool PreserveMemberRefRids {
			get => GetFlagValue(MetadataFlags.PreserveMemberRefRids);
			set => SetFlagValue(MetadataFlags.PreserveMemberRefRids, value, nameof(PreserveRids), nameof(PreserveMemberRefRids));
		}

		public bool PreserveStandAloneSigRids {
			get => GetFlagValue(MetadataFlags.PreserveStandAloneSigRids);
			set => SetFlagValue(MetadataFlags.PreserveStandAloneSigRids, value, nameof(PreserveRids), nameof(PreserveStandAloneSigRids));
		}

		public bool PreserveEventRids {
			get => GetFlagValue(MetadataFlags.PreserveEventRids);
			set => SetFlagValue(MetadataFlags.PreserveEventRids, value, nameof(PreserveRids), nameof(PreserveEventRids));
		}

		public bool PreservePropertyRids {
			get => GetFlagValue(MetadataFlags.PreservePropertyRids);
			set => SetFlagValue(MetadataFlags.PreservePropertyRids, value, nameof(PreserveRids), nameof(PreservePropertyRids));
		}

		public bool PreserveTypeSpecRids {
			get => GetFlagValue(MetadataFlags.PreserveTypeSpecRids);
			set => SetFlagValue(MetadataFlags.PreserveTypeSpecRids, value, nameof(PreserveRids), nameof(PreserveTypeSpecRids));
		}

		public bool PreserveMethodSpecRids {
			get => GetFlagValue(MetadataFlags.PreserveMethodSpecRids);
			set => SetFlagValue(MetadataFlags.PreserveMethodSpecRids, value, nameof(PreserveRids), nameof(PreserveMethodSpecRids));
		}

		public bool? PreserveRids {
			get {
				var val = Flags & MetadataFlags.PreserveRids;
				if (val == MetadataFlags.PreserveRids)
					return true;
				if (val == 0)
					return false;
				return null;
			}
			set {
				if (value is not null && value != PreserveRids) {
					if (value.Value)
						Flags |= MetadataFlags.PreserveRids;
					else
						Flags &= ~MetadataFlags.PreserveRids;
					OnPropertyChanged(nameof(PreserveRids));
					OnPropertyChanged(nameof(PreserveTypeRefRids));
					OnPropertyChanged(nameof(PreserveTypeDefRids));
					OnPropertyChanged(nameof(PreserveFieldRids));
					OnPropertyChanged(nameof(PreserveMethodRids));
					OnPropertyChanged(nameof(PreserveParamRids));
					OnPropertyChanged(nameof(PreserveMemberRefRids));
					OnPropertyChanged(nameof(PreserveStandAloneSigRids));
					OnPropertyChanged(nameof(PreserveEventRids));
					OnPropertyChanged(nameof(PreservePropertyRids));
					OnPropertyChanged(nameof(PreserveTypeSpecRids));
					OnPropertyChanged(nameof(PreserveMethodSpecRids));
				}
			}
		}

		public bool PreserveStringsOffsets {
			get => GetFlagValue(MetadataFlags.PreserveStringsOffsets);
			set => SetFlagValue(MetadataFlags.PreserveStringsOffsets, value, nameof(PreserveStringsOffsets));
		}

		public bool PreserveUSOffsets {
			get => GetFlagValue(MetadataFlags.PreserveUSOffsets);
			set => SetFlagValue(MetadataFlags.PreserveUSOffsets, value, nameof(PreserveUSOffsets));
		}

		public bool PreserveBlobOffsets {
			get => GetFlagValue(MetadataFlags.PreserveBlobOffsets);
			set => SetFlagValue(MetadataFlags.PreserveBlobOffsets, value, nameof(PreserveBlobOffsets));
		}

		public bool PreserveExtraSignatureData {
			get => GetFlagValue(MetadataFlags.PreserveExtraSignatureData);
			set => SetFlagValue(MetadataFlags.PreserveExtraSignatureData, value, nameof(PreserveExtraSignatureData));
		}

		public bool KeepOldMaxStack {
			get => GetFlagValue(MetadataFlags.KeepOldMaxStack);
			set => SetFlagValue(MetadataFlags.KeepOldMaxStack, value, nameof(KeepOldMaxStack));
		}

		public bool AlwaysCreateGuidHeap {
			get => GetFlagValue(MetadataFlags.AlwaysCreateGuidHeap);
			set => SetFlagValue(MetadataFlags.AlwaysCreateGuidHeap, value, nameof(AlwaysCreateGuidHeap));
		}

		public bool AlwaysCreateStringsHeap {
			get => GetFlagValue(MetadataFlags.AlwaysCreateStringsHeap);
			set => SetFlagValue(MetadataFlags.AlwaysCreateStringsHeap, value, nameof(AlwaysCreateStringsHeap));
		}

		public bool AlwaysCreateUSHeap {
			get => GetFlagValue(MetadataFlags.AlwaysCreateUSHeap);
			set => SetFlagValue(MetadataFlags.AlwaysCreateUSHeap, value, nameof(AlwaysCreateUSHeap));
		}

		public bool AlwaysCreateBlobHeap {
			get => GetFlagValue(MetadataFlags.AlwaysCreateBlobHeap);
			set => SetFlagValue(MetadataFlags.AlwaysCreateBlobHeap, value, nameof(AlwaysCreateBlobHeap));
		}

		public bool PreserveOtherMetadataStreams {
			get => preserveOtherMetadataStreams;
			set {
				if (preserveOtherMetadataStreams != value) {
					preserveOtherMetadataStreams = value;
					OnPropertyChanged(nameof(PreserveOtherMetadataStreams));
					if (preserveOtherMetadataStreams && module is ModuleDefMD mod) {
						if (mod.Metadata.GuidStream.StartOffset != 0)
							AlwaysCreateGuidHeap = true;
						if (mod.Metadata.StringsStream.StartOffset != 0)
							AlwaysCreateStringsHeap = true;
						if (mod.Metadata.USStream.StartOffset != 0)
							AlwaysCreateUSHeap = true;
						if (mod.Metadata.BlobStream.StartOffset != 0)
							AlwaysCreateBlobHeap = true;
					}
				}
			}
		}
		bool preserveOtherMetadataStreams;

		bool GetFlagValue(MetadataFlags flag) => (Flags & flag) != 0;

		void SetFlagValue(MetadataFlags flag, bool value, string prop1, string? prop2 = null) {
			bool origValue = (Flags & flag) != 0;
			if (origValue == value)
				return;

			if (value)
				Flags |= flag;
			else
				Flags &= ~flag;

			OnPropertyChanged(prop1);
			if (prop2 is not null)
				OnPropertyChanged(prop2);
		}

		public void CopyTo(MetadataOptions options) {
			MetadataHeaderOptions.CopyTo(options.MetadataHeaderOptions);
			TablesHeapOptions.CopyTo(options.TablesHeapOptions);
			options.Flags = Flags;
		}

		public void InitializeFrom(MetadataOptions options) {
			MetadataHeaderOptions.InitializeFrom(options.MetadataHeaderOptions);
			TablesHeapOptions.InitializeFrom(options.TablesHeapOptions);
			Flags = options.Flags;
			PreserveOtherMetadataStreams = false;
			OnFlagsChanged();
		}

		void OnFlagsChanged() {
			OnPropertyChanged(nameof(PreserveTypeRefRids));
			OnPropertyChanged(nameof(PreserveTypeDefRids));
			OnPropertyChanged(nameof(PreserveFieldRids));
			OnPropertyChanged(nameof(PreserveMethodRids));
			OnPropertyChanged(nameof(PreserveParamRids));
			OnPropertyChanged(nameof(PreserveMemberRefRids));
			OnPropertyChanged(nameof(PreserveStandAloneSigRids));
			OnPropertyChanged(nameof(PreserveEventRids));
			OnPropertyChanged(nameof(PreservePropertyRids));
			OnPropertyChanged(nameof(PreserveTypeSpecRids));
			OnPropertyChanged(nameof(PreserveMethodSpecRids));
			OnPropertyChanged(nameof(PreserveRids));
			OnPropertyChanged(nameof(PreserveStringsOffsets));
			OnPropertyChanged(nameof(PreserveUSOffsets));
			OnPropertyChanged(nameof(PreserveBlobOffsets));
			OnPropertyChanged(nameof(PreserveExtraSignatureData));
			OnPropertyChanged(nameof(KeepOldMaxStack));
			OnPropertyChanged(nameof(AlwaysCreateGuidHeap));
			OnPropertyChanged(nameof(AlwaysCreateStringsHeap));
			OnPropertyChanged(nameof(AlwaysCreateUSHeap));
			OnPropertyChanged(nameof(AlwaysCreateBlobHeap));
		}

		public override bool HasError {
			get {
				return MetadataHeaderOptions.HasError ||
					TablesHeapOptions.HasError;
			}
		}
	}

	sealed class MetadataHeaderOptionsVM : ViewModelBase {
		public MetadataHeaderOptionsVM() {
			Signature = new NullableUInt32VM(a => HasErrorUpdated());
			MajorVersion = new NullableUInt16VM(a => HasErrorUpdated());
			MinorVersion = new NullableUInt16VM(a => HasErrorUpdated());
			Reserved1 = new NullableUInt32VM(a => HasErrorUpdated());
			StorageFlags = new NullableByteVM(a => HasErrorUpdated());
			Reserved2 = new NullableByteVM(a => HasErrorUpdated());
		}

		public NullableUInt32VM Signature { get; }
		public NullableUInt16VM MajorVersion { get; }
		public NullableUInt16VM MinorVersion { get; }
		public NullableUInt32VM Reserved1 { get; }

		public string? VersionString {
			get => versionString;
			set {
				versionString = value;
				OnPropertyChanged(nameof(VersionString));
				HasErrorUpdated();
			}
		}
		string? versionString;

		public NullableByteVM StorageFlags { get; }
		public NullableByteVM Reserved2 { get; }

		public void CopyTo(MetadataHeaderOptions options) {
			options.Signature = Signature.Value;
			options.MajorVersion = MajorVersion.Value;
			options.MinorVersion = MinorVersion.Value;
			options.Reserved1 = Reserved1.Value;
			options.VersionString = string.IsNullOrEmpty(VersionString) ? null : VersionString;
			options.StorageFlags = (StorageFlags?)StorageFlags.Value;
			options.Reserved2 = Reserved2.Value;
		}

		public void InitializeFrom(MetadataHeaderOptions options) {
			Signature.Value = options.Signature;
			MajorVersion.Value = options.MajorVersion;
			MinorVersion.Value = options.MinorVersion;
			Reserved1.Value = options.Reserved1;
			VersionString = options.VersionString;
			StorageFlags.Value = (byte?)options.StorageFlags;
			Reserved2.Value = options.Reserved2;
		}

		protected override string? Verify(string columnName) {
			if (columnName == nameof(VersionString))
				return ValidateVersionString(versionString);

			return string.Empty;
		}

		public override bool HasError {
			get {
				if (!string.IsNullOrEmpty(Verify(nameof(VersionString))))
					return true;

				return Signature.HasError ||
					MajorVersion.HasError ||
					MinorVersion.HasError ||
					Reserved1.HasError ||
					StorageFlags.HasError ||
					Reserved2.HasError;
			}
		}

		internal static string ValidateVersionString(string? versionString) {
			var bytes = Encoding.UTF8.GetBytes(versionString + "\0");
			if (bytes.Length > 256)
				return dnSpy_AsmEditor_Resources.Error_VersionStringTooLong;

			return string.Empty;
		}
	}

	sealed class TablesHeapOptionsVM : ViewModelBase {
		public TablesHeapOptionsVM() {
			Reserved1 = new NullableUInt32VM(a => HasErrorUpdated());
			MajorVersion = new NullableByteVM(a => HasErrorUpdated());
			MinorVersion = new NullableByteVM(a => HasErrorUpdated());
			ExtraData = new NullableUInt32VM(a => HasErrorUpdated());
		}

		public NullableUInt32VM Reserved1 { get; }
		public NullableByteVM MajorVersion { get; }
		public NullableByteVM MinorVersion { get; }

		public bool? UseENC {
			get => useENC;
			set {
				useENC = value;
				OnPropertyChanged(nameof(UseENC));
			}
		}
		bool? useENC;

		public NullableUInt32VM ExtraData { get; }

		public bool? HasDeletedRows {
			get => hasDeletedRows;
			set {
				hasDeletedRows = value;
				OnPropertyChanged(nameof(HasDeletedRows));
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
				return Reserved1.HasError ||
					MajorVersion.HasError ||
					MinorVersion.HasError ||
					ExtraData.HasError;
			}
		}
	}
}
