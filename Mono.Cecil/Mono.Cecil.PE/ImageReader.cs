//
// ImageReader.cs
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
using System.IO;

using Mono.Cecil.Metadata;

using RVA = System.UInt32;

namespace Mono.Cecil.PE {

	sealed class ImageReader : BinaryStreamReader {

		readonly Image image;

		DataDirectory cli;
		DataDirectory metadata;

		public ImageReader (Stream stream)
			: base (stream)
		{
			image = new Image ();

			image.FileName = stream.GetFullyQualifiedName ();
		}

		void MoveTo (DataDirectory directory)
		{
			BaseStream.Position = image.ResolveVirtualAddress (directory.VirtualAddress);
		}

		void MoveTo (uint position)
		{
			BaseStream.Position = position;
		}

		void ReadImage ()
		{
			if (BaseStream.Length < 128)
				throw new BadImageFormatException ();

			// - DOSHeader

			// PE					2
			// Start				58
			// Lfanew				4
			// End					64

			if (ReadUInt16 () != 0x5a4d)
				throw new BadImageFormatException ();

			Advance (58);

			MoveTo (ReadUInt32 ());

			if (ReadUInt32 () != 0x00004550)
				throw new BadImageFormatException ();

			// - PEFileHeader

			// Machine				2
			image.Architecture = ReadArchitecture ();

			// NumberOfSections		2
			ushort sections = ReadUInt16 ();

			// TimeDateStamp		4
			// PointerToSymbolTable	4
			// NumberOfSymbols		4
			// OptionalHeaderSize	2
			Advance (14);

			// Characteristics		2
			ushort characteristics = ReadUInt16 ();

			ushort subsystem, dll_characteristics;
			ReadOptionalHeaders (out subsystem, out dll_characteristics);
			ReadSections (sections);
			ReadCLIHeader ();
			ReadMetadata ();

			image.Kind = GetModuleKind (characteristics, subsystem);
			image.Characteristics = (ModuleCharacteristics) dll_characteristics;
		}

		TargetArchitecture ReadArchitecture ()
		{
			var machine = ReadUInt16 ();
			switch (machine) {
			case 0x014c:
				return TargetArchitecture.I386;
			case 0x8664:
				return TargetArchitecture.AMD64;
			case 0x0200:
				return TargetArchitecture.IA64;
			case 0x01c4:
				return TargetArchitecture.ARMv7;
			}

			throw new NotSupportedException ();
		}

		static ModuleKind GetModuleKind (ushort characteristics, ushort subsystem)
		{
			if ((characteristics & 0x2000) != 0) // ImageCharacteristics.Dll
				return ModuleKind.Dll;

			if (subsystem == 0x2 || subsystem == 0x9) // SubSystem.WindowsGui || SubSystem.WindowsCeGui
				return ModuleKind.Windows;

			return ModuleKind.Console;
		}

		void ReadOptionalHeaders (out ushort subsystem, out ushort dll_characteristics)
		{
			// - PEOptionalHeader
			//   - StandardFieldsHeader

			// Magic				2
			bool pe64 = ReadUInt16 () == 0x20b;

			//						pe32 || pe64

			// LMajor				1
			// LMinor				1
			// CodeSize				4
			// InitializedDataSize	4
			// UninitializedDataSize4
			// EntryPointRVA		4
			// BaseOfCode			4
			// BaseOfData			4 || 0

			//   - NTSpecificFieldsHeader

			// ImageBase			4 || 8
			// SectionAlignment		4
			// FileAlignement		4
			// OSMajor				2
			// OSMinor				2
			// UserMajor			2
			// UserMinor			2
			// SubSysMajor			2
			// SubSysMinor			2
			// Reserved				4
			// ImageSize			4
			// HeaderSize			4
			// FileChecksum			4
			Advance (66);

			// SubSystem			2
			subsystem = ReadUInt16 ();

			// DLLFlags				2
			dll_characteristics = ReadUInt16 ();
			// StackReserveSize		4 || 8
			// StackCommitSize		4 || 8
			// HeapReserveSize		4 || 8
			// HeapCommitSize		4 || 8
			// LoaderFlags			4
			// NumberOfDataDir		4

			//   - DataDirectoriesHeader

			// ExportTable			8
			// ImportTable			8
			// ResourceTable		8
			// ExceptionTable		8
			// CertificateTable		8
			// BaseRelocationTable	8

			Advance (pe64 ? 88 : 72);

			// Debug				8
			image.Debug = ReadDataDirectory ();

			// Copyright			8
			// GlobalPtr			8
			// TLSTable				8
			// LoadConfigTable		8
			// BoundImport			8
			// IAT					8
			// DelayImportDescriptor8
			Advance (56);

			// CLIHeader			8
			cli = ReadDataDirectory ();

			if (cli.IsZero)
				throw new BadImageFormatException ();

			// Reserved				8
			Advance (8);
		}

