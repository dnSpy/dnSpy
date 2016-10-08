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
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.CodeEditor {
	sealed class CodeEditorOptionsStorage {
		static readonly Guid SETTINGS_GUID = new Guid("0F4757A7-B66D-4433-8BA2-8E81EEB6C3C7");
		const string OPTIONS_NAME = "Options";
		const string GuidAttr = "guid";

		readonly ISettingsSection settingsSection;
		readonly Dictionary<CodeEditorOptions, ISettingsSection> toSettingsSection;

		public CodeEditorOptionsStorage(ISettingsService settingsService, CodeEditorOptionsCollection codeEditorOptionsCollection) {
			if (settingsService == null)
				throw new ArgumentNullException(nameof(settingsService));
			if (codeEditorOptionsCollection == null)
				throw new ArgumentNullException(nameof(codeEditorOptionsCollection));
			this.toSettingsSection = new Dictionary<CodeEditorOptions, ISettingsSection>();
			this.settingsSection = settingsService.GetOrCreateSection(SETTINGS_GUID);
			ReadSettings(codeEditorOptionsCollection);
		}

		void ReadSettings(CodeEditorOptionsCollection codeEditorOptionsCollection) {
			foreach (var sect in settingsSection.SectionsWithName(OPTIONS_NAME)) {
				var guid = sect.Attribute<Guid?>(GuidAttr);
				var options = guid == null ? null : codeEditorOptionsCollection.Find(guid.Value);
				if (options == null)
					continue;

				toSettingsSection[options] = sect;
				options.UseVirtualSpace = sect.Attribute<bool?>(nameof(options.UseVirtualSpace)) ?? DefaultCodeEditorOptions.UseVirtualSpace;
				options.WordWrapStyle = sect.Attribute<WordWrapStyles?>(nameof(options.WordWrapStyle)) ?? DefaultCodeEditorOptions.WordWrapStyle;
				options.ShowLineNumbers = sect.Attribute<bool?>(nameof(options.ShowLineNumbers)) ?? DefaultCodeEditorOptions.ShowLineNumbers;
				options.TabSize = sect.Attribute<int?>(nameof(options.TabSize)) ?? DefaultCodeEditorOptions.TabSize;
				options.IndentSize = sect.Attribute<int?>(nameof(options.IndentSize)) ?? DefaultCodeEditorOptions.IndentSize;
				options.ConvertTabsToSpaces = sect.Attribute<bool?>(nameof(options.ConvertTabsToSpaces)) ?? DefaultCodeEditorOptions.ConvertTabsToSpaces;
			}
		}

		public void Write(CodeEditorOptions options) {
			ISettingsSection sect;
			if (!toSettingsSection.TryGetValue(options, out sect)) {
				toSettingsSection.Add(options, sect = settingsSection.CreateSection(OPTIONS_NAME));
				sect.Attribute(GuidAttr, options.Guid);
			}

			sect.Attribute(nameof(options.UseVirtualSpace), options.UseVirtualSpace);
			sect.Attribute(nameof(options.WordWrapStyle), options.WordWrapStyle);
			sect.Attribute(nameof(options.ShowLineNumbers), options.ShowLineNumbers);
			sect.Attribute(nameof(options.TabSize), options.TabSize);
			sect.Attribute(nameof(options.IndentSize), options.IndentSize);
			sect.Attribute(nameof(options.ConvertTabsToSpaces), options.ConvertTabsToSpaces);
		}
	}
}
