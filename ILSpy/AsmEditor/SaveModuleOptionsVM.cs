
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.PE;
using dnlib.W32Resources;

namespace ICSharpCode.ILSpy.AsmEditor
{
	sealed class EnumVM
	{
		readonly object value;
		readonly string name;

		public object Value {
			get { return value; }
		}

		public string Name {
			get { return name; }
		}

		public EnumVM(object value)
		{
			this.value = value;
			this.name = Enum.GetName(value.GetType(), value);
		}

		public static EnumVM[] Create(Type enumType, params object[] values)
		{
			var list = new List<EnumVM>();
			foreach (var value in enumType.GetEnumValues()) {
				if (values.Any(a => a.Equals(value)))
					continue;
				list.Add(new EnumVM(value));
			}
			list.Sort((a, b) => a.Name.ToUpperInvariant().CompareTo(b.Name.ToUpperInvariant()));
			for (int i = 0; i < values.Length; i++)
				list.Insert(i, new EnumVM(values[i]));
			return list.ToArray();
		}
	}

	sealed class EnumListVM : INotifyPropertyChanged
	{
		readonly IList<EnumVM> list;
		readonly Action onChanged;
		int index;

		public IList<EnumVM> Items {
			get { return list; }
		}

		public int SelectedIndex {
			get { return index; }
			set {
				if (index != value) {
					Debug.Assert(value >= 0 && value < list.Count);
					index = value;
					OnPropertyChanged("SelectedIndex");
					OnPropertyChanged("SelectedItem");
					if (onChanged != null)
						onChanged();
				}
			}
		}

		public object SelectedItem {
			get {
				if (index < 0 || index >= list.Count)
					return null;
				return list[index].Value;
			}
			set {
				if (SelectedItem != value)
					SelectedIndex = GetIndex(value);
			}
		}

		public EnumListVM(IList<EnumVM> list, Action onChanged = null)
		{
			this.list = list;
			this.index = 0;
			this.onChanged = onChanged;
		}

		int GetIndex(object value)
		{
			for (int i = 0; i < list.Count; i++) {
				if (list[i].Value.Equals(value))
					return i;
			}
			Debug.Fail(string.Format("Could not find {0}", value));
			return -1;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}

	sealed class SaveModuleOptionsVM : INotifyPropertyChanged
	{
		public ModuleDef Module {
			get { return module; }
		}
		readonly ModuleDef module;

		public ICommand ReinitializeCommand {
			get { return reinitializeCommand ?? (reinitializeCommand = new RelayCommand(a => Reinitialize())); }
		}
		ICommand reinitializeCommand;

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

		public string FileName {
			get { return filename; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				// Use Ordinal and not OrdinalIgnoreCase so it gets updated in the UI
				if (!filename.Equals(value, StringComparison.Ordinal)) {
					filename = value;
					OnPropertyChanged("FileName");
				}
			}
		}
		string filename = string.Empty;

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

		static readonly EnumVM[] moduleKindList = EnumVM.Create(typeof(dnlib.DotNet.ModuleKind));

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
		readonly Cor20HeaderOptionsVM cor20HeaderOptions = new Cor20HeaderOptionsVM();

		public MetaDataOptionsVM MetaDataOptions {
			get { return metaDataOptions; }
		}
		readonly MetaDataOptionsVM metaDataOptions = new MetaDataOptionsVM();

		public SaveModuleOptionsVM(ModuleDef module)
		{
			this.module = module;
			this.peHeadersOptions = new PEHeadersOptionsVM(module.Machine, GetSubsystem(module.Kind));

			moduleKindVM = new EnumListVM(moduleKindList, () => {
				OnPropertyChanged("Extension");
				PEHeadersOptions.Subsystem.SelectedItem = GetSubsystem((dnlib.DotNet.ModuleKind)ModuleKind.SelectedItem);
			});

			Reinitialize();
		}

		static Subsystem GetSubsystem(ModuleKind moduleKind)
		{
			if (moduleKind == dnlib.DotNet.ModuleKind.Windows)
				return Subsystem.WindowsGui;
			return Subsystem.WindowsCui;
		}

		void Reinitialize()
		{
			FileName = module.Location;
			if (UseMixedMode == IsMixedModeModule)
				ReinitializeModuleWriterOptions();
			else
				UseMixedMode = IsMixedModeModule;
		}

