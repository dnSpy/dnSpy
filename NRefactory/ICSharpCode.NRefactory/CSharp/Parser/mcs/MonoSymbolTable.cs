//
// Mono.CSharp.Debugger/MonoSymbolTable.cs
//
// Author:
//   Martin Baulig (martin@ximian.com)
//
// (C) 2002 Ximian, Inc.  http://www.ximian.com
//

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
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.IO;

//
// Parts which are actually written into the symbol file are marked with
//
//         #region This is actually written to the symbol file
//         #endregion
//
// Please do not modify these regions without previously talking to me.
//
// All changes to the file format must be synchronized in several places:
//
// a) The fields in these regions (and their order) must match the actual
//    contents of the symbol file.
//
//    This helps people to understand the symbol file format without reading
//    too much source code, ie. you look at the appropriate region and then
//    you know what's actually in the file.
//
//    It is also required to help me enforce b).
//
// b) The regions must be kept in sync with the unmanaged code in
//    mono/metadata/debug-mono-symfile.h
//
// When making changes to the file format, you must also increase two version
// numbers:
//
// i)  OffsetTable.Version in this file.
// ii) MONO_SYMBOL_FILE_VERSION in mono/metadata/debug-mono-symfile.h
//
// After doing so, recompile everything, including the debugger.  Symbol files
// with different versions are incompatible to each other and the debugger and
// the runtime enfore this, so you need to recompile all your assemblies after
// changing the file format.
//

namespace Mono.CompilerServices.SymbolWriter
{
	public class OffsetTable
	{
		public const int  MajorVersion = 50;
		public const int  MinorVersion = 0;
		public const long Magic        = 0x45e82623fd7fa614;

		#region This is actually written to the symbol file
		public int TotalFileSize;
		public int DataSectionOffset;
		public int DataSectionSize;
		public int CompileUnitCount;
		public int CompileUnitTableOffset;
		public int CompileUnitTableSize;
		public int SourceCount;
		public int SourceTableOffset;
		public int SourceTableSize;
		public int MethodCount;
		public int MethodTableOffset;
		public int MethodTableSize;
		public int TypeCount;
		public int AnonymousScopeCount;
		public int AnonymousScopeTableOffset;
		public int AnonymousScopeTableSize;

		[Flags]
		public enum Flags
		{
			IsAspxSource		= 1,
			WindowsFileNames	= 2
		}

		public Flags FileFlags;

		public int LineNumberTable_LineBase = LineNumberTable.Default_LineBase;
		public int LineNumberTable_LineRange = LineNumberTable.Default_LineRange;
		public int LineNumberTable_OpcodeBase = LineNumberTable.Default_OpcodeBase;
		#endregion

		internal OffsetTable ()
		{
			int platform = (int) Environment.OSVersion.Platform;
			if ((platform != 4) && (platform != 128))
				FileFlags |= Flags.WindowsFileNames;
		}

		internal OffsetTable (BinaryReader reader, int major_version, int minor_version)
		{
			TotalFileSize = reader.ReadInt32 ();
			DataSectionOffset = reader.ReadInt32 ();
			DataSectionSize = reader.ReadInt32 ();
			CompileUnitCount = reader.ReadInt32 ();
			CompileUnitTableOffset = reader.ReadInt32 ();
			CompileUnitTableSize = reader.ReadInt32 ();
			SourceCount = reader.ReadInt32 ();
			SourceTableOffset = reader.ReadInt32 ();
			SourceTableSize = reader.ReadInt32 ();
			MethodCount = reader.ReadInt32 ();
			MethodTableOffset = reader.ReadInt32 ();
			MethodTableSize = reader.ReadInt32 ();
			TypeCount = reader.ReadInt32 ();

			AnonymousScopeCount = reader.ReadInt32 ();
			AnonymousScopeTableOffset = reader.ReadInt32 ();
			AnonymousScopeTableSize = reader.ReadInt32 ();

			LineNumberTable_LineBase = reader.ReadInt32 ();
			LineNumberTable_LineRange = reader.ReadInt32 ();
			LineNumberTable_OpcodeBase = reader.ReadInt32 ();

			FileFlags = (Flags) reader.ReadInt32 ();
		}

		internal void Write (BinaryWriter bw, int major_version, int minor_version)
		{
			bw.Write (TotalFileSize);
			bw.Write (DataSectionOffset);
			bw.Write (DataSectionSize);
			bw.Write (CompileUnitCount);
			bw.Write (CompileUnitTableOffset);
			bw.Write (CompileUnitTableSize);
			bw.Write (SourceCount);
			bw.Write (SourceTableOffset);
			bw.Write (SourceTableSize);
			bw.Write (MethodCount);
			bw.Write (MethodTableOffset);
			bw.Write (MethodTableSize);
			bw.Write (TypeCount);

			bw.Write (AnonymousScopeCount);
			bw.Write (AnonymousScopeTableOffset);
			bw.Write (AnonymousScopeTableSize);

			bw.Write (LineNumberTable_LineBase);
			bw.Write (LineNumberTable_LineRange);
			bw.Write (LineNumberTable_OpcodeBase);

			bw.Write ((int) FileFlags);
		}

