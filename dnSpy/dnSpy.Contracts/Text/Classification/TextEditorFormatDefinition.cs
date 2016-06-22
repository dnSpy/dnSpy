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
using System.Windows.Media;
using dnSpy.Contracts.Themes;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Base class of all default text editor format definitions. Use <see cref="ExportTextEditorFormatDefinitionAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class TextEditorFormatDefinition : TextFormatDefinition {
		/// <summary>
		/// Gets the name shown in the UI
		/// </summary>
		public abstract string DisplayName { get; }

		/// <summary>
		/// Gets the background brush of the window
		/// </summary>
		/// <param name="theme">Theme</param>
		/// <returns></returns>
		public virtual Brush GetWindowBackground(ITheme theme) => null;
	}

	/// <summary>Metadata</summary>
	public interface ITextEditorFormatDefinitionMetadata {
		/// <summary>See <see cref="ExportTextEditorFormatDefinitionAttribute.Category"/></summary>
		string Category { get; }
		/// <summary>See <see cref="ExportTextEditorFormatDefinitionAttribute.BaseType"/></summary>
		string BaseType { get; }
	}

	/// <summary>
	/// Exports an <see cref="TextEditorFormatDefinition"/>
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportTextEditorFormatDefinitionAttribute : ExportAttribute, ITextEditorFormatDefinitionMetadata {
		/// <summary>
		/// Gets the category, eg. <see cref="AppearanceCategoryConstants.TextEditor"/>
		/// </summary>
		public string Category { get; }

		/// <summary>
		/// Gets the base type category or null
		/// </summary>
		public string BaseType { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="category">Category, eg. <see cref="AppearanceCategoryConstants.Viewer"/></param>
		/// <param name="baseType">Base type (eg. <see cref="AppearanceCategoryConstants.TextEditor"/>) or null</param>
		public ExportTextEditorFormatDefinitionAttribute(string category, string baseType = null)
			: base(typeof(TextEditorFormatDefinition)) {
			if (string.IsNullOrEmpty(category))
				throw new ArgumentOutOfRangeException(nameof(category));
			Category = category;
			BaseType = baseType;
		}
	}
}
