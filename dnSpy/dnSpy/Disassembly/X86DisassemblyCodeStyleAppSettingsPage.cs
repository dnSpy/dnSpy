/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.Collections.Generic;
using System.ComponentModel;
using dnSpy.Contracts.Disassembly;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using Iced.Intel;

namespace dnSpy.Disassembly {
	abstract class X86DisassemblyCodeStyleAppSettingsPage : AppSettingsPage {
		const ulong X86_RIP = 0x7FFF_FFFF_FFFF_FFF0;
		protected readonly DisassemblySettings _global_disassemblySettings;
		protected readonly DisassemblySettings disassemblySettings;
		readonly StringBuilderFormatterOutput x86Output;
		readonly Formatter formatter;
		readonly List<X86DisasmBooleanSetting> boolSettings;

		public IDisassemblySettings Settings => disassemblySettings;
		public sealed override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_DISASSEMBLER_CODESTYLE);
		public sealed override object UIObject => this;

		public X86DisasmBooleanSetting UseHexNumbers { get; }
		public X86DisasmBooleanSetting UpperCasePrefixes { get; }
		public X86DisasmBooleanSetting UpperCaseMnemonics { get; }
		public X86DisasmBooleanSetting UpperCaseRegisters { get; }
		public X86DisasmBooleanSetting UpperCaseKeywords { get; }
		public X86DisasmBooleanSetting UpperCaseHex { get; }
		public X86DisasmBooleanSetting UpperCaseAll { get; }
		public X86DisasmBooleanSetting SpaceAfterOperandSeparator { get; }
		public X86DisasmBooleanSetting SpaceAfterMemoryBracket { get; }
		public X86DisasmBooleanSetting SpacesBetweenMemoryAddOperators { get; }
		public X86DisasmBooleanSetting SpacesBetweenMemoryMulOperators { get; }
		public X86DisasmBooleanSetting ScaleBeforeIndex { get; }
		public X86DisasmBooleanSetting AlwaysShowScale { get; }
		public X86DisasmBooleanSetting AlwaysShowSegmentRegister { get; }
		public X86DisasmBooleanSetting ShowZeroDisplacements { get; }
		public X86DisasmBooleanSetting ShortNumbers { get; }
		public X86DisasmBooleanSetting ShortBranchNumbers { get; }
		public X86DisasmBooleanSetting SmallHexNumbersInDecimal { get; }
		public X86DisasmBooleanSetting AddLeadingZeroToHexNumbers { get; }
		public X86DisasmBooleanSetting SignedImmediateOperands { get; }
		public X86DisasmBooleanSetting SignedMemoryDisplacements { get; }
		public X86DisasmBooleanSetting AlwaysShowMemorySize { get; }
		public X86DisasmBooleanSetting RipRelativeAddresses { get; }
		public X86DisasmBooleanSetting ShowBranchSize { get; }
		public X86DisasmBooleanSetting UsePseudoOps { get; }

		public Int32VM OperandColumnVM { get; }
		public Int32VM TabSizeVM { get; }

		public string HexPrefix {
			get => disassemblySettings.HexPrefix ?? string.Empty;
			set {
				if (value != disassemblySettings.HexPrefix) {
					disassemblySettings.HexPrefix = value;
					OnPropertyChanged(nameof(HexPrefix));
					RefreshDisassembly();
				}
			}
		}

		public string HexSuffix {
			get => disassemblySettings.HexSuffix ?? string.Empty;
			set {
				if (value != disassemblySettings.HexSuffix) {
					disassemblySettings.HexSuffix = value;
					OnPropertyChanged(nameof(HexSuffix));
					RefreshDisassembly();
				}
			}
		}

		public string DigitSeparator {
			get => disassemblySettings.DigitSeparator ?? string.Empty;
			set {
				if (value != disassemblySettings.DigitSeparator) {
					disassemblySettings.DigitSeparator = value;
					disassemblySettings.AddDigitSeparators = !string.IsNullOrEmpty(value);
					OnPropertyChanged(nameof(DigitSeparator));
					RefreshDisassembly();
				}
			}
		}

