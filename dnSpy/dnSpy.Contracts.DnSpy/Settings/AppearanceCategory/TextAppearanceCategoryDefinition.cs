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

using dnSpy.Contracts.Themes;

namespace dnSpy.Contracts.Settings.AppearanceCategory {
	/// <summary>
	/// Text appearance category definition
	/// </summary>
	abstract class TextAppearanceCategoryDefinition {
		/// <summary>
		/// Constructor
		/// </summary>
		protected TextAppearanceCategoryDefinition() { }

		/// <summary>
		/// true if the user can change the settings
		/// </summary>
		public virtual bool IsUserVisible => true;

		/// <summary>
		/// Text shown in the UI
		/// </summary>
		public abstract string DisplayName { get; }

		/// <summary>
		/// Appearance category, eg. <see cref="AppearanceCategoryConstants.TextEditor"/>
		/// </summary>
		public abstract string Category { get; }

		/// <summary>
		/// Text color
		/// </summary>
		public abstract ColorType ColorType { get; }
	}
}
