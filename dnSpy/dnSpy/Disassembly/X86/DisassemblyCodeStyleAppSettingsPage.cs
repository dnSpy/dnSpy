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
using System.Collections.Generic;
using System.ComponentModel;
using dnSpy.Contracts.Disassembly;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using Iced.Intel;

namespace dnSpy.Disassembly.X86 {
	abstract class DisassemblyCodeStyleAppSettingsPage : AppSettingsPage {
		const ulong X86_RIP = 0x7FFF_FFFF_FFFF_FFF0;
		const ulong SYMBOLADDR = 0x5AA556789ABCDEF0UL;
		const string SYMBOLNAME = "secret_data";

		protected readonly DisassemblySettings _global_x86DisassemblySettings;
		protected readonly DisassemblySettings x86DisassemblySettings;
		readonly StringOutput x86Output;
		readonly Formatter formatter;
		readonly List<DisasmBooleanSetting> boolSettings;

		public IX86DisassemblySettings Settings => x86DisassemblySettings;
		public sealed override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_DISASSEMBLER_CODESTYLE);
		public sealed override object? UIObject => this;

		public DisasmBooleanSetting UseHexNumbers { get; }
		public DisasmBooleanSetting UppercasePrefixes { get; }
		public DisasmBooleanSetting UppercaseMnemonics { get; }
		public DisasmBooleanSetting UppercaseRegisters { get; }
		public DisasmBooleanSetting UppercaseKeywords { get; }
		public DisasmBooleanSetting UppercaseHex { get; }
		public DisasmBooleanSetting UppercaseAll { get; }
		public DisasmBooleanSetting SpaceAfterOperandSeparator { get; }
		public DisasmBooleanSetting SpaceAfterMemoryBracket { get; }
		public DisasmBooleanSetting SpaceBetweenMemoryAddOperators { get; }
		public DisasmBooleanSetting SpaceBetweenMemoryMulOperators { get; }
		public DisasmBooleanSetting ScaleBeforeIndex { get; }
		public DisasmBooleanSetting AlwaysShowScale { get; }
		public DisasmBooleanSetting AlwaysShowSegmentRegister { get; }
		public DisasmBooleanSetting ShowZeroDisplacements { get; }
		public DisasmBooleanSetting LeadingZeroes { get; }
		public DisasmBooleanSetting BranchLeadingZeroes { get; }
		public DisasmBooleanSetting SmallHexNumbersInDecimal { get; }
		public DisasmBooleanSetting AddLeadingZeroToHexNumbers { get; }
		public DisasmBooleanSetting SignedImmediateOperands { get; }
		public DisasmBooleanSetting SignedMemoryDisplacements { get; }
		public DisasmBooleanSetting AlwaysShowMemorySize { get; }
		public DisasmBooleanSetting RipRelativeAddresses { get; }
		public DisasmBooleanSetting ShowBranchSize { get; }
		public DisasmBooleanSetting UsePseudoOps { get; }
		public DisasmBooleanSetting ShowSymbolAddress { get; }
		public DisasmBooleanSetting GasNakedRegisters { get; }
		public DisasmBooleanSetting GasShowMnemonicSizeSuffix { get; }
		public DisasmBooleanSetting GasSpaceAfterMemoryOperandComma { get; }

		public Int32VM OperandColumnVM { get; }

		public string HexPrefix {
			get => x86DisassemblySettings.HexPrefix ?? string.Empty;
			set {
				if (value != x86DisassemblySettings.HexPrefix) {
					x86DisassemblySettings.HexPrefix = value;
					OnPropertyChanged(nameof(HexPrefix));
					RefreshDisassembly();
				}
			}
		}

		public string HexSuffix {
			get => x86DisassemblySettings.HexSuffix ?? string.Empty;
			set {
				if (value != x86DisassemblySettings.HexSuffix) {
					x86DisassemblySettings.HexSuffix = value;
					OnPropertyChanged(nameof(HexSuffix));
					RefreshDisassembly();
				}
			}
		}

