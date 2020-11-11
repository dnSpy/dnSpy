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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using dnSpy.Contracts.Disassembly;
using dnSpy.Contracts.Disassembly.Viewer;
using dnSpy.Contracts.Text;
using dnSpy.Disassembly.X86;
using dnSpy.Properties;
using Iced.Intel;

namespace dnSpy.Disassembly.Viewer.X86 {
	static class DisassemblyContentGenerator {
		const int HEXBYTES_COLUMN_BYTE_LENGTH = 10;

		sealed class AsmReferenceFactory {
			readonly Dictionary<(FormatterTextKind kind, string value), AsmReference> refDict = new Dictionary<(FormatterTextKind kind, string value), AsmReference>();
			readonly Dictionary<(Code code, string mnemonic, CpuidFeature[] cpuidFeatures), MnemonicReference> mnemonicDict = new Dictionary<(Code code, string mnemonic, CpuidFeature[] cpuidFeatures), MnemonicReference>(new MnemonicComparer());

			sealed class MnemonicComparer : IEqualityComparer<(Code code, string mnemonic, CpuidFeature[] cpuidFeatures)> {
				public bool Equals([AllowNull] (Code code, string mnemonic, CpuidFeature[] cpuidFeatures) x, [AllowNull] (Code code, string mnemonic, CpuidFeature[] cpuidFeatures) y) {
					if (x.code != y.code)
						return false;
					if (x.mnemonic != y.mnemonic)
						return false;
					var xc = x.cpuidFeatures;
					var yc = y.cpuidFeatures;
					if (xc.Length != yc.Length)
						return false;
					for (int i = 0; i < xc.Length; i++) {
						if (xc[i] != yc[i])
							return false;
					}
					return true;
				}

				public int GetHashCode([DisallowNull] (Code code, string mnemonic, CpuidFeature[] cpuidFeatures) obj) =>
					(int)obj.code ^ StringComparer.Ordinal.GetHashCode(obj.mnemonic);
			}

			public AsmReference Create(FormatterTextKind kind, string value) {
				var key = (kind, value);
				if (!refDict.TryGetValue(key, out var asmRef))
					refDict[key] = asmRef = new AsmReference(kind, value);
				return asmRef;
			}

			public MnemonicReference Create(in Instruction instruction, string mnemonic) {
				var key = (instruction.Code, mnemonic, instruction.CpuidFeatures);
				if (!mnemonicDict.TryGetValue(key, out var mnemonicRef))
					mnemonicDict[key] = mnemonicRef = new MnemonicReference(instruction.Code, mnemonic, instruction.CpuidFeatures);
				return mnemonicRef;
			}
		}

		sealed class AsmReference {
			readonly FormatterTextKind kind;
			readonly string value;
			public AsmReference(FormatterTextKind kind, string value) {
				this.kind = kind;
				this.value = value;
			}
			public override bool Equals(object? obj) => obj is AsmReference other && kind == other.kind && StringComparer.Ordinal.Equals(value, other.value);
			public override int GetHashCode() => (int)kind ^ StringComparer.Ordinal.GetHashCode(value ?? string.Empty);
		}

		sealed class FormatterOutputImpl : FormatterOutput {
			readonly AsmReferenceFactory refFactory;
			readonly DisassemblyContentOutput output;

			public FormatterOutputImpl(AsmReferenceFactory refFactory, DisassemblyContentOutput output) {
				this.refFactory = refFactory;
				this.output = output;
			}

			public override void Write(string text, FormatterTextKind kind) {
				var color = GetColor(kind);
				switch (kind) {
				case FormatterTextKind.Directive:
				case FormatterTextKind.Prefix:
				case FormatterTextKind.Mnemonic:
				case FormatterTextKind.Keyword:
				case FormatterTextKind.Register:
				case FormatterTextKindExtensions.UnknownSymbol:
				case FormatterTextKind.Data:
				case FormatterTextKind.Label:
				case FormatterTextKind.Function:
				case FormatterTextKind.Decorator:
					output.Write(text, refFactory.Create(kind, text), DisassemblyReferenceFlags.Local, color);
					break;

				default:
					output.Write(text, color);
					break;
				}
			}

			public override void WriteNumber(in Instruction instruction, int operand, int instructionOperand, string text, ulong value, NumberKind numberKind, FormatterTextKind kind) {
				var color = GetColor(kind);
				const DisassemblyReferenceFlags flags = DisassemblyReferenceFlags.Local | DisassemblyReferenceFlags.NoFollow;
				output.Write(text, GetValue(value, numberKind), flags, color);
			}

			static object GetValue(ulong value, NumberKind numberKind) {
				switch (numberKind) {
				case NumberKind.Int8:	return (sbyte)value;
				case NumberKind.UInt8:	return (byte)value;
				case NumberKind.Int16:	return (short)value;
				case NumberKind.UInt16:	return (ushort)value;
				case NumberKind.Int32:	return (int)value;
				case NumberKind.UInt32:	return (uint)value;
				case NumberKind.Int64:	return (long)value;
				case NumberKind.UInt64:	return (ulong)value;
				default:
					Debug.Fail($"Unknown number kind: {numberKind}");
					throw new ArgumentOutOfRangeException(nameof(numberKind), $"Unknown number kind: {numberKind}");
				}
			}

