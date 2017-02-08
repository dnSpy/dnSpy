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

using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Data consumed by <see cref="IBlockStructureService"/>
	/// </summary>
	struct BlockStructureData {
		/// <summary>
		/// Span of start block
		/// </summary>
		public SnapshotSpan Top { get; }

		/// <summary>
		/// Span of end block
		/// </summary>
		public SnapshotSpan Bottom { get; }

		/// <summary>
		/// Block kind
		/// </summary>
		public BlockStructureKind BlockKind { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blockTop">Start block span</param>
		/// <param name="blockBottom">End block span</param>
		/// <param name="blockKind">Block kind</param>
		public BlockStructureData(SnapshotSpan blockTop, SnapshotSpan blockBottom, BlockStructureKind blockKind) {
			Top = blockTop;
			Bottom = blockBottom;
			BlockKind = blockKind;
		}
	}
}
