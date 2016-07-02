/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text.MEF;

namespace dnSpy.Text.Classification {
	sealed class TextEditorFontSettingsDictionaryCreator {
		public Dictionary<string, TextEditorFontSettings> Result { get; }
		public TextEditorFontSettings DefaultSettings { get; }
		readonly ITextEditorSettings textEditorSettings;
		readonly Dictionary<string, Lazy<TextEditorFormatDefinition, ITextEditorFormatDefinitionMetadata>> toDef;

		public TextEditorFontSettingsDictionaryCreator(ITextEditorSettings textEditorSettings, IEnumerable<Lazy<TextEditorFormatDefinition, ITextEditorFormatDefinitionMetadata>> textEditorFormatDefinitions) {
			Result = new Dictionary<string, TextEditorFontSettings>(StringComparer.Ordinal);
			DefaultSettings = CreateDefaultTextEditorFontSettings(textEditorSettings);
			Result.Add(AppearanceCategoryConstants.TextEditor, DefaultSettings);
			this.textEditorSettings = textEditorSettings;
			this.toDef = new Dictionary<string, Lazy<TextEditorFormatDefinition, ITextEditorFormatDefinitionMetadata>>(StringComparer.Ordinal);
			var allDefs = textEditorFormatDefinitions.ToArray();
			foreach (var def in allDefs) {
				Debug.Assert(!toDef.ContainsKey(def.Metadata.Name));
				toDef[def.Metadata.Name] = def;
			}
			foreach (var def in allDefs)
				Create(def.Metadata.Name);
		}

		sealed class TextEditorFormatDefinitionMetadata : ITextEditorFormatDefinitionMetadata {
			public string BaseDefinition { get; }
			public string Name { get; }
			public TextEditorFormatDefinitionMetadata(string name) {
				Name = name;
			}
		}

		TextEditorFontSettings CreateDefaultTextEditorFontSettings(ITextEditorSettings textEditorSettings) {
			var md = new TextEditorFormatDefinitionMetadata(AppearanceCategoryConstants.TextEditor);
			var def = new DefaultTextEditorFormatDefinition(textEditorSettings);
			var lazy = new Lazy<TextEditorFormatDefinition, ITextEditorFormatDefinitionMetadata>(() => def, md);
			var dummyInitValue = lazy.Value;
			return new TextEditorFontSettings(textEditorSettings, lazy, null);
		}

		TextEditorFontSettings Create(string category) {
			if (category == null)
				return null;
			TextEditorFontSettings settings;
			if (Result.TryGetValue(category, out settings))
				return settings;

			Lazy<TextEditorFormatDefinition, ITextEditorFormatDefinitionMetadata> def;
			if (!toDef.TryGetValue(category, out def))
				return null;

			var baseType = Create(def.Metadata.BaseDefinition) ?? DefaultSettings;
			settings = new TextEditorFontSettings(textEditorSettings, def, baseType);
			Result.Add(category, settings);
			return settings;
		}
	}
}
