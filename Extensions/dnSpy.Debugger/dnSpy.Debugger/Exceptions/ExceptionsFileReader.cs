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
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using dnSpy.Contracts.Debugger.Exceptions;

namespace dnSpy.Debugger.Exceptions {
	sealed class ExceptionsFileReader {
		public List<DbgExceptionCategoryDefinition> CategoryDefinitions { get; } = new List<DbgExceptionCategoryDefinition>();
		public List<DbgExceptionDefinition> ExceptionDefinitions { get; } = new List<DbgExceptionDefinition>();

		public void Read(string filename) {
			try {
				if (!File.Exists(filename))
					return;
				var doc = XDocument.Load(filename, LoadOptions.None);
				var root = doc.Root;
				if (root?.Name == "Exceptions") {
					foreach (var categoryDefElem in root.Elements("CategoryDef")) {
						var name = (string?)categoryDefElem.Attribute("Name");
						var displayName = (string?)categoryDefElem.Attribute("DisplayName");
						var shortDisplayName = (string?)categoryDefElem.Attribute("ShortDisplayName");
						var flagsAttr = (string?)categoryDefElem.Attribute("Flags");
						if (string2.IsNullOrWhiteSpace(name) || string2.IsNullOrWhiteSpace(displayName) || string2.IsNullOrWhiteSpace(shortDisplayName))
							continue;
						var flags = ParseCategoryFlags(flagsAttr);
						CategoryDefinitions.Add(new DbgExceptionCategoryDefinition(flags, name, displayName, shortDisplayName));
					}
					foreach (var exDefCollElem in root.Elements("ExceptionDefs")) {
						var category = (string?)exDefCollElem.Attribute("Category");
						if (string2.IsNullOrWhiteSpace(category))
							continue;
						foreach (var exDefElem in exDefCollElem.Elements("Exception")) {
							var name = (string?)exDefElem.Attribute("Name");
							var code = (string?)exDefElem.Attribute("Code");
							string? description = (string?)exDefElem.Attribute("Description");
							if (string2.IsNullOrWhiteSpace(description))
								description = null;
							var flagsAttr = (string?)exDefElem.Attribute("Flags");
							DbgExceptionId id;
							if (code is null) {
								if (string2.IsNullOrWhiteSpace(name))
									continue;
								id = new DbgExceptionId(category, name);
							}
							else {
								code = code.Trim();
								bool isHex = code.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || code.StartsWith("&H", StringComparison.OrdinalIgnoreCase);
								if (isHex) {
									code = code.Substring(2);
									if (code != code.Trim() || code.StartsWith("-") || code.StartsWith("+"))
										continue;
									if (!int.TryParse(code, NumberStyles.HexNumber, null, out int codeValue)) {
										if (!uint.TryParse(code, NumberStyles.HexNumber, null, out uint codeValueU))
											continue;
										codeValue = (int)codeValueU;
									}
								}
								else {
									if (!int.TryParse(code, out int codeValue)) {
										if (!uint.TryParse(code, out uint codeValueU))
											continue;
										codeValue = (int)codeValueU;
									}
								}
								id = new DbgExceptionId(category, code);
							}
							ExceptionDefinitions.Add(new DbgExceptionDefinition(id, ParseExceptionFlags(flagsAttr), description));
						}
					}
				}
			}
			catch {
			}
		}

		static readonly char[] flagsSeparators = new char[] { ',' };
		static DbgExceptionCategoryDefinitionFlags ParseCategoryFlags(string? flagsAttr) {
			var flags = DbgExceptionCategoryDefinitionFlags.None;
			if (flagsAttr is not null) {
				foreach (var name in flagsAttr.Split(flagsSeparators, StringSplitOptions.RemoveEmptyEntries)) {
					switch (name.Trim().ToLowerInvariant()) {
					case "code":		flags |= DbgExceptionCategoryDefinitionFlags.Code; break;
					case "decimal":		flags |= DbgExceptionCategoryDefinitionFlags.DecimalCode; break;
					case "unsigned":	flags |= DbgExceptionCategoryDefinitionFlags.UnsignedCode; break;
					}
				}
			}
			return flags;
		}

		static DbgExceptionDefinitionFlags ParseExceptionFlags(string? flagsAttr) {
			var flags = DbgExceptionDefinitionFlags.None;
			if (flagsAttr is not null) {
				foreach (var name in flagsAttr.Split(flagsSeparators, StringSplitOptions.RemoveEmptyEntries)) {
					switch (name.Trim().ToLowerInvariant()) {
					case "stop1":		flags |= DbgExceptionDefinitionFlags.StopFirstChance; break;
					case "stop2":		flags |= DbgExceptionDefinitionFlags.StopSecondChance; break;
					}
				}
			}
			return flags;
		}
	}
}
