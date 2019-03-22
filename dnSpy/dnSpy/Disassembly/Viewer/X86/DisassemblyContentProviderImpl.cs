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
using System.Diagnostics;
using dnSpy.Contracts.Disassembly;
using dnSpy.Contracts.Disassembly.Viewer;
using dnSpy.Disassembly.X86;
using Iced.Intel;

namespace dnSpy.Disassembly.Viewer.X86 {
	sealed class DisassemblyContentProviderImpl : DisassemblyContentProvider {
		readonly int bitness;
		readonly CachedSymbolResolver cachedSymbolResolver;
		readonly DisassemblyContentSettings disasmSettings;
		readonly IMasmDisassemblySettings masmSettings;
		readonly INasmDisassemblySettings nasmSettings;
		readonly IGasDisassemblySettings gasSettings;
		readonly DisassemblyContentFormatterOptions formatterOptions;
		readonly string header;
		readonly NativeCodeOptimization optimization;
		readonly Block[] blocks;
		readonly X86NativeCodeInfo codeInfo;
		readonly NativeVariableInfo[] variableInfo;
		readonly string methodName;
		readonly string moduleName;
		readonly SymbolResolverImpl symbolResolver;
		bool hasRegisteredEvents;
		bool closed;

		bool EmptyLineBetweenBasicBlocks =>
			(formatterOptions & DisassemblyContentFormatterOptions.EmptyLineBetweenBasicBlocks) != 0 ||
			((formatterOptions & DisassemblyContentFormatterOptions.NoEmptyLineBetweenBasicBlocks) == 0 && disasmSettings.EmptyLineBetweenBasicBlocks);
		bool InstructionAddresses =>
			(formatterOptions & DisassemblyContentFormatterOptions.InstructionAddresses) != 0 ||
			((formatterOptions & DisassemblyContentFormatterOptions.NoInstructionAddresses) == 0 && disasmSettings.ShowInstructionAddress);
		bool InstructionBytes =>
			(formatterOptions & DisassemblyContentFormatterOptions.InstructionBytes) != 0 ||
			((formatterOptions & DisassemblyContentFormatterOptions.NoInstructionBytes) == 0 && disasmSettings.ShowInstructionBytes);
		bool AddLabels =>
			(formatterOptions & DisassemblyContentFormatterOptions.AddLabels) != 0 ||
			((formatterOptions & DisassemblyContentFormatterOptions.NoAddLabels) == 0 && disasmSettings.AddLabels);

		const string MASM_COMMENT = ";";
		const string NASM_COMMENT = ";";
		const string GAS_COMMENT = "//";

		public override event EventHandler OnContentChanged;

		sealed class SymbolResolverImpl : Iced.Intel.ISymbolResolver {
			readonly DisassemblyContentProviderImpl owner;
			public SymbolResolverImpl(DisassemblyContentProviderImpl owner) => this.owner = owner;

			public bool TryGetSymbol(int operand, int instructionOperand, ref Instruction instruction, ulong address, int addressSize, out SymbolResult symbol) {
				if (owner.cachedSymbolResolver.TryResolve(address, out var symResult, out bool fakeSymbol)) {
					if (!fakeSymbol || owner.AddLabels) {
						symbol = new SymbolResult(symResult.Address, symResult.Symbol, SymbolKindUtils.ToFormatterOutputTextKind(symResult.Kind), SymbolFlags.None);
						if (instruction.OpCount == 1 && (instruction.Op0Kind == OpKind.Memory || instruction.Op0Kind == OpKind.Memory64)) {
							var code = instruction.Code;
							if (code == Code.Call_rm32 || code == Code.Jmp_rm32)
								symbol = new SymbolResult(symbol.Address, symbol.Text, symbol.Flags, MemorySize.DwordOffset);
							else if (code == Code.Call_rm64 || code == Code.Jmp_rm64)
								symbol = new SymbolResult(symbol.Address, symbol.Text, symbol.Flags, MemorySize.QwordOffset);
						}
						return true;
					}
				}
				symbol = default;
				return false;
			}
		}

		public DisassemblyContentProviderImpl(int bitness, CachedSymbolResolver cachedSymbolResolver, DisassemblyContentSettings disasmSettings, IMasmDisassemblySettings masmSettings, INasmDisassemblySettings nasmSettings, IGasDisassemblySettings gasSettings, DisassemblyContentFormatterOptions formatterOptions, string header, NativeCodeOptimization optimization, Block[] blocks, X86NativeCodeInfo codeInfo, NativeVariableInfo[] variableInfo, string methodName, string moduleName) {
			this.bitness = bitness;
			this.cachedSymbolResolver = cachedSymbolResolver ?? throw new ArgumentNullException(nameof(cachedSymbolResolver));
			this.disasmSettings = disasmSettings ?? throw new ArgumentNullException(nameof(disasmSettings));
			this.masmSettings = masmSettings ?? throw new ArgumentNullException(nameof(masmSettings));
			this.nasmSettings = nasmSettings ?? throw new ArgumentNullException(nameof(nasmSettings));
			this.gasSettings = gasSettings ?? throw new ArgumentNullException(nameof(gasSettings));
			this.formatterOptions = formatterOptions;
			this.header = header;
			this.optimization = optimization;
			this.blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
			this.codeInfo = codeInfo;
			this.variableInfo = variableInfo;
			this.methodName = methodName;
			this.moduleName = moduleName;
			symbolResolver = new SymbolResolverImpl(this);
		}

