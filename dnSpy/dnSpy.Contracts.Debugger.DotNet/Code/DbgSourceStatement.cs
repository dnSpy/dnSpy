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

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// Source statement
	/// </summary>
	public readonly struct DbgSourceStatement : IEquatable<DbgSourceStatement> {
		internal static readonly SpanStartComparerImpl SpanStartComparer = new SpanStartComparerImpl();
		readonly DbgILSpan ilSpan;
		readonly DbgTextSpan textSpan;

		/// <summary>
		/// IL span
		/// </summary>
		public DbgILSpan ILSpan => ilSpan;

		/// <summary>
		/// Text span
		/// </summary>
		public DbgTextSpan TextSpan => textSpan;

		internal sealed class SpanStartComparerImpl : IComparer<DbgSourceStatement> {
			public int Compare(DbgSourceStatement x, DbgSourceStatement y) => (int)(x.ilSpan.Start - y.ilSpan.Start);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ilSpan">IL span</param>
		/// <param name="textSpan">Text span</param>
		public DbgSourceStatement(DbgILSpan ilSpan, DbgTextSpan textSpan) {
			this.ilSpan = ilSpan;
			this.textSpan = textSpan;
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(DbgSourceStatement left, DbgSourceStatement right) => left.Equals(right);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(DbgSourceStatement left, DbgSourceStatement right) => !left.Equals(right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DbgSourceStatement other) => ilSpan.Equals(other.ilSpan) && textSpan.Equals(other.textSpan);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is DbgSourceStatement && Equals((DbgSourceStatement)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => ilSpan.GetHashCode() ^ textSpan.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "{" + ilSpan.ToString() + "," + textSpan.ToString() + "}";
	}
}
