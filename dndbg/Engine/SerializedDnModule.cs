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

namespace dndbg.Engine {
	public struct SerializedDnModule : IEquatable<SerializedDnModule> {
		/// <summary>
		/// Name of module. This is the filename if <see cref="IsInMemory"/> is false, else it's <see cref="dnlib.DotNet.ModuleDef.Name"/>
		/// </summary>
		public string Name {
			get { return name; }
		}
		readonly string name;

		/// <summary>
		/// true if it's a dynamic module (types can be added)
		/// </summary>
		public bool IsDynamic {
			get { return isDynamic; }
		}
		readonly bool isDynamic;

		/// <summary>
		/// true if the module only exists in memory
		/// </summary>
		public bool IsInMemory {
			get { return isInMemory; }
		}
		readonly bool isInMemory;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Full path of module on disk</param>
		public SerializedDnModule(string filename)
			: this(filename, false, false) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Full path of module on disk or name of module if it's an in-memory module</param>
		/// <param name="isDynamic">true if it's a dynamic module (types can be added)</param>
		/// <param name="isInMemory">true if it exists only in memory</param>
		public SerializedDnModule(string name, bool isDynamic, bool isInMemory) {
			this.name = name ?? string.Empty;
			this.isDynamic = isDynamic;
			this.isInMemory = isInMemory;
		}

		public static bool operator ==(SerializedDnModule a, SerializedDnModule b) {
			return a.Equals(b);
		}

		public static bool operator !=(SerializedDnModule a, SerializedDnModule b) {
			return !a.Equals(b);
		}

		public bool Equals(SerializedDnModule other) {
			return isDynamic == other.isDynamic &&
				isInMemory == other.isInMemory &&
				StringComparer.OrdinalIgnoreCase.Equals(name ?? string.Empty, other.name ?? string.Empty);
		}

		public override bool Equals(object obj) {
			return obj is SerializedDnModule && Equals((SerializedDnModule)obj);
		}

		public override int GetHashCode() {
			return StringComparer.OrdinalIgnoreCase.GetHashCode(name ?? string.Empty) ^
					(isDynamic ? int.MinValue : 0) ^
					(isInMemory ? 0x40000000 : 0);
		}

		public override string ToString() {
			return string.Format("DYN={0} MEM={1} {2}", isDynamic ? 1 : 0, isInMemory ? 1 : 0, name ?? string.Empty);
		}
	}
}