		protected X86DisassemblyCodeStyleAppSettingsPage(DisassemblySettings global_disassemblySettings, DisassemblySettings disassemblySettings, Formatter formatter) {
			_global_disassemblySettings = global_disassemblySettings ?? throw new ArgumentNullException(nameof(global_disassemblySettings));
			this.disassemblySettings = disassemblySettings ?? throw new ArgumentNullException(nameof(disassemblySettings));
			x86Output = new StringBuilderFormatterOutput();
			this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
			boolSettings = new List<X86DisasmBooleanSetting>();

			UseHexNumbers = AddDisasmBoolSetting(
				() => Settings.NumberBase == Contracts.Disassembly.NumberBase.Hexadecimal,
				value => Settings.NumberBase = value ? Contracts.Disassembly.NumberBase.Hexadecimal : Contracts.Disassembly.NumberBase.Decimal,
				Instruction.Create(Code.Mov_r64_imm64, Register.RDX, 0x123456789ABCDEF0));
			var prefixInstr = Instruction.CreateString_ESRDI_SegRSI(Code.Movsb_m8_m8, Register.RDI, Register.RSI);
			prefixInstr.HasPrefixRepe = true;
			UpperCasePrefixes = AddDisasmBoolSetting(() => Settings.UpperCasePrefixes, value => Settings.UpperCasePrefixes = value, prefixInstr);
			UpperCaseMnemonics = AddDisasmBoolSetting(() => Settings.UpperCaseMnemonics, value => Settings.UpperCaseMnemonics = value, Instruction.Create(Code.Xchg_r64_RAX, Register.RSI, Register.RAX));
			UpperCaseRegisters = AddDisasmBoolSetting(() => Settings.UpperCaseRegisters, value => Settings.UpperCaseRegisters = value, Instruction.Create(Code.Xchg_r64_RAX, Register.RSI, Register.RAX));
			UpperCaseKeywords = AddDisasmBoolSetting(() => Settings.UpperCaseKeywords, value => Settings.UpperCaseKeywords = value, Instruction.Create(Code.Mov_rm8_imm8, new MemoryOperand(Register.RCX, 4, 1), 0x5A));
			UpperCaseHex = AddDisasmBoolSetting(() => Settings.UpperCaseHex, value => Settings.UpperCaseHex = value, Instruction.Create(Code.Mov_r64_imm64, Register.RDX, 0x123456789ABCDEF0));
			UpperCaseAll = AddDisasmBoolSetting(() => Settings.UpperCaseAll, value => Settings.UpperCaseAll = value, prefixInstr);
			SpaceAfterOperandSeparator = AddDisasmBoolSetting(() => Settings.SpaceAfterOperandSeparator, value => Settings.SpaceAfterOperandSeparator = value, Instruction.Create(Code.Shld_rm16_r16_CL, Register.DX, Register.AX, Register.CL));
			SpaceAfterMemoryBracket = AddDisasmBoolSetting(() => Settings.SpaceAfterMemoryBracket, value => Settings.SpaceAfterMemoryBracket = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			SpacesBetweenMemoryAddOperators = AddDisasmBoolSetting(() => Settings.SpacesBetweenMemoryAddOperators, value => Settings.SpacesBetweenMemoryAddOperators = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			SpacesBetweenMemoryMulOperators = AddDisasmBoolSetting(() => Settings.SpacesBetweenMemoryMulOperators, value => Settings.SpacesBetweenMemoryMulOperators = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			ScaleBeforeIndex = AddDisasmBoolSetting(() => Settings.ScaleBeforeIndex, value => Settings.ScaleBeforeIndex = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			AlwaysShowScale = AddDisasmBoolSetting(() => Settings.AlwaysShowScale, value => Settings.AlwaysShowScale = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 1, -0x12345678, 8, false, Register.None)));
			AlwaysShowSegmentRegister = AddDisasmBoolSetting(() => Settings.AlwaysShowSegmentRegister, value => Settings.AlwaysShowSegmentRegister = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			ShowZeroDisplacements = AddDisasmBoolSetting(() => Settings.ShowZeroDisplacements, value => Settings.ShowZeroDisplacements = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.None, 1, 0, 1, false, Register.None)));
			ShortNumbers = AddDisasmBoolSetting(() => Settings.ShortNumbers, value => Settings.ShortNumbers = value, Instruction.Create(Code.Mov_rm32_imm32, Register.EDI, 0x123));
			ShortBranchNumbers = AddDisasmBoolSetting(() => Settings.ShortBranchNumbers, value => Settings.ShortBranchNumbers = value, Instruction.CreateBranch(Code.Je_rel8_64, 0x12345), false);
			SmallHexNumbersInDecimal = AddDisasmBoolSetting(() => Settings.SmallHexNumbersInDecimal, value => Settings.SmallHexNumbersInDecimal = value, Instruction.Create(Code.Or_rm64_imm8, Register.RDX, 4));
			AddLeadingZeroToHexNumbers = AddDisasmBoolSetting(() => Settings.AddLeadingZeroToHexNumbers, value => Settings.AddLeadingZeroToHexNumbers = value, Instruction.Create(Code.Mov_rm8_imm8, Register.AL, 0xA5));
			SignedImmediateOperands = AddDisasmBoolSetting(() => Settings.SignedImmediateOperands, value => Settings.SignedImmediateOperands = value, Instruction.Create(Code.Or_rm64_imm8, Register.RDX, -0x1234));
			SignedMemoryDisplacements = AddDisasmBoolSetting(() => Settings.SignedMemoryDisplacements, value => Settings.SignedMemoryDisplacements = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			AlwaysShowMemorySize = AddDisasmBoolSetting(() => Settings.AlwaysShowMemorySize, value => Settings.AlwaysShowMemorySize = value, Instruction.Create(Code.Mov_rm64_r64, new MemoryOperand(Register.RAX, 0, 0), Register.RCX));
			RipRelativeAddresses = AddDisasmBoolSetting(() => Settings.RipRelativeAddresses, value => Settings.RipRelativeAddresses = value, Instruction.Create(Code.Inc_rm64, new MemoryOperand(Register.RIP, Register.None, 1, -0x12345678, 8)));
			ShowBranchSize = AddDisasmBoolSetting(() => Settings.ShowBranchSize, value => Settings.ShowBranchSize = value, Instruction.CreateBranch(Code.Je_rel8_64, X86_RIP + 5));
			UsePseudoOps = AddDisasmBoolSetting(() => Settings.UsePseudoOps, value => Settings.UsePseudoOps = value, Instruction.Create(Code.EVEX_Vcmpps_k_k1_ymm_ymmm256b32_imm8, Register.K3, Register.YMM2, Register.YMM27, 7));

			OperandColumnVM = new Int32VM(disassemblySettings.FirstOperandCharIndex + 1, a => {
				if (!OperandColumnVM.HasError)
					this.disassemblySettings.FirstOperandCharIndex = OperandColumnVM.Value - 1;
			}, useDecimal: true) { Min = 1, Max = 100 };
			TabSizeVM = new Int32VM(disassemblySettings.TabSize, a => {
				if (!TabSizeVM.HasError)
					this.disassemblySettings.TabSize = TabSizeVM.Value;
			}, useDecimal: true) { Min = 0, Max = 100 };

			if (!disassemblySettings.AddDigitSeparators)
				DigitSeparator = null;

			RefreshDisassembly();
		}

		protected X86DisasmBooleanSetting AddDisasmBoolSetting(Func<bool> getValue, Action<bool> setValue, Instruction instruction, bool fixRip = true) {
			if (fixRip)
				instruction.IP64 = X86_RIP;
			var boolSetting = new X86DisasmBooleanSetting(x86Output, getValue, setValue, formatter, instruction);
			boolSetting.PropertyChanged += DisasmBooleanSetting_PropertyChanged;
			boolSettings.Add(boolSetting);
			return boolSetting;
		}

		void DisasmBooleanSetting_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(X86DisasmBooleanSetting.Disassembly))
				return;
			RefreshDisassembly();
		}

