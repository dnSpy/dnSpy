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

namespace dnSpy.Decompiler.Shared {
	/// <summary>
	/// Source statement
	/// </summary>
	public struct SourceStatement : IEquatable<SourceStatement> {
		internal static readonly IComparer<SourceStatement> SpanStartComparer = new SpanStartComparerImpl();
		readonly BinSpan binSpan;
		readonly TextSpan textSpan;

		/// <summary>
		/// Binary span
		/// </summary>
		public BinSpan BinSpan => binSpan;

		/// <summary>
		/// Text span
		/// </summary>
		public TextSpan TextSpan => textSpan;

		sealed class SpanStartComparerImpl : IComparer<SourceStatement> {
			public int Compare(SourceStatement x, SourceStatement y) => (int)(x.binSpan.Start - y.binSpan.Start);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="binSpan">Binary span</param>
		/// <param name="textSpan">Text span</param>
		public SourceStatement(BinSpan binSpan, TextSpan textSpan) {
			this.binSpan = binSpan;
			this.textSpan = textSpan;
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(SourceStatement left, SourceStatement right) => left.Equals(right);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(SourceStatement left, SourceStatement right) => !left.Equals(right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(SourceStatement other) => binSpan.Equals(other.binSpan) && textSpan.Equals(other.textSpan);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is SourceStatement && Equals((SourceStatement)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => binSpan.GetHashCode() ^ textSpan.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "{" + binSpan.ToString() + "," + textSpan.ToString() + "}";
	}
}
