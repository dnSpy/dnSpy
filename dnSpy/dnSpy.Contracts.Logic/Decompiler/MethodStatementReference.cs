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
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Method statement reference
	/// </summary>
	public sealed class MethodStatementReference {
		/// <summary>
		/// Gets the method
		/// </summary>
		public MethodDef Method { get; }

		/// <summary>
		/// Gets the offset or null
		/// </summary>
		public uint? Offset { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="offset">Offset or null</param>
		public MethodStatementReference(MethodDef method, uint? offset) {
			Method = method ?? throw new ArgumentNullException(nameof(method));
			Offset = offset;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			var other = obj as MethodStatementReference;
			return other != null &&
				Method == other.Method &&
				Offset == other.Offset;
		}

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => Method.GetHashCode() ^ (int)(Offset ?? 0);
	}
}
