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
using dnSpy.Contracts.Hex.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Settings.HexGroups {
	/// <summary>
	/// Option definition
	/// </summary>
	public class TagOptionDefinition {
		/// <summary>
		/// Sub group, eg. <see cref="PredefinedHexViewRoles.HexEditorGroupDefault"/>.
		/// Use <see cref="string.Empty"/> to add default options.
		/// </summary>
		public string SubGroup { get; set; }

		/// <summary>
		/// Hex view option name, eg. <see cref="DefaultHexViewOptions.BytesPerLineId"/>
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Default value
		/// </summary>
		public object DefaultValue { get; set; }

		/// <summary>
		/// Gets the type
		/// </summary>
		public Type Type { get; set; }

		/// <summary>
		/// true if the option can be saved
		/// </summary>
		public bool CanBeSaved { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		protected TagOptionDefinition() => CanBeSaved = true;
	}

	/// <summary>
	/// Option definition
	/// </summary>
	public class TagOptionDefinition<T> : TagOptionDefinition {
		/// <summary>
		/// Constructor
		/// </summary>
		public TagOptionDefinition() {
			DefaultValue = default(T);
			Type = typeof(T);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="option">Name of option</param>
		public TagOptionDefinition(VSTE.EditorOptionKey<T> option)
			: this() => Name = option.Name;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="optionId">Name of option</param>
		public TagOptionDefinition(string optionId)
			: this() => Name = optionId;
	}
}
