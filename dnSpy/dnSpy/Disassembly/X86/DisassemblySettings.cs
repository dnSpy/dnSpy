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
using System.ComponentModel;
using dnSpy.Contracts.Disassembly;
using dnSpy.Contracts.Settings;

namespace dnSpy.Disassembly.X86 {
	abstract class DisassemblySettings : IX86DisassemblySettings {
		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		public bool UpperCasePrefixes {
			get => upperCasePrefixes;
			set {
				if (value != upperCasePrefixes) {
					upperCasePrefixes = value;
					OnPropertyChanged(nameof(UpperCasePrefixes));
				}
			}
		}
		bool upperCasePrefixes;

		public bool UpperCaseMnemonics {
			get => upperCaseMnemonics;
			set {
				if (value != upperCaseMnemonics) {
					upperCaseMnemonics = value;
					OnPropertyChanged(nameof(UpperCaseMnemonics));
				}
			}
		}
		bool upperCaseMnemonics;

		public bool UpperCaseRegisters {
			get => upperCaseRegisters;
			set {
				if (value != upperCaseRegisters) {
					upperCaseRegisters = value;
					OnPropertyChanged(nameof(UpperCaseRegisters));
				}
			}
		}
		bool upperCaseRegisters;

		public bool UpperCaseKeywords {
			get => upperCaseKeywords;
			set {
				if (value != upperCaseKeywords) {
					upperCaseKeywords = value;
					OnPropertyChanged(nameof(UpperCaseKeywords));
				}
			}
		}
		bool upperCaseKeywords;

		public bool UpperCaseDecorators {
			get => upperCaseDecorators;
			set {
				if (value != upperCaseDecorators) {
					upperCaseDecorators = value;
					OnPropertyChanged(nameof(UpperCaseDecorators));
				}
			}
		}
		bool upperCaseDecorators;

		public bool UpperCaseAll {
			get => upperCaseAll;
			set {
				if (value != upperCaseAll) {
					upperCaseAll = value;
					OnPropertyChanged(nameof(UpperCaseAll));
				}
			}
		}
		bool upperCaseAll;

		public int FirstOperandCharIndex {
			get => firstOperandCharIndex;
			set {
				if (value != firstOperandCharIndex) {
					firstOperandCharIndex = value;
					OnPropertyChanged(nameof(FirstOperandCharIndex));
				}
			}
		}
		int firstOperandCharIndex = 8;

		public int TabSize {
			get => tabSize;
			set {
				if (value != tabSize) {
					tabSize = value;
					OnPropertyChanged(nameof(TabSize));
				}
			}
		}
		int tabSize;

		public bool SpaceAfterOperandSeparator {
			get => spaceAfterOperandSeparator;
			set {
				if (value != spaceAfterOperandSeparator) {
					spaceAfterOperandSeparator = value;
					OnPropertyChanged(nameof(SpaceAfterOperandSeparator));
				}
			}
		}
		bool spaceAfterOperandSeparator;

		public bool SpaceAfterMemoryBracket {
			get => spaceAfterMemoryBracket;
			set {
				if (value != spaceAfterMemoryBracket) {
					spaceAfterMemoryBracket = value;
					OnPropertyChanged(nameof(SpaceAfterMemoryBracket));
				}
			}
		}
		bool spaceAfterMemoryBracket;

		public bool SpaceBetweenMemoryAddOperators {
			get => spaceBetweenMemoryAddOperators;
			set {
				if (value != spaceBetweenMemoryAddOperators) {
					spaceBetweenMemoryAddOperators = value;
					OnPropertyChanged(nameof(SpaceBetweenMemoryAddOperators));
				}
			}
		}
		bool spaceBetweenMemoryAddOperators;

		public bool SpaceBetweenMemoryMulOperators {
			get => spaceBetweenMemoryMulOperators;
			set {
				if (value != spaceBetweenMemoryMulOperators) {
					spaceBetweenMemoryMulOperators = value;
					OnPropertyChanged(nameof(SpaceBetweenMemoryMulOperators));
				}
			}
		}
		bool spaceBetweenMemoryMulOperators;

		public bool ScaleBeforeIndex {
			get => scaleBeforeIndex;
			set {
				if (value != scaleBeforeIndex) {
					scaleBeforeIndex = value;
					OnPropertyChanged(nameof(ScaleBeforeIndex));
				}
			}
		}
		bool scaleBeforeIndex;