		public override string ToString ()
		{
			return String.Format (
				"OffsetTable [{0} - {1}:{2} - {3}:{4}:{5} - {6}:{7}:{8} - {9}]",
				TotalFileSize, DataSectionOffset, DataSectionSize, SourceCount,
				SourceTableOffset, SourceTableSize, MethodCount, MethodTableOffset,
				MethodTableSize, TypeCount);
		}
	}

	public class LineNumberEntry
	{
		#region This is actually written to the symbol file
		public readonly int Row;
		public readonly int File;
		public readonly int Offset;
		public readonly bool IsHidden;
		#endregion

		public LineNumberEntry (int file, int row, int offset)
			: this (file, row, offset, false)
		{ }

		public LineNumberEntry (int file, int row, int offset, bool is_hidden)
		{
			this.File = file;
			this.Row = row;
			this.Offset = offset;
			this.IsHidden = is_hidden;
		}

		public static LineNumberEntry Null = new LineNumberEntry (0, 0, 0);

		private class OffsetComparerClass : IComparer<LineNumberEntry>
		{
			public int Compare (LineNumberEntry l1, LineNumberEntry l2)
			{
				if (l1.Offset < l2.Offset)
					return -1;
				else if (l1.Offset > l2.Offset)
					return 1;
				else
					return 0;
			}
		}

		private class RowComparerClass : IComparer<LineNumberEntry>
		{
			public int Compare (LineNumberEntry l1, LineNumberEntry l2)
			{
				if (l1.Row < l2.Row)
					return -1;
				else if (l1.Row > l2.Row)
					return 1;
				else
					return 0;
			}
		}

		public static readonly IComparer<LineNumberEntry> OffsetComparer = new OffsetComparerClass ();
		public static readonly IComparer<LineNumberEntry> RowComparer = new RowComparerClass ();

		public override string ToString ()
		{
			return String.Format ("[Line {0}:{1}:{2}]", File, Row, Offset);
		}
	}

	public class CodeBlockEntry
	{
		public int Index;
		#region This is actually written to the symbol file
		public int Parent;
		public Type BlockType;
		public int StartOffset;
		public int EndOffset;
		#endregion

		public enum Type {
			Lexical			= 1,
			CompilerGenerated	= 2,
			IteratorBody		= 3,
			IteratorDispatcher	= 4
		}

		public CodeBlockEntry (int index, int parent, Type type, int start_offset)
		{
			this.Index = index;
			this.Parent = parent;
			this.BlockType = type;
			this.StartOffset = start_offset;
		}

		internal CodeBlockEntry (int index, MyBinaryReader reader)
		{
			this.Index = index;
			int type_flag = reader.ReadLeb128 ();
			BlockType = (Type) (type_flag & 0x3f);
			this.Parent = reader.ReadLeb128 ();
			this.StartOffset = reader.ReadLeb128 ();
			this.EndOffset = reader.ReadLeb128 ();

			/* Reserved for future extensions. */
			if ((type_flag & 0x40) != 0) {
				int data_size = reader.ReadInt16 ();
				reader.BaseStream.Position += data_size;
			}				
		}

		public void Close (int end_offset)
		{
			this.EndOffset = end_offset;
		}

		internal void Write (MyBinaryWriter bw)
		{
			bw.WriteLeb128 ((int) BlockType);
			bw.WriteLeb128 (Parent);
			bw.WriteLeb128 (StartOffset);
			bw.WriteLeb128 (EndOffset);
		}

		public override string ToString ()
		{
			return String.Format ("[CodeBlock {0}:{1}:{2}:{3}:{4}]",
					      Index, Parent, BlockType, StartOffset, EndOffset);
		}
	}

	public struct LocalVariableEntry
	{
		#region This is actually written to the symbol file
		public readonly int Index;
		public readonly string Name;
		public readonly int BlockIndex;
		#endregion

		public LocalVariableEntry (int index, string name, int block)
		{
			this.Index = index;
			this.Name = name;
			this.BlockIndex = block;
		}

		internal LocalVariableEntry (MonoSymbolFile file, MyBinaryReader reader)
		{
			Index = reader.ReadLeb128 ();
			Name = reader.ReadString ();
			BlockIndex = reader.ReadLeb128 ();
		}

		internal void Write (MonoSymbolFile file, MyBinaryWriter bw)
		{
			bw.WriteLeb128 (Index);
			bw.Write (Name);
			bw.WriteLeb128 (BlockIndex);
		}

		public override string ToString ()
		{
			return String.Format ("[LocalVariable {0}:{1}:{2}]",
					      Name, Index, BlockIndex - 1);
		}
	}