			public override void WriteMnemonic(in Instruction instruction, string text) {
				var color = GetColor(FormatterTextKind.Mnemonic);
				output.Write(text, refFactory.Create(instruction, text), DisassemblyReferenceFlags.Local, color);
			}
		}

		static object GetColor(FormatterTextKind kind) {
			switch (kind) {
			case FormatterTextKind.Text:
				return BoxedTextColor.Text;
			case FormatterTextKind.Directive:
				return BoxedTextColor.AsmDirective;
			case FormatterTextKind.Prefix:
				return BoxedTextColor.AsmPrefix;
			case FormatterTextKind.Mnemonic:
				return BoxedTextColor.AsmMnemonic;
			case FormatterTextKind.Keyword:
				return BoxedTextColor.AsmKeyword;
			case FormatterTextKind.Operator:
				return BoxedTextColor.AsmOperator;
			case FormatterTextKind.Punctuation:
				return BoxedTextColor.AsmPunctuation;
			case FormatterTextKind.Number:
				return BoxedTextColor.AsmNumber;
			case FormatterTextKind.Register:
				return BoxedTextColor.AsmRegister;
			case FormatterTextKind.SelectorValue:
				return BoxedTextColor.AsmSelectorValue;
			case FormatterTextKind.LabelAddress:
				return BoxedTextColor.AsmLabelAddress;
			case FormatterTextKind.FunctionAddress:
				return BoxedTextColor.AsmFunctionAddress;
			case FormatterTextKindExtensions.UnknownSymbol:
				return BoxedTextColor.AsmLabel;
			case FormatterTextKind.Data:
				return BoxedTextColor.AsmData;
			case FormatterTextKind.Label:
				return BoxedTextColor.AsmLabel;
			case FormatterTextKind.Function:
				return BoxedTextColor.AsmFunction;
			case FormatterTextKind.Decorator:
				return BoxedTextColor.Text;
			default:
				Debug.Fail($"Unknown output kind: {kind}");
				return BoxedTextColor.Error;
			}
		}

		static readonly char[] newlineChar = new[] { '\n' };
		static void WriteComment(DisassemblyContentOutput output, string commentPrefix, string text) {
			var lines = text.Replace("\r\n", "\n").Split(newlineChar);
			for (int i = 0; i < lines.Length; i++) {
				var line = lines[i];
				if (i + 1 == lines.Length && line.Length == 0)
					break;
				if (line.Length == 0)
					output.Write(commentPrefix, BoxedTextColor.AsmComment);
				else
					output.Write(commentPrefix + " " + line, BoxedTextColor.AsmComment);
				output.Write(Environment.NewLine, BoxedTextColor.Text);
			}
		}

		static string? GetName(NativeVariableInfo[] variableInfo, in X86Variable varInfo) {
			foreach (var info in variableInfo) {
				if (info.IsLocal == varInfo.IsLocal && info.Index == varInfo.Index)
					return info.Name;
			}
			return null;
		}

		internal const string LINE = "********************************************";