		void ReinitializeModuleWriterOptions()
		{
			if (UseMixedMode)
				InitializeFrom(new NativeModuleWriterOptions((ModuleDefMD)module));
			else
				InitializeFrom(new ModuleWriterOptions(module));
			WritePdb = CanWritePdb;
		}

		public ModuleWriterOptionsBase CreateWriterOptions()
		{
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
				return options;
			}
		}

		void CopyTo(ModuleWriterOptionsBase options)
		{
			peHeadersOptions.CopyTo(options.PEHeadersOptions);
			cor20HeaderOptions.CopyTo(options.Cor20HeaderOptions);
			metaDataOptions.CopyTo(options.MetaDataOptions);

			options.WritePdb = WritePdb;
			options.ShareMethodBodies = ShareMethodBodies;
			options.AddCheckSum = AddCheckSum;
			options.Win32Resources = Win32Resources;
			options.ModuleKind = (dnlib.DotNet.ModuleKind)moduleKindVM.SelectedItem;
		}

		public SaveModuleOptionsVM Clone()
		{
			return CopyTo(new SaveModuleOptionsVM(module));
		}

		public SaveModuleOptionsVM CopyTo(SaveModuleOptionsVM other)
		{
			other.FileName = FileName;
			other.UseMixedMode = UseMixedMode;
			other.InitializeFrom(CreateWriterOptions());
			return other;
		}

		public void InitializeFrom(ModuleWriterOptionsBase options)
		{
			if (options is ModuleWriterOptions)
				InitializeFrom((ModuleWriterOptions)options);
			else
				InitializeFrom((NativeModuleWriterOptions)options);
		}

		public void InitializeFrom(ModuleWriterOptions options)
		{
			InitializeFromInternal((ModuleWriterOptionsBase)options);
			KeepExtraPEData = false;
			KeepWin32Resources = false;
		}

		public void InitializeFrom(NativeModuleWriterOptions options)
		{
			InitializeFromInternal((ModuleWriterOptionsBase)options);
			KeepExtraPEData = options.KeepExtraPEData;
			KeepWin32Resources = options.KeepWin32Resources;
		}

		void InitializeFromInternal(ModuleWriterOptionsBase options)
		{
			peHeadersOptions.InitializeFrom(options.PEHeadersOptions);
			cor20HeaderOptions.InitializeFrom(options.Cor20HeaderOptions);
			metaDataOptions.InitializeFrom(options.MetaDataOptions);

			WritePdb = options.WritePdb;
			ShareMethodBodies = options.ShareMethodBodies;
			AddCheckSum = options.AddCheckSum;
			Win32Resources = options.Win32Resources;
			moduleKindVM.SelectedItem = options.ModuleKind;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}

	sealed class PEHeadersOptionsVM : INotifyPropertyChanged
	{
		readonly Machine defaultMachine;
		readonly Subsystem defaultSubsystem;

		public PEHeadersOptionsVM(Machine defaultMachine, Subsystem defaultSubsystem)
		{
			this.defaultMachine = defaultMachine;
			this.defaultSubsystem = defaultSubsystem;
		}

		public EnumListVM Machine {
			get { return machineVM; }
		}
		static readonly EnumVM[] machineList = EnumVM.Create(typeof(dnlib.PE.Machine), dnlib.PE.Machine.I386, dnlib.PE.Machine.AMD64, dnlib.PE.Machine.IA64, dnlib.PE.Machine.ARMNT, dnlib.PE.Machine.ARM64);
		readonly EnumListVM machineVM = new EnumListVM(machineList);

		public uint? TimeDateStamp {
			get { return timeDateStamp; }
			set {
				if (timeDateStamp != value) {
					timeDateStamp = value;
					OnPropertyChanged("TimeDateStamp");
				}
			}
		}
		uint? timeDateStamp;

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

		bool? GetFlagValue(Characteristics flag)
		{
			return Characteristics == null ? (bool?)null : (Characteristics.Value & flag) != 0;
		}

		void SetFlagValue(Characteristics flag, bool? value)
		{
			if (Characteristics == null)
				Characteristics = 0;
			if (value ?? false)
				Characteristics |= flag;
			else
				Characteristics &= ~flag;
		}

