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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Source statement
	/// </summary>
	public readonly struct SourceStatement : IEquatable<SourceStatement> {
		internal static readonly SpanStartComparerImpl SpanStartComparer = new SpanStartComparerImpl();
		readonly ILSpan ilSpan;
		readonly TextSpan textSpan;

		/// <summary>
		/// IL span
		/// </summary>
		public ILSpan ILSpan => ilSpan;

		/// <summary>
		/// Text span
		/// </summary>
		public TextSpan TextSpan => textSpan;

		internal sealed class SpanStartComparerImpl : IComparer<SourceStatement> {
			public int Compare(SourceStatement x, SourceStatement y) => (int)(x.ilSpan.Start - y.ilSpan.Start);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ilSpan">IL span</param>
		/// <param name="textSpan">Text span</param>
		public SourceStatement(ILSpan ilSpan, TextSpan textSpan) {
			this.ilSpan = ilSpan;
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
		public bool Equals(SourceStatement other) => ilSpan.Equals(other.ilSpan) && textSpan.Equals(other.textSpan);

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
		public override int GetHashCode() => ilSpan.GetHashCode() ^ textSpan.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "{" + ilSpan.ToString() + "," + textSpan.ToString() + "}";
	}
}