		string ReadAlignedString (int length)
		{
			int read = 0;
			var buffer = new char [length];
			while (read < length) {
				var current = ReadByte ();
				if (current == 0)
					break;

				buffer [read++] = (char) current;
			}

			Advance (-1 + ((read + 4) & ~3) - read);

			return new string (buffer, 0, read);
		}

		string ReadZeroTerminatedString (int length)
		{
			int read = 0;
			var buffer = new char [length];
			var bytes = ReadBytes (length);
			while (read < length) {
				var current = bytes [read];
				if (current == 0)
					break;

				buffer [read++] = (char) current;
			}

			return new string (buffer, 0, read);
		}

		void ReadSections (ushort count)
		{
			var sections = new Section [count];

			for (int i = 0; i < count; i++) {
				var section = new Section ();

				// Name
				section.Name = ReadZeroTerminatedString (8);

				// VirtualSize		4
				Advance (4);

				// VirtualAddress	4
				section.VirtualAddress = ReadUInt32 ();
				// SizeOfRawData	4
				section.SizeOfRawData = ReadUInt32 ();
				// PointerToRawData	4
				section.PointerToRawData = ReadUInt32 ();

				// PointerToRelocations		4
				// PointerToLineNumbers		4
				// NumberOfRelocations		2
				// NumberOfLineNumbers		2
				// Characteristics			4
				Advance (16);

				sections [i] = section;

				ReadSectionData (section);
			}

			image.Sections = sections;
		}

		void ReadSectionData (Section section)
		{
			var position = BaseStream.Position;

			MoveTo (section.PointerToRawData);

			var length = (int) section.SizeOfRawData;
			var data = new byte [length];
			int offset = 0, read;

			while ((read = Read (data, offset, length - offset)) > 0)
				offset += read;

			section.Data = data;

			BaseStream.Position = position;
		}

		void ReadCLIHeader ()
		{
			MoveTo (cli);

			// - CLIHeader

			// Cb						4
			// MajorRuntimeVersion		2
			// MinorRuntimeVersion		2
			Advance (8);

			// Metadata					8
			metadata = ReadDataDirectory ();
			// Flags					4
			image.Attributes = (ModuleAttributes) ReadUInt32 ();
			// EntryPointToken			4
			image.EntryPointToken = ReadUInt32 ();
			// Resources				8
			image.Resources = ReadDataDirectory ();
			// StrongNameSignature		8
			image.StrongName = ReadDataDirectory ();
			// CodeManagerTable			8
			// VTableFixups				8
			// ExportAddressTableJumps	8
			// ManagedNativeHeader		8
		}

		void ReadMetadata ()
		{
			MoveTo (metadata);

			if (ReadUInt32 () != 0x424a5342)
				throw new BadImageFormatException ();

			// MajorVersion			2
			// MinorVersion			2
			// Reserved				4
			Advance (8);

			var version = ReadZeroTerminatedString (ReadInt32 ());
			image.Runtime = version.ParseRuntime ();

			// Flags		2
			Advance (2);

			var streams = ReadUInt16 ();

			var section = image.GetSectionAtVirtualAddress (metadata.VirtualAddress);
			if (section == null)
				throw new BadImageFormatException ();

			image.MetadataSection = section;

			for (int i = 0; i < streams; i++)
				ReadMetadataStream (section);

			if (image.TableHeap != null)
				ReadTableHeap ();
		}

		void ReadMetadataStream (Section section)
		{
			// Offset		4
			uint start = metadata.VirtualAddress - section.VirtualAddress + ReadUInt32 (); // relative to the section start

			// Size			4
			uint size = ReadUInt32 ();

			var name = ReadAlignedString (16);
			switch (name) {
			case "#~":
			case "#-":
				image.TableHeap = new TableHeap (section, start, size);
				break;
			case "#Strings":
				image.StringHeap = new StringHeap (section, start, size);
				break;
			case "#Blob":
				image.BlobHeap = new BlobHeap (section, start, size);
				break;
			case "#GUID":
				image.GuidHeap = new GuidHeap (section, start, size);
				break;
			case "#US":
				image.UserStringHeap = new UserStringHeap (section, start, size);
				break;
			}
		}

		void ReadTableHeap ()
		{
			var heap = image.TableHeap;

			uint start = heap.Section.PointerToRawData;

			MoveTo (heap.Offset + start);

			// Reserved			4
			// MajorVersion		1
			// MinorVersion		1
			Advance (6);

			// HeapSizes		1
			var sizes = ReadByte ();

			// Reserved2		1
			Advance (1);

			// Valid			8
			heap.Valid = ReadInt64 ();

			// Sorted			8
			heap.Sorted = ReadInt64 ();

			for (int i = 0; i < TableHeap.TableCount; i++) {
				if (!heap.HasTable ((Table) i))
					continue;

				heap.Tables [i].Length = ReadUInt32 ();
			}

			SetIndexSize (image.StringHeap, sizes, 0x1);
			SetIndexSize (image.GuidHeap, sizes, 0x2);
			SetIndexSize (image.BlobHeap, sizes, 0x4);

			ComputeTableInformations ();
		}