		public byte? MajorLinkerVersion {
			get { return majorLinkerVersion; }
			set {
				if (majorLinkerVersion != value) {
					majorLinkerVersion = value;
					OnPropertyChanged("MajorLinkerVersion");
				}
			}
		}
		byte? majorLinkerVersion;

		public byte? MinorLinkerVersion {
			get { return minorLinkerVersion; }
			set {
				if (minorLinkerVersion != value) {
					minorLinkerVersion = value;
					OnPropertyChanged("MinorLinkerVersion");
				}
			}
		}
		byte? minorLinkerVersion;

		public ulong? ImageBase {
			get { return imageBase; }
			set {
				if (imageBase != value) {
					imageBase = value;
					OnPropertyChanged("ImageBase");
				}
			}
		}
		ulong? imageBase;

		public uint? SectionAlignment {
			get { return sectionAlignment; }
			set {
				if (sectionAlignment != value) {
					sectionAlignment = value;
					OnPropertyChanged("SectionAlignment");
				}
			}
		}
		uint? sectionAlignment;

		public uint? FileAlignment {
			get { return fileAlignment; }
			set {
				if (fileAlignment != value) {
					fileAlignment = value;
					OnPropertyChanged("FileAlignment");
				}
			}
		}
		uint? fileAlignment;

		public ushort? MajorOperatingSystemVersion {
			get { return majorOperatingSystemVersion; }
			set {
				if (majorOperatingSystemVersion != value) {
					majorOperatingSystemVersion = value;
					OnPropertyChanged("MajorOperatingSystemVersion");
				}
			}
		}
		ushort? majorOperatingSystemVersion;

		public ushort? MinorOperatingSystemVersion {
			get { return minorOperatingSystemVersion; }
			set {
				if (minorOperatingSystemVersion != value) {
					minorOperatingSystemVersion = value;
					OnPropertyChanged("MinorOperatingSystemVersion");
				}
			}
		}
		ushort? minorOperatingSystemVersion;

		public ushort? MajorImageVersion {
			get { return majorImageVersion; }
			set {
				if (majorImageVersion != value) {
					majorImageVersion = value;
					OnPropertyChanged("MajorImageVersion");
				}
			}
		}
		ushort? majorImageVersion;

		public ushort? MinorImageVersion {
			get { return minorImageVersion; }
			set {
				if (minorImageVersion != value) {
					minorImageVersion = value;
					OnPropertyChanged("MinorImageVersion");
				}
			}
		}
		ushort? minorImageVersion;

		public ushort? MajorSubsystemVersion {
			get { return majorSubsystemVersion; }
			set {
				if (majorSubsystemVersion != value) {
					majorSubsystemVersion = value;
					OnPropertyChanged("MajorSubsystemVersion");
				}
			}
		}
		ushort? majorSubsystemVersion;

		public ushort? MinorSubsystemVersion {
			get { return minorSubsystemVersion; }
			set {
				if (minorSubsystemVersion != value) {
					minorSubsystemVersion = value;
					OnPropertyChanged("MinorSubsystemVersion");
				}
			}
		}
		ushort? minorSubsystemVersion;

		public uint? Win32VersionValue {
			get { return win32VersionValue; }
			set {
				if (win32VersionValue != value) {
					win32VersionValue = value;
					OnPropertyChanged("Win32VersionValue");
				}
			}
		}
		uint? win32VersionValue;

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

		bool? GetFlagValue(DllCharacteristics flag)
		{
			return DllCharacteristics == null ? (bool?)null : (DllCharacteristics.Value & flag) != 0;
		}

		void SetFlagValue(DllCharacteristics flag, bool? value)
		{
			if (DllCharacteristics == null)
				DllCharacteristics = 0;
			if (value ?? false)
				DllCharacteristics |= flag;
			else
				DllCharacteristics &= ~flag;
		}

		public ulong? SizeOfStackReserve {
			get { return sizeOfStackReserve; }
			set {
				if (sizeOfStackReserve != value) {
					sizeOfStackReserve = value;
					OnPropertyChanged("SizeOfStackReserve");
				}
			}
		}
		ulong? sizeOfStackReserve;

		public ulong? SizeOfStackCommit {
			get { return sizeOfStackCommit; }
			set {
				if (sizeOfStackCommit != value) {
					sizeOfStackCommit = value;
					OnPropertyChanged("SizeOfStackCommit");
				}
			}
		}
		ulong? sizeOfStackCommit;

