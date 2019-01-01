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
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Settings.Groups {
	/// <summary>
	/// Contains a group of <see cref="ITextView"/>s that share a subset of all <see cref="ITextView"/> options
	/// </summary>
	public interface ITextViewOptionsGroup {
		/// <summary>
		/// Gets all text views in this group
		/// </summary>
		IEnumerable<IWpfTextView> TextViews { get; }

		/// <summary>
		/// Raised when an option has changed
		/// </summary>
		event EventHandler<TextViewOptionChangedEventArgs> TextViewOptionChanged;

		/// <summary>
		/// Returns true if the option is shared by all text views in this group
		/// </summary>
		/// <param name="contentType">Content type, eg. <see cref="ContentTypes.CSharpRoslyn"/></param>
		/// <param name="optionId">Option name</param>
		/// <returns></returns>
		bool HasOption(string contentType, string optionId);

		/// <summary>
		/// Returns true if the option is shared by all text views in this group
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="contentType">Content type, eg. <see cref="ContentTypes.CSharpRoslyn"/></param>
		/// <param name="option">Option</param>
		/// <returns></returns>
		bool HasOption<T>(string contentType, EditorOptionKey<T> option);

		/// <summary>
		/// Gets the current value
		/// </summary>
		/// <param name="contentType">Content type, eg. <see cref="ContentTypes.CSharpRoslyn"/></param>
		/// <param name="optionId">Option name</param>
		/// <returns></returns>
		object GetOptionValue(string contentType, string optionId);

		/// <summary>
		/// Gets the current value
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="contentType">Content type, eg. <see cref="ContentTypes.CSharpRoslyn"/></param>
		/// <param name="option">Option</param>
		/// <returns></returns>
		T GetOptionValue<T>(string contentType, EditorOptionKey<T> option);

		/// <summary>
		/// Writes a new value
		/// </summary>
		/// <param name="contentType">Content type, eg. <see cref="ContentTypes.CSharpRoslyn"/></param>
		/// <param name="optionId">Option name</param>
		/// <param name="value">New value</param>
		void SetOptionValue(string contentType, string optionId, object value);

		/// <summary>
		/// Writes a new value
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="contentType">Content type, eg. <see cref="ContentTypes.CSharpRoslyn"/></param>
		/// <param name="option">Option</param>
		/// <param name="value">New value</param>
		void SetOptionValue<T>(string contentType, EditorOptionKey<T> option, T value);
	}
}
