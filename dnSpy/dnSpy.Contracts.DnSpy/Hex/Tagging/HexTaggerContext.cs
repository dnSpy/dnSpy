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
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex.Tagging {
	/// <summary>
	/// Hex tagger context
	/// </summary>
	public readonly struct HexTaggerContext {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => Line is null;

		/// <summary>
		/// Gets the line info
		/// </summary>
		public HexBufferLine Line { get; }

		/// <summary>
		/// Line span to tag
		/// </summary>
		public VST.Span LineSpan { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="line">Line info</param>
		/// <param name="lineSpan">Line span to tag</param>
		public HexTaggerContext(HexBufferLine line, VST.Span lineSpan) {
			if (line is null)
				throw new ArgumentNullException(nameof(line));
			if (!line.TextSpan.Contains(lineSpan))
				throw new ArgumentOutOfRangeException(nameof(lineSpan));
			Line = line;
			LineSpan = lineSpan;
		}
	}
}
