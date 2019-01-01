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
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	sealed class GuidMDHeap : MDHeap {
		readonly MetadataEditor mdEditor;
		readonly GuidStream guidStream;
		uint currentOffset;
		readonly List<byte[]> newData;

		public string Name => guidStream.Name;
		public bool IsBig => currentOffset > 0xFFFF;
		internal GuidStream GuidStream => guidStream;
		internal List<byte[]> NewData => newData;

		public GuidMDHeap(MetadataEditor mdEditor, GuidStream guidStream) {
			this.mdEditor = mdEditor ?? throw new ArgumentNullException(nameof(mdEditor));
			this.guidStream = guidStream ?? throw new ArgumentNullException(nameof(guidStream));
			currentOffset = guidStream.StreamLength;
			newData = new List<byte[]>();
		}

		public override bool MustRewriteHeap() => newData.Count > 0;
		public override bool ExistsInMetadata => guidStream.StreamHeader != null;
	}
}