	public struct CapturedVariable
	{
		#region This is actually written to the symbol file
		public readonly string Name;
		public readonly string CapturedName;
		public readonly CapturedKind Kind;
		#endregion

		public enum CapturedKind : byte
		{
			Local,
			Parameter,
			This
		}

		public CapturedVariable (string name, string captured_name,
					 CapturedKind kind)
		{
			this.Name = name;
			this.CapturedName = captured_name;
			this.Kind = kind;
		}

		internal CapturedVariable (MyBinaryReader reader)
		{
			Name = reader.ReadString ();
			CapturedName = reader.ReadString ();
			Kind = (CapturedKind) reader.ReadByte ();
		}

		internal void Write (MyBinaryWriter bw)
		{
			bw.Write (Name);
			bw.Write (CapturedName);
			bw.Write ((byte) Kind);
		}

		public override string ToString ()
		{
			return String.Format ("[CapturedVariable {0}:{1}:{2}]",
					      Name, CapturedName, Kind);
		}
	}

	public struct CapturedScope
	{
		#region This is actually written to the symbol file
		public readonly int Scope;
		public readonly string CapturedName;
		#endregion

		public CapturedScope (int scope, string captured_name)
		{
			this.Scope = scope;
			this.CapturedName = captured_name;
		}

		internal CapturedScope (MyBinaryReader reader)
		{
			Scope = reader.ReadLeb128 ();
			CapturedName = reader.ReadString ();
		}

		internal void Write (MyBinaryWriter bw)
		{
			bw.WriteLeb128 (Scope);
			bw.Write (CapturedName);
		}

		public override string ToString ()
		{
			return String.Format ("[CapturedScope {0}:{1}]",
					      Scope, CapturedName);
		}
	}

	public struct ScopeVariable
	{
		#region This is actually written to the symbol file
		public readonly int Scope;
		public readonly int Index;
		#endregion

		public ScopeVariable (int scope, int index)
		{
			this.Scope = scope;
			this.Index = index;
		}

		internal ScopeVariable (MyBinaryReader reader)
		{
			Scope = reader.ReadLeb128 ();
			Index = reader.ReadLeb128 ();
		}

		internal void Write (MyBinaryWriter bw)
		{
			bw.WriteLeb128 (Scope);
			bw.WriteLeb128 (Index);
		}

		public override string ToString ()
		{
			return String.Format ("[ScopeVariable {0}:{1}]", Scope, Index);
		}
	}

	public class AnonymousScopeEntry
	{
		#region This is actually written to the symbol file
		public readonly int ID;
		#endregion

		List<CapturedVariable> captured_vars = new List<CapturedVariable> ();
		List<CapturedScope> captured_scopes = new List<CapturedScope> ();

		public AnonymousScopeEntry (int id)
		{
			this.ID = id;
		}

		internal AnonymousScopeEntry (MyBinaryReader reader)
		{
			ID = reader.ReadLeb128 ();

			int num_captured_vars = reader.ReadLeb128 ();
			for (int i = 0; i < num_captured_vars; i++)
				captured_vars.Add (new CapturedVariable (reader));

			int num_captured_scopes = reader.ReadLeb128 ();
			for (int i = 0; i < num_captured_scopes; i++)
				captured_scopes.Add (new CapturedScope (reader));
		}

		internal void AddCapturedVariable (string name, string captured_name,
						   CapturedVariable.CapturedKind kind)
		{
			captured_vars.Add (new CapturedVariable (name, captured_name, kind));
		}

		public CapturedVariable[] CapturedVariables {
			get {
				CapturedVariable[] retval = new CapturedVariable [captured_vars.Count];
				captured_vars.CopyTo (retval, 0);
				return retval;
			}
		}

		internal void AddCapturedScope (int scope, string captured_name)
		{
			captured_scopes.Add (new CapturedScope (scope, captured_name));
		}

		public CapturedScope[] CapturedScopes {
			get {
				CapturedScope[] retval = new CapturedScope [captured_scopes.Count];
				captured_scopes.CopyTo (retval, 0);
				return retval;
			}
		}

		internal void Write (MyBinaryWriter bw)
		{
			bw.WriteLeb128 (ID);

			bw.WriteLeb128 (captured_vars.Count);
			foreach (CapturedVariable cv in captured_vars)
				cv.Write (bw);

			bw.WriteLeb128 (captured_scopes.Count);
			foreach (CapturedScope cs in captured_scopes)
				cs.Write (bw);
		}

		public override string ToString ()
		{
			return String.Format ("[AnonymousScope {0}]", ID);
		}
	}

	public class CompileUnitEntry : ICompileUnit
	{
		#region This is actually written to the symbol file
		public readonly int Index;
		int DataOffset;
		#endregion

		MonoSymbolFile file;
		SourceFileEntry source;
		List<SourceFileEntry> include_files;
		List<NamespaceEntry> namespaces;

		bool creating;

		public static int Size {
			get { return 8; }
		}

