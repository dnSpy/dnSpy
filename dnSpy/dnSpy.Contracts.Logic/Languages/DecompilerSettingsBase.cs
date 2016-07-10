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
	/// Decompiler settings
	/// </summary>
	public abstract class DecompilerSettingsBase {
		/// <summary>
		/// Clones the settings
		/// </summary>
		/// <returns></returns>
		public abstract DecompilerSettingsBase Clone();

		/// <summary>
		/// Gets all options
		/// </summary>
		public abstract IEnumerable<IDecompilerOption> Options { get; }

		/// <summary>
		/// Returns an option or null
		/// </summary>
		/// <param name="guid">Guid</param>
		/// <returns></returns>
		public IDecompilerOption TryGetOption(Guid guid) => Options.FirstOrDefault(a => a.Guid == guid);

		/// <summary>
		/// Returns an option or null
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public IDecompilerOption TryGetOption(string name) => Options.FirstOrDefault(a => StringComparer.Ordinal.Equals(a.Name, name));

		/// <summary>
		/// Returns a boolean or false if the option doesn't exist
		/// </summary>
		/// <param name="guid">Guid</param>
		/// <returns></returns>
		public bool GetBoolean(Guid guid) => TryGetOption(guid)?.Value as bool? ?? false;

		/// <summary>
		/// Returns a boolean or false if the option doesn't exist
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public bool GetBoolean(string name) => TryGetOption(name)?.Value as bool? ?? false;

		/// <summary>
		/// Returns true if this instance equals <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Other object, may be null</param>
		/// <returns></returns>
		protected abstract bool EqualsCore(object obj);

		/// <summary>
		/// Gets the hash code of this instance
		/// </summary>
		/// <returns></returns>
		protected abstract int GetHashCodeCore();

		/// <summary>
		/// Returns true if this instance equals <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Other object, may be null</param>
		/// <returns></returns>
		public sealed override bool Equals(object obj) => EqualsCore(obj);

		/// <summary>
		/// Gets the hash code of this instance
		/// </summary>
		/// <returns></returns>
		public sealed override int GetHashCode() => GetHashCodeCore();
	}
}
