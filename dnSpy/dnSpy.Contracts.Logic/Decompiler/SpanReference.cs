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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Contains a span and a reference. Should be used for references to eg. keywords and
	/// is used by the Visual Basic decompiler to highlight eg. 'While', 'End While', etc.
	/// 
	/// Normal clickable references should be created by calling
	/// <see cref="IDecompilerOutput.Write(string, object, DecompilerReferenceFlags, object)"/>.
	/// 
	/// Use <see cref="DecompilerOutputExtensions.AddSpanReference(IDecompilerOutput, SpanReference)"/>
	/// to add an instance.
	/// </summary>
	public struct SpanReference {
		/// <summary>
		/// Gets the reference
		/// </summary>
		public object Reference { get; }

		/// <summary>
		/// Gets the span
		/// </summary>
		public TextSpan Span { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reference">Reference</param>
		/// <param name="span">Span</param>
		public SpanReference(object reference, TextSpan span) {
			Reference = reference;
			Span = span;
		}
	}
}
