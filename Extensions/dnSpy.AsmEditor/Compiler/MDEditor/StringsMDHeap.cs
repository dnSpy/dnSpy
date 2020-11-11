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
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dnSpy.AsmEditor.Compiler.MDEditor {
	sealed class StringsMDHeap : MDHeap {
		readonly MetadataEditor mdEditor;
		readonly StringsStream stringsStream;
		uint currentOffset;
		readonly List<byte[]> newData;
		static readonly byte[] nulByte = new byte[1];

		public string Name => stringsStream.Name;
		public bool IsBig => currentOffset > 0xFFFF;
		internal StringsStream StringsStream => stringsStream;
		internal List<byte[]> NewData => newData;

		public StringsMDHeap(MetadataEditor mdEditor, StringsStream stringsStream) {
			this.mdEditor = mdEditor ?? throw new ArgumentNullException(nameof(mdEditor));
			this.stringsStream = stringsStream ?? throw new ArgumentNullException(nameof(stringsStream));
			currentOffset = stringsStream.StreamLength;
			newData = new List<byte[]>();
		}

		public uint Create(string name) => Create(string.IsNullOrEmpty(name) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(name));
		public uint Create(UTF8String name) => Create(UTF8String.IsNullOrEmpty(name) ? Array.Empty<byte>() : name.Data);

		uint Create(byte[] data) {
			if (currentOffset == 0) {
				newData.Add(new byte[1]);
				currentOffset++;
			}
			if (data is null || data.Length == 0)
				return 0;
			CheckValidName(data);
			newData.Add(data);
			newData.Add(nulByte);
			Debug.Assert(nulByte.Length == 1);
			var res = currentOffset;
			currentOffset += (uint)data.Length + 1;
			return res;
		}

		[Conditional("DEBUG")]
		static void CheckValidName(byte[] bytes) {
			for (int i = 0; i < bytes.Length; i++) {
				if (bytes[i] == 0) {
					Debug.Fail("Invalid #Strings name. It must not contain a NUL char");
					return;
				}
			}
		}

		public override bool MustRewriteHeap() => newData.Count > 0;
		public override bool ExistsInMetadata => stringsStream.StreamHeader is not null;
	}
}
