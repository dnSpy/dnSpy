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

using System;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Method and statement
	/// </summary>
	public struct MethodSourceStatement : IEquatable<MethodSourceStatement> {
		/// <summary>
		/// Gets the method
		/// </summary>
		public MethodDef Method { get; }

		/// <summary>
		/// Gets the statement
		/// </summary>
		public SourceStatement Statement { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="statement">Statement</param>
		public MethodSourceStatement(MethodDef method, SourceStatement statement) {
			Method = method ?? throw new ArgumentNullException(nameof(method));
			Statement = statement;
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(MethodSourceStatement left, MethodSourceStatement right) => left.Equals(right);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(MethodSourceStatement left, MethodSourceStatement right) => !left.Equals(right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(MethodSourceStatement other) => Method == other.Method && Statement.Equals(other.Statement);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is MethodSourceStatement && Equals((MethodSourceStatement)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => Method.GetHashCode() ^ Statement.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "{" + Statement.ToString() + "," + Method.ToString() + "}";
	}
}