		public static void Write(int bitness, DisassemblyContentOutput output, string? header, NativeCodeOptimization optimization, Formatter formatter, string commentPrefix, InternalFormatterOptions formatterOptions, Block[] blocks, X86NativeCodeInfo? codeInfo, NativeVariableInfo[]? variableInfo, string? methodName, string? moduleName) {
			if (variableInfo is null)
				variableInfo = Array.Empty<NativeVariableInfo>();
			if (optimization == NativeCodeOptimization.Unoptimized) {
				WriteComment(output, commentPrefix, LINE);
				WriteComment(output, commentPrefix, dnSpy_Resources.Disassembly_MethodIsNotOptimized);
				WriteComment(output, commentPrefix, LINE);
				output.Write(Environment.NewLine, BoxedTextColor.Text);
			}
			if (header is not null) {
				WriteComment(output, commentPrefix, header);
				output.Write(Environment.NewLine, BoxedTextColor.Text);
			}

			if (moduleName is not null)
				WriteComment(output, commentPrefix, moduleName);
			if (methodName is not null)
				WriteComment(output, commentPrefix, methodName);
			WriteComment(output, commentPrefix, GetCodeSizeString(blocks));
			output.Write(Environment.NewLine, BoxedTextColor.Text);

			bool uppercaseHex = (formatterOptions & InternalFormatterOptions.UppercaseHex) != 0;
			var variables = codeInfo?.Variables ?? Array.Empty<X86Variable>();
			if (variables.Length != 0) {
				var sb = new System.Text.StringBuilder();
				foreach (var varInfo in variables) {
					bool printedName = false;
					if (varInfo.Index >= 0) {
						printedName = true;
						sb.Append(varInfo.IsLocal ? "local" : "arg");
						sb.Append(" #");
						sb.Append(varInfo.Index);
						sb.Append(' ');
					}
					var name = varInfo.Name ?? GetName(variableInfo, varInfo);
					if (name is not null) {
						printedName = true;
						if (varInfo.Index >= 0)
							sb.Append('(');
						sb.Append(name);
						if (varInfo.Index >= 0)
							sb.Append(')');
						sb.Append(' ');
					}
					if (!printedName) {
						sb.Append("???");
						sb.Append(' ');
					}
					sb.Append(FormatAddress(bitness, varInfo.LiveAddress, uppercaseHex));
					sb.Append('-');
					sb.Append(FormatAddress(bitness, varInfo.LiveAddress + varInfo.LiveLength, uppercaseHex));
					sb.Append(' ');
					switch (varInfo.LocationKind) {
					case X86VariableLocationKind.Other:
						sb.Append("???");
						break;

					case X86VariableLocationKind.Register:
						sb.Append(formatter.Format(varInfo.Register.ToIcedRegister()));
						break;

					case X86VariableLocationKind.Memory:
						sb.Append('[');
						sb.Append(formatter.Format(varInfo.Register.ToIcedRegister()));
						int memOffs = varInfo.MemoryOffset;
						if (memOffs < 0) {
							sb.Append('-');
							memOffs = -memOffs;
						}
						else if (memOffs > 0)
							sb.Append('+');
						if (memOffs != 0) {
							sb.Append(formatter.Options.HexPrefix ?? string.Empty);
							sb.Append(memOffs.ToString(uppercaseHex ? "X2" : "x2"));
							sb.Append(formatter.Options.HexSuffix ?? string.Empty);
						}
						sb.Append(']');
						break;

					default:
						Debug.Fail($"Unknown location kind: {varInfo.LocationKind}");
						break;
					}
					WriteComment(output, commentPrefix, sb.ToString());
					sb.Clear();
				}
				output.Write(Environment.NewLine, BoxedTextColor.Text);
			}

			var refFactory = new AsmReferenceFactory();
			var formatterOutput = new FormatterOutputImpl(refFactory, output);
			for (int i = 0; i < blocks.Length; i++) {
				ref readonly var block = ref blocks[i];
				if (i > 0 && (formatterOptions & InternalFormatterOptions.EmptyLineBetweenBasicBlocks) != 0)
					output.Write(Environment.NewLine, BoxedTextColor.Text);
				if (!string2.IsNullOrEmpty(block.Comment))
					WriteComment(output, commentPrefix, block.Comment);
				if ((formatterOptions & InternalFormatterOptions.AddLabels) != 0 && !string2.IsNullOrEmpty(block.Label)) {
					output.Write(block.Label, refFactory.Create(block.LabelKind, block.Label), DisassemblyReferenceFlags.Definition | DisassemblyReferenceFlags.Local, GetColor(block.LabelKind));
					output.Write(":", BoxedTextColor.AsmPunctuation);
					output.Write(Environment.NewLine, BoxedTextColor.Text);
				}

				var instrs = block.Instructions;
				for (int j = 0; j < instrs.Length; j++) {
					ref var instr = ref instrs[j].Instruction;
					if ((formatterOptions & InternalFormatterOptions.InstructionAddresses) != 0) {
						var address = FormatAddress(bitness, instr.IP, uppercaseHex);
						output.Write(address, BoxedTextColor.AsmAddress);
						output.Write(" ", BoxedTextColor.Text);
					}
					else
						output.Write(formatter.Options.TabSize > 0 ? "\t\t" : "        ", BoxedTextColor.Text);

					if ((formatterOptions & InternalFormatterOptions.InstructionBytes) != 0) {
						var code = instrs[j].Code;
						var codeBytes = code.Array!;
						for (int k = 0; k < code.Count; k++) {
							byte b = codeBytes[k + code.Offset];
							output.Write(b.ToString(uppercaseHex ? "X2" : "x2"), BoxedTextColor.AsmHexBytes);
						}
						int missingBytes = HEXBYTES_COLUMN_BYTE_LENGTH - code.Count;
						for (int k = 0; k < missingBytes; k++)
							output.Write("  ", BoxedTextColor.Text);
						output.Write(" ", BoxedTextColor.Text);
					}

					formatter.Format(instr, formatterOutput);
					output.Write(Environment.NewLine, BoxedTextColor.Text);
				}
			}
		}

		internal static string GetCodeSizeString(Block[] blocks) {
			ulong codeSize = GetCodeSize(blocks);
			return $"Size: {codeSize} (0x{codeSize:X})";
		}

		static ulong GetCodeSize(Block[] blocks) {
			ulong codeSize = 0;
			foreach (var block in blocks) {
				var instrs = block.Instructions;
				if (instrs.Length > 0)
					codeSize += instrs[instrs.Length - 1].Instruction.NextIP - block.Address;
			}
			return codeSize;
		}

		static string FormatAddress(int bitness, ulong address, bool uppercaseHex) {
			switch (bitness) {
			case 16:
				return address.ToString(uppercaseHex ? "X4" : "x4");

			case 32:
				return address.ToString(uppercaseHex ? "X8" : "x8");

			case 64:
				return address.ToString(uppercaseHex ? "X16" : "x16");

			default:
				Debug.Fail($"Unknown bitness: {bitness}");
				goto case 64;
			}
		}
	}
}
