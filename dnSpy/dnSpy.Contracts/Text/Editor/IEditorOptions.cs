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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Text editor options
	/// </summary>
	public interface IEditorOptions {
		/// <summary>
		/// Gets/sets the parent options
		/// </summary>
		IEditorOptions Parent { get; set; }

		/// <summary>
		/// Gets the global options
		/// </summary>
		IEditorOptions GlobalOptions { get; }

		/// <summary>
		/// Gets all supported options
		/// </summary>
		IEnumerable<EditorOptionDefinition> SupportedOptions { get; }

		/// <summary>
		/// Raised when an option has been changed
		/// </summary>
		event EventHandler<EditorOptionChangedEventArgs> OptionChanged;

		/// <summary>
		/// Returns true if the option is defined
		/// </summary>
		/// <param name="optionId">Option id</param>
		/// <param name="localScopeOnly">true to only check the current scope</param>
		/// <returns></returns>
		bool IsOptionDefined(string optionId, bool localScopeOnly);

		/// <summary>
		/// Returns true if the option is defined
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="key">Option key</param>
		/// <param name="localScopeOnly">true to only check the current scope</param>
		/// <returns></returns>
		bool IsOptionDefined<T>(EditorOptionKey<T> key, bool localScopeOnly);

		/// <summary>
		/// Gets the value
		/// </summary>
		/// <param name="optionId">Option id</param>
		/// <returns></returns>
		object GetOptionValue(string optionId);

		/// <summary>
		/// Gets the value
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="optionId">Option id</param>
		/// <returns></returns>
		T GetOptionValue<T>(string optionId);

		/// <summary>
		/// Gets the value
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="key">Option key</param>
		/// <returns></returns>
		T GetOptionValue<T>(EditorOptionKey<T> key);

		/// <summary>
		/// Sets a new value
		/// </summary>
		/// <param name="optionId">Option id</param>
		/// <param name="value">Value</param>
		void SetOptionValue(string optionId, object value);

		/// <summary>
		/// Sets a new value
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="key">Option key</param>
		/// <param name="value">Value</param>
		void SetOptionValue<T>(EditorOptionKey<T> key, T value);

		/// <summary>
		/// Clears the option
		/// </summary>
		/// <param name="optionId">Option id</param>
		/// <returns></returns>
		bool ClearOptionValue(string optionId);

		/// <summary>
		/// Clears the option
		/// </summary>
		/// <typeparam name="T">Value type</typeparam>
		/// <param name="key">Option key</param>
		/// <returns></returns>
		bool ClearOptionValue<T>(EditorOptionKey<T> key);
	}
}
