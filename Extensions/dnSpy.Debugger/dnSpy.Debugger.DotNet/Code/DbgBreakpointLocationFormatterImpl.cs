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

		public DbgBreakpointLocationFormatterImpl(BreakpointFormatterServiceImpl owner, DbgDotNetCodeLocationImpl location) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			this.location = location ?? throw new ArgumentNullException(nameof(location));
		}

		public override void Dispose() => location.Formatter = null;

		internal void RefreshName() => RaiseNameChanged();

		string GetHexPrefix() {
			if (owner.MethodDecompiler.GenericGuid == DecompilerConstants.LANGUAGE_VISUALBASIC)
				return "&H";
			return "0x";
		}

		void WriteILOffset(ITextColorWriter output, uint offset) {
			// Offsets are always in hex
			if (offset <= ushort.MaxValue)
				output.Write(BoxedTextColor.Number, GetHexPrefix() + offset.ToString("X4"));
			else
				output.Write(BoxedTextColor.Number, GetHexPrefix() + offset.ToString("X8"));
		}

		void WriteToken(ITextColorWriter output, uint token) =>
			output.Write(BoxedTextColor.Number, GetHexPrefix() + token.ToString("X8"));

		public override void WriteName(ITextColorWriter output, DbgBreakpointLocationFormatterOptions options) {
			bool printedToken = false;
			if ((options & DbgBreakpointLocationFormatterOptions.Tokens) != 0) {
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
				owner.MethodDecompiler.Write(output, method, GetFormatterOptions(options));

			output.WriteSpace();
			output.Write(BoxedTextColor.Operator, "+");
			output.WriteSpace();
			WriteILOffset(output, location.Offset);
		}

		FormatterOptions GetFormatterOptions(DbgBreakpointLocationFormatterOptions options) {
			FormatterOptions flags = 0;
			if ((options & DbgBreakpointLocationFormatterOptions.ModuleNames) != 0)
				flags |= FormatterOptions.ShowModuleNames;
			if ((options & DbgBreakpointLocationFormatterOptions.ParameterTypes) != 0)
				flags |= FormatterOptions.ShowParameterTypes;
			if ((options & DbgBreakpointLocationFormatterOptions.ParameterNames) != 0)
				flags |= FormatterOptions.ShowParameterNames;
			if ((options & DbgBreakpointLocationFormatterOptions.DeclaringTypes) != 0)
				flags |= FormatterOptions.ShowDeclaringTypes;
			if ((options & DbgBreakpointLocationFormatterOptions.ReturnTypes) != 0)
				flags |= FormatterOptions.ShowReturnTypes;
			if ((options & DbgBreakpointLocationFormatterOptions.Namespaces) != 0)
				flags |= FormatterOptions.ShowNamespaces;
			if ((options & DbgBreakpointLocationFormatterOptions.IntrinsicTypeKeywords) != 0)
				flags |= FormatterOptions.ShowIntrinsicTypeKeywords;
			if ((options & DbgBreakpointLocationFormatterOptions.DigitSeparators) != 0)
				flags |= FormatterOptions.DigitSeparators;
			if ((options & DbgBreakpointLocationFormatterOptions.Decimal) != 0)
				flags |= FormatterOptions.UseDecimal;
			return flags;
		}

		public override void WriteModule(ITextColorWriter output) =>
			output.WriteFilename(location.Module.ModuleName);
	}
}