		public bool AlwaysShowScale {
			get => alwaysShowScale;
			set {
				if (value != alwaysShowScale) {
					alwaysShowScale = value;
					OnPropertyChanged(nameof(AlwaysShowScale));
				}
			}
		}
		bool alwaysShowScale;

		public bool AlwaysShowSegmentRegister {
			get => alwaysShowSegmentRegister;
			set {
				if (value != alwaysShowSegmentRegister) {
					alwaysShowSegmentRegister = value;
					OnPropertyChanged(nameof(AlwaysShowSegmentRegister));
				}
			}
		}
		bool alwaysShowSegmentRegister;

		public bool ShowZeroDisplacements {
			get => showZeroDisplacements;
			set {
				if (value != showZeroDisplacements) {
					showZeroDisplacements = value;
					OnPropertyChanged(nameof(ShowZeroDisplacements));
				}
			}
		}
		bool showZeroDisplacements;

		public string? HexPrefix {
			get => hexPrefix;
			set {
				if (value != hexPrefix) {
					hexPrefix = value;
					OnPropertyChanged(nameof(HexPrefix));
				}
			}
		}
		string? hexPrefix;

		public string? HexSuffix {
			get => hexSuffix;
			set {
				if (value != hexSuffix) {
					hexSuffix = value;
					OnPropertyChanged(nameof(HexSuffix));
				}
			}
		}
		string? hexSuffix;

		public int HexDigitGroupSize {
			get => hexDigitGroupSize;
			set {
				if (value != hexDigitGroupSize) {
					hexDigitGroupSize = value;
					OnPropertyChanged(nameof(HexDigitGroupSize));
				}
			}
		}
		int hexDigitGroupSize = 4;

		public string? DecimalPrefix {
			get => decimalPrefix;
			set {
				if (value != decimalPrefix) {
					decimalPrefix = value;
					OnPropertyChanged(nameof(DecimalPrefix));
				}
			}
		}
		string? decimalPrefix;

		public string? DecimalSuffix {
			get => decimalSuffix;
			set {
				if (value != decimalSuffix) {
					decimalSuffix = value;
					OnPropertyChanged(nameof(DecimalSuffix));
				}
			}
		}
		string? decimalSuffix;

		public int DecimalDigitGroupSize {
			get => decimalDigitGroupSize;
			set {
				if (value != decimalDigitGroupSize) {
					decimalDigitGroupSize = value;
					OnPropertyChanged(nameof(DecimalDigitGroupSize));
				}
			}
		}
		int decimalDigitGroupSize = 3;

		public string? OctalPrefix {
			get => octalPrefix;
			set {
				if (value != octalPrefix) {
					octalPrefix = value;
					OnPropertyChanged(nameof(OctalPrefix));
				}
			}
		}
		string? octalPrefix;

		public string? OctalSuffix {
			get => octalSuffix;
			set {
				if (value != octalSuffix) {
					octalSuffix = value;
					OnPropertyChanged(nameof(OctalSuffix));
				}
			}
		}
		string? octalSuffix;

		public int OctalDigitGroupSize {
			get => octalDigitGroupSize;
			set {
				if (value != octalDigitGroupSize) {
					octalDigitGroupSize = value;
					OnPropertyChanged(nameof(OctalDigitGroupSize));
				}
			}
		}
		int octalDigitGroupSize = 4;

		public string? BinaryPrefix {
			get => binaryPrefix;
			set {
				if (value != binaryPrefix) {
					binaryPrefix = value;
					OnPropertyChanged(nameof(BinaryPrefix));
				}
			}
		}
		string? binaryPrefix;

		public string? BinarySuffix {
			get => binarySuffix;
			set {
				if (value != binarySuffix) {
					binarySuffix = value;
					OnPropertyChanged(nameof(BinarySuffix));
				}
			}
		}
		string? binarySuffix;

		public int BinaryDigitGroupSize {
			get => binaryDigitGroupSize;
			set {
				if (value != binaryDigitGroupSize) {
					binaryDigitGroupSize = value;
					OnPropertyChanged(nameof(BinaryDigitGroupSize));
				}
			}
		}
		int binaryDigitGroupSize = 4;

		public string? DigitSeparator {
			get => digitSeparator;
			set {
				if (value != digitSeparator) {
					digitSeparator = value;
					OnPropertyChanged(nameof(DigitSeparator));
				}
			}
		}
		string? digitSeparator = null;

