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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Settings.HexGroups {
	/// <summary>
	/// Contains a group of <see cref="HexView"/>s that share a subset of all <see cref="HexView"/> options
	/// </summary>
	public abstract class HexViewOptionsGroup {
		/// <summary>
		/// Gets all hex views in this group
		/// </summary>
		public abstract IEnumerable<WpfHexView> HexViews { get; }

		/// <summary>
		/// Raised when an option has changed
		/// </summary>
		public abstract event EventHandler<HexViewOptionChangedEventArgs> HexViewOptionChanged;

		/// <summary>
		/// Returns true if the option is shared by all hex views in this group
		/// </summary>
		/// <param name="tag">Hex buffer tag, eg. <see cref="PredefinedHexBufferTags.File"/></param>
		/// <param name="optionId">Option name</param>
		/// <returns></returns>
		public abstract bool HasOption(string tag, string optionId);

		/// <summary>
		/// Returns true if the option is shared by all hex views in this group
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="tag">Hex buffer tag, eg. <see cref="PredefinedHexBufferTags.File"/></param>
		/// <param name="option">Option</param>
		/// <returns></returns>
		public abstract bool HasOption<T>(string tag, VSTE.EditorOptionKey<T> option);

		/// <summary>
		/// Gets the current value
		/// </summary>
		/// <param name="tag">Hex buffer tag, eg. <see cref="PredefinedHexBufferTags.File"/></param>
		/// <param name="optionId">Option name</param>
		/// <returns></returns>
		public abstract object GetOptionValue(string tag, string optionId);

		/// <summary>
		/// Gets the current value
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="tag">Hex buffer tag, eg. <see cref="PredefinedHexBufferTags.File"/></param>
		/// <param name="option">Option</param>
		/// <returns></returns>
		public abstract T GetOptionValue<T>(string tag, VSTE.EditorOptionKey<T> option);

		/// <summary>
		/// Writes a new value
		/// </summary>
		/// <param name="tag">Hex buffer tag, eg. <see cref="PredefinedHexBufferTags.File"/></param>
		/// <param name="optionId">Option name</param>
		/// <param name="value">New value</param>
		public abstract void SetOptionValue(string tag, string optionId, object value);

		/// <summary>
		/// Writes a new value
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="tag">Hex buffer tag, eg. <see cref="PredefinedHexBufferTags.File"/></param>
		/// <param name="option">Option</param>
		/// <param name="value">New value</param>
		public abstract void SetOptionValue<T>(string tag, VSTE.EditorOptionKey<T> option, T value);
	}
}
