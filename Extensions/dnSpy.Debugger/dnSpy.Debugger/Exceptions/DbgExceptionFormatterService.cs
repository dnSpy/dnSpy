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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Exceptions {
	abstract class DbgExceptionFormatterService {
		public abstract void WriteName(IDbgTextWriter writer, DbgExceptionDefinition definition, bool includeDescription);
		public abstract void WriteName(IDbgTextWriter writer, DbgExceptionId id, bool includeDescription);

		public string ToString(DbgExceptionId id, bool includeDescription = true) {
			var writer = new DbgStringBuilderTextWriter();
			WriteName(writer, id, includeDescription);
			return writer.ToString();
		}
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
				if (!dict.TryGetValue(lz.Metadata.Category, out var list))
					dict.Add(lz.Metadata.Category, list = new List<Lazy<DbgExceptionFormatter, IDbgExceptionFormatterMetadata>>());
				list.Add(lz);
			}
			toFormatters = new Dictionary<string, Lazy<DbgExceptionFormatter, IDbgExceptionFormatterMetadata>[]>(dict.Count);
			foreach (var kv in dict)
				toFormatters[kv.Key] = kv.Value.ToArray();
		}

		public override void WriteName(IDbgTextWriter writer, DbgExceptionDefinition definition, bool includeDescription) {
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));
			if (definition.Id.Category == null)
				throw new ArgumentException();
			WriteNameCore(writer, definition);
			if (includeDescription && definition.Description != null) {
				writer.Write(DbgTextColor.Text, " ");
				WriteDescription(writer, definition);
			}
		}

		void WriteNameCore(IDbgTextWriter writer, DbgExceptionDefinition definition) {
			if (!definition.Id.IsDefaultId && toFormatters.TryGetValue(definition.Id.Category, out var formatters)) {
				foreach (var formatter in formatters) {
					if (formatter.Value.WriteName(writer, definition))
						return;
				}
			}
			DefaultWriteName(writer, definition);
		}

		void DefaultWriteName(IDbgTextWriter output, DbgExceptionDefinition definition) {
			switch (definition.Id.Kind) {
			case DbgExceptionIdKind.DefaultId:
				if (exceptionSettingsService.Value.TryGetCategoryDefinition(definition.Id.Category, out var categoryDef))
					output.Write(DbgTextColor.Text, string.Format(dnSpy_Debugger_Resources.AllRemainingExceptionsNotInList, categoryDef.DisplayName));
				else
					WriteError(output);
				break;

			case DbgExceptionIdKind.Code:
				DbgExceptionCategoryDefinitionFlags flags;
				if (exceptionSettingsService.Value.TryGetCategoryDefinition(definition.Id.Category, out categoryDef))
					flags = categoryDef.Flags;
				else
					flags = DbgExceptionCategoryDefinitionFlags.None;
				if ((flags & DbgExceptionCategoryDefinitionFlags.DecimalCode) == 0)
					output.Write(DbgTextColor.Number, "0x" + definition.Id.Code.ToString("X8"));
				else if ((flags & DbgExceptionCategoryDefinitionFlags.UnsignedCode) != 0)
					output.Write(DbgTextColor.Number, ((uint)definition.Id.Code).ToString());
				else
					output.Write(DbgTextColor.Number, definition.Id.Code.ToString());
				break;

			case DbgExceptionIdKind.Name:
				output.Write(DbgTextColor.Keyword, definition.Id.Name);
				break;

			default:
				WriteError(output);
				break;
			}
		}

		void WriteDescription(IDbgTextWriter writer, DbgExceptionDefinition definition) {
			if (definition.Description == null)
				return;
			writer.Write(DbgTextColor.Comment, "(");
			writer.Write(DbgTextColor.Comment, definition.Description);
			writer.Write(DbgTextColor.Comment, ")");
		}

		void WriteError(IDbgTextWriter output) => output.Write(DbgTextColor.Error, "???");

		public override void WriteName(IDbgTextWriter writer, DbgExceptionId id, bool includeDescription) {
			if (!exceptionSettingsService.Value.TryGetDefinition(id, out var def))
				def = new DbgExceptionDefinition(id, DbgExceptionDefinitionFlags.None, description: null);
			WriteName(writer, def, includeDescription);
		}
	}
}
