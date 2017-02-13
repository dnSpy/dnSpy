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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Settings.Fonts {
	/// <summary>
	/// Defines a font. Use <see cref="ExportThemeFontSettingsDefinitionAttribute"/> to
	/// export a field.
	/// </summary>
	public sealed class ThemeFontSettingsDefinition {
	}

	/// <summary>Metadata</summary>
	public interface IThemeFontSettingsDefinitionMetadata {
		/// <summary>See <see cref="ExportThemeFontSettingsDefinitionAttribute.Name"/></summary>
		string Name { get; }
		/// <summary>See <see cref="ExportThemeFontSettingsDefinitionAttribute.FontType"/></summary>
		FontType FontType { get; }
	}

	/// <summary>
	/// Exports a <see cref="ThemeFontSettingsDefinition"/> field
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ExportThemeFontSettingsDefinitionAttribute : ExportAttribute, IThemeFontSettingsDefinitionMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name</param>
		/// <param name="fontType">Font type</param>
		public ExportThemeFontSettingsDefinitionAttribute(string name, FontType fontType) {
			Name = name ?? throw new ArgumentNullException(nameof(name));
			FontType = fontType;
		}

		/// <summary>
		/// Name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Font type
		/// </summary>
		public FontType FontType { get; }
	}
}