		public bool LeadingZeroes {
			get => leadingZeroes;
			set {
				if (value != leadingZeroes) {
					leadingZeroes = value;
					OnPropertyChanged(nameof(LeadingZeroes));
				}
			}
		}
		bool leadingZeroes;

		public bool UpperCaseHex {
			get => upperCaseHex;
			set {
				if (value != upperCaseHex) {
					upperCaseHex = value;
					OnPropertyChanged(nameof(UpperCaseHex));
				}
			}
		}
		bool upperCaseHex = true;

		public bool SmallHexNumbersInDecimal {
			get => smallHexNumbersInDecimal;
			set {
				if (value != smallHexNumbersInDecimal) {
					smallHexNumbersInDecimal = value;
					OnPropertyChanged(nameof(SmallHexNumbersInDecimal));
				}
			}
		}
		bool smallHexNumbersInDecimal = true;

		public bool AddLeadingZeroToHexNumbers {
			get => addLeadingZeroToHexNumbers;
			set {
				if (value != addLeadingZeroToHexNumbers) {
					addLeadingZeroToHexNumbers = value;
					OnPropertyChanged(nameof(AddLeadingZeroToHexNumbers));
				}
			}
		}
		bool addLeadingZeroToHexNumbers = true;

		public NumberBase NumberBase {
			get => numberBase;
			set {
				var newValue = value;
				if ((uint)newValue > (uint)NumberBase.Binary)
					newValue = NumberBase.Hexadecimal;
				if (newValue != numberBase) {
					numberBase = newValue;
					OnPropertyChanged(nameof(NumberBase));
				}
			}
		}
		NumberBase numberBase = NumberBase.Hexadecimal;

		public bool BranchLeadingZeroes {
			get => branchLeadingZeroes;
			set {
				if (value != branchLeadingZeroes) {
					branchLeadingZeroes = value;
					OnPropertyChanged(nameof(BranchLeadingZeroes));
				}
			}
		}
		bool branchLeadingZeroes;

		public bool SignedImmediateOperands {
			get => signedImmediateOperands;
			set {
				if (value != signedImmediateOperands) {
					signedImmediateOperands = value;
					OnPropertyChanged(nameof(SignedImmediateOperands));
				}
			}
		}
		bool signedImmediateOperands;

		public bool SignedMemoryDisplacements {
			get => signedMemoryDisplacements;
			set {
				if (value != signedMemoryDisplacements) {
					signedMemoryDisplacements = value;
					OnPropertyChanged(nameof(SignedMemoryDisplacements));
				}
			}
		}
		bool signedMemoryDisplacements = true;

		public bool DisplacementLeadingZeroes {
			get => displacementLeadingZeroes;
			set {
				if (value != displacementLeadingZeroes) {
					displacementLeadingZeroes = value;
					OnPropertyChanged(nameof(DisplacementLeadingZeroes));
				}
			}
		}
		bool displacementLeadingZeroes;

		public MemorySizeOptions MemorySizeOptions {
			get => memorySizeOptions;
			set {
				if (value != memorySizeOptions) {
					memorySizeOptions = value;
					OnPropertyChanged(nameof(MemorySizeOptions));
				}
			}
		}
		MemorySizeOptions memorySizeOptions = MemorySizeOptions.Minimum;

		public bool RipRelativeAddresses {
			get => ripRelativeAddresses;
			set {
				if (value != ripRelativeAddresses) {
					ripRelativeAddresses = value;
					OnPropertyChanged(nameof(RipRelativeAddresses));
				}
			}
		}
		bool ripRelativeAddresses;

		public bool ShowBranchSize {
			get => showBranchSize;
			set {
				if (value != showBranchSize) {
					showBranchSize = value;
					OnPropertyChanged(nameof(ShowBranchSize));
				}
			}
		}
		bool showBranchSize = true;

		public bool UsePseudoOps {
			get => usePseudoOps;
			set {
				if (value != usePseudoOps) {
					usePseudoOps = value;
					OnPropertyChanged(nameof(UsePseudoOps));
				}
			}
		}
		bool usePseudoOps = true;

		public bool ShowSymbolAddress {
			get => showSymbolAddress;
			set {
				if (value != showSymbolAddress) {
					showSymbolAddress = value;
					OnPropertyChanged(nameof(ShowSymbolAddress));
				}
			}
		}
		bool showSymbolAddress = true;

