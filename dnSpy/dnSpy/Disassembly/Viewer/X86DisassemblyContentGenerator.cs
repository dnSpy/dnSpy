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
using System.Diagnostics;
using dnSpy.Contracts.Disassembly;
using dnSpy.Contracts.Disassembly.Viewer;
using dnSpy.Contracts.Text;
using dnSpy.Properties;
using Iced.Intel;

namespace dnSpy.Disassembly.Viewer {
	static class X86DisassemblyContentGenerator {
		const int HEXBYTES_COLUMN_BYTE_LENGTH = 10;

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
			readonly DisassemblyContentOutput output;

			public FormatterOutputImpl(DisassemblyContentOutput output) => this.output = output;

			public override void Write(string text, FormatterOutputTextKind kind) {
				var color = GetColor(kind);
				switch (kind) {
				case FormatterOutputTextKind.Directive:
				case FormatterOutputTextKind.Prefix:
				case FormatterOutputTextKind.Mnemonic:
				case FormatterOutputTextKind.Keyword:
				case FormatterOutputTextKind.Register:
				case FormatterOutputTextKindExtensions.UnknownSymbol:
				case FormatterOutputTextKindExtensions.Data:
				case FormatterOutputTextKindExtensions.Label:
				case FormatterOutputTextKindExtensions.Function:
					output.Write(text, new AsmReference(kind, text), DisassemblyReferenceFlags.Local, color);
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
			case FormatterOutputTextKindExtensions.Data:
				return BoxedTextColor.AsmData;
			case FormatterOutputTextKindExtensions.Label:
				return BoxedTextColor.AsmLabel;
			case FormatterOutputTextKindExtensions.Function:
				return BoxedTextColor.AsmFunction;
			default:
				Debug.Fail($"Unknown output kind: {kind}");
				return BoxedTextColor.Error;
			}
		}

		static void WriteComment(DisassemblyContentOutput output, string commentPrefix, string text) {
			var lines = text.Replace("\r\n", "\n").Split(new char[] { '\n' });
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

		public static void Write(int bitness, DisassemblyContentOutput output, string header, NativeCodeOptimization optimization, Formatter formatter, string commentPrefix, InternalFormatterOptions formatterOptions, X86Block[] blocks, X86NativeCodeInfo codeInfo, NativeVariableInfo[] variableInfo) {
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
						sb.Append(varInfo.Register.ToString());
						break;

					case X86VariableLocationKind.Memory:
						sb.Append('[');
						sb.Append(varInfo.Register.ToString());
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

			var formatterOutput = new FormatterOutputImpl(output);
			for (int i = 0; i < blocks.Length; i++) {
				ref readonly var block = ref blocks[i];
				if (i > 0 && (formatterOptions & InternalFormatterOptions.EmptyLineBetweenBasicBlocks) != 0)
					output.Write(Environment.NewLine, BoxedTextColor.Text);
				if (!string.IsNullOrEmpty(block.Comment))
					WriteComment(output, commentPrefix, block.Comment);
				if ((formatterOptions & InternalFormatterOptions.AddLabels) != 0 && !string.IsNullOrEmpty(block.Label)) {
					output.Write(block.Label, new AsmReference(block.LabelKind, block.Label), DisassemblyReferenceFlags.Definition | DisassemblyReferenceFlags.Local, GetColor(block.LabelKind));
					output.Write(":", BoxedTextColor.AsmPunctuation);
					output.Write(Environment.NewLine, BoxedTextColor.Text);
				}

				foreach (var info in block.Instructions) {
					var instr = info.Instruction;
					if ((formatterOptions & InternalFormatterOptions.InstructionAddresses) != 0) {
						var address = FormatAddress(bitness, instr.IP64, upperCaseHex);
						output.Write(address, BoxedTextColor.AsmAddress);
						output.Write(" ", BoxedTextColor.Text);
					}
					else
						output.Write(formatter.Options.TabSize > 0 ? "\t\t" : "        ", BoxedTextColor.Text);

					if ((formatterOptions & InternalFormatterOptions.InstructionBytes) != 0) {
						var codeBytes = info.Code.Array;
						for (int j = 0; j < info.Code.Count; j++) {
							byte b = codeBytes[j + info.Code.Offset];
							output.Write(b.ToString(upperCaseHex ? "X2" : "x2"), BoxedTextColor.AsmHexBytes);
						}
						int missingBytes = HEXBYTES_COLUMN_BYTE_LENGTH - info.Code.Count;
						for (int j = 0; j < missingBytes; j++)
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