		static void SetIndexSize (Heap heap, uint sizes, byte flag)
		{
			if (heap == null)
				return;

			heap.IndexSize = (sizes & flag) > 0 ? 4 : 2;
		}

		int GetTableIndexSize (Table table)
		{
			return image.GetTableIndexSize (table);
		}

		int GetCodedIndexSize (CodedIndex index)
		{
			return image.GetCodedIndexSize (index);
		}

		void ComputeTableInformations ()
		{
			uint offset = (uint) BaseStream.Position - image.MetadataSection.PointerToRawData; // header

			int stridx_size = image.StringHeap.IndexSize;
			int blobidx_size = image.BlobHeap != null ? image.BlobHeap.IndexSize : 2;

			var heap = image.TableHeap;
			var tables = heap.Tables;

			for (int i = 0; i < TableHeap.TableCount; i++) {
				var table = (Table) i;
				if (!heap.HasTable (table))
					continue;

				int size;
				switch (table) {
				case Table.Module:
					size = 2	// Generation
						+ stridx_size	// Name
						+ (image.GuidHeap.IndexSize * 3);	// Mvid, EncId, EncBaseId
					break;
				case Table.TypeRef:
					size = GetCodedIndexSize (CodedIndex.ResolutionScope)	// ResolutionScope
						+ (stridx_size * 2);	// Name, Namespace
					break;
				case Table.TypeDef:
					size = 4	// Flags
						+ (stridx_size * 2)	// Name, Namespace
						+ GetCodedIndexSize (CodedIndex.TypeDefOrRef)	// BaseType
						+ GetTableIndexSize (Table.Field)	// FieldList
						+ GetTableIndexSize (Table.Method);	// MethodList
					break;
				case Table.FieldPtr:
					size = GetTableIndexSize (Table.Field);	// Field
					break;
				case Table.Field:
					size = 2	// Flags
						+ stridx_size	// Name
						+ blobidx_size;	// Signature
					break;
				case Table.MethodPtr:
					size = GetTableIndexSize (Table.Method);	// Method
					break;
				case Table.Method:
					size = 8	// Rva 4, ImplFlags 2, Flags 2
						+ stridx_size	// Name
						+ blobidx_size	// Signature
						+ GetTableIndexSize (Table.Param); // ParamList
					break;
				case Table.ParamPtr:
					size = GetTableIndexSize (Table.Param); // Param
					break;
				case Table.Param:
					size = 4	// Flags 2, Sequence 2
						+ stridx_size;	// Name
					break;
				case Table.InterfaceImpl:
					size = GetTableIndexSize (Table.TypeDef)	// Class
						+ GetCodedIndexSize (CodedIndex.TypeDefOrRef);	// Interface
					break;
				case Table.MemberRef:
					size = GetCodedIndexSize (CodedIndex.MemberRefParent)	// Class
						+ stridx_size	// Name
						+ blobidx_size;	// Signature
					break;
				case Table.Constant:
					size = 2	// Type
						+ GetCodedIndexSize (CodedIndex.HasConstant)	// Parent
						+ blobidx_size;	// Value
					break;
				case Table.CustomAttribute:
					size = GetCodedIndexSize (CodedIndex.HasCustomAttribute)	// Parent
						+ GetCodedIndexSize (CodedIndex.CustomAttributeType)	// Type
						+ blobidx_size;	// Value
					break;
				case Table.FieldMarshal:
					size = GetCodedIndexSize (CodedIndex.HasFieldMarshal)	// Parent
						+ blobidx_size;	// NativeType
					break;
				case Table.DeclSecurity:
					size = 2	// Action
						+ GetCodedIndexSize (CodedIndex.HasDeclSecurity)	// Parent
						+ blobidx_size;	// PermissionSet
					break;
				case Table.ClassLayout:
					size = 6	// PackingSize 2, ClassSize 4
						+ GetTableIndexSize (Table.TypeDef);	// Parent
					break;
				case Table.FieldLayout:
					size = 4	// Offset
						+ GetTableIndexSize (Table.Field);	// Field
					break;
				case Table.StandAloneSig:
					size = blobidx_size;	// Signature
					break;
				case Table.EventMap:
					size = GetTableIndexSize (Table.TypeDef)	// Parent
						+ GetTableIndexSize (Table.Event);	// EventList
					break;
				case Table.EventPtr:
					size = GetTableIndexSize (Table.Event);	// Event
					break;
				case Table.Event:
					size = 2	// Flags
						+ stridx_size // Name
						+ GetCodedIndexSize (CodedIndex.TypeDefOrRef);	// EventType
					break;
				case Table.PropertyMap:
					size = GetTableIndexSize (Table.TypeDef)	// Parent
						+ GetTableIndexSize (Table.Property);	// PropertyList
					break;
				case Table.PropertyPtr:
					size = GetTableIndexSize (Table.Property);	// Property
					break;
				case Table.Property:
					size = 2	// Flags
						+ stridx_size	// Name
						+ blobidx_size;	// Type
					break;
				case Table.MethodSemantics:
					size = 2	// Semantics
						+ GetTableIndexSize (Table.Method)	// Method
						+ GetCodedIndexSize (CodedIndex.HasSemantics);	// Association
					break;
				case Table.MethodImpl:
					size = GetTableIndexSize (Table.TypeDef)	// Class
						+ GetCodedIndexSize (CodedIndex.MethodDefOrRef)	// MethodBody
						+ GetCodedIndexSize (CodedIndex.MethodDefOrRef);	// MethodDeclaration
					break;
				case Table.ModuleRef:
					size = stridx_size;	// Name
					break;
				case Table.TypeSpec:
					size = blobidx_size;	// Signature
					break;
				case Table.ImplMap:
					size = 2	// MappingFlags
						+ GetCodedIndexSize (CodedIndex.MemberForwarded)	// MemberForwarded
						+ stridx_size	// ImportName
						+ GetTableIndexSize (Table.ModuleRef);	// ImportScope
					break;
				case Table.FieldRVA:
					size = 4	// RVA
						+ GetTableIndexSize (Table.Field);	// Field
					break;
				case Table.EncLog:
				case Table.EncMap:
					size = 4;
					break;
				case Table.Assembly:
					size = 16 // HashAlgId 4, Version 4 * 2, Flags 4
						+ blobidx_size	// PublicKey
						+ (stridx_size * 2);	// Name, Culture
					break;
				case Table.AssemblyProcessor:
					size = 4;	// Processor
					break;
				case Table.AssemblyOS:
					size = 12;	// Platform 4, Version 2 * 4
					break;
				case Table.AssemblyRef:
					size = 12	// Version 2 * 4 + Flags 4
						+ (blobidx_size * 2)	// PublicKeyOrToken, HashValue
						+ (stridx_size * 2);	// Name, Culture
					break;
				case Table.AssemblyRefProcessor:
					size = 4	// Processor
						+ GetTableIndexSize (Table.AssemblyRef);	// AssemblyRef
					break;
				case Table.AssemblyRefOS:
					size = 12	// Platform 4, Version 2 * 4
						+ GetTableIndexSize (Table.AssemblyRef);	// AssemblyRef
					break;
				case Table.File:
					size = 4	// Flags
						+ stridx_size	// Name
						+ blobidx_size;	// HashValue
					break;
				case Table.ExportedType:
					size = 8	// Flags 4, TypeDefId 4
						+ (stridx_size * 2)	// Name, Namespace
						+ GetCodedIndexSize (CodedIndex.Implementation);	// Implementation
					break;
				case Table.ManifestResource:
					size = 8	// Offset, Flags
						+ stridx_size	// Name
						+ GetCodedIndexSize (CodedIndex.Implementation);	// Implementation
					break;
				case Table.NestedClass:
					size = GetTableIndexSize (Table.TypeDef)	// NestedClass
						+ GetTableIndexSize (Table.TypeDef);	// EnclosingClass
					break;
				case Table.GenericParam:
					size = 4	// Number, Flags
						+ GetCodedIndexSize (CodedIndex.TypeOrMethodDef)	// Owner
						+ stridx_size;	// Name
					break;
				case Table.MethodSpec:
					size = GetCodedIndexSize (CodedIndex.MethodDefOrRef)	// Method
						+ blobidx_size;	// Instantiation
					break;
				case Table.GenericParamConstraint:
					size = GetTableIndexSize (Table.GenericParam)	// Owner
						+ GetCodedIndexSize (CodedIndex.TypeDefOrRef);	// Constraint
					break;
				default:
					throw new NotSupportedException ();
				}

				tables [i].RowSize = (uint) size;
				tables [i].Offset = offset;

				offset += (uint) size * tables [i].Length;
			}
		}

		public static Image ReadImageFrom (Stream stream)
		{
			try {
				var reader = new ImageReader (stream);
				reader.ReadImage ();
				return reader.image;
			} catch (EndOfStreamException e) {
				throw new BadImageFormatException (stream.GetFullyQualifiedName (), e);
			}
		}
	}
}