		void RefreshDisassembly() {
			InitializeFormatterOptions(formatter.Options);
			foreach (var setting in boolSettings)
				setting.RaiseDisassemblyChanged();
		}

		void InitializeFormatterOptions(FormatterOptions options) {
			InitializeFormatterOptionsCore(options);

			options.UpperCasePrefixes = disassemblySettings.UpperCasePrefixes;
			options.UpperCaseMnemonics = disassemblySettings.UpperCaseMnemonics;
			options.UpperCaseRegisters = disassemblySettings.UpperCaseRegisters;
			options.UpperCaseKeywords = disassemblySettings.UpperCaseKeywords;
			options.UpperCaseOther = disassemblySettings.UpperCaseOther;
			options.UpperCaseAll = disassemblySettings.UpperCaseAll;
			options.FirstOperandCharIndex = disassemblySettings.FirstOperandCharIndex;
			options.TabSize = disassemblySettings.TabSize;
			options.SpaceAfterOperandSeparator = disassemblySettings.SpaceAfterOperandSeparator;
			options.SpaceAfterMemoryOpenBracket = disassemblySettings.SpaceAfterMemoryBracket;
			options.SpaceBeforeMemoryCloseBracket = disassemblySettings.SpaceAfterMemoryBracket;
			options.SpacesBetweenMemoryAddOperators = disassemblySettings.SpacesBetweenMemoryAddOperators;
			options.SpacesBetweenMemoryMulOperators = disassemblySettings.SpacesBetweenMemoryMulOperators;
			options.ScaleBeforeIndex = disassemblySettings.ScaleBeforeIndex;
			options.AlwaysShowScale = disassemblySettings.AlwaysShowScale;
			options.AlwaysShowSegmentRegister = disassemblySettings.AlwaysShowSegmentRegister;
			options.ShowZeroDisplacements = disassemblySettings.ShowZeroDisplacements;
			options.HexPrefix = disassemblySettings.HexPrefix;
			options.HexSuffix = disassemblySettings.HexSuffix;
			options.HexDigitGroupSize = disassemblySettings.HexDigitGroupSize;
			options.DecimalPrefix = disassemblySettings.DecimalPrefix;
			options.DecimalSuffix = disassemblySettings.DecimalSuffix;
			options.DecimalDigitGroupSize = disassemblySettings.DecimalDigitGroupSize;
			options.OctalPrefix = disassemblySettings.OctalPrefix;
			options.OctalSuffix = disassemblySettings.OctalSuffix;
			options.OctalDigitGroupSize = disassemblySettings.OctalDigitGroupSize;
			options.BinaryPrefix = disassemblySettings.BinaryPrefix;
			options.BinarySuffix = disassemblySettings.BinarySuffix;
			options.BinaryDigitGroupSize = disassemblySettings.BinaryDigitGroupSize;
			options.DigitSeparator = disassemblySettings.DigitSeparator;
			options.AddDigitSeparators = disassemblySettings.AddDigitSeparators;
			options.ShortNumbers = disassemblySettings.ShortNumbers;
			options.UpperCaseHex = disassemblySettings.UpperCaseHex;
			options.SmallHexNumbersInDecimal = disassemblySettings.SmallHexNumbersInDecimal;
			options.AddLeadingZeroToHexNumbers = disassemblySettings.AddLeadingZeroToHexNumbers;
			options.NumberBase = UseHexNumbers.Value ? Iced.Intel.NumberBase.Hexadecimal : Iced.Intel.NumberBase.Decimal;
			options.ShortBranchNumbers = disassemblySettings.ShortBranchNumbers;
			options.SignedImmediateOperands = disassemblySettings.SignedImmediateOperands;
			options.SignedMemoryDisplacements = disassemblySettings.SignedMemoryDisplacements;
			options.SignExtendMemoryDisplacements = disassemblySettings.SignExtendMemoryDisplacements;
			options.AlwaysShowMemorySize = disassemblySettings.AlwaysShowMemorySize;
			options.RipRelativeAddresses = disassemblySettings.RipRelativeAddresses;
			options.ShowBranchSize = disassemblySettings.ShowBranchSize;
			options.UsePseudoOps = disassemblySettings.UsePseudoOps;

			// The options are only used to show an example so ignore these properties
			options.TabSize = 0;
			options.FirstOperandCharIndex = 0;
		}

		protected abstract void InitializeFormatterOptionsCore(FormatterOptions options);
	}
}
