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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.DotNet.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.DotNet.Breakpoints.Code {
	sealed class DbgBreakpointLocationFormatterImpl : DbgBreakpointLocationFormatter {
		readonly DbgDotNetBreakpointLocation breakpoint;
		readonly BreakpointFormatterServiceImpl owner;
		readonly CodeBreakpointDisplaySettings codeBreakpointDisplaySettings;

		public DbgBreakpointLocationFormatterImpl(BreakpointFormatterServiceImpl owner, CodeBreakpointDisplaySettings codeBreakpointDisplaySettings, DbgDotNetBreakpointLocation breakpoint) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.codeBreakpointDisplaySettings = codeBreakpointDisplaySettings ?? throw new ArgumentNullException(nameof(codeBreakpointDisplaySettings));
			this.breakpoint = breakpoint ?? throw new ArgumentNullException(nameof(breakpoint));
		}

		internal void RefreshName() => RaiseNameChanged();

		void WriteILOffset(IDebugOutputWriter output, uint offset) {
			// Offsets are always in hex
			if (offset <= ushort.MaxValue)
				output.Write(BoxedTextColor.Number, "0x" + offset.ToString("X4"));
			else
				output.Write(BoxedTextColor.Number, "0x" + offset.ToString("X8"));
		}

		void WriteToken(IDebugOutputWriter output, uint token) =>
			output.Write(BoxedTextColor.Number, "0x" + token.ToString("X8"));

		void WriteSpace(IDebugOutputWriter output) => output.Write(BoxedTextColor.Text, " ");

		public override void WriteName(IDebugOutputWriter output) {
			bool printedToken = false;
			if (codeBreakpointDisplaySettings.ShowTokens) {
				WriteToken(output, breakpoint.Token);
				WriteSpace(output);
				printedToken = true;
			}

			var method = owner.GetMethodDef(breakpoint);
			if (method == null) {
				if (printedToken)
					output.Write(BoxedTextColor.Error, "???");
				else
					WriteToken(output, breakpoint.Token);
			}
			else
				owner.MethodDecompiler.Write(new TextColorWriterImpl(output), method, GetPrinterFlags());

			WriteSpace(output);
			output.Write(BoxedTextColor.Operator, "+");
			WriteSpace(output);
			WriteILOffset(output, breakpoint.Offset);
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

		public override void WriteModule(IDebugOutputWriter output) =>
			new TextColorWriterImpl(output).WriteFilename(breakpoint.Module.ModuleName);
	}
}
