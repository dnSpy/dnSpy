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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Defines code editor options that will be shown in the UI. Use <see cref="ExportCodeEditorOptionsDefinitionAttribute"/>
	/// to export an instance.
	/// </summary>
	public sealed class CodeEditorOptionsDefinition {
	}

	/// <summary>Metadata</summary>
	public interface ICodeEditorOptionsDefinitionMetadata {
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.ContentType"/></summary>
		string ContentType { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.Guid"/></summary>
		string Guid { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.LanguageName"/></summary>
		string LanguageName { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.UseVirtualSpace"/></summary>
		bool UseVirtualSpace { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.WordWrapStyle"/></summary>
		WordWrapStyles WordWrapStyle { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.ShowLineNumbers"/></summary>
		bool ShowLineNumbers { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.TabSize"/></summary>
		int TabSize { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.IndentSize"/></summary>
		int IndentSize { get; }
		/// <summary>See <see cref="ExportCodeEditorOptionsDefinitionAttribute.ConvertTabsToSpaces"/></summary>
		bool ConvertTabsToSpaces { get; }
	}

	/// <summary>
	/// Exports a <see cref="CodeEditorOptionsDefinition"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ExportCodeEditorOptionsDefinitionAttribute : ExportAttribute, ICodeEditorOptionsDefinitionMetadata {
		/// <summary>Constructor</summary>
		/// <param name="languageName">Language name shown in the UI</param>
		/// <param name="contentType">Content type, eg. <see cref="ContentTypes.CSharpRoslyn"/></param>
		/// <param name="guid">Guid of settings, eg. <see cref="AppSettingsConstants.GUID_CODE_EDITOR_CSHARP_ROSLYN"/></param>
		public ExportCodeEditorOptionsDefinitionAttribute(string languageName, string contentType, string guid)
			: base(typeof(CodeEditorOptionsDefinition)) {
			if (languageName == null)
				throw new ArgumentNullException(nameof(languageName));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			if (guid == null)
				throw new ArgumentNullException(nameof(guid));
			ContentType = contentType;
			Guid = guid;
			LanguageName = languageName;
			UseVirtualSpace = DefaultCodeEditorOptions.UseVirtualSpace;
			WordWrapStyle = DefaultCodeEditorOptions.WordWrapStyle;
			ShowLineNumbers = DefaultCodeEditorOptions.ShowLineNumbers;
			TabSize = DefaultCodeEditorOptions.TabSize;
			IndentSize = DefaultCodeEditorOptions.IndentSize;
			ConvertTabsToSpaces = DefaultCodeEditorOptions.ConvertTabsToSpaces;
		}

		/// <summary>
		/// Content type
		/// </summary>
		public string ContentType { get; }

		/// <summary>
		/// Guid of settings
		/// </summary>
		public string Guid { get; }

		/// <summary>
		/// Language name
		/// </summary>
		public string LanguageName { get; }

		/// <summary>
		/// Use virtual space, default value is <see cref="DefaultCodeEditorOptions.UseVirtualSpace"/>
		/// </summary>
		public bool UseVirtualSpace { get; set; }

		/// <summary>
		/// Word wrap style, default value is <see cref="DefaultCodeEditorOptions.WordWrapStyle"/>
		/// </summary>
		public WordWrapStyles WordWrapStyle { get; set; }

		/// <summary>
		/// Show line numbers, default value is <see cref="DefaultCodeEditorOptions.ShowLineNumbers"/>
		/// </summary>
		public bool ShowLineNumbers { get; set; }

		/// <summary>
		/// Tab size, default value is <see cref="DefaultCodeEditorOptions.TabSize"/>
		/// </summary>
		public int TabSize { get; set; }

		/// <summary>
		/// Indent size, default value is <see cref="DefaultCodeEditorOptions.IndentSize"/>
		/// </summary>
		public int IndentSize { get; set; }

		/// <summary>
		/// true to convert tabs to spaces, default value is <see cref="DefaultCodeEditorOptions.ConvertTabsToSpaces"/>
		/// </summary>
		public bool ConvertTabsToSpaces { get; set; }
	}
}
