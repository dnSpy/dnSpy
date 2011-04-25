//
// TextMap.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

#if !READ_ONLY

using RVA = System.UInt32;

namespace Mono.Cecil.PE {

	enum TextSegment {
		ImportAddressTable,
		CLIHeader,
		Code,
		Resources,
		Data,
		StrongNameSignature,

		// Metadata
		MetadataHeader,
		TableHeap,
		StringHeap,
		UserStringHeap,
		GuidHeap,
		BlobHeap,
		// End Metadata

		DebugDirectory,
		ImportDirectory,
		ImportHintNameTable,
		StartupStub,
	}

	sealed class TextMap {

		readonly Range [] map = new Range [16 /*Enum.GetValues (typeof (TextSegment)).Length*/];

		public void AddMap (TextSegment segment, int length)
		{
			map [(int) segment] = new Range (GetStart (segment), (uint) length);
		}

		public void AddMap (TextSegment segment, int length, int align)
		{
			align--;

			AddMap (segment, (length + align) & ~align);
		}

		public void AddMap (TextSegment segment, Range range)
		{
			map [(int) segment] = range;
		}

		public Range GetRange (TextSegment segment)
		{
			return map [(int) segment];
		}

		public DataDirectory GetDataDirectory (TextSegment segment)
		{
			var range = map [(int) segment];

			return new DataDirectory (range.Length == 0 ? 0 : range.Start, range.Length);
		}

		public RVA GetRVA (TextSegment segment)
		{
			return map [(int) segment].Start;
		}

		public RVA GetNextRVA (TextSegment segment)
		{
			var i = (int) segment;
			return map [i].Start + map [i].Length;
		}

		public int GetLength (TextSegment segment)
		{
			return (int) map [(int) segment].Length;
		}

		RVA GetStart (TextSegment segment)
		{
			var index = (int) segment;
			return index == 0 ? ImageWriter.text_rva : ComputeStart (index);
		}

		RVA ComputeStart (int index)
		{
			index--;
			return map [index].Start + map [index].Length;
		}

		public uint GetLength ()
		{
			var range = map [(int) TextSegment.StartupStub];
			return range.Start - ImageWriter.text_rva + range.Length;
		}
	}
}

#endif