		protected void ReadSettings(ISettingsSection sect) {
			UpperCasePrefixes = sect.Attribute<bool?>(nameof(UpperCasePrefixes)) ?? UpperCasePrefixes;
			UpperCaseMnemonics = sect.Attribute<bool?>(nameof(UpperCaseMnemonics)) ?? UpperCaseMnemonics;
			UpperCaseRegisters = sect.Attribute<bool?>(nameof(UpperCaseRegisters)) ?? UpperCaseRegisters;
			UpperCaseKeywords = sect.Attribute<bool?>(nameof(UpperCaseKeywords)) ?? UpperCaseKeywords;
			UpperCaseDecorators = sect.Attribute<bool?>(nameof(UpperCaseDecorators)) ?? UpperCaseDecorators;
			UpperCaseAll = sect.Attribute<bool?>(nameof(UpperCaseAll)) ?? UpperCaseAll;
			FirstOperandCharIndex = sect.Attribute<int?>(nameof(FirstOperandCharIndex)) ?? FirstOperandCharIndex;
			TabSize = sect.Attribute<int?>(nameof(TabSize)) ?? TabSize;
			SpaceAfterOperandSeparator = sect.Attribute<bool?>(nameof(SpaceAfterOperandSeparator)) ?? SpaceAfterOperandSeparator;
			SpaceAfterMemoryBracket = sect.Attribute<bool?>(nameof(SpaceAfterMemoryBracket)) ?? SpaceAfterMemoryBracket;
			SpaceBetweenMemoryAddOperators = sect.Attribute<bool?>(nameof(SpaceBetweenMemoryAddOperators)) ?? SpaceBetweenMemoryAddOperators;
			SpaceBetweenMemoryMulOperators = sect.Attribute<bool?>(nameof(SpaceBetweenMemoryMulOperators)) ?? SpaceBetweenMemoryMulOperators;
			ScaleBeforeIndex = sect.Attribute<bool?>(nameof(ScaleBeforeIndex)) ?? ScaleBeforeIndex;
			AlwaysShowScale = sect.Attribute<bool?>(nameof(AlwaysShowScale)) ?? AlwaysShowScale;
			AlwaysShowSegmentRegister = sect.Attribute<bool?>(nameof(AlwaysShowSegmentRegister)) ?? AlwaysShowSegmentRegister;
			ShowZeroDisplacements = sect.Attribute<bool?>(nameof(ShowZeroDisplacements)) ?? ShowZeroDisplacements;
			HexPrefix = sect.Attribute<string>(nameof(HexPrefix)) ?? HexPrefix;
			HexSuffix = sect.Attribute<string>(nameof(HexSuffix)) ?? HexSuffix;
			HexDigitGroupSize = sect.Attribute<int?>(nameof(HexDigitGroupSize)) ?? HexDigitGroupSize;
			DecimalPrefix = sect.Attribute<string>(nameof(DecimalPrefix)) ?? DecimalPrefix;
			DecimalSuffix = sect.Attribute<string>(nameof(DecimalSuffix)) ?? DecimalSuffix;
			DecimalDigitGroupSize = sect.Attribute<int?>(nameof(DecimalDigitGroupSize)) ?? DecimalDigitGroupSize;
			OctalPrefix = sect.Attribute<string>(nameof(OctalPrefix)) ?? OctalPrefix;
			OctalSuffix = sect.Attribute<string>(nameof(OctalSuffix)) ?? OctalSuffix;
			OctalDigitGroupSize = sect.Attribute<int?>(nameof(OctalDigitGroupSize)) ?? OctalDigitGroupSize;
			BinaryPrefix = sect.Attribute<string>(nameof(BinaryPrefix)) ?? BinaryPrefix;
			BinarySuffix = sect.Attribute<string>(nameof(BinarySuffix)) ?? BinarySuffix;
			BinaryDigitGroupSize = sect.Attribute<int?>(nameof(BinaryDigitGroupSize)) ?? BinaryDigitGroupSize;
			DigitSeparator = sect.Attribute<string>(nameof(DigitSeparator)) ?? DigitSeparator;
			LeadingZeroes = sect.Attribute<bool?>(nameof(LeadingZeroes)) ?? LeadingZeroes;
			UpperCaseHex = sect.Attribute<bool?>(nameof(UpperCaseHex)) ?? UpperCaseHex;
			SmallHexNumbersInDecimal = sect.Attribute<bool?>(nameof(SmallHexNumbersInDecimal)) ?? SmallHexNumbersInDecimal;
			AddLeadingZeroToHexNumbers = sect.Attribute<bool?>(nameof(AddLeadingZeroToHexNumbers)) ?? AddLeadingZeroToHexNumbers;
			NumberBase = sect.Attribute<NumberBase?>(nameof(NumberBase)) ?? NumberBase;
			BranchLeadingZeroes = sect.Attribute<bool?>(nameof(BranchLeadingZeroes)) ?? BranchLeadingZeroes;
			SignedImmediateOperands = sect.Attribute<bool?>(nameof(SignedImmediateOperands)) ?? SignedImmediateOperands;
			SignedMemoryDisplacements = sect.Attribute<bool?>(nameof(SignedMemoryDisplacements)) ?? SignedMemoryDisplacements;
			DisplacementLeadingZeroes = sect.Attribute<bool?>(nameof(DisplacementLeadingZeroes)) ?? DisplacementLeadingZeroes;
			MemorySizeOptions = sect.Attribute<MemorySizeOptions?>(nameof(MemorySizeOptions)) ?? MemorySizeOptions;
			RipRelativeAddresses = sect.Attribute<bool?>(nameof(RipRelativeAddresses)) ?? RipRelativeAddresses;
			ShowBranchSize = sect.Attribute<bool?>(nameof(ShowBranchSize)) ?? ShowBranchSize;
			UsePseudoOps = sect.Attribute<bool?>(nameof(UsePseudoOps)) ?? UsePseudoOps;
			ShowSymbolAddress = sect.Attribute<bool?>(nameof(ShowSymbolAddress)) ?? ShowSymbolAddress;
		}

