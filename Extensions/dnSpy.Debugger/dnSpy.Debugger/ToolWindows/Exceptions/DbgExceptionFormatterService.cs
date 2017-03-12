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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	abstract class DbgExceptionFormatterService {
		public abstract void WriteName(IDebugOutputWriter writer, DbgExceptionDefinition definition, bool includeDescription);
	}

	[Export(typeof(DbgExceptionFormatterService))]
	sealed class DbgExceptionFormatterServiceImpl : DbgExceptionFormatterService {
		readonly Lazy<DbgExceptionSettingsService> exceptionSettingsService;
		readonly Dictionary<string, Lazy<DbgExceptionFormatter, IDbgExceptionFormatterMetadata>[]> toFormatters;

		[ImportingConstructor]
		DbgExceptionFormatterServiceImpl(Lazy<DbgExceptionSettingsService> exceptionSettingsService, [ImportMany] IEnumerable<Lazy<DbgExceptionFormatter, IDbgExceptionFormatterMetadata>> dbgExceptionFormatters) {
			this.exceptionSettingsService = exceptionSettingsService;
			var dict = new Dictionary<string, List<Lazy<DbgExceptionFormatter, IDbgExceptionFormatterMetadata>>>(StringComparer.Ordinal);
			foreach (var lz in dbgExceptionFormatters.OrderBy(a => a.Metadata.Order)) {
				if (!dict.TryGetValue(lz.Metadata.Group, out var list))
					dict.Add(lz.Metadata.Group, list = new List<Lazy<DbgExceptionFormatter, IDbgExceptionFormatterMetadata>>());
				list.Add(lz);
			}
			toFormatters = new Dictionary<string, Lazy<DbgExceptionFormatter, IDbgExceptionFormatterMetadata>[]>(dict.Count);
			foreach (var kv in dict)
				toFormatters[kv.Key] = kv.Value.ToArray();
		}

		public override void WriteName(IDebugOutputWriter writer, DbgExceptionDefinition definition, bool includeDescription) {
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			if (definition.Id.Group == null)
				throw new ArgumentException();
			WriteNameCore(writer, definition);
			if (includeDescription && definition.Description != null) {
				writer.Write(BoxedTextColor.Text, " ");
				WriteDescription(writer, definition);
			}
		}

		void WriteNameCore(IDebugOutputWriter writer, DbgExceptionDefinition definition) {
			if (!definition.Id.IsDefaultId && toFormatters.TryGetValue(definition.Id.Group, out var formatters)) {
				foreach (var formatter in formatters) {
					if (formatter.Value.WriteName(writer, definition))
						return;
				}
			}
			DefaultWriteName(writer, definition);
		}

		void DefaultWriteName(IDebugOutputWriter output, DbgExceptionDefinition definition) {
			if (definition.Id.HasCode) {
				DbgExceptionGroupDefinitionFlags flags;
				if (exceptionSettingsService.Value.TryGetGroupDefinition(definition.Id.Group, out var groupDef))
					flags = groupDef.Flags;
				else
					flags = DbgExceptionGroupDefinitionFlags.None;
				if ((flags & DbgExceptionGroupDefinitionFlags.DecimalCode) == 0)
					output.Write(BoxedTextColor.Number, "0x" + definition.Id.Code.ToString("X8"));
				else if ((flags & DbgExceptionGroupDefinitionFlags.UnsignedCode) != 0)
					output.Write(BoxedTextColor.Number, ((uint)definition.Id.Code).ToString());
				else
					output.Write(BoxedTextColor.Number, definition.Id.Code.ToString());
			}
			else if (definition.Id.HasName)
				output.Write(BoxedTextColor.Text, definition.Id.Name);
			else if (definition.Id.IsDefaultId) {
				if (exceptionSettingsService.Value.TryGetGroupDefinition(definition.Id.Group, out var groupDef))
					output.Write(BoxedTextColor.Text, string.Format(dnSpy_Debugger_Resources.AllRemainingExceptionsNotInList, groupDef.DisplayName));
				else
					WriteError(output);
			}
			else
				WriteError(output);
		}

		void WriteDescription(IDebugOutputWriter writer, DbgExceptionDefinition definition) {
			if (definition.Description == null)
				return;
			writer.Write(BoxedTextColor.Comment, "(");
			writer.Write(BoxedTextColor.Comment, definition.Description);
			writer.Write(BoxedTextColor.Comment, ")");
		}

		void WriteError(IDebugOutputWriter output) => output.Write(BoxedTextColor.Error, "???");
	}
}