		public ulong? SizeOfHeapReserve {
			get { return sizeOfHeapReserve; }
			set {
				if (sizeOfHeapReserve != value) {
					sizeOfHeapReserve = value;
					OnPropertyChanged("SizeOfHeapReserve");
				}
			}
		}
		ulong? sizeOfHeapReserve;

		public ulong? SizeOfHeapCommit {
			get { return sizeOfHeapCommit; }
			set {
				if (sizeOfHeapCommit != value) {
					sizeOfHeapCommit = value;
					OnPropertyChanged("SizeOfHeapCommit");
				}
			}
		}
		ulong? sizeOfHeapCommit;

		public uint? LoaderFlags {
			get { return loaderFlags; }
			set {
				if (loaderFlags != value) {
					loaderFlags = value;
					OnPropertyChanged("LoaderFlags");
				}
			}
		}
		uint? loaderFlags;

		public uint? NumberOfRvaAndSizes {
			get { return numberOfRvaAndSizes; }
			set {
				if (numberOfRvaAndSizes != value) {
					numberOfRvaAndSizes = value;
					OnPropertyChanged("NumberOfRvaAndSizes");
				}
			}
		}
		uint? numberOfRvaAndSizes;

		public void CopyTo(PEHeadersOptions options)
		{
			options.Machine = (dnlib.PE.Machine)machineVM.SelectedItem;
			options.TimeDateStamp = TimeDateStamp;
			options.Characteristics = Characteristics;
			options.MajorLinkerVersion = MajorLinkerVersion;
			options.MinorLinkerVersion = MinorLinkerVersion;
			options.ImageBase = ImageBase;
			options.SectionAlignment = SectionAlignment;
			options.FileAlignment = FileAlignment;
			options.MajorOperatingSystemVersion = MajorOperatingSystemVersion;
			options.MinorOperatingSystemVersion = MinorOperatingSystemVersion;
			options.MajorImageVersion = MajorImageVersion;
			options.MinorImageVersion = MinorImageVersion;
			options.MajorSubsystemVersion = MajorSubsystemVersion;
			options.MinorSubsystemVersion = MinorSubsystemVersion;
			options.Win32VersionValue = Win32VersionValue;
			options.Subsystem = (dnlib.PE.Subsystem)Subsystem.SelectedItem;
			options.DllCharacteristics = DllCharacteristics;
			options.SizeOfStackReserve = SizeOfStackReserve;
			options.SizeOfStackCommit = SizeOfStackCommit;
			options.SizeOfHeapReserve = SizeOfHeapReserve;
			options.SizeOfHeapCommit = SizeOfHeapCommit;
			options.LoaderFlags = LoaderFlags;
			options.NumberOfRvaAndSizes = NumberOfRvaAndSizes;
		}

		public void InitializeFrom(PEHeadersOptions options)
		{
			machineVM.SelectedItem = options.Machine ?? defaultMachine;
			TimeDateStamp = options.TimeDateStamp;
			Characteristics = options.Characteristics;
			MajorLinkerVersion = options.MajorLinkerVersion;
			MinorLinkerVersion = options.MinorLinkerVersion;
			ImageBase = options.ImageBase;
			SectionAlignment = options.SectionAlignment;
			FileAlignment = options.FileAlignment;
			MajorOperatingSystemVersion = options.MajorOperatingSystemVersion;
			MinorOperatingSystemVersion = options.MinorOperatingSystemVersion;
			MajorImageVersion = options.MajorImageVersion;
			MinorImageVersion = options.MinorImageVersion;
			MajorSubsystemVersion = options.MajorSubsystemVersion;
			MinorSubsystemVersion = options.MinorSubsystemVersion;
			Win32VersionValue = options.Win32VersionValue;
			Subsystem.SelectedItem = options.Subsystem ?? defaultSubsystem;
			DllCharacteristics = options.DllCharacteristics;
			SizeOfStackReserve = options.SizeOfStackReserve;
			SizeOfStackCommit = options.SizeOfStackCommit;
			SizeOfHeapReserve = options.SizeOfHeapReserve;
			SizeOfHeapCommit = options.SizeOfHeapCommit;
			LoaderFlags = options.LoaderFlags;
			NumberOfRvaAndSizes = options.NumberOfRvaAndSizes;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}