		protected void WriteSettings(ISettingsSection sect) {
			sect.Attribute(nameof(UpperCasePrefixes), UpperCasePrefixes);
			sect.Attribute(nameof(UpperCaseMnemonics), UpperCaseMnemonics);
			sect.Attribute(nameof(UpperCaseRegisters), UpperCaseRegisters);
			sect.Attribute(nameof(UpperCaseKeywords), UpperCaseKeywords);
			sect.Attribute(nameof(UpperCaseDecorators), UpperCaseDecorators);
			sect.Attribute(nameof(UpperCaseAll), UpperCaseAll);
			sect.Attribute(nameof(FirstOperandCharIndex), FirstOperandCharIndex);
			sect.Attribute(nameof(TabSize), TabSize);
			sect.Attribute(nameof(SpaceAfterOperandSeparator), SpaceAfterOperandSeparator);
			sect.Attribute(nameof(SpaceAfterMemoryBracket), SpaceAfterMemoryBracket);
			sect.Attribute(nameof(SpaceBetweenMemoryAddOperators), SpaceBetweenMemoryAddOperators);
			sect.Attribute(nameof(SpaceBetweenMemoryMulOperators), SpaceBetweenMemoryMulOperators);
			sect.Attribute(nameof(ScaleBeforeIndex), ScaleBeforeIndex);
			sect.Attribute(nameof(AlwaysShowScale), AlwaysShowScale);
			sect.Attribute(nameof(AlwaysShowSegmentRegister), AlwaysShowSegmentRegister);
			sect.Attribute(nameof(ShowZeroDisplacements), ShowZeroDisplacements);
			sect.Attribute(nameof(HexPrefix), HexPrefix);
			sect.Attribute(nameof(HexSuffix), HexSuffix);
			sect.Attribute(nameof(HexDigitGroupSize), HexDigitGroupSize);
			sect.Attribute(nameof(DecimalPrefix), DecimalPrefix);
			sect.Attribute(nameof(DecimalSuffix), DecimalSuffix);
			sect.Attribute(nameof(DecimalDigitGroupSize), DecimalDigitGroupSize);
			sect.Attribute(nameof(OctalPrefix), OctalPrefix);
			sect.Attribute(nameof(OctalSuffix), OctalSuffix);
			sect.Attribute(nameof(OctalDigitGroupSize), OctalDigitGroupSize);
			sect.Attribute(nameof(BinaryPrefix), BinaryPrefix);
			sect.Attribute(nameof(BinarySuffix), BinarySuffix);
			sect.Attribute(nameof(BinaryDigitGroupSize), BinaryDigitGroupSize);
			sect.Attribute(nameof(DigitSeparator), DigitSeparator);
			sect.Attribute(nameof(LeadingZeroes), LeadingZeroes);
			sect.Attribute(nameof(UpperCaseHex), UpperCaseHex);
			sect.Attribute(nameof(SmallHexNumbersInDecimal), SmallHexNumbersInDecimal);
			sect.Attribute(nameof(AddLeadingZeroToHexNumbers), AddLeadingZeroToHexNumbers);
			sect.Attribute(nameof(NumberBase), NumberBase);
			sect.Attribute(nameof(BranchLeadingZeroes), BranchLeadingZeroes);
			sect.Attribute(nameof(SignedImmediateOperands), SignedImmediateOperands);
			sect.Attribute(nameof(SignedMemoryDisplacements), SignedMemoryDisplacements);
			sect.Attribute(nameof(DisplacementLeadingZeroes), DisplacementLeadingZeroes);
			sect.Attribute(nameof(MemorySizeOptions), MemorySizeOptions);
			sect.Attribute(nameof(RipRelativeAddresses), RipRelativeAddresses);
			sect.Attribute(nameof(ShowBranchSize), ShowBranchSize);
			sect.Attribute(nameof(UsePseudoOps), UsePseudoOps);
			sect.Attribute(nameof(ShowSymbolAddress), ShowSymbolAddress);
		}

