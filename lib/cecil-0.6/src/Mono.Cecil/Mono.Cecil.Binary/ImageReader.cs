//
// ImageReader.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 - 2007 Jb Evain
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

	using System;
	using System.IO;
	using System.Text;

	using Mono.Cecil.Metadata;

	class ImageReader : BaseImageVisitor {

		MetadataReader m_mdReader;
		BinaryReader m_binaryReader;
		Image m_image;

		public MetadataReader MetadataReader {
			get { return m_mdReader; }
		}

		public Image Image {
			get { return m_image; }
		}

		ImageReader (Image img, BinaryReader reader)
		{
			m_image = img;
			m_binaryReader = reader;
		}

		static ImageReader Read (Image img, Stream stream)
		{
			ImageReader reader = new ImageReader (img, new BinaryReader (stream));
			img.Accept (reader);
			return reader;
		}

		public static ImageReader Read (string file)
		{
			if (file == null)
				throw new ArgumentNullException ("file");

			FileInfo fi = new FileInfo (file);
			if (!File.Exists (fi.FullName))
			#if CF_1_0 || CF_2_0
				throw new FileNotFoundException (fi.FullName);
			#else
				throw new FileNotFoundException (string.Format ("File '{0}' not found.", fi.FullName), fi.FullName);
			#endif

			return Read (new Image (fi), new FileStream (
				fi.FullName, FileMode.Open,
				FileAccess.Read, FileShare.Read));
		}

		public static ImageReader Read (byte [] image)
		{
			if (image == null)
				throw new ArgumentNullException ("image");

			if (image.Length == 0)
				throw new ArgumentException ("Empty image array");

			return Read (new Image (), new MemoryStream (image));
		}

		public static ImageReader Read (Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");

			if (!stream.CanRead)
				throw new ArgumentException ("Can not read from stream");

			return Read (new Image (), stream);
		}

		public BinaryReader GetReader ()
		{
			return m_binaryReader;
		}

		public override void VisitImage (Image img)
		{
			m_mdReader = new MetadataReader (this);
		}

		void SetPositionToAddress (RVA address)
		{
			m_binaryReader.BaseStream.Position = m_image.ResolveVirtualAddress (address);
		}

		public override void VisitDOSHeader (DOSHeader header)
		{
			header.Start = m_binaryReader.ReadBytes (60);
			header.Lfanew = m_binaryReader.ReadUInt32 ();
			header.End = m_binaryReader.ReadBytes (64);

			m_binaryReader.BaseStream.Position = header.Lfanew;

			if (m_binaryReader.ReadUInt16 () != 0x4550 ||
				m_binaryReader.ReadUInt16 () != 0)

				throw new ImageFormatException ("Invalid PE File Signature");
		}

		public override void VisitPEFileHeader (PEFileHeader header)
		{
			header.Machine = m_binaryReader.ReadUInt16 ();
			header.NumberOfSections = m_binaryReader.ReadUInt16 ();
			header.TimeDateStamp = m_binaryReader.ReadUInt32 ();
			header.PointerToSymbolTable = m_binaryReader.ReadUInt32 ();
			header.NumberOfSymbols = m_binaryReader.ReadUInt32 ();
			header.OptionalHeaderSize = m_binaryReader.ReadUInt16 ();
			header.Characteristics = (ImageCharacteristics) m_binaryReader.ReadUInt16 ();
		}

		ulong ReadIntOrLong ()
		{
			return m_image.PEOptionalHeader.StandardFields.IsPE64 ?
				m_binaryReader.ReadUInt64 () :
				m_binaryReader.ReadUInt32 ();
		}

		RVA ReadRVA ()
		{
			return m_binaryReader.ReadUInt32 ();
		}

		DataDirectory ReadDataDirectory ()
		{
			return new DataDirectory (ReadRVA (), m_binaryReader.ReadUInt32 ());
		}

		public override void VisitNTSpecificFieldsHeader (PEOptionalHeader.NTSpecificFieldsHeader header)
		{
			header.ImageBase = ReadIntOrLong ();
			header.SectionAlignment = m_binaryReader.ReadUInt32 ();
			header.FileAlignment = m_binaryReader.ReadUInt32 ();
			header.OSMajor = m_binaryReader.ReadUInt16 ();
			header.OSMinor = m_binaryReader.ReadUInt16 ();
			header.UserMajor = m_binaryReader.ReadUInt16 ();
			header.UserMinor = m_binaryReader.ReadUInt16 ();
			header.SubSysMajor = m_binaryReader.ReadUInt16 ();
			header.SubSysMinor = m_binaryReader.ReadUInt16 ();
			header.Reserved = m_binaryReader.ReadUInt32 ();
			header.ImageSize = m_binaryReader.ReadUInt32 ();
			header.HeaderSize = m_binaryReader.ReadUInt32 ();
			header.FileChecksum = m_binaryReader.ReadUInt32 ();
			header.SubSystem = (SubSystem) m_binaryReader.ReadUInt16 ();
			header.DLLFlags = m_binaryReader.ReadUInt16 ();
			header.StackReserveSize = ReadIntOrLong ();
			header.StackCommitSize = ReadIntOrLong ();
			header.HeapReserveSize = ReadIntOrLong ();
			header.HeapCommitSize = ReadIntOrLong ();
			header.LoaderFlags = m_binaryReader.ReadUInt32 ();
			header.NumberOfDataDir = m_binaryReader.ReadUInt32 ();
		}

		public override void VisitStandardFieldsHeader (PEOptionalHeader.StandardFieldsHeader header)
		{
			header.Magic = m_binaryReader.ReadUInt16 ();
			header.LMajor = m_binaryReader.ReadByte ();
			header.LMinor = m_binaryReader.ReadByte ();
			header.CodeSize = m_binaryReader.ReadUInt32 ();
			header.InitializedDataSize = m_binaryReader.ReadUInt32 ();
			header.UninitializedDataSize = m_binaryReader.ReadUInt32 ();
			header.EntryPointRVA = ReadRVA ();
			header.BaseOfCode = ReadRVA ();
			if (!header.IsPE64)
				header.BaseOfData = ReadRVA ();
		}

		public override void VisitDataDirectoriesHeader (PEOptionalHeader.DataDirectoriesHeader header)
		{
			header.ExportTable = ReadDataDirectory ();
			header.ImportTable = ReadDataDirectory ();
			header.ResourceTable = ReadDataDirectory ();
			header.ExceptionTable = ReadDataDirectory ();
			header.CertificateTable = ReadDataDirectory ();
			header.BaseRelocationTable = ReadDataDirectory ();
			header.Debug = ReadDataDirectory ();
			header.Copyright = ReadDataDirectory ();
			header.GlobalPtr = ReadDataDirectory ();
			header.TLSTable = ReadDataDirectory ();
			header.LoadConfigTable = ReadDataDirectory ();
			header.BoundImport = ReadDataDirectory ();
			header.IAT = ReadDataDirectory ();
			header.DelayImportDescriptor = ReadDataDirectory ();
			header.CLIHeader = ReadDataDirectory ();
			header.Reserved = ReadDataDirectory ();

			if (header.CLIHeader != DataDirectory.Zero)
				m_image.CLIHeader = new CLIHeader ();
			if (header.ExportTable != DataDirectory.Zero)
				m_image.ExportTable = new ExportTable ();
		}

		public override void VisitSectionCollection (SectionCollection coll)
		{
			for (int i = 0; i < m_image.PEFileHeader.NumberOfSections; i++)
				coll.Add (new Section ());
		}

		public override void VisitSection (Section sect)
		{
			char [] buffer = new char [8];
			int read = 0;
			while (read < 8) {
				char cur = (char) m_binaryReader.ReadSByte ();
				if (cur == '\0') {
					m_binaryReader.BaseStream.Position += 8 - read - 1;
					break;
				}
				buffer [read++] = cur;
			}
			sect.Name = read == 0 ? string.Empty : new string (buffer, 0, read);
			if (sect.Name == Section.Text)
				m_image.TextSection = sect;

			sect.VirtualSize = m_binaryReader.ReadUInt32 ();
			sect.VirtualAddress = ReadRVA ();
			sect.SizeOfRawData = m_binaryReader.ReadUInt32 ();
			sect.PointerToRawData = ReadRVA ();
			sect.PointerToRelocations = ReadRVA ();
			sect.PointerToLineNumbers = ReadRVA ();
			sect.NumberOfRelocations = m_binaryReader.ReadUInt16 ();
			sect.NumberOfLineNumbers = m_binaryReader.ReadUInt16 ();
			sect.Characteristics = (SectionCharacteristics) m_binaryReader.ReadUInt32 ();

			long pos = m_binaryReader.BaseStream.Position;
			m_binaryReader.BaseStream.Position = sect.PointerToRawData;
			sect.Data = m_binaryReader.ReadBytes ((int) sect.SizeOfRawData);
			m_binaryReader.BaseStream.Position = pos;
		}

		public override void VisitImportAddressTable (ImportAddressTable iat)
		{
			if (m_image.PEOptionalHeader.DataDirectories.IAT.VirtualAddress == RVA.Zero)
				return;

			SetPositionToAddress (m_image.PEOptionalHeader.DataDirectories.IAT.VirtualAddress);

			iat.HintNameTableRVA = ReadRVA ();
		}

		public override void VisitCLIHeader (CLIHeader header)
		{
			if (m_image.PEOptionalHeader.DataDirectories.Debug != DataDirectory.Zero) {
				m_image.DebugHeader = new DebugHeader ();
				VisitDebugHeader (m_image.DebugHeader);
			}

			SetPositionToAddress (m_image.PEOptionalHeader.DataDirectories.CLIHeader.VirtualAddress);
			header.Cb = m_binaryReader.ReadUInt32 ();
			header.MajorRuntimeVersion = m_binaryReader.ReadUInt16 ();
			header.MinorRuntimeVersion = m_binaryReader.ReadUInt16 ();
			header.Metadata = ReadDataDirectory ();
			header.Flags = (RuntimeImage) m_binaryReader.ReadUInt32 ();
			header.EntryPointToken = m_binaryReader.ReadUInt32 ();
			header.Resources = ReadDataDirectory ();
			header.StrongNameSignature = ReadDataDirectory ();
			header.CodeManagerTable = ReadDataDirectory ();
			header.VTableFixups = ReadDataDirectory ();
			header.ExportAddressTableJumps = ReadDataDirectory ();
			header.ManagedNativeHeader = ReadDataDirectory ();

			if (header.StrongNameSignature != DataDirectory.Zero) {
				SetPositionToAddress (header.StrongNameSignature.VirtualAddress);
				header.ImageHash = m_binaryReader.ReadBytes ((int) header.StrongNameSignature.Size);
			} else
				header.ImageHash = new byte [0];

			SetPositionToAddress (m_image.CLIHeader.Metadata.VirtualAddress);
			m_image.MetadataRoot.Accept (m_mdReader);
		}

		public override void VisitDebugHeader (DebugHeader header)
		{
			if (m_image.PEOptionalHeader.DataDirectories.Debug == DataDirectory.Zero)
				return;

			long pos = m_binaryReader.BaseStream.Position;

			SetPositionToAddress (m_image.PEOptionalHeader.DataDirectories.Debug.VirtualAddress);
			header.Characteristics = m_binaryReader.ReadUInt32 ();
			header.TimeDateStamp = m_binaryReader.ReadUInt32 ();
			header.MajorVersion = m_binaryReader.ReadUInt16 ();
			header.MinorVersion = m_binaryReader.ReadUInt16 ();
			header.Type = (DebugStoreType) m_binaryReader.ReadUInt32 ();
			header.SizeOfData = m_binaryReader.ReadUInt32 ();
			header.AddressOfRawData = ReadRVA ();
			header.PointerToRawData = m_binaryReader.ReadUInt32 ();

			m_binaryReader.BaseStream.Position = header.PointerToRawData;

			header.Magic = m_binaryReader.ReadUInt32 ();
			header.Signature = new Guid (m_binaryReader.ReadBytes (16));
			header.Age = m_binaryReader.ReadUInt32 ();
			header.FileName = ReadZeroTerminatedString ();

			m_binaryReader.BaseStream.Position = pos;
		}

		string ReadZeroTerminatedString ()
		{
			StringBuilder sb = new StringBuilder ();
			while (true) {
				byte chr = m_binaryReader.ReadByte ();
				if (chr == 0)
					break;
				sb.Append ((char) chr);
			}
			return sb.ToString ();
		}

		public override void VisitImportTable (ImportTable it)
		{
			if (m_image.PEOptionalHeader.DataDirectories.ImportTable.VirtualAddress == RVA.Zero)
				return;

			SetPositionToAddress (m_image.PEOptionalHeader.DataDirectories.ImportTable.VirtualAddress);

			it.ImportLookupTable = ReadRVA ();
			it.DateTimeStamp = m_binaryReader.ReadUInt32 ();
			it.ForwardChain = m_binaryReader.ReadUInt32 ();
			it.Name = ReadRVA ();
			it.ImportAddressTable = ReadRVA ();
		}

		public override void VisitImportLookupTable (ImportLookupTable ilt)
		{
			if (m_image.ImportTable.ImportLookupTable == RVA.Zero)
				return;

			SetPositionToAddress (m_image.ImportTable.ImportLookupTable);

			ilt.HintNameRVA = ReadRVA ();
		}

		public override void VisitHintNameTable (HintNameTable hnt)
		{
			if (m_image.ImportAddressTable.HintNameTableRVA == RVA.Zero)
				return;

			SetPositionToAddress (m_image.ImportAddressTable.HintNameTableRVA);

			hnt.Hint = m_binaryReader.ReadUInt16 ();

			byte [] bytes = m_binaryReader.ReadBytes (11);
			hnt.RuntimeMain = Encoding.ASCII.GetString (bytes, 0, bytes.Length);

			SetPositionToAddress (m_image.ImportTable.Name);

			bytes = m_binaryReader.ReadBytes (11);
			hnt.RuntimeLibrary = Encoding.ASCII.GetString (bytes, 0, bytes.Length);

			SetPositionToAddress (m_image.PEOptionalHeader.StandardFields.EntryPointRVA);
			hnt.EntryPoint = m_binaryReader.ReadUInt16 ();
			hnt.RVA = ReadRVA ();
		}

		public override void VisitExportTable (ExportTable et)
		{
			SetPositionToAddress (m_image.PEOptionalHeader.DataDirectories.ExportTable.VirtualAddress);

			et.Characteristics = m_binaryReader.ReadUInt32 ();
			et.TimeDateStamp = m_binaryReader.ReadUInt32 ();
			et.MajorVersion = m_binaryReader.ReadUInt16 ();
			et.MinorVersion = m_binaryReader.ReadUInt16 ();

			//et.Name =
			m_binaryReader.ReadUInt32 ();

			et.Base = m_binaryReader.ReadUInt32 ();
			et.NumberOfFunctions = m_binaryReader.ReadUInt32 ();
			et.NumberOfNames = m_binaryReader.ReadUInt32 ();
			et.AddressOfFunctions = m_binaryReader.ReadUInt32 ();
			et.AddressOfNames = m_binaryReader.ReadUInt32 ();
			et.AddressOfNameOrdinals = m_binaryReader.ReadUInt32 ();

			et.AddressesOfFunctions = ReadArrayOfRVA (et.AddressOfFunctions, et.NumberOfFunctions);
			et.AddressesOfNames = ReadArrayOfRVA (et.AddressOfNames, et.NumberOfNames);
			et.NameOrdinals = ReadArrayOfUInt16 (et.AddressOfNameOrdinals, et.NumberOfNames);
			et.Names = new string [et.NumberOfFunctions];

			for (int i = 0; i < et.NumberOfFunctions; i++) {
				if (et.AddressesOfFunctions [i] == 0)
					continue;

				et.Names [i] = ReadFunctionName (et, i);
			}
		}

		string ReadFunctionName (ExportTable et, int index)
		{
			for (int i = 0; i < et.NumberOfNames; i++) {
				if (et.NameOrdinals [i] != index)
					continue;

				SetPositionToAddress (et.AddressesOfNames [i]);
				return ReadZeroTerminatedString ();
			}

			return string.Empty;
		}

		ushort [] ReadArrayOfUInt16 (RVA position, uint length)
		{
			SetPositionToAddress (position);
			ushort [] array = new ushort [length];
			for (int i = 0; i < length; i++)
				array [i] = m_binaryReader.ReadUInt16 ();

			return array;
		}

		RVA [] ReadArrayOfRVA (RVA position, uint length)
		{
			SetPositionToAddress (position);
			RVA [] addresses = new RVA [length];
			for (int i = 0; i < length; i++)
				addresses [i] = m_binaryReader.ReadUInt32 ();

			return addresses;
		}

		public override void TerminateImage(Image img)
		{
			m_binaryReader.Close ();

			try {
				ResourceReader resReader = new ResourceReader (img);
				img.ResourceDirectoryRoot = resReader.Read ();
			} catch {
				img.ResourceDirectoryRoot = null;
			}
		}
	}
}