		CompileUnitEntry ICompileUnit.Entry {
			get { return this; }
		}

		public CompileUnitEntry (MonoSymbolFile file, SourceFileEntry source)
		{
			this.file = file;
			this.source = source;

			this.Index = file.AddCompileUnit (this);

			creating = true;
			namespaces = new List<NamespaceEntry> ();
		}

		public void AddFile (SourceFileEntry file)
		{
			if (!creating)
				throw new InvalidOperationException ();

			if (include_files == null)
				include_files = new List<SourceFileEntry> ();

			include_files.Add (file);
		}

		public SourceFileEntry SourceFile {
			get {
				if (creating)
					return source;

				ReadData ();
				return source;
			}
		}

		public int DefineNamespace (string name, string[] using_clauses, int parent)
		{
			if (!creating)
				throw new InvalidOperationException ();

			int index = file.GetNextNamespaceIndex ();
			NamespaceEntry ns = new NamespaceEntry (name, index, using_clauses, parent);
			namespaces.Add (ns);
			return index;
		}

		internal void WriteData (MyBinaryWriter bw)
		{
			DataOffset = (int) bw.BaseStream.Position;
			bw.WriteLeb128 (source.Index);

			int count_includes = include_files != null ? include_files.Count : 0;
			bw.WriteLeb128 (count_includes);
			if (include_files != null) {
				foreach (SourceFileEntry entry in include_files)
					bw.WriteLeb128 (entry.Index);
			}

			bw.WriteLeb128 (namespaces.Count);
			foreach (NamespaceEntry ns in namespaces)
				ns.Write (file, bw);
		}

		internal void Write (BinaryWriter bw)
		{
			bw.Write (Index);
			bw.Write (DataOffset);
		}

		internal CompileUnitEntry (MonoSymbolFile file, MyBinaryReader reader)
		{
			this.file = file;

			Index = reader.ReadInt32 ();
			DataOffset = reader.ReadInt32 ();
		}

		void ReadData ()
		{
			if (creating)
				throw new InvalidOperationException ();

			lock (file) {
				if (namespaces != null)
					return;

				MyBinaryReader reader = file.BinaryReader;
				int old_pos = (int) reader.BaseStream.Position;

				reader.BaseStream.Position = DataOffset;

				int source_idx = reader.ReadLeb128 ();
				source = file.GetSourceFile (source_idx);

				int count_includes = reader.ReadLeb128 ();
				if (count_includes > 0) {
					include_files = new List<SourceFileEntry> ();
					for (int i = 0; i < count_includes; i++)
						include_files.Add (file.GetSourceFile (reader.ReadLeb128 ()));
				}

				int count_ns = reader.ReadLeb128 ();
				namespaces = new List<NamespaceEntry> ();
				for (int i = 0; i < count_ns; i ++)
					namespaces.Add (new NamespaceEntry (file, reader));

				reader.BaseStream.Position = old_pos;
			}
		}

		public NamespaceEntry[] Namespaces {
			get {
				ReadData ();
				NamespaceEntry[] retval = new NamespaceEntry [namespaces.Count];
				namespaces.CopyTo (retval, 0);
				return retval;
			}
		}

		public SourceFileEntry[] IncludeFiles {
			get {
				ReadData ();
				if (include_files == null)
					return new SourceFileEntry [0];

				SourceFileEntry[] retval = new SourceFileEntry [include_files.Count];
				include_files.CopyTo (retval, 0);
				return retval;
			}
		}
	}

	public class SourceFileEntry
	{
		#region This is actually written to the symbol file
		public readonly int Index;
		int DataOffset;
		#endregion

		MonoSymbolFile file;
		string file_name;
		byte[] guid;
		byte[] hash;
		bool creating;
		bool auto_generated;

		public static int Size {
			get { return 8; }
		}

		public SourceFileEntry (MonoSymbolFile file, string file_name)
		{
			this.file = file;
			this.file_name = file_name;
			this.Index = file.AddSource (this);

			creating = true;
		}

		public SourceFileEntry (MonoSymbolFile file, string file_name,
					byte[] guid, byte[] checksum)
			: this (file, file_name)
		{
			this.guid = guid;
			this.hash = checksum;
		}

		internal void WriteData (MyBinaryWriter bw)
		{
			DataOffset = (int) bw.BaseStream.Position;
			bw.Write (file_name);

			if (guid == null) {
				guid = Guid.NewGuid ().ToByteArray ();
				try {
					using (FileStream fs = new FileStream (file_name, FileMode.Open, FileAccess.Read)) {
						MD5 md5 = MD5.Create ();
						hash = md5.ComputeHash (fs);
					}
				} catch {
					hash = new byte [16];
				}
			}

			bw.Write (guid);
			bw.Write (hash);
			bw.Write ((byte) (auto_generated ? 1 : 0));
		}

		internal void Write (BinaryWriter bw)
		{
			bw.Write (Index);
			bw.Write (DataOffset);
		}