		protected DisassemblySettings CopyTo(DisassemblySettings other) {
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			other.UpperCasePrefixes = UpperCasePrefixes;
			other.UpperCaseMnemonics = UpperCaseMnemonics;
			other.UpperCaseRegisters = UpperCaseRegisters;
			other.UpperCaseKeywords = UpperCaseKeywords;
			other.UpperCaseDecorators = UpperCaseDecorators;
			other.UpperCaseAll = UpperCaseAll;
			other.FirstOperandCharIndex = FirstOperandCharIndex;
			other.TabSize = TabSize;
			other.SpaceAfterOperandSeparator = SpaceAfterOperandSeparator;
			other.SpaceAfterMemoryBracket = SpaceAfterMemoryBracket;
			other.SpaceBetweenMemoryAddOperators = SpaceBetweenMemoryAddOperators;
			other.SpaceBetweenMemoryMulOperators = SpaceBetweenMemoryMulOperators;
			other.ScaleBeforeIndex = ScaleBeforeIndex;
			other.AlwaysShowScale = AlwaysShowScale;
			other.AlwaysShowSegmentRegister = AlwaysShowSegmentRegister;
			other.ShowZeroDisplacements = ShowZeroDisplacements;
			other.HexPrefix = HexPrefix;
			other.HexSuffix = HexSuffix;
			other.HexDigitGroupSize = HexDigitGroupSize;
			other.DecimalPrefix = DecimalPrefix;
			other.DecimalSuffix = DecimalSuffix;
			other.DecimalDigitGroupSize = DecimalDigitGroupSize;
			other.OctalPrefix = OctalPrefix;
			other.OctalSuffix = OctalSuffix;
			other.OctalDigitGroupSize = OctalDigitGroupSize;
			other.BinaryPrefix = BinaryPrefix;
			other.BinarySuffix = BinarySuffix;
			other.BinaryDigitGroupSize = BinaryDigitGroupSize;
			other.DigitSeparator = DigitSeparator;
			other.LeadingZeroes = LeadingZeroes;
			other.UpperCaseHex = UpperCaseHex;
			other.SmallHexNumbersInDecimal = SmallHexNumbersInDecimal;
			other.AddLeadingZeroToHexNumbers = AddLeadingZeroToHexNumbers;
			other.NumberBase = NumberBase;
			other.BranchLeadingZeroes = BranchLeadingZeroes;
			other.SignedImmediateOperands = SignedImmediateOperands;
			other.SignedMemoryDisplacements = SignedMemoryDisplacements;
			other.DisplacementLeadingZeroes = DisplacementLeadingZeroes;
			other.MemorySizeOptions = MemorySizeOptions;
			other.RipRelativeAddresses = RipRelativeAddresses;
			other.ShowBranchSize = ShowBranchSize;
			other.UsePseudoOps = UsePseudoOps;
			other.ShowSymbolAddress = ShowSymbolAddress;
			return other;
		}
	}
}