	sealed class Cor20HeaderOptionsVM : INotifyPropertyChanged
	{
		public ushort? MajorRuntimeVersion {
			get { return majorRuntimeVersion; }
			set {
				if (majorRuntimeVersion != value) {
					majorRuntimeVersion = value;
					OnPropertyChanged("MajorRuntimeVersion");
				}
			}
		}
		ushort? majorRuntimeVersion;

		public ushort? MinorRuntimeVersion {
			get { return minorRuntimeVersion; }
			set {
				if (minorRuntimeVersion != value) {
					minorRuntimeVersion = value;
					OnPropertyChanged("MinorRuntimeVersion");
				}
			}
		}
		ushort? minorRuntimeVersion;

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
					OnPropertyChanged("NativeEntryPoint");
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

		public bool? NativeEntryPoint {
			get { return GetFlagValue(ComImageFlags.NativeEntryPoint); }
			set { SetFlagValue(ComImageFlags.NativeEntryPoint, value); }
		}

		public bool? TrackDebugData {
			get { return GetFlagValue(ComImageFlags.TrackDebugData); }
			set { SetFlagValue(ComImageFlags.TrackDebugData, value); }
		}

		public bool? Bit32Preferred {
			get { return GetFlagValue(ComImageFlags._32BitPreferred); }
			set { SetFlagValue(ComImageFlags._32BitPreferred, value); }
		}

		bool? GetFlagValue(ComImageFlags flag)
		{
			return Flags == null ? (bool?)null : (Flags.Value & flag) != 0;
		}

		void SetFlagValue(ComImageFlags flag, bool? value)
		{
			if (Flags == null)
				Flags = 0;
			if (value ?? false)
				Flags |= flag;
			else
				Flags &= ~flag;
		}

		public uint? EntryPoint {
			get { return entryPoint; }
			set {
				if (entryPoint != value) {
					entryPoint = value;
					OnPropertyChanged("EntryPoint");
				}
			}
		}
		uint? entryPoint;

		public void CopyTo(Cor20HeaderOptions options)
		{
			options.MajorRuntimeVersion = MajorRuntimeVersion;
			options.MinorRuntimeVersion = MinorRuntimeVersion;
			options.Flags = Flags;
			options.EntryPoint = EntryPoint;
		}

		public void InitializeFrom(Cor20HeaderOptions options)
		{
			MajorRuntimeVersion = options.MajorRuntimeVersion;
			MinorRuntimeVersion = options.MinorRuntimeVersion;
			Flags = options.Flags;
			EntryPoint = options.EntryPoint;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}

	sealed class MetaDataOptionsVM : INotifyPropertyChanged
	{
		public MetaDataHeaderOptionsVM MetaDataHeaderOptions {
			get { return metaDataHeaderOptions; }
		}
		MetaDataHeaderOptionsVM metaDataHeaderOptions = new MetaDataHeaderOptionsVM();

		public TablesHeapOptionsVM TablesHeapOptions {
			get { return tablesHeapOptions; }
		}
		TablesHeapOptionsVM tablesHeapOptions = new TablesHeapOptionsVM();

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

		bool GetFlagValue(MetaDataFlags flag)
		{
			return (Flags & flag) != 0;
		}

