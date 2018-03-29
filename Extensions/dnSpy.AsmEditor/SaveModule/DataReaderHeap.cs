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

using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using dnlib.IO;

namespace dnSpy.AsmEditor.SaveModule {
	sealed class DataReaderHeap : HeapBase {
		public override string Name { get; }

		DataReader streamReader;

		public DataReaderHeap(DotNetStream stream) {
			streamReader = stream.GetReader();
			Name = stream.Name;
		}

		public override uint GetRawLength() => streamReader.Length;
		protected override void WriteToImpl(DataWriter writer) => streamReader.CopyTo(writer);
	}
}