		internal SourceFileEntry (MonoSymbolFile file, MyBinaryReader reader)
		{
			this.file = file;

			Index = reader.ReadInt32 ();
			DataOffset = reader.ReadInt32 ();

			int old_pos = (int) reader.BaseStream.Position;
			reader.BaseStream.Position = DataOffset;

			file_name = reader.ReadString ();
			guid = reader.ReadBytes (16);
			hash = reader.ReadBytes (16);
			auto_generated = reader.ReadByte () == 1;

			reader.BaseStream.Position = old_pos;
		}

		public string FileName {
			get { return file_name; }
		}

		public bool AutoGenerated {
			get { return auto_generated; }
		}

		public void SetAutoGenerated ()
		{
			if (!creating)
				throw new InvalidOperationException ();

			auto_generated = true;
			file.OffsetTable.FileFlags |= OffsetTable.Flags.IsAspxSource;
		}

		public bool CheckChecksum ()
		{
			try {
				using (FileStream fs = new FileStream (file_name, FileMode.Open)) {
					MD5 md5 = MD5.Create ();
					byte[] data = md5.ComputeHash (fs);
					for (int i = 0; i < 16; i++)
						if (data [i] != hash [i])
							return false;
					return true;
				}
			} catch {
				return false;
			}
		}

		public override string ToString ()
		{
			return String.Format ("SourceFileEntry ({0}:{1})", Index, DataOffset);
		}
	}

	public class LineNumberTable
	{
		protected LineNumberEntry[] _line_numbers;
		public LineNumberEntry[] LineNumbers {
			get { return _line_numbers; }
		}

		public readonly int LineBase;
		public readonly int LineRange;
		public readonly byte OpcodeBase;
		public readonly int MaxAddressIncrement;

#region Configurable constants
		public const int Default_LineBase = -1;
		public const int Default_LineRange = 8;
		public const byte Default_OpcodeBase = 9;

		public const bool SuppressDuplicates = true;
#endregion

		public const byte DW_LNS_copy = 1;
		public const byte DW_LNS_advance_pc = 2;
		public const byte DW_LNS_advance_line = 3;
		public const byte DW_LNS_set_file = 4;
		public const byte DW_LNS_const_add_pc = 8;

		public const byte DW_LNE_end_sequence = 1;

		// MONO extensions.
		public const byte DW_LNE_MONO_negate_is_hidden = 0x40;

		internal const byte DW_LNE_MONO__extensions_start = 0x40;
		internal const byte DW_LNE_MONO__extensions_end   = 0x7f;

		protected LineNumberTable (MonoSymbolFile file)
		{
			this.LineBase = file.OffsetTable.LineNumberTable_LineBase;
			this.LineRange = file.OffsetTable.LineNumberTable_LineRange;
			this.OpcodeBase = (byte) file.OffsetTable.LineNumberTable_OpcodeBase;
			this.MaxAddressIncrement = (255 - OpcodeBase) / LineRange;
		}

		internal LineNumberTable (MonoSymbolFile file, LineNumberEntry[] lines)
			: this (file)
		{
			this._line_numbers = lines;
		}

		internal void Write (MonoSymbolFile file, MyBinaryWriter bw)
		{
			int start = (int) bw.BaseStream.Position;

			bool last_is_hidden = false;
			int last_line = 1, last_offset = 0, last_file = 1;
			for (int i = 0; i < LineNumbers.Length; i++) {
				int line_inc = LineNumbers [i].Row - last_line;
				int offset_inc = LineNumbers [i].Offset - last_offset;

				if (SuppressDuplicates && (i+1 < LineNumbers.Length)) {
					if (LineNumbers [i+1].Equals (LineNumbers [i]))
						continue;
				}

				if (LineNumbers [i].File != last_file) {
					bw.Write (DW_LNS_set_file);
					bw.WriteLeb128 (LineNumbers [i].File);
					last_file = LineNumbers [i].File;
				}

				if (LineNumbers [i].IsHidden != last_is_hidden) {
					bw.Write ((byte) 0);
					bw.Write ((byte) 1);
					bw.Write (DW_LNE_MONO_negate_is_hidden);
					last_is_hidden = LineNumbers [i].IsHidden;
				}

				if (offset_inc >= MaxAddressIncrement) {
					if (offset_inc < 2 * MaxAddressIncrement) {
						bw.Write (DW_LNS_const_add_pc);
						offset_inc -= MaxAddressIncrement;
					} else {
						bw.Write (DW_LNS_advance_pc);
						bw.WriteLeb128 (offset_inc);
						offset_inc = 0;
					}
				}

				if ((line_inc < LineBase) || (line_inc >= LineBase + LineRange)) {
					bw.Write (DW_LNS_advance_line);
					bw.WriteLeb128 (line_inc);
					if (offset_inc != 0) {
						bw.Write (DW_LNS_advance_pc);
						bw.WriteLeb128 (offset_inc);
					}
					bw.Write (DW_LNS_copy);
				} else {
					byte opcode;
					opcode = (byte) (line_inc - LineBase + (LineRange * offset_inc) +
							 OpcodeBase);
					bw.Write (opcode);
				}

				last_line = LineNumbers [i].Row;
				last_offset = LineNumbers [i].Offset;
			}

			bw.Write ((byte) 0);
			bw.Write ((byte) 1);
			bw.Write (DW_LNE_end_sequence);

			file.ExtendedLineNumberSize += (int) bw.BaseStream.Position - start;
		}

