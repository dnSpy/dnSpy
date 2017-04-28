/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.Breakpoints;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.CorDebug.Breakpoints {
	sealed class DbgBreakpointLocationFormatterImpl : DbgBreakpointLocationFormatter {
		readonly DbgDotNetNativeBreakpointLocation location;
		readonly BreakpointFormatterServiceImpl owner;
		readonly CodeBreakpointDisplaySettings codeBreakpointDisplaySettings;

		public DbgBreakpointLocationFormatterImpl(BreakpointFormatterServiceImpl owner, CodeBreakpointDisplaySettings codeBreakpointDisplaySettings, DbgDotNetNativeBreakpointLocation location) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.codeBreakpointDisplaySettings = codeBreakpointDisplaySettings ?? throw new ArgumentNullException(nameof(codeBreakpointDisplaySettings));
			this.location = location ?? throw new ArgumentNullException(nameof(location));
		}

		internal void RefreshName() => RaiseNameChanged();

		void WriteILOffset(ITextColorWriter output, uint offset) {
			// Offsets are always in hex
			if (offset <= ushort.MaxValue)
				output.Write(BoxedTextColor.Number, "0x" + offset.ToString("X4"));
			else
				output.Write(BoxedTextColor.Number, "0x" + offset.ToString("X8"));
		}

		void WriteToken(ITextColorWriter output, uint token) =>
			output.Write(BoxedTextColor.Number, "0x" + token.ToString("X8"));

		public override void WriteName(ITextColorWriter output) {
			bool printedToken = false;
			if (codeBreakpointDisplaySettings.ShowTokens) {
				WriteToken(output, location.Token);
				output.WriteSpace();
				printedToken = true;
			}

			var method = owner.GetDefinition<MethodDef>(location.Module, location.Token);
			if (method == null) {
				if (printedToken)
					output.Write(BoxedTextColor.Error, "???");
				else
					WriteToken(output, location.Token);
			}
			else
				owner.MethodDecompiler.Write(output, method, GetPrinterFlags());

			switch (location.ILOffsetMapping) {
			case DbgILOffsetMapping.Exact:
			case DbgILOffsetMapping.Approximate:
				output.WriteSpace();
				output.Write(BoxedTextColor.Operator, "+");
				output.WriteSpace();
				if (location.ILOffsetMapping == DbgILOffsetMapping.Approximate)
					output.Write(BoxedTextColor.Operator, "~");
				WriteILOffset(output, location.ILOffset);
				break;

			case DbgILOffsetMapping.Prolog:
				WriteText(output, "prolog");
				break;

			case DbgILOffsetMapping.Epilog:
				WriteText(output, "epilog");
				break;

			case DbgILOffsetMapping.Unknown:
			case DbgILOffsetMapping.NoInfo:
			case DbgILOffsetMapping.UnmappedAddress:
				WriteText(output, "???");
				break;

			default: throw new InvalidOperationException();
			}

			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, "(");
			output.Write(BoxedTextColor.Number, "0x" + location.NativeMethodAddress.ToString("X8"));
			output.Write(BoxedTextColor.Operator, "+");
			output.Write(BoxedTextColor.Number, "0x" + location.NativeMethodOffset.ToString("X"));
			output.Write(BoxedTextColor.Punctuation, ")");
		}

		static void WriteText(ITextColorWriter output, string text) {
			output.WriteSpace();
			output.Write(BoxedTextColor.Punctuation, "(");
			output.Write(BoxedTextColor.Text, text);
			output.Write(BoxedTextColor.Punctuation, ")");
		}

		SimplePrinterFlags GetPrinterFlags() {
			SimplePrinterFlags flags = 0;
			if (codeBreakpointDisplaySettings.ShowModuleNames)				flags |= SimplePrinterFlags.ShowModuleNames;
			if (codeBreakpointDisplaySettings.ShowParameterTypes)			flags |= SimplePrinterFlags.ShowParameterTypes;
			if (codeBreakpointDisplaySettings.ShowParameterNames)			flags |= SimplePrinterFlags.ShowParameterNames;
			if (codeBreakpointDisplaySettings.ShowDeclaringTypes)			flags |= SimplePrinterFlags.ShowOwnerTypes;
			if (codeBreakpointDisplaySettings.ShowReturnTypes)				flags |= SimplePrinterFlags.ShowReturnTypes;
			if (codeBreakpointDisplaySettings.ShowNamespaces)				flags |= SimplePrinterFlags.ShowNamespaces;
			if (codeBreakpointDisplaySettings.ShowIntrinsicTypeKeywords)	flags |= SimplePrinterFlags.ShowTypeKeywords;
			return flags;
		}

		public override void WriteModule(ITextColorWriter output) =>
			output.WriteFilename(location.Module.ModuleName);
	}
}
