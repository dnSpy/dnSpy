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

using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex.Classification {
	/// <summary>
	/// Hex classification context type
	/// </summary>
	public enum HexClassificationContextType {
		/// <summary>
		/// Offset column (<see cref="HexOffsetClassificationContext"/>)
		/// </summary>
		Offset,

		/// <summary>
		/// Bytes column (<see cref="HexBytesClassificationContext"/>)
		/// </summary>
		Bytes,

		/// <summary>
		/// ASCII column (<see cref="HexAsciiClassificationContext"/>)
		/// </summary>
		Ascii,
	}

	/// <summary>
	/// Hex classification context
	/// </summary>
	public abstract class HexClassificationContext {
		/// <summary>
		/// Gets the classification context type
		/// </summary>
		public HexClassificationContextType Type { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Classification context type</param>
		internal HexClassificationContext(HexClassificationContextType type) {
			Type = type;
		}

		/// <summary>
		/// Line span, some of the bytes could be hidden
		/// </summary>
		public abstract HexBufferSpan LineSpan { get; }

		/// <summary>
		/// The visible bytes shown in the UI
		/// </summary>
		public abstract HexBufferSpan VisibleBytesSpan { get; }

		/// <summary>
		/// Text shown in the UI
		/// </summary>
		public abstract string Text { get; }
	}

	/// <summary>
	/// Hex offset classification context
	/// </summary>
	public abstract class HexOffsetClassificationContext : HexClassificationContext {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexOffsetClassificationContext() : base(HexClassificationContextType.Offset) { }
	}

	/// <summary>
	/// Hex bytes classification context
	/// </summary>
	public abstract class HexBytesClassificationContext : HexClassificationContext {
		/// <summary>
		/// All raw visible bytes
		/// </summary>
		public abstract HexBytes VisibleHexBytes { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBytesClassificationContext() : base(HexClassificationContextType.Bytes) { }

		/// <summary>
		/// Gets the span of a hex byte in <see cref="HexClassificationContext.Text"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract Span GetSpan(ulong position);

		/// <summary>
		/// Gets the span of hex bytes in <see cref="HexClassificationContext.Text"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public abstract Span GetSpan(HexSpan span);
	}

	/// <summary>
	/// Hex ASCII chars classification context
	/// </summary>
	public abstract class HexAsciiClassificationContext : HexClassificationContext {
		/// <summary>
		/// All raw visible bytes
		/// </summary>
		public abstract HexBytes VisibleHexBytes { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		protected HexAsciiClassificationContext() : base(HexClassificationContextType.Ascii) { }

		/// <summary>
		/// Gets the span of an ASCII character in <see cref="HexClassificationContext.Text"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract Span GetSpan(ulong position);

		/// <summary>
		/// Gets the span of ASCII characters in <see cref="HexClassificationContext.Text"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public abstract Span GetSpan(HexSpan span);
	}
}