		internal static LineNumberTable Read (MonoSymbolFile file, MyBinaryReader br)
		{
			LineNumberTable lnt = new LineNumberTable (file);
			lnt.DoRead (file, br);
			return lnt;
		}

		void DoRead (MonoSymbolFile file, MyBinaryReader br)
		{
			var lines = new List<LineNumberEntry> ();

			bool is_hidden = false, modified = false;
			int stm_line = 1, stm_offset = 0, stm_file = 1;
			while (true) {
				byte opcode = br.ReadByte ();

				if (opcode == 0) {
					byte size = br.ReadByte ();
					long end_pos = br.BaseStream.Position + size;
					opcode = br.ReadByte ();

					if (opcode == DW_LNE_end_sequence) {
						if (modified)
							lines.Add (new LineNumberEntry (
								stm_file, stm_line, stm_offset, is_hidden));
						break;
					} else if (opcode == DW_LNE_MONO_negate_is_hidden) {
						is_hidden = !is_hidden;
						modified = true;
					} else if ((opcode >= DW_LNE_MONO__extensions_start) &&
						   (opcode <= DW_LNE_MONO__extensions_end)) {
						; // reserved for future extensions
					} else {
						throw new MonoSymbolFileException (
							"Unknown extended opcode {0:x} in LNT ({1})",
							opcode, file.FileName);
					}

					br.BaseStream.Position = end_pos;
					continue;
				} else if (opcode < OpcodeBase) {
					switch (opcode) {
					case DW_LNS_copy:
						lines.Add (new LineNumberEntry (
							stm_file, stm_line, stm_offset, is_hidden));
						modified = false;
						break;
					case DW_LNS_advance_pc:
						stm_offset += br.ReadLeb128 ();
						modified = true;
						break;
					case DW_LNS_advance_line:
						stm_line += br.ReadLeb128 ();
						modified = true;
						break;
					case DW_LNS_set_file:
						stm_file = br.ReadLeb128 ();
						modified = true;
						break;
					case DW_LNS_const_add_pc:
						stm_offset += MaxAddressIncrement;
						modified = true;
						break;
					default:
						throw new MonoSymbolFileException (
							"Unknown standard opcode {0:x} in LNT",
							opcode);
					}
				} else {
					opcode -= OpcodeBase;

					stm_offset += opcode / LineRange;
					stm_line += LineBase + (opcode % LineRange);
					lines.Add (new LineNumberEntry (
						stm_file, stm_line, stm_offset, is_hidden));
					modified = false;
				}
			}

			_line_numbers = new LineNumberEntry [lines.Count];
			lines.CopyTo (_line_numbers, 0);
		}

		public bool GetMethodBounds (out LineNumberEntry start, out LineNumberEntry end)
		{
			if (_line_numbers.Length > 1) {
				start = _line_numbers [0];
				end = _line_numbers [_line_numbers.Length - 1];
				return true;
			}

			start = LineNumberEntry.Null;
			end = LineNumberEntry.Null;
			return false;
		}
	}

	public class MethodEntry : IComparable
	{
		#region This is actually written to the symbol file
		public readonly int CompileUnitIndex;
		public readonly int Token;
		public readonly int NamespaceID;

		int DataOffset;
		int LocalVariableTableOffset;
		int LineNumberTableOffset;
		int CodeBlockTableOffset;
		int ScopeVariableTableOffset;
		int RealNameOffset;
		Flags flags;
		#endregion

		int index;

		public Flags MethodFlags {
			get { return flags; }
		}

		public readonly CompileUnitEntry CompileUnit;

		LocalVariableEntry[] locals;
		CodeBlockEntry[] code_blocks;
		ScopeVariable[] scope_vars;
		LineNumberTable lnt;
		string real_name;

		public readonly MonoSymbolFile SymbolFile;

		public int Index {
			get { return index; }
			set { index = value; }
		}

		[Flags]
		public enum Flags
		{
			LocalNamesAmbiguous	= 1
		}

		public const int Size = 12;

