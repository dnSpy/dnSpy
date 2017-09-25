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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.DotNet.Code {
	sealed class DbgBreakpointLocationFormatterImpl : DbgBreakpointLocationFormatter {
		readonly DbgDotNetCodeLocationImpl location;
		readonly BreakpointFormatterServiceImpl owner;
		readonly DbgCodeBreakpointDisplaySettings dbgCodeBreakpointDisplaySettings;

		public DbgBreakpointLocationFormatterImpl(BreakpointFormatterServiceImpl owner, DbgCodeBreakpointDisplaySettings dbgCodeBreakpointDisplaySettings, DbgDotNetCodeLocationImpl location) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.dbgCodeBreakpointDisplaySettings = dbgCodeBreakpointDisplaySettings ?? throw new ArgumentNullException(nameof(dbgCodeBreakpointDisplaySettings));
			this.location = location ?? throw new ArgumentNullException(nameof(location));
		}

		public override void Dispose() => location.Formatter = null;

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
			if (dbgCodeBreakpointDisplaySettings.ShowTokens) {
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

			output.WriteSpace();
			output.Write(BoxedTextColor.Operator, "+");
			output.WriteSpace();
			WriteILOffset(output, location.Offset);
		}

		FormatterOptions GetPrinterFlags() {
			FormatterOptions flags = 0;
			if (dbgCodeBreakpointDisplaySettings.ShowModuleNames)			flags |= FormatterOptions.ShowModuleNames;
			if (dbgCodeBreakpointDisplaySettings.ShowParameterTypes)		flags |= FormatterOptions.ShowParameterTypes;
			if (dbgCodeBreakpointDisplaySettings.ShowParameterNames)		flags |= FormatterOptions.ShowParameterNames;
			if (dbgCodeBreakpointDisplaySettings.ShowDeclaringTypes)		flags |= FormatterOptions.ShowDeclaringTypes;
			if (dbgCodeBreakpointDisplaySettings.ShowReturnTypes)			flags |= FormatterOptions.ShowReturnTypes;
			if (dbgCodeBreakpointDisplaySettings.ShowNamespaces)			flags |= FormatterOptions.ShowNamespaces;
			if (dbgCodeBreakpointDisplaySettings.ShowIntrinsicTypeKeywords)	flags |= FormatterOptions.ShowIntrinsicTypeKeywords;
			return flags;
		}

		public override void WriteModule(ITextColorWriter output) =>
			output.WriteFilename(location.Module.ModuleName);
	}
}
