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
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using dnSpy.Contracts.Debugger.Exceptions;

namespace dnSpy.Debugger.Exceptions {
	sealed class ExceptionsFileReader {
		public List<DbgExceptionGroupDefinition> GroupDefinitions { get; } = new List<DbgExceptionGroupDefinition>();
		public List<DbgExceptionDefinition> ExceptionDefinitions { get; } = new List<DbgExceptionDefinition>();

		public void Read(string filename) {
			try {
				if (!File.Exists(filename))
					return;
				var doc = XDocument.Load(filename, LoadOptions.None);
				var root = doc.Root;
				if (root.Name == "Exceptions") {
					foreach (var groupDefElem in root.Elements("GroupDef")) {
						var name = (string)groupDefElem.Attribute("Name");
						var displayName = (string)groupDefElem.Attribute("DisplayName");
						var shortDisplayName = (string)groupDefElem.Attribute("ShortDisplayName");
						var flagsAttr = (string)groupDefElem.Attribute("Flags");
						if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(shortDisplayName))
							continue;
						var flags = ParseGroupFlags(flagsAttr);
						GroupDefinitions.Add(new DbgExceptionGroupDefinition(flags, name, displayName, shortDisplayName));
					}
					foreach (var exDefCollElem in root.Elements("ExceptionDefs")) {
						var groupName = (string)exDefCollElem.Attribute("Group");
						if (string.IsNullOrWhiteSpace(groupName))
							continue;
						foreach (var exDefElem in exDefCollElem.Elements("Exception")) {
							var name = (string)exDefElem.Attribute("Name");
							var code = (string)exDefElem.Attribute("Code");
							var description = (string)exDefElem.Attribute("Description");
							if (string.IsNullOrWhiteSpace(description))
								description = null;
							var flagsAttr = (string)exDefElem.Attribute("Flags");
							DbgExceptionId id;
							if (code == null) {
								if (string.IsNullOrWhiteSpace(name))
									continue;
								id = new DbgExceptionId(groupName, name);
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
								id = new DbgExceptionId(groupName, code);
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
		static DbgExceptionGroupDefinitionFlags ParseGroupFlags(string flagsAttr) {
			var flags = DbgExceptionGroupDefinitionFlags.None;
			if (flagsAttr != null) {
				foreach (var name in flagsAttr.Split(flagsSeparators, StringSplitOptions.RemoveEmptyEntries)) {
					switch (name.Trim().ToLowerInvariant()) {
					case "code":		flags |= DbgExceptionGroupDefinitionFlags.Code; break;
					case "decimal":		flags |= DbgExceptionGroupDefinitionFlags.DecimalCode; break;
					case "unsigned":	flags |= DbgExceptionGroupDefinitionFlags.UnsignedCode; break;
					}
				}
			}
			return flags;
		}

		static DbgExceptionDefinitionFlags ParseExceptionFlags(string flagsAttr) {
			var flags = DbgExceptionDefinitionFlags.None;
			if (flagsAttr != null) {
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