		void SetFlagValue(MetaDataFlags flag, bool value, string prop1, string prop2 = null)
		{
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

		public void CopyTo(MetaDataOptions options)
		{
			MetaDataHeaderOptions.CopyTo(options.MetaDataHeaderOptions);
			TablesHeapOptions.CopyTo(options.TablesHeapOptions);
			options.Flags = Flags;
		}

		public void InitializeFrom(MetaDataOptions options)
		{
			MetaDataHeaderOptions.InitializeFrom(options.MetaDataHeaderOptions);
			TablesHeapOptions.InitializeFrom(options.TablesHeapOptions);
			Flags = options.Flags;
			OnFlagsChanged();
		}

		void OnFlagsChanged()
		{
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

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}

	sealed class MetaDataHeaderOptionsVM : INotifyPropertyChanged
	{
		public uint? Signature {
			get { return signature; }
			set {
				if (signature != value) {
					signature = value;
					OnPropertyChanged("Signature");
				}
			}
		}
		uint? signature;

		public ushort? MajorVersion {
			get { return majorVersion; }
			set {
				if (majorVersion != value) {
					majorVersion = value;
					OnPropertyChanged("MajorVersion");
				}
			}
		}
		ushort? majorVersion;

		public ushort? MinorVersion {
			get { return minorVersion; }
			set {
				if (minorVersion != value) {
					minorVersion = value;
					OnPropertyChanged("MinorVersion");
				}
			}
		}
		ushort? minorVersion;

		public uint? Reserved1 {
			get { return reserved1; }
			set {
				if (reserved1 != value) {
					reserved1 = value;
					OnPropertyChanged("Reserved1");
				}
			}
		}
		uint? reserved1;

		public string VersionString {
			get { return versionString; }
			set {
				if (versionString != value) {
					versionString = value;
					OnPropertyChanged("VersionString");
				}
			}
		}
		string versionString;

		public byte? StorageFlags {
			get { return storageFlags; }
			set {
				if (storageFlags != value) {
					storageFlags = value;
					OnPropertyChanged("StorageFlags");
				}
			}
		}
		byte? storageFlags;

		public byte? Reserved2 {
			get { return reserved2; }
			set {
				if (reserved2 != value) {
					reserved2 = value;
					OnPropertyChanged("Reserved2");
				}
			}
		}
		byte? reserved2;

		public void CopyTo(MetaDataHeaderOptions options)
		{
			options.Signature = Signature;
			options.MajorVersion = MajorVersion;
			options.MinorVersion = MinorVersion;
			options.Reserved1 = Reserved1;
			options.VersionString = string.IsNullOrEmpty(VersionString) ? null : VersionString;
			options.StorageFlags = (StorageFlags?)StorageFlags;
			options.Reserved2 = Reserved2;
		}

		public void InitializeFrom(MetaDataHeaderOptions options)
		{
			Signature = options.Signature;
			MajorVersion = options.MajorVersion;
			MinorVersion = options.MinorVersion;
			Reserved1 = options.Reserved1;
			VersionString = options.VersionString;
			StorageFlags = (byte?)options.StorageFlags;
			Reserved2 = options.Reserved2;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}

	sealed class TablesHeapOptionsVM : INotifyPropertyChanged
	{
		public uint? Reserved1 {
			get { return reserved1; }
			set {
				if (reserved1 != value) {
					reserved1 = value;
					OnPropertyChanged("Reserved1");
				}
			}
		}
		uint? reserved1;

		public byte? MajorVersion {
			get { return majorVersion; }
			set {
				if (majorVersion != value) {
					majorVersion = value;
					OnPropertyChanged("MajorVersion");
				}
			}
		}
		byte? majorVersion;

		public byte? MinorVersion {
			get { return minorVersion; }
			set {
				if (minorVersion != value) {
					minorVersion = value;
					OnPropertyChanged("MinorVersion");
				}
			}
		}
		byte? minorVersion;

		public bool? UseENC {
			get { return useENC; }
			set {
				if (useENC != value) {
					useENC = value;
					OnPropertyChanged("UseENC");
				}
			}
		}
		bool? useENC;

		public uint? ExtraData {
			get { return extraData; }
			set {
				if (extraData != value) {
					extraData = value;
					OnPropertyChanged("ExtraData");
				}
			}
		}
		uint? extraData;

		public bool? HasDeletedRows {
			get { return hasDeletedRows; }
			set {
				if (hasDeletedRows != value) {
					hasDeletedRows = value;
					OnPropertyChanged("HasDeletedRows");
				}
			}
		}
		bool? hasDeletedRows;

		public void CopyTo(TablesHeapOptions options)
		{
			options.Reserved1 = Reserved1;
			options.MajorVersion = MajorVersion;
			options.MinorVersion = MinorVersion;
			options.UseENC = UseENC;
			options.ExtraData = ExtraData;
			options.HasDeletedRows = HasDeletedRows;
		}

		public void InitializeFrom(TablesHeapOptions options)
		{
			Reserved1 = options.Reserved1;
			MajorVersion = options.MajorVersion;
			MinorVersion = options.MinorVersion;
			UseENC = options.UseENC;
			ExtraData = options.ExtraData;
			HasDeletedRows = options.HasDeletedRows;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}
}