		public string DigitSeparator {
			get => x86DisassemblySettings.DigitSeparator ?? string.Empty;
			set {
				if (value != x86DisassemblySettings.DigitSeparator) {
					x86DisassemblySettings.DigitSeparator = value;
					OnPropertyChanged(nameof(DigitSeparator));
					RefreshDisassembly();
				}
			}
		}

		protected sealed class SymbolResolver : Iced.Intel.ISymbolResolver {
			public static readonly Iced.Intel.ISymbolResolver Instance = new SymbolResolver();
			SymbolResolver() { }

			public bool TryGetSymbol(in Instruction instruction, int operand, int instructionOperand, ulong address, int addressSize, out SymbolResult symbol) {
				if (address == SYMBOLADDR) {
					symbol = new SymbolResult(SYMBOLADDR, new TextInfo(SYMBOLNAME, FormatterTextKind.Data), SymbolFlags.None);
					return true;
				}
				symbol = default;
				return false;
			}
		}

		protected DisassemblyCodeStyleAppSettingsPage(DisassemblySettings global_x86DisassemblySettings, DisassemblySettings x86DisassemblySettings, Formatter formatter) {
			_global_x86DisassemblySettings = global_x86DisassemblySettings ?? throw new ArgumentNullException(nameof(global_x86DisassemblySettings));
			this.x86DisassemblySettings = x86DisassemblySettings ?? throw new ArgumentNullException(nameof(x86DisassemblySettings));
			x86Output = new StringOutput();
			this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
			boolSettings = new List<DisasmBooleanSetting>();

			UseHexNumbers = AddDisasmBoolSetting(
				() => Settings.NumberBase == Contracts.Disassembly.NumberBase.Hexadecimal,
				value => Settings.NumberBase = value ? Contracts.Disassembly.NumberBase.Hexadecimal : Contracts.Disassembly.NumberBase.Decimal,
				Instruction.Create(Code.Mov_r64_imm64, Register.RDX, 0x123456789ABCDEF0));
			UppercasePrefixes = AddDisasmBoolSetting(() => Settings.UppercasePrefixes, value => Settings.UppercasePrefixes = value, Instruction.CreateMovsb(64, repPrefix: RepPrefixKind.Repe));
			UppercaseMnemonics = AddDisasmBoolSetting(() => Settings.UppercaseMnemonics, value => Settings.UppercaseMnemonics = value, Instruction.Create(Code.Xchg_r64_RAX, Register.RSI, Register.RAX));
			UppercaseRegisters = AddDisasmBoolSetting(() => Settings.UppercaseRegisters, value => Settings.UppercaseRegisters = value, Instruction.Create(Code.Xchg_r64_RAX, Register.RSI, Register.RAX));
			UppercaseKeywords = AddDisasmBoolSetting(() => Settings.UppercaseKeywords, value => Settings.UppercaseKeywords = value, Instruction.Create(Code.Mov_rm8_imm8, new MemoryOperand(Register.RCX, 4, 1), 0x5A));
			UppercaseHex = AddDisasmBoolSetting(() => Settings.UppercaseHex, value => Settings.UppercaseHex = value, Instruction.Create(Code.Mov_r64_imm64, Register.RDX, 0x123456789ABCDEF0));
			UppercaseAll = AddDisasmBoolSetting(() => Settings.UppercaseAll, value => Settings.UppercaseAll = value, Instruction.CreateMovsb(64, repPrefix: RepPrefixKind.Repe));
			SpaceAfterOperandSeparator = AddDisasmBoolSetting(() => Settings.SpaceAfterOperandSeparator, value => Settings.SpaceAfterOperandSeparator = value, Instruction.Create(Code.Shld_rm16_r16_CL, Register.DX, Register.AX, Register.CL));
			SpaceAfterMemoryBracket = AddDisasmBoolSetting(() => Settings.SpaceAfterMemoryBracket, value => Settings.SpaceAfterMemoryBracket = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			SpaceBetweenMemoryAddOperators = AddDisasmBoolSetting(() => Settings.SpaceBetweenMemoryAddOperators, value => Settings.SpaceBetweenMemoryAddOperators = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			SpaceBetweenMemoryMulOperators = AddDisasmBoolSetting(() => Settings.SpaceBetweenMemoryMulOperators, value => Settings.SpaceBetweenMemoryMulOperators = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			ScaleBeforeIndex = AddDisasmBoolSetting(() => Settings.ScaleBeforeIndex, value => Settings.ScaleBeforeIndex = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			AlwaysShowScale = AddDisasmBoolSetting(() => Settings.AlwaysShowScale, value => Settings.AlwaysShowScale = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 1, -0x12345678, 8, false, Register.None)));
			AlwaysShowSegmentRegister = AddDisasmBoolSetting(() => Settings.AlwaysShowSegmentRegister, value => Settings.AlwaysShowSegmentRegister = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			ShowZeroDisplacements = AddDisasmBoolSetting(() => Settings.ShowZeroDisplacements, value => Settings.ShowZeroDisplacements = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.None, 1, 0, 1, false, Register.None)));
			LeadingZeroes = AddDisasmBoolSetting(() => Settings.LeadingZeroes, value => Settings.LeadingZeroes = value, Instruction.Create(Code.Mov_rm32_imm32, Register.EDI, 0x123));
			BranchLeadingZeroes = AddDisasmBoolSetting(() => Settings.BranchLeadingZeroes, value => Settings.BranchLeadingZeroes = value, Instruction.CreateBranch(Code.Je_rel8_64, 0x12345), false);
			SmallHexNumbersInDecimal = AddDisasmBoolSetting(() => Settings.SmallHexNumbersInDecimal, value => Settings.SmallHexNumbersInDecimal = value, Instruction.Create(Code.Or_rm64_imm8, Register.RDX, 4));
			AddLeadingZeroToHexNumbers = AddDisasmBoolSetting(() => Settings.AddLeadingZeroToHexNumbers, value => Settings.AddLeadingZeroToHexNumbers = value, Instruction.Create(Code.Mov_rm8_imm8, Register.AL, 0xA5));
			SignedImmediateOperands = AddDisasmBoolSetting(() => Settings.SignedImmediateOperands, value => Settings.SignedImmediateOperands = value, Instruction.Create(Code.Or_rm64_imm8, Register.RDX, -0x12));
			SignedMemoryDisplacements = AddDisasmBoolSetting(() => Settings.SignedMemoryDisplacements, value => Settings.SignedMemoryDisplacements = value, Instruction.Create(Code.Push_rm64, new MemoryOperand(Register.RBP, Register.RDI, 4, -0x12345678, 8, false, Register.None)));
			AlwaysShowMemorySize = AddDisasmBoolSetting(() => Settings.MemorySizeOptions == Contracts.Disassembly.MemorySizeOptions.Always, value => Settings.MemorySizeOptions = value ? Contracts.Disassembly.MemorySizeOptions.Always : Contracts.Disassembly.MemorySizeOptions.Default, Instruction.Create(Code.Mov_rm64_r64, new MemoryOperand(Register.RAX, 0, 0), Register.RCX));
			RipRelativeAddresses = AddDisasmBoolSetting(() => Settings.RipRelativeAddresses, value => Settings.RipRelativeAddresses = value, Instruction.Create(Code.Inc_rm64, new MemoryOperand(Register.RIP, Register.None, 1, -0x12345678, 8)));
			ShowBranchSize = AddDisasmBoolSetting(() => Settings.ShowBranchSize, value => Settings.ShowBranchSize = value, Instruction.CreateBranch(Code.Je_rel8_64, X86_RIP + 5));
			UsePseudoOps = AddDisasmBoolSetting(() => Settings.UsePseudoOps, value => Settings.UsePseudoOps = value, Instruction.Create(Code.EVEX_Vcmpps_k_k1_ymm_ymmm256b32_imm8, Register.K3, Register.YMM2, Register.YMM27, 7));
			ShowSymbolAddress = AddDisasmBoolSetting(() => Settings.ShowSymbolAddress, value => Settings.ShowSymbolAddress = value, Instruction.Create(Code.Mov_r64_imm64, Register.RCX, SYMBOLADDR));
			GasNakedRegisters = AddDisasmBoolSetting(() => Settings.GasNakedRegisters, value => Settings.GasNakedRegisters = value, Instruction.Create(Code.Xchg_r64_RAX, Register.RSI, Register.RAX));
			GasShowMnemonicSizeSuffix = AddDisasmBoolSetting(() => Settings.GasShowMnemonicSizeSuffix, value => Settings.GasShowMnemonicSizeSuffix = value, Instruction.Create(Code.Xchg_r64_RAX, Register.RSI, Register.RAX));
			GasSpaceAfterMemoryOperandComma = AddDisasmBoolSetting(() => Settings.GasSpaceAfterMemoryOperandComma, value => Settings.GasSpaceAfterMemoryOperandComma = value, Instruction.Create(Code.Mov_rm64_r64, new MemoryOperand(Register.RAX, Register.RDI, 4, 0x12345678, 8), Register.RCX));

			OperandColumnVM = new Int32VM(x86DisassemblySettings.FirstOperandCharIndex + 1, a => {
				if (!OperandColumnVM.HasError)
					this.x86DisassemblySettings.FirstOperandCharIndex = OperandColumnVM.Value - 1;
			}, useDecimal: true) { Min = 1, Max = 100 };

			RefreshDisassembly();
		}

		protected DisasmBooleanSetting AddDisasmBoolSetting(Func<bool> getValue, Action<bool> setValue, Instruction instruction, bool fixRip = true) {
			if (fixRip)
				instruction.IP = X86_RIP;
			var boolSetting = new DisasmBooleanSetting(x86Output, getValue, setValue, formatter, instruction);
			boolSetting.PropertyChanged += DisasmBooleanSetting_PropertyChanged;
			boolSettings.Add(boolSetting);
			return boolSetting;
		}

		void DisasmBooleanSetting_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(DisasmBooleanSetting.Disassembly))
				return;
			RefreshDisassembly();
		}

		void RefreshDisassembly() {
			InitializeFormatterOptions(formatter.Options);
			foreach (var setting in boolSettings)
				setting.RaiseDisassemblyChanged();
		}

		void InitializeFormatterOptions(FormatterOptions options) {
			options.UppercasePrefixes = x86DisassemblySettings.UppercasePrefixes;
			options.UppercaseMnemonics = x86DisassemblySettings.UppercaseMnemonics;
			options.UppercaseRegisters = x86DisassemblySettings.UppercaseRegisters;
			options.UppercaseKeywords = x86DisassemblySettings.UppercaseKeywords;
			options.UppercaseDecorators = x86DisassemblySettings.UppercaseDecorators;
			options.UppercaseAll = x86DisassemblySettings.UppercaseAll;
			options.FirstOperandCharIndex = x86DisassemblySettings.FirstOperandCharIndex;
			options.TabSize = x86DisassemblySettings.TabSize;
			options.SpaceAfterOperandSeparator = x86DisassemblySettings.SpaceAfterOperandSeparator;
			options.SpaceAfterMemoryBracket = x86DisassemblySettings.SpaceAfterMemoryBracket;
			options.SpaceBetweenMemoryAddOperators = x86DisassemblySettings.SpaceBetweenMemoryAddOperators;
			options.SpaceBetweenMemoryMulOperators = x86DisassemblySettings.SpaceBetweenMemoryMulOperators;
			options.ScaleBeforeIndex = x86DisassemblySettings.ScaleBeforeIndex;
			options.AlwaysShowScale = x86DisassemblySettings.AlwaysShowScale;
			options.AlwaysShowSegmentRegister = x86DisassemblySettings.AlwaysShowSegmentRegister;
			options.ShowZeroDisplacements = x86DisassemblySettings.ShowZeroDisplacements;
			options.HexPrefix = x86DisassemblySettings.HexPrefix;
			options.HexSuffix = x86DisassemblySettings.HexSuffix;
			options.HexDigitGroupSize = x86DisassemblySettings.HexDigitGroupSize;
			options.DecimalPrefix = x86DisassemblySettings.DecimalPrefix;
			options.DecimalSuffix = x86DisassemblySettings.DecimalSuffix;
			options.DecimalDigitGroupSize = x86DisassemblySettings.DecimalDigitGroupSize;
			options.OctalPrefix = x86DisassemblySettings.OctalPrefix;
			options.OctalSuffix = x86DisassemblySettings.OctalSuffix;
			options.OctalDigitGroupSize = x86DisassemblySettings.OctalDigitGroupSize;
			options.BinaryPrefix = x86DisassemblySettings.BinaryPrefix;
			options.BinarySuffix = x86DisassemblySettings.BinarySuffix;
			options.BinaryDigitGroupSize = x86DisassemblySettings.BinaryDigitGroupSize;
			options.DigitSeparator = x86DisassemblySettings.DigitSeparator;
			options.LeadingZeroes = x86DisassemblySettings.LeadingZeroes;
			options.UppercaseHex = x86DisassemblySettings.UppercaseHex;
			options.SmallHexNumbersInDecimal = x86DisassemblySettings.SmallHexNumbersInDecimal;
			options.AddLeadingZeroToHexNumbers = x86DisassemblySettings.AddLeadingZeroToHexNumbers;
			options.NumberBase = UseHexNumbers.Value ? Iced.Intel.NumberBase.Hexadecimal : Iced.Intel.NumberBase.Decimal;
			options.BranchLeadingZeroes = x86DisassemblySettings.BranchLeadingZeroes;
			options.SignedImmediateOperands = x86DisassemblySettings.SignedImmediateOperands;
			options.SignedMemoryDisplacements = x86DisassemblySettings.SignedMemoryDisplacements;
			options.DisplacementLeadingZeroes = x86DisassemblySettings.DisplacementLeadingZeroes;
			options.MemorySizeOptions = DisassemblySettingsUtils.ToMemorySizeOptions(x86DisassemblySettings.MemorySizeOptions);
			options.RipRelativeAddresses = x86DisassemblySettings.RipRelativeAddresses;
			options.ShowBranchSize = x86DisassemblySettings.ShowBranchSize;
			options.UsePseudoOps = x86DisassemblySettings.UsePseudoOps;
			options.ShowSymbolAddress = x86DisassemblySettings.ShowSymbolAddress;
			options.GasNakedRegisters = x86DisassemblySettings.GasNakedRegisters;
			options.GasShowMnemonicSizeSuffix = x86DisassemblySettings.GasShowMnemonicSizeSuffix;
			options.GasSpaceAfterMemoryOperandComma = x86DisassemblySettings.GasSpaceAfterMemoryOperandComma;
			options.MasmAddDsPrefix32 = x86DisassemblySettings.MasmAddDsPrefix32;
			options.MasmDisplInBrackets = x86DisassemblySettings.MasmDisplInBrackets;
			options.MasmSymbolDisplInBrackets = x86DisassemblySettings.MasmSymbolDisplInBrackets;
			options.NasmShowSignExtendedImmediateSize = x86DisassemblySettings.NasmShowSignExtendedImmediateSize;

			// The options are only used to show an example so ignore these properties
			options.TabSize = 0;
			options.FirstOperandCharIndex = 0;
		}
	}
}
