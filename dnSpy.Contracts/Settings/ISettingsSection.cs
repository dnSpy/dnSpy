/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Linq;

namespace dnSpy.Contracts.Settings {
	/// <summary>
	/// Settings section
	/// </summary>
	public interface ISettingsSection : ISettingsSectionCreator {
		/// <summary>
		/// Name of section
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets all attributes
		/// </summary>
		Tuple<string, string>[] Attributes { get; }

		/// <summary>
		/// Adds or overwrites an existing attribute with a new value
		/// </summary>
		/// <typeparam name="T">Type of value</typeparam>
		/// <param name="name">Name of attribute</param>
		/// <param name="value">Value</param>
		void Attribute<T>(string name, T value);

		/// <summary>
		/// Gets the value of the attribute or the default value if it's not present
		/// </summary>
		/// <typeparam name="T">Type of value</typeparam>
		/// <param name="name">Name of attribute</param>
		/// <returns></returns>
		T Attribute<T>(string name);

		/// <summary>
		/// Removes an attribute
		/// </summary>
		/// <param name="name">Name of attribute</param>
		void RemoveAttribute(string name);
	}
}
