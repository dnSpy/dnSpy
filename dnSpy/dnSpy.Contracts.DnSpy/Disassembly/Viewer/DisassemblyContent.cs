/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Disassembly.Viewer {
	/// <summary>
	/// Disassembled content shown in the disassembly viewer
	/// </summary>
	public readonly struct DisassemblyContent {
		/// <summary>
		/// Gets the text
		/// </summary>
		public DisassemblyText[] Text { get; }

		/// <summary>
		/// Gets the references
		/// </summary>
		public SpanDataCollection<DisassemblyReferenceInfo> ReferenceCollection { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Disassembly</param>
		/// <param name="referenceCollection">References</param>
		public DisassemblyContent(DisassemblyText[] text, SpanDataCollection<DisassemblyReferenceInfo> referenceCollection) {
			Text = text ?? throw new ArgumentNullException(nameof(text));
			ReferenceCollection = referenceCollection ?? throw new ArgumentNullException(nameof(referenceCollection));
		}
	}
}
