//
// Image.cs
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

using Mono;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;

using RVA = System.UInt32;

namespace Mono.Cecil.PE {

	sealed class Image {

		public ModuleKind Kind;
		public TargetRuntime Runtime;
		public TargetArchitecture Architecture;
		public ModuleCharacteristics Characteristics;
		public string FileName;

		public Section [] Sections;

		public Section MetadataSection;

		public uint EntryPointToken;
		public ModuleAttributes Attributes;

		public DataDirectory Debug;
		public DataDirectory Resources;
		public DataDirectory StrongName;

		public StringHeap StringHeap;
		public BlobHeap BlobHeap;
		public UserStringHeap UserStringHeap;
		public GuidHeap GuidHeap;
		public TableHeap TableHeap;

		readonly int [] coded_index_sizes = new int [13];

		readonly Func<Table, int> counter;

		public Image ()
		{
			counter = GetTableLength;
		}

		public bool HasTable (Table table)
		{
			return GetTableLength (table) > 0;
		}

		public int GetTableLength (Table table)
		{
			return (int) TableHeap [table].Length;
		}

		public int GetTableIndexSize (Table table)
		{
			return GetTableLength (table) < 65536 ? 2 : 4;
		}

		public int GetCodedIndexSize (CodedIndex coded_index)
		{
			var index = (int) coded_index;
			var size = coded_index_sizes [index];
			if (size != 0)
				return size;

			return coded_index_sizes [index] = coded_index.GetSize (counter);
		}

		public uint ResolveVirtualAddress (RVA rva)
		{
			var section = GetSectionAtVirtualAddress (rva);
			if (section == null)
				throw new ArgumentOutOfRangeException ();

			return ResolveVirtualAddressInSection (rva, section);
		}

		public uint ResolveVirtualAddressInSection (RVA rva, Section section)
		{
			return rva + section.PointerToRawData - section.VirtualAddress;
		}

		public Section GetSection (string name)
		{
			var sections = this.Sections;
			for (int i = 0; i < sections.Length; i++) {
				var section = sections [i];
				if (section.Name == name)
					return section;
			}

			return null;
		}

		public Section GetSectionAtVirtualAddress (RVA rva)
		{
			var sections = this.Sections;
			for (int i = 0; i < sections.Length; i++) {
				var section = sections [i];
				if (rva >= section.VirtualAddress && rva < section.VirtualAddress + section.SizeOfRawData)
					return section;
			}

			return null;
		}

		public ImageDebugDirectory GetDebugHeader (out byte [] header)
		{
			var section = GetSectionAtVirtualAddress (Debug.VirtualAddress);
			var buffer = new ByteBuffer (section.Data);
			buffer.position = (int) (Debug.VirtualAddress - section.VirtualAddress);

			var directory = new ImageDebugDirectory {
				Characteristics = buffer.ReadInt32 (),
				TimeDateStamp = buffer.ReadInt32 (),
				MajorVersion = buffer.ReadInt16 (),
				MinorVersion = buffer.ReadInt16 (),
				Type = buffer.ReadInt32 (),
				SizeOfData = buffer.ReadInt32 (),
				AddressOfRawData = buffer.ReadInt32 (),
				PointerToRawData = buffer.ReadInt32 (),
			};

			buffer.position = (int) (directory.PointerToRawData - section.PointerToRawData);

			header = new byte [directory.SizeOfData];
			Buffer.BlockCopy (buffer.buffer, buffer.position, header, 0, header.Length);

			return directory;
		}
	}
}
