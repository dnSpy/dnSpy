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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Debugger.Text.DnSpy;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.DotNet.CorDebug.Code {
	sealed class DbgBreakpointLocationFormatterImpl : DbgBreakpointLocationFormatter {
		readonly DbgDotNetNativeCodeLocationImpl location;
		readonly BreakpointFormatterServiceImpl owner;
		WeakReference? weakMethod;

		public DbgBreakpointLocationFormatterImpl(BreakpointFormatterServiceImpl owner, DbgDotNetNativeCodeLocationImpl location) {
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

		void WriteILOffset(IDbgTextWriter output, uint offset) {
			// Offsets are always in hex
			if (offset <= ushort.MaxValue)
				output.Write(DbgTextColor.Number, GetHexPrefix() + offset.ToString("X4"));
			else
				output.Write(DbgTextColor.Number, GetHexPrefix() + offset.ToString("X8"));
		}

		void WriteToken(IDbgTextWriter output, uint token) =>
			output.Write(DbgTextColor.Number, GetHexPrefix() + token.ToString("X8"));

		public override void WriteName(IDbgTextWriter output, DbgBreakpointLocationFormatterOptions options) {
			bool printedToken = false;
			if ((options & DbgBreakpointLocationFormatterOptions.Tokens) != 0) {
				WriteToken(output, location.Token);
				output.Write(DbgTextColor.Text, " ");
				printedToken = true;
			}

			var method = weakMethod?.Target as MethodDef ?? owner.GetDefinition<MethodDef>(location.Module, location.Token);
			if (method is null) {
				if (printedToken)
					output.Write(DbgTextColor.Error, "???");
				else
					WriteToken(output, location.Token);
			}
			else {
				if (weakMethod?.Target != method)
					weakMethod = new WeakReference(method);
				owner.MethodDecompiler.Write(new DbgTextColorWriter(output), method, GetFormatterOptions(options));
			}

			switch (location.ILOffsetMapping) {
			case DbgILOffsetMapping.Exact:
			case DbgILOffsetMapping.Approximate:
				output.Write(DbgTextColor.Text, " ");
				output.Write(DbgTextColor.Operator, "+");
				output.Write(DbgTextColor.Text, " ");
				if (location.ILOffsetMapping == DbgILOffsetMapping.Approximate)
					output.Write(DbgTextColor.Operator, "~");
				WriteILOffset(output, location.Offset);
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

			default:
				Debug.Fail($"Unknown IL offset mapping: {location.ILOffsetMapping}");
				goto case DbgILOffsetMapping.Unknown;
			}

			output.Write(DbgTextColor.Text, " ");
			output.Write(DbgTextColor.Punctuation, "(");
			output.Write(DbgTextColor.Number, GetHexPrefix() + location.NativeAddress.Address.ToString("X8"));
			output.Write(DbgTextColor.Operator, "+");
			output.Write(DbgTextColor.Number,
				(options & DbgBreakpointLocationFormatterOptions.Decimal) != 0 ?
				location.NativeAddress.Offset.ToString() :
				GetHexPrefix() + location.NativeAddress.Offset.ToString("X"));
			output.Write(DbgTextColor.Punctuation, ")");
		}

		static void WriteText(IDbgTextWriter output, string text) {
			output.Write(DbgTextColor.Text, " ");
			output.Write(DbgTextColor.Punctuation, "(");
			output.Write(DbgTextColor.Text, text);
			output.Write(DbgTextColor.Punctuation, ")");
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

		public override void WriteModule(IDbgTextWriter output) =>
			new DbgTextColorWriter(output).WriteFilename(location.Module.ModuleName);
	}
}
