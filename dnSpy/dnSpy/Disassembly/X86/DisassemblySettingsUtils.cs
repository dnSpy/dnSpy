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

using System.Diagnostics;
using dnSpy.Contracts.Disassembly;
using Iced.Intel;

namespace dnSpy.Disassembly.X86 {
	static class DisassemblySettingsUtils {
		static Iced.Intel.NumberBase ToIcedNumberBase(Contracts.Disassembly.NumberBase numberBase) {
			switch (numberBase) {
			case Contracts.Disassembly.NumberBase.Hexadecimal: return Iced.Intel.NumberBase.Hexadecimal;
			case Contracts.Disassembly.NumberBase.Decimal: return Iced.Intel.NumberBase.Decimal;
			case Contracts.Disassembly.NumberBase.Octal: return Iced.Intel.NumberBase.Octal;
			case Contracts.Disassembly.NumberBase.Binary: return Iced.Intel.NumberBase.Binary;
			default:
				Debug.Fail($"Unknown number base: {numberBase}");
				return Iced.Intel.NumberBase.Hexadecimal;
			}
		}

		public static Iced.Intel.MemorySizeOptions ToMemorySizeOptions(Contracts.Disassembly.MemorySizeOptions memorySizeOptions) {
			switch (memorySizeOptions) {
			case Contracts.Disassembly.MemorySizeOptions.Default:	return Iced.Intel.MemorySizeOptions.Default;
			case Contracts.Disassembly.MemorySizeOptions.Always:	return Iced.Intel.MemorySizeOptions.Always;
			case Contracts.Disassembly.MemorySizeOptions.Minimum:	return Iced.Intel.MemorySizeOptions.Minimum;
			case Contracts.Disassembly.MemorySizeOptions.Never:		return Iced.Intel.MemorySizeOptions.Never;
			default:
				Debug.Fail($"Unknown mem size options: {memorySizeOptions}");
				return Iced.Intel.MemorySizeOptions.Default;
			}
		}

		static void CopyBase(FormatterOptions options, IX86DisassemblySettings settings) {
			options.OctalPrefix = settings.OctalPrefix;
			options.OctalSuffix = settings.OctalSuffix;
			options.OctalDigitGroupSize = settings.OctalDigitGroupSize;
			options.BinaryPrefix = settings.BinaryPrefix;
			options.BinarySuffix = settings.BinarySuffix;
			options.BinaryDigitGroupSize = settings.BinaryDigitGroupSize;
			options.DigitSeparator = settings.DigitSeparator;
			options.LeadingZeroes = settings.LeadingZeroes;
			options.UpperCaseHex = settings.UpperCaseHex;
			options.SmallHexNumbersInDecimal = settings.SmallHexNumbersInDecimal;
			options.AddLeadingZeroToHexNumbers = settings.AddLeadingZeroToHexNumbers;
			options.NumberBase = ToIcedNumberBase(settings.NumberBase);
			options.BranchLeadingZeroes = settings.BranchLeadingZeroes;
			options.SignedImmediateOperands = settings.SignedImmediateOperands;
			options.SignedMemoryDisplacements = settings.SignedMemoryDisplacements;
			options.DisplacementLeadingZeroes = settings.DisplacementLeadingZeroes;
			options.MemorySizeOptions = ToMemorySizeOptions(settings.MemorySizeOptions);
			options.RipRelativeAddresses = settings.RipRelativeAddresses;
			options.DecimalDigitGroupSize = settings.DecimalDigitGroupSize;
			options.ShowBranchSize = settings.ShowBranchSize;
			options.DecimalSuffix = settings.DecimalSuffix;
			options.HexDigitGroupSize = settings.HexDigitGroupSize;
			options.UpperCasePrefixes = settings.UpperCasePrefixes;
			options.UpperCaseMnemonics = settings.UpperCaseMnemonics;
			options.UpperCaseRegisters = settings.UpperCaseRegisters;
			options.UpperCaseKeywords = settings.UpperCaseKeywords;
			options.UpperCaseDecorators = settings.UpperCaseDecorators;
			options.UpperCaseAll = settings.UpperCaseAll;
			options.FirstOperandCharIndex = settings.FirstOperandCharIndex;
			options.TabSize = settings.TabSize;
			options.SpaceAfterOperandSeparator = settings.SpaceAfterOperandSeparator;
			options.SpaceAfterMemoryBracket = settings.SpaceAfterMemoryBracket;
			options.SpaceBetweenMemoryAddOperators = settings.SpaceBetweenMemoryAddOperators;
			options.SpaceBetweenMemoryMulOperators = settings.SpaceBetweenMemoryMulOperators;
			options.ScaleBeforeIndex = settings.ScaleBeforeIndex;
			options.AlwaysShowScale = settings.AlwaysShowScale;
			options.AlwaysShowSegmentRegister = settings.AlwaysShowSegmentRegister;
			options.ShowZeroDisplacements = settings.ShowZeroDisplacements;
			options.HexPrefix = settings.HexPrefix;
			options.HexSuffix = settings.HexSuffix;
			options.DecimalPrefix = settings.DecimalPrefix;
			options.UsePseudoOps = settings.UsePseudoOps;
			options.ShowSymbolAddress = settings.ShowSymbolAddress;
		}

		public static MasmFormatterOptions ToMasm(this IMasmDisassemblySettings settings) {
			var options = new MasmFormatterOptions();
			CopyBase(options, settings);
			return options;
		}

		public static NasmFormatterOptions ToNasm(this INasmDisassemblySettings settings) {
			var options = new NasmFormatterOptions();
			CopyBase(options, settings);
			options.ShowSignExtendedImmediateSize = settings.ShowSignExtendedImmediateSize;
			return options;
		}

		public static GasFormatterOptions ToGas(this IGasDisassemblySettings settings) {
			var options = new GasFormatterOptions();
			CopyBase(options, settings);
			options.NakedRegisters = settings.NakedRegisters;
			options.ShowMnemonicSizeSuffix = settings.ShowMnemonicSizeSuffix;
			options.SpaceAfterMemoryOperandComma = settings.SpaceAfterMemoryOperandComma;
			return options;
		}
	}
}