		internal MethodEntry (MonoSymbolFile file, MyBinaryReader reader, int index)
		{
			this.SymbolFile = file;
			this.index = index;

			Token = reader.ReadInt32 ();
			DataOffset = reader.ReadInt32 ();
			LineNumberTableOffset = reader.ReadInt32 ();

			long old_pos = reader.BaseStream.Position;
			reader.BaseStream.Position = DataOffset;

			CompileUnitIndex = reader.ReadLeb128 ();
			LocalVariableTableOffset = reader.ReadLeb128 ();
			NamespaceID = reader.ReadLeb128 ();

			CodeBlockTableOffset = reader.ReadLeb128 ();
			ScopeVariableTableOffset = reader.ReadLeb128 ();

			RealNameOffset = reader.ReadLeb128 ();

			flags = (Flags) reader.ReadLeb128 ();

			reader.BaseStream.Position = old_pos;

			CompileUnit = file.GetCompileUnit (CompileUnitIndex);
		}

		internal MethodEntry (MonoSymbolFile file, CompileUnitEntry comp_unit,
				      int token, ScopeVariable[] scope_vars,
				      LocalVariableEntry[] locals, LineNumberEntry[] lines,
				      CodeBlockEntry[] code_blocks, string real_name,
				      Flags flags, int namespace_id)
		{
			this.SymbolFile = file;
			this.real_name = real_name;
			this.locals = locals;
			this.code_blocks = code_blocks;
			this.scope_vars = scope_vars;
			this.flags = flags;

			index = -1;

			Token = token;
			CompileUnitIndex = comp_unit.Index;
			CompileUnit = comp_unit;
			NamespaceID = namespace_id;

			CheckLineNumberTable (lines);
			lnt = new LineNumberTable (file, lines);
			file.NumLineNumbers += lines.Length;

			int num_locals = locals != null ? locals.Length : 0;

			if (num_locals <= 32) {
				// Most of the time, the O(n^2) factor is actually
				// less than the cost of allocating the hash table,
				// 32 is a rough number obtained through some testing.
				
				for (int i = 0; i < num_locals; i ++) {
					string nm = locals [i].Name;
					
					for (int j = i + 1; j < num_locals; j ++) {
						if (locals [j].Name == nm) {
							flags |= Flags.LocalNamesAmbiguous;
							goto locals_check_done;
						}
					}
				}
			locals_check_done :
				;
			} else {
				var local_names = new Dictionary<string, LocalVariableEntry> ();
				foreach (LocalVariableEntry local in locals) {
					if (local_names.ContainsKey (local.Name)) {
						flags |= Flags.LocalNamesAmbiguous;
						break;
					}
					local_names.Add (local.Name, local);
				}
			}
		}
		
		void CheckLineNumberTable (LineNumberEntry[] line_numbers)
		{
			int last_offset = -1;
			int last_row = -1;

			if (line_numbers == null)
				return;
			
			for (int i = 0; i < line_numbers.Length; i++) {
				LineNumberEntry line = line_numbers [i];

				if (line.Equals (LineNumberEntry.Null))
					throw new MonoSymbolFileException ();

				if (line.Offset < last_offset)
					throw new MonoSymbolFileException ();

				if (line.Offset > last_offset) {
					last_row = line.Row;
					last_offset = line.Offset;
				} else if (line.Row > last_row) {
					last_row = line.Row;
				}
			}
		}

		internal void Write (MyBinaryWriter bw)
		{
			if ((index <= 0) || (DataOffset == 0))
				throw new InvalidOperationException ();

			bw.Write (Token);
			bw.Write (DataOffset);
			bw.Write (LineNumberTableOffset);
		}

		internal void WriteData (MonoSymbolFile file, MyBinaryWriter bw)
		{
			if (index <= 0)
				throw new InvalidOperationException ();

			LocalVariableTableOffset = (int) bw.BaseStream.Position;
			int num_locals = locals != null ? locals.Length : 0;
			bw.WriteLeb128 (num_locals);
			for (int i = 0; i < num_locals; i++)
				locals [i].Write (file, bw);
			file.LocalCount += num_locals;

			CodeBlockTableOffset = (int) bw.BaseStream.Position;
			int num_code_blocks = code_blocks != null ? code_blocks.Length : 0;
			bw.WriteLeb128 (num_code_blocks);
			for (int i = 0; i < num_code_blocks; i++)
				code_blocks [i].Write (bw);

			ScopeVariableTableOffset = (int) bw.BaseStream.Position;
			int num_scope_vars = scope_vars != null ? scope_vars.Length : 0;
			bw.WriteLeb128 (num_scope_vars);
			for (int i = 0; i < num_scope_vars; i++)
				scope_vars [i].Write (bw);

			if (real_name != null) {
				RealNameOffset = (int) bw.BaseStream.Position;
				bw.Write (real_name);
			}

			LineNumberTableOffset = (int) bw.BaseStream.Position;
			lnt.Write (file, bw);

			DataOffset = (int) bw.BaseStream.Position;

			bw.WriteLeb128 (CompileUnitIndex);
			bw.WriteLeb128 (LocalVariableTableOffset);
			bw.WriteLeb128 (NamespaceID);

			bw.WriteLeb128 (CodeBlockTableOffset);
			bw.WriteLeb128 (ScopeVariableTableOffset);

			bw.WriteLeb128 (RealNameOffset);
			bw.WriteLeb128 ((int) flags);
		}

