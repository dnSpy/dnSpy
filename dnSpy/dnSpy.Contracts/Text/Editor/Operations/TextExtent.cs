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

namespace dnSpy.Contracts.Text.Editor.Operations {
	/// <summary>
	/// Text extent
	/// </summary>
	public struct TextExtent : IEquatable<TextExtent> {
		/// <summary>
		/// false for whitespace or other insignificant characters that should be ignored during navigation
		/// </summary>
		public bool IsSignificant { get; }

		/// <summary>
		/// Gets the span
		/// </summary>
		public SnapshotSpan Span { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="textExtent">Other instance</param>
		public TextExtent(TextExtent textExtent) {
			this = textExtent;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="isSignificant">true if it's a significant span</param>
		public TextExtent(SnapshotSpan span, bool isSignificant) {
			IsSignificant = isSignificant;
			Span = span;
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(TextExtent left, TextExtent right) => left.Equals(right);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(TextExtent left, TextExtent right) => !left.Equals(right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(TextExtent other) => IsSignificant == other.IsSignificant && Span == other.Span;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is TextExtent && Equals((TextExtent)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (IsSignificant ? int.MaxValue : 0) ^ Span.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"{(IsSignificant ? "Y" : "N")} {Span.ToString()}";
	}
}
