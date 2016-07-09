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
using System.Linq;

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Decompiler settings. The class must override <see cref="object.GetHashCode()"/> and
	/// <see cref="object.Equals(object)"/>.
	/// </summary>
	public interface IDecompilerSettings {
		/// <summary>
		/// Clones the settings
		/// </summary>
		/// <returns></returns>
		IDecompilerSettings Clone();

		/// <summary>
		/// Gets all options
		/// </summary>
		IEnumerable<IDecompilerOption> Options { get; }
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class DecomplierSettingsExtensions {
		/// <summary>
		/// Returns an option or null
		/// </summary>
		/// <param name="self">This</param>
		/// <param name="guid">Guid</param>
		/// <returns></returns>
		public static IDecompilerOption TryGetOption(this IDecompilerSettings self, Guid guid) => self.Options.FirstOrDefault(a => a.Guid == guid);

		/// <summary>
		/// Returns an option or null
		/// </summary>
		/// <param name="self">This</param>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public static IDecompilerOption TryGetOption(this IDecompilerSettings self, string name) => self.Options.FirstOrDefault(a => StringComparer.Ordinal.Equals(a.Name, name));

		/// <summary>
		/// Returns a boolean or false if the option doesn't exist
		/// </summary>
		/// <param name="self">This</param>
		/// <param name="guid">Guid</param>
		/// <returns></returns>
		public static bool GetBoolean(this IDecompilerSettings self, Guid guid) => self.TryGetOption(guid)?.Value as bool? ?? false;

		/// <summary>
		/// Returns a boolean or false if the option doesn't exist
		/// </summary>
		/// <param name="self">This</param>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public static bool GetBoolean(this IDecompilerSettings self, string name) => self.TryGetOption(name)?.Value as bool? ?? false;
	}
}