		public LineNumberTable GetLineNumberTable ()
		{
			lock (SymbolFile) {
				if (lnt != null)
					return lnt;

				if (LineNumberTableOffset == 0)
					return null;

				MyBinaryReader reader = SymbolFile.BinaryReader;
				long old_pos = reader.BaseStream.Position;
				reader.BaseStream.Position = LineNumberTableOffset;

				lnt = LineNumberTable.Read (SymbolFile, reader);

				reader.BaseStream.Position = old_pos;
				return lnt;
			}
		}

		public LocalVariableEntry[] GetLocals ()
		{
			lock (SymbolFile) {
				if (locals != null)
					return locals;

				if (LocalVariableTableOffset == 0)
					return null;

				MyBinaryReader reader = SymbolFile.BinaryReader;
				long old_pos = reader.BaseStream.Position;
				reader.BaseStream.Position = LocalVariableTableOffset;

				int num_locals = reader.ReadLeb128 ();
				locals = new LocalVariableEntry [num_locals];

				for (int i = 0; i < num_locals; i++)
					locals [i] = new LocalVariableEntry (SymbolFile, reader);

				reader.BaseStream.Position = old_pos;
				return locals;
			}
		}

		public CodeBlockEntry[] GetCodeBlocks ()
		{
			lock (SymbolFile) {
				if (code_blocks != null)
					return code_blocks;

				if (CodeBlockTableOffset == 0)
					return null;

				MyBinaryReader reader = SymbolFile.BinaryReader;
				long old_pos = reader.BaseStream.Position;
				reader.BaseStream.Position = CodeBlockTableOffset;

				int num_code_blocks = reader.ReadLeb128 ();
				code_blocks = new CodeBlockEntry [num_code_blocks];

				for (int i = 0; i < num_code_blocks; i++)
					code_blocks [i] = new CodeBlockEntry (i, reader);

				reader.BaseStream.Position = old_pos;
				return code_blocks;
			}
		}

		public ScopeVariable[] GetScopeVariables ()
		{
			lock (SymbolFile) {
				if (scope_vars != null)
					return scope_vars;

				if (ScopeVariableTableOffset == 0)
					return null;

				MyBinaryReader reader = SymbolFile.BinaryReader;
				long old_pos = reader.BaseStream.Position;
				reader.BaseStream.Position = ScopeVariableTableOffset;

				int num_scope_vars = reader.ReadLeb128 ();
				scope_vars = new ScopeVariable [num_scope_vars];

				for (int i = 0; i < num_scope_vars; i++)
					scope_vars [i] = new ScopeVariable (reader);

				reader.BaseStream.Position = old_pos;
				return scope_vars;
			}
		}

		public string GetRealName ()
		{
			lock (SymbolFile) {
				if (real_name != null)
					return real_name;

				if (RealNameOffset == 0)
					return null;

				real_name = SymbolFile.BinaryReader.ReadString (RealNameOffset);
				return real_name;
			}
		}

		public int CompareTo (object obj)
		{
			MethodEntry method = (MethodEntry) obj;

			if (method.Token < Token)
				return 1;
			else if (method.Token > Token)
				return -1;
			else
				return 0;
		}

		public override string ToString ()
		{
			return String.Format ("[Method {0}:{1:x}:{2}:{3}]",
					      index, Token, CompileUnitIndex, CompileUnit);
		}
	}

	public struct NamespaceEntry
	{
		#region This is actually written to the symbol file
		public readonly string Name;
		public readonly int Index;
		public readonly int Parent;
		public readonly string[] UsingClauses;
		#endregion

		public NamespaceEntry (string name, int index, string[] using_clauses, int parent)
		{
			this.Name = name;
			this.Index = index;
			this.Parent = parent;
			this.UsingClauses = using_clauses != null ? using_clauses : new string [0];
		}

		internal NamespaceEntry (MonoSymbolFile file, MyBinaryReader reader)
		{
			Name = reader.ReadString ();
			Index = reader.ReadLeb128 ();
			Parent = reader.ReadLeb128 ();

			int count = reader.ReadLeb128 ();
			UsingClauses = new string [count];
			for (int i = 0; i < count; i++)
				UsingClauses [i] = reader.ReadString ();
		}

		internal void Write (MonoSymbolFile file, MyBinaryWriter bw)
		{
			bw.Write (Name);
			bw.WriteLeb128 (Index);
			bw.WriteLeb128 (Parent);
			bw.WriteLeb128 (UsingClauses.Length);
			foreach (string uc in UsingClauses)
				bw.Write (uc);
		}

		public override string ToString ()
		{
			return String.Format ("[Namespace {0}:{1}:{2}]", Name, Index, Parent);
		}
	}
}