		public override DisassemblyContentProvider Clone() =>
			new DisassemblyContentProviderImpl(bitness, cachedSymbolResolver, disasmSettings, masmSettings, nasmSettings, gasSettings, formatterOptions, header, optimization, blocks, codeInfo, variableInfo, methodName, moduleName);

		(Formatter formatter, string commentPrefix, DisassemblyContentKind contentKind) GetDisassemblerInfo(X86Disassembler disasm) {
			switch (disasm) {
			case X86Disassembler.Masm:
				return (new MasmFormatter(masmSettings.ToMasm(), symbolResolver), MASM_COMMENT, DisassemblyContentKind.Masm);

			case X86Disassembler.Nasm:
				return (new NasmFormatter(nasmSettings.ToNasm(), symbolResolver), NASM_COMMENT, DisassemblyContentKind.Nasm);

			case X86Disassembler.Gas:
				return (new GasFormatter(gasSettings.ToGas(), symbolResolver), GAS_COMMENT, DisassemblyContentKind.ATT);

			default:
				Debug.Fail($"Unknown disassembler: {disasm}");
				goto case X86Disassembler.Masm;
			}
		}

		InternalFormatterOptions GetInternalFormatterOptions(bool upperCaseHex) {
			var options = InternalFormatterOptions.None;
			if (EmptyLineBetweenBasicBlocks)
				options |= InternalFormatterOptions.EmptyLineBetweenBasicBlocks;
			if (InstructionAddresses)
				options |= InternalFormatterOptions.InstructionAddresses;
			if (InstructionBytes)
				options |= InternalFormatterOptions.InstructionBytes;
			if (AddLabels)
				options |= InternalFormatterOptions.AddLabels;
			if (upperCaseHex)
				options |= InternalFormatterOptions.UpperCaseHex;
			return options;
		}

		public override DisassemblyContent GetContent() {
			if (!hasRegisteredEvents) {
				hasRegisteredEvents = true;
				disasmSettings.PropertyChanged += DisassemblyContentSettings_PropertyChanged;
				masmSettings.PropertyChanged += MasmDisassemblySettings_PropertyChanged;
				nasmSettings.PropertyChanged += NasmDisassemblySettings_PropertyChanged;
				gasSettings.PropertyChanged += GasDisassemblySettings_PropertyChanged;
			}

			var output = new DisassemblyContentOutput();
			var disasmInfo = GetDisassemblerInfo(disasmSettings.X86Disassembler);
			DisassemblyContentGenerator.Write(bitness, output, header, optimization, disasmInfo.formatter, disasmInfo.commentPrefix, GetInternalFormatterOptions(disasmInfo.formatter.Options.UpperCaseHex), blocks, codeInfo, variableInfo, methodName, moduleName);
			return output.Create(disasmInfo.contentKind);
		}

		void DisassemblyContentSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			bool refresh;
			switch (e.PropertyName) {
			case nameof(DisassemblyContentSettings.ShowInstructionAddress):
				refresh = (formatterOptions & (DisassemblyContentFormatterOptions.InstructionAddresses | DisassemblyContentFormatterOptions.NoInstructionAddresses)) == 0;
				break;

			case nameof(DisassemblyContentSettings.ShowInstructionBytes):
				refresh = (formatterOptions & (DisassemblyContentFormatterOptions.InstructionBytes | DisassemblyContentFormatterOptions.NoInstructionBytes)) == 0;
				break;

			case nameof(DisassemblyContentSettings.EmptyLineBetweenBasicBlocks):
				refresh = (formatterOptions & (DisassemblyContentFormatterOptions.EmptyLineBetweenBasicBlocks | DisassemblyContentFormatterOptions.NoEmptyLineBetweenBasicBlocks)) == 0;
				break;

			case nameof(DisassemblyContentSettings.AddLabels):
				refresh = (formatterOptions & (DisassemblyContentFormatterOptions.AddLabels | DisassemblyContentFormatterOptions.NoAddLabels)) == 0;
				break;

			case nameof(DisassemblyContentSettings.X86Disassembler):
				refresh = true;
				break;

			case nameof(DisassemblyContentSettings.ShowILCode):
			case nameof(DisassemblyContentSettings.ShowCode):
				// IL code and decompiled code are shown in comments and aren't created by this class
				refresh = false;
				break;

			default:
				Debug.Fail($"Unknown property: {e.PropertyName}");
				refresh = false;
				break;
			}

			if (refresh)
				RefreshContent();
		}

		void MasmDisassemblySettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (disasmSettings.X86Disassembler == X86Disassembler.Masm)
				RefreshContent();
		}

		void NasmDisassemblySettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (disasmSettings.X86Disassembler == X86Disassembler.Nasm)
				RefreshContent();
		}

		void GasDisassemblySettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (disasmSettings.X86Disassembler == X86Disassembler.Gas)
				RefreshContent();
		}

		void RefreshContent() {
			if (!closed)
				OnContentChanged?.Invoke(this, EventArgs.Empty);
		}

		public override void Dispose() {
			closed = true;
			disasmSettings.PropertyChanged -= DisassemblyContentSettings_PropertyChanged;
			masmSettings.PropertyChanged -= MasmDisassemblySettings_PropertyChanged;
			nasmSettings.PropertyChanged -= NasmDisassemblySettings_PropertyChanged;
			gasSettings.PropertyChanged -= GasDisassemblySettings_PropertyChanged;
		}
	}
}
