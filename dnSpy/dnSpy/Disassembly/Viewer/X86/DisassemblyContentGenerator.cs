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
			readonly Dictionary<(FormatterOutputTextKind kind, string value), AsmReference> dict = new Dictionary<(FormatterOutputTextKind kind, string value), AsmReference>();

			public AsmReference Create(FormatterOutputTextKind kind, string value) {
				var key = (kind, value);
				if (!dict.TryGetValue(key, out var asmRef))
					dict[key] = asmRef = new AsmReference(kind, value);
				return asmRef;
			}
		}

		sealed class AsmReference {
			readonly FormatterOutputTextKind kind;
			readonly string value;
			public AsmReference(FormatterOutputTextKind kind, string value) {
				this.kind = kind;
				this.value = value;
			}
			public override bool Equals(object obj) => obj is AsmReference other && kind == other.kind && StringComparer.Ordinal.Equals(value, other.value);
			public override int GetHashCode() => (int)kind ^ StringComparer.Ordinal.GetHashCode(value ?? string.Empty);
		}

		sealed class FormatterOutputImpl : FormatterOutput {
			readonly AsmReferenceFactory refFactory;
			readonly DisassemblyContentOutput output;

			public FormatterOutputImpl(AsmReferenceFactory refFactory, DisassemblyContentOutput output) {
				this.refFactory = refFactory;
				this.output = output;
			}

			public override void Write(string text, FormatterOutputTextKind kind) {
				var color = GetColor(kind);
				switch (kind) {
				case FormatterOutputTextKind.Directive:
				case FormatterOutputTextKind.Prefix:
				case FormatterOutputTextKind.Mnemonic:
				case FormatterOutputTextKind.Keyword:
				case FormatterOutputTextKind.Register:
				case FormatterOutputTextKindExtensions.UnknownSymbol:
				case FormatterOutputTextKind.Data:
				case FormatterOutputTextKind.Label:
				case FormatterOutputTextKind.Function:
				case FormatterOutputTextKind.Decorator:
					output.Write(text, refFactory.Create(kind, text), DisassemblyReferenceFlags.Local, color);
					break;

				default:
					output.Write(text, color);
					break;
				}
			}
		}

		static object GetColor(FormatterOutputTextKind kind) {
			switch (kind) {
			case FormatterOutputTextKind.Text:
				return BoxedTextColor.Text;
			case FormatterOutputTextKind.Directive:
				return BoxedTextColor.AsmDirective;
			case FormatterOutputTextKind.Prefix:
				return BoxedTextColor.AsmPrefix;
			case FormatterOutputTextKind.Mnemonic:
				return BoxedTextColor.AsmMnemonic;
			case FormatterOutputTextKind.Keyword:
				return BoxedTextColor.AsmKeyword;
			case FormatterOutputTextKind.Operator:
				return BoxedTextColor.AsmOperator;
			case FormatterOutputTextKind.Punctuation:
				return BoxedTextColor.AsmPunctuation;
			case FormatterOutputTextKind.Number:
				return BoxedTextColor.AsmNumber;
			case FormatterOutputTextKind.Register:
				return BoxedTextColor.AsmRegister;
			case FormatterOutputTextKind.SelectorValue:
				return BoxedTextColor.AsmSelectorValue;
			case FormatterOutputTextKind.LabelAddress:
				return BoxedTextColor.AsmLabelAddress;
			case FormatterOutputTextKind.FunctionAddress:
				return BoxedTextColor.AsmFunctionAddress;
			case FormatterOutputTextKindExtensions.UnknownSymbol:
				return BoxedTextColor.AsmLabel;
			case FormatterOutputTextKind.Data:
				return BoxedTextColor.AsmData;
			case FormatterOutputTextKind.Label:
				return BoxedTextColor.AsmLabel;
			case FormatterOutputTextKind.Function:
				return BoxedTextColor.AsmFunction;
			case FormatterOutputTextKind.Decorator:
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

		static string GetName(NativeVariableInfo[] variableInfo, in X86Variable varInfo) {
			foreach (var info in variableInfo) {
				if (info.IsLocal == varInfo.IsLocal && info.Index == varInfo.Index)
					return info.Name;
			}
			return null;
		}

		public static void Write(int bitness, DisassemblyContentOutput output, string header, NativeCodeOptimization optimization, Formatter formatter, string commentPrefix, InternalFormatterOptions formatterOptions, Block[] blocks, X86NativeCodeInfo codeInfo, NativeVariableInfo[] variableInfo, string methodName, string moduleName) {
			if (variableInfo == null)
				variableInfo = Array.Empty<NativeVariableInfo>();
			if (optimization == NativeCodeOptimization.Unoptimized) {
				const string LINE = "********************************************";
				WriteComment(output, commentPrefix, LINE);
				WriteComment(output, commentPrefix, dnSpy_Resources.Disassembly_MethodIsNotOptimized);
				WriteComment(output, commentPrefix, LINE);
				output.Write(Environment.NewLine, BoxedTextColor.Text);
			}
			if (header != null) {
				WriteComment(output, commentPrefix, header);
				output.Write(Environment.NewLine, BoxedTextColor.Text);
			}

			if (moduleName != null)
				WriteComment(output, commentPrefix, moduleName);
			if (methodName != null)
				WriteComment(output, commentPrefix, methodName);
			ulong codeSize = 0;
			foreach (var block in blocks) {
				var instrs = block.Instructions;
				if (instrs.Length > 0)
					codeSize += instrs[instrs.Length - 1].Instruction.NextIP - block.Address;
			}
			WriteComment(output, commentPrefix, $"Size: {codeSize} (0x{codeSize:X})");
			output.Write(Environment.NewLine, BoxedTextColor.Text);

			bool upperCaseHex = (formatterOptions & InternalFormatterOptions.UpperCaseHex) != 0;
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
					if (name != null) {
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
					sb.Append(FormatAddress(bitness, varInfo.LiveAddress, upperCaseHex));
					sb.Append('-');
					sb.Append(FormatAddress(bitness, varInfo.LiveAddress + varInfo.LiveLength, upperCaseHex));
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
							sb.Append(memOffs.ToString(upperCaseHex ? "X2" : "x2"));
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
				if (!string.IsNullOrEmpty(block.Comment))
					WriteComment(output, commentPrefix, block.Comment);
				if ((formatterOptions & InternalFormatterOptions.AddLabels) != 0 && !string.IsNullOrEmpty(block.Label)) {
					output.Write(block.Label, refFactory.Create(block.LabelKind, block.Label), DisassemblyReferenceFlags.Definition | DisassemblyReferenceFlags.Local, GetColor(block.LabelKind));
					output.Write(":", BoxedTextColor.AsmPunctuation);
					output.Write(Environment.NewLine, BoxedTextColor.Text);
				}

				var instrs = block.Instructions;
				for (int j = 0; j < instrs.Length; j++) {
					ref var instr = ref instrs[j].Instruction;
					if ((formatterOptions & InternalFormatterOptions.InstructionAddresses) != 0) {
						var address = FormatAddress(bitness, instr.IP, upperCaseHex);
						output.Write(address, BoxedTextColor.AsmAddress);
						output.Write(" ", BoxedTextColor.Text);
					}
					else
						output.Write(formatter.Options.TabSize > 0 ? "\t\t" : "        ", BoxedTextColor.Text);

					if ((formatterOptions & InternalFormatterOptions.InstructionBytes) != 0) {
						var code = instrs[j].Code;
						var codeBytes = code.Array;
						for (int k = 0; k < code.Count; k++) {
							byte b = codeBytes[k + code.Offset];
							output.Write(b.ToString(upperCaseHex ? "X2" : "x2"), BoxedTextColor.AsmHexBytes);
						}
						int missingBytes = HEXBYTES_COLUMN_BYTE_LENGTH - code.Count;
						for (int k = 0; k < missingBytes; k++)
							output.Write("  ", BoxedTextColor.Text);
						output.Write(" ", BoxedTextColor.Text);
					}

					formatter.Format(ref instr, formatterOutput);
					output.Write(Environment.NewLine, BoxedTextColor.Text);
				}
			}
		}

		static string FormatAddress(int bitness, ulong address, bool upperCaseHex) {
			switch (bitness) {
			case 16:
				return address.ToString(upperCaseHex ? "X4" : "x4");

			case 32:
				return address.ToString(upperCaseHex ? "X8" : "x8");

			case 64:
				return address.ToString(upperCaseHex ? "X16" : "x16");

			default:
				Debug.Fail($"Unknown bitness: {bitness}");
				goto case 64;
			}
		}
	}
}
