//
// PEOptionalHeader.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

namespace Mono.Cecil.Binary {

	public sealed class PEOptionalHeader : IHeader, IBinaryVisitable {

		public StandardFieldsHeader StandardFields;
		public NTSpecificFieldsHeader NTSpecificFields;
		public DataDirectoriesHeader DataDirectories;

		internal PEOptionalHeader ()
		{
			StandardFields = new StandardFieldsHeader ();
			NTSpecificFields = new NTSpecificFieldsHeader ();
			DataDirectories = new DataDirectoriesHeader ();
		}

		public void SetDefaultValues ()
		{
		}

		public void Accept (IBinaryVisitor visitor)
		{
			visitor.VisitPEOptionalHeader (this);

			StandardFields.Accept (visitor);
			NTSpecificFields.Accept (visitor);
			DataDirectories.Accept (visitor);
		}

		public sealed class StandardFieldsHeader : IHeader, IBinaryVisitable {

			public ushort Magic;
			public byte LMajor;
			public byte LMinor;
			public uint CodeSize;
			public uint InitializedDataSize;
			public uint UninitializedDataSize;
			public RVA EntryPointRVA;
			public RVA BaseOfCode;
			public RVA BaseOfData;

			public bool IsPE64 {
				get { return Magic == 0x20b; }
				set {
					if (value)
						Magic = 0x20b;
					else
						Magic = 0x10b;
				}
			}

			internal StandardFieldsHeader ()
			{
			}

			public void SetDefaultValues ()
			{
				Magic = 0x10b;
				LMajor = 6;
				LMinor = 0;
			}

			public void Accept (IBinaryVisitor visitor)
			{
				visitor.VisitStandardFieldsHeader (this);
			}
		}

		public sealed class NTSpecificFieldsHeader : IHeader, IBinaryVisitable {

			public ulong ImageBase;
			public uint SectionAlignment;
			public uint FileAlignment;
			public ushort OSMajor;
			public ushort OSMinor;
			public ushort UserMajor;
			public ushort UserMinor;
			public ushort SubSysMajor;
			public ushort SubSysMinor;
			public uint Reserved;
			public uint ImageSize;
			public uint HeaderSize;
			public uint FileChecksum;
			public SubSystem SubSystem;
			public ushort DLLFlags;
			public ulong StackReserveSize;
			public ulong StackCommitSize;
			public ulong HeapReserveSize;
			public ulong HeapCommitSize;
			public uint LoaderFlags;
			public uint NumberOfDataDir;

			internal NTSpecificFieldsHeader ()
			{
			}

			public void SetDefaultValues ()
			{
				ImageBase = 0x400000;
				SectionAlignment = 0x2000;
				FileAlignment = 0x200;
				OSMajor = 4;
				OSMinor = 0;
				UserMajor = 0;
				UserMinor = 0;
				SubSysMajor = 4;
				SubSysMinor = 0;
				Reserved = 0;
				HeaderSize = 0x200;
				FileChecksum = 0;
				DLLFlags = 0;
				StackReserveSize = 0x100000;
				StackCommitSize = 0x1000;
				HeapReserveSize = 0x100000;
				HeapCommitSize = 0x1000;
				LoaderFlags = 0;
				NumberOfDataDir = 0x10;
			}

			public void Accept (IBinaryVisitor visitor)
			{
				visitor.VisitNTSpecificFieldsHeader (this);
			}
		}

		public sealed class DataDirectoriesHeader : IHeader, IBinaryVisitable {

			public DataDirectory ExportTable;
			public DataDirectory ImportTable;
			public DataDirectory ResourceTable;
			public DataDirectory ExceptionTable;
			public DataDirectory CertificateTable;
			public DataDirectory BaseRelocationTable;
			public DataDirectory Debug;
			public DataDirectory Copyright;
			public DataDirectory GlobalPtr;
			public DataDirectory TLSTable;
			public DataDirectory LoadConfigTable;
			public DataDirectory BoundImport;
			public DataDirectory IAT;
			public DataDirectory DelayImportDescriptor;
			public DataDirectory CLIHeader;
			public DataDirectory Reserved;

			internal DataDirectoriesHeader ()
			{
			}

			public void SetDefaultValues ()
			{
				ExportTable = DataDirectory.Zero;
				ResourceTable = DataDirectory.Zero;
				ExceptionTable = DataDirectory.Zero;
				CertificateTable = DataDirectory.Zero;
				Debug = DataDirectory.Zero;
				Copyright = DataDirectory.Zero;
				GlobalPtr = DataDirectory.Zero;
				TLSTable = DataDirectory.Zero;
				LoadConfigTable = DataDirectory.Zero;
				BoundImport = DataDirectory.Zero;
				IAT = new DataDirectory (new RVA (0x2000), 8);
				DelayImportDescriptor = DataDirectory.Zero;
				CLIHeader = new DataDirectory (new RVA (0x2008), 0x48);
				Reserved = DataDirectory.Zero;
			}

			public void Accept (IBinaryVisitor visitor)
			{
				visitor.VisitDataDirectoriesHeader (this);
			}
		}
	}
}
