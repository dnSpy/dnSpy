//
// Mono.CSharp.Debugger/MonoSymbolFile.cs
//
// Author:
//   Martin Baulig (martin@ximian.com)
//
// (C) 2003 Ximian, Inc.  http://www.ximian.com
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
using System.Reflection;
using SRE = System.Reflection.Emit;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

namespace Mono.CompilerServices.SymbolWriter
{
	public class MonoSymbolFileException : Exception
	{
		public MonoSymbolFileException ()
			: base ()
		{ }

		public MonoSymbolFileException (string message, params object[] args)
			: base (String.Format (message, args))
		{ }
	}

	internal class MyBinaryWriter : BinaryWriter
	{
		public MyBinaryWriter (Stream stream)
			: base (stream)
		{ }

		public void WriteLeb128 (int value)
		{
			base.Write7BitEncodedInt (value);
		}
	}

	internal class MyBinaryReader : BinaryReader
	{
		public MyBinaryReader (Stream stream)
			: base (stream)
		{ }

		public int ReadLeb128 ()
		{
			return base.Read7BitEncodedInt ();
		}

		public string ReadString (int offset)
		{
			long old_pos = BaseStream.Position;
			BaseStream.Position = offset;

			string text = ReadString ();

			BaseStream.Position = old_pos;
			return text;
		}
	}

	public interface ISourceFile
	{
		SourceFileEntry Entry {
			get;
		}
	}

	public interface ICompileUnit
	{
		CompileUnitEntry Entry {
			get;
		}
	}

	public interface IMethodDef
	{
		string Name {
			get;
		}

		int Token {
			get;
		}
	}

#if !CECIL
	internal class MonoDebuggerSupport
	{
		static GetMethodTokenFunc get_method_token;
		static GetGuidFunc get_guid;
		static GetLocalIndexFunc get_local_index;

		delegate int GetMethodTokenFunc (MethodBase method);
		delegate Guid GetGuidFunc (Module module);
		delegate int GetLocalIndexFunc (SRE.LocalBuilder local);

		static Delegate create_delegate (Type type, Type delegate_type, string name)
		{
			MethodInfo mi = type.GetMethod (name, BindingFlags.Static |
							BindingFlags.NonPublic);
			if (mi == null)
				throw new Exception ("Can't find " + name);

			return Delegate.CreateDelegate (delegate_type, mi);
		}

		static MonoDebuggerSupport ()
		{
			get_method_token = (GetMethodTokenFunc) create_delegate (
				typeof (Assembly), typeof (GetMethodTokenFunc),
				"MonoDebugger_GetMethodToken");

			get_guid = (GetGuidFunc) create_delegate (
				typeof (Module), typeof (GetGuidFunc), "Mono_GetGuid");

			get_local_index = (GetLocalIndexFunc) create_delegate (
				typeof (SRE.LocalBuilder), typeof (GetLocalIndexFunc),
				"Mono_GetLocalIndex");
		}

		public static int GetMethodToken (MethodBase method)
		{
			return get_method_token (method);
		}

		public static Guid GetGuid (Module module)
		{
			return get_guid (module);
		}

		public static int GetLocalIndex (SRE.LocalBuilder local)
		{
			return get_local_index (local);
		}
	}
#endif

	public class MonoSymbolFile : IDisposable
	{
		List<MethodEntry> methods = new List<MethodEntry> ();
		List<SourceFileEntry> sources = new List<SourceFileEntry> ();
		List<CompileUnitEntry> comp_units = new List<CompileUnitEntry> ();
		Dictionary<Type, int> type_hash = new Dictionary<Type, int> ();
		Dictionary<int, AnonymousScopeEntry> anonymous_scopes;

		OffsetTable ot;
		int last_type_index;
		int last_method_index;
		int last_namespace_index;

		public readonly string FileName = "<dynamic>";
		public readonly int MajorVersion = OffsetTable.MajorVersion;
		public readonly int MinorVersion = OffsetTable.MinorVersion;

		public int NumLineNumbers;

		internal MonoSymbolFile ()
		{
			ot = new OffsetTable ();
		}

		internal int AddSource (SourceFileEntry source)
		{
			sources.Add (source);
			return sources.Count;
		}

		internal int AddCompileUnit (CompileUnitEntry entry)
		{
			comp_units.Add (entry);
			return comp_units.Count;
		}

		internal int DefineType (Type type)
		{
			int index;
			if (type_hash.TryGetValue (type, out index))
				return index;

			index = ++last_type_index;
			type_hash.Add (type, index);
			return index;
		}

		internal void AddMethod (MethodEntry entry)
		{
			methods.Add (entry);
		}

		public MethodEntry DefineMethod (CompileUnitEntry comp_unit, int token,
						 ScopeVariable[] scope_vars, LocalVariableEntry[] locals,
						 LineNumberEntry[] lines, CodeBlockEntry[] code_blocks,
						 string real_name, MethodEntry.Flags flags,
						 int namespace_id)
		{
			if (reader != null)
				throw new InvalidOperationException ();

			MethodEntry method = new MethodEntry (
				this, comp_unit, token, scope_vars, locals, lines, code_blocks,
				real_name, flags, namespace_id);
			AddMethod (method);
			return method;
		}

		internal void DefineAnonymousScope (int id)
		{
			if (reader != null)
				throw new InvalidOperationException ();

			if (anonymous_scopes == null)
				anonymous_scopes = new Dictionary<int, AnonymousScopeEntry>  ();

			anonymous_scopes.Add (id, new AnonymousScopeEntry (id));
		}

		internal void DefineCapturedVariable (int scope_id, string name, string captured_name,
						      CapturedVariable.CapturedKind kind)
		{
			if (reader != null)
				throw new InvalidOperationException ();

			AnonymousScopeEntry scope = anonymous_scopes [scope_id];
			scope.AddCapturedVariable (name, captured_name, kind);
		}

		internal void DefineCapturedScope (int scope_id, int id, string captured_name)
		{
			if (reader != null)
				throw new InvalidOperationException ();

			AnonymousScopeEntry scope = anonymous_scopes [scope_id];
			scope.AddCapturedScope (id, captured_name);
		}

		internal int GetNextTypeIndex ()
		{
			return ++last_type_index;
		}

		internal int GetNextMethodIndex ()
		{
			return ++last_method_index;
		}

		internal int GetNextNamespaceIndex ()
		{
			return ++last_namespace_index;
		}

		void Write (MyBinaryWriter bw, Guid guid)
		{
			// Magic number and file version.
			bw.Write (OffsetTable.Magic);
			bw.Write (MajorVersion);
			bw.Write (MinorVersion);

			bw.Write (guid.ToByteArray ());

			//
			// Offsets of file sections; we must write this after we're done
			// writing the whole file, so we just reserve the space for it here.
			//
			long offset_table_offset = bw.BaseStream.Position;
			ot.Write (bw, MajorVersion, MinorVersion);

			//
			// Sort the methods according to their tokens and update their index.
			//
			methods.Sort ();
			for (int i = 0; i < methods.Count; i++)
				((MethodEntry) methods [i]).Index = i + 1;

			//
			// Write data sections.
			//
			ot.DataSectionOffset = (int) bw.BaseStream.Position;
			foreach (SourceFileEntry source in sources)
				source.WriteData (bw);
			foreach (CompileUnitEntry comp_unit in comp_units)
				comp_unit.WriteData (bw);
			foreach (MethodEntry method in methods)
				method.WriteData (this, bw);
			ot.DataSectionSize = (int) bw.BaseStream.Position - ot.DataSectionOffset;

			//
			// Write the method index table.
			//
			ot.MethodTableOffset = (int) bw.BaseStream.Position;
			for (int i = 0; i < methods.Count; i++) {
				MethodEntry entry = (MethodEntry) methods [i];
				entry.Write (bw);
			}
			ot.MethodTableSize = (int) bw.BaseStream.Position - ot.MethodTableOffset;

			//
			// Write source table.
			//
			ot.SourceTableOffset = (int) bw.BaseStream.Position;
			for (int i = 0; i < sources.Count; i++) {
				SourceFileEntry source = (SourceFileEntry) sources [i];
				source.Write (bw);
			}
			ot.SourceTableSize = (int) bw.BaseStream.Position - ot.SourceTableOffset;

			//
			// Write compilation unit table.
			//
			ot.CompileUnitTableOffset = (int) bw.BaseStream.Position;
			for (int i = 0; i < comp_units.Count; i++) {
				CompileUnitEntry unit = (CompileUnitEntry) comp_units [i];
				unit.Write (bw);
			}
			ot.CompileUnitTableSize = (int) bw.BaseStream.Position - ot.CompileUnitTableOffset;

			//
			// Write anonymous scope table.
			//
			ot.AnonymousScopeCount = anonymous_scopes != null ? anonymous_scopes.Count : 0;
			ot.AnonymousScopeTableOffset = (int) bw.BaseStream.Position;
			if (anonymous_scopes != null) {
				foreach (AnonymousScopeEntry scope in anonymous_scopes.Values)
					scope.Write (bw);
			}
			ot.AnonymousScopeTableSize = (int) bw.BaseStream.Position - ot.AnonymousScopeTableOffset;

			//
			// Fixup offset table.
			//
			ot.TypeCount = last_type_index;
			ot.MethodCount = methods.Count;
			ot.SourceCount = sources.Count;
			ot.CompileUnitCount = comp_units.Count;

			//
			// Write offset table.
			//
			ot.TotalFileSize = (int) bw.BaseStream.Position;
			bw.Seek ((int) offset_table_offset, SeekOrigin.Begin);
			ot.Write (bw, MajorVersion, MinorVersion);
			bw.Seek (0, SeekOrigin.End);

#if false
			Console.WriteLine ("TOTAL: {0} line numbes, {1} bytes, extended {2} bytes, " +
					   "{3} methods.", NumLineNumbers, LineNumberSize,
					   ExtendedLineNumberSize, methods.Count);
#endif
		}

		public void CreateSymbolFile (Guid guid, FileStream fs)
		{
			if (reader != null)
				throw new InvalidOperationException ();

			Write (new MyBinaryWriter (fs), guid);
		}

		MyBinaryReader reader;
		Dictionary<int, SourceFileEntry> source_file_hash;
		Dictionary<int, CompileUnitEntry> compile_unit_hash;

		List<MethodEntry> method_list;
		Dictionary<int, MethodEntry> method_token_hash;
		Dictionary<string, int> source_name_hash;

		Guid guid;

		MonoSymbolFile (string filename)
		{
			this.FileName = filename;
			FileStream stream = new FileStream (filename, FileMode.Open, FileAccess.Read);
			reader = new MyBinaryReader (stream);

			try {
				long magic = reader.ReadInt64 ();
				int major_version = reader.ReadInt32 ();
				int minor_version = reader.ReadInt32 ();

				if (magic != OffsetTable.Magic)
					throw new MonoSymbolFileException (
						"Symbol file `{0}' is not a valid " +
						"Mono symbol file", filename);
				if (major_version != OffsetTable.MajorVersion)
					throw new MonoSymbolFileException (
						"Symbol file `{0}' has version {1}, " +
						"but expected {2}", filename, major_version,
						OffsetTable.MajorVersion);
				if (minor_version != OffsetTable.MinorVersion)
					throw new MonoSymbolFileException (
						"Symbol file `{0}' has version {1}.{2}, " +
						"but expected {3}.{4}", filename, major_version,
						minor_version, OffsetTable.MajorVersion,
						OffsetTable.MinorVersion);

				MajorVersion = major_version;
				MinorVersion = minor_version;
				guid = new Guid (reader.ReadBytes (16));

				ot = new OffsetTable (reader, major_version, minor_version);
			} catch {
				throw new MonoSymbolFileException (
					"Cannot read symbol file `{0}'", filename);
			}

			source_file_hash = new Dictionary<int, SourceFileEntry> ();
			compile_unit_hash = new Dictionary<int, CompileUnitEntry> ();
		}

		void CheckGuidMatch (Guid other, string filename, string assembly)
		{
			if (other == guid)
				return;

			throw new MonoSymbolFileException (
				"Symbol file `{0}' does not match assembly `{1}'",
				filename, assembly);
		}

#if CECIL
		protected MonoSymbolFile (string filename, Mono.Cecil.ModuleDefinition module)
			: this (filename)
		{
			// Check that the MDB file matches the module, if we have been
			// passed a module.
			if (module == null)
				return;

			CheckGuidMatch (module.Mvid, filename, module.FullyQualifiedName);
		}

		public static MonoSymbolFile ReadSymbolFile (Mono.Cecil.ModuleDefinition module)
		{
			return ReadSymbolFile (module, module.FullyQualifiedName);
		}

		public static MonoSymbolFile ReadSymbolFile (Mono.Cecil.ModuleDefinition module, string filename)
		{
			string name = filename + ".mdb";

			return new MonoSymbolFile (name, module);
		}
#else
		protected MonoSymbolFile (string filename, Assembly assembly) : this (filename)
		{
			// Check that the MDB file matches the assembly, if we have been
			// passed an assembly.
			if (assembly == null)
				return;

			Module[] modules = assembly.GetModules ();
			Guid assembly_guid = MonoDebuggerSupport.GetGuid (modules [0]);

			CheckGuidMatch (assembly_guid, filename, assembly.Location);
		}

		public static MonoSymbolFile ReadSymbolFile (Assembly assembly)
		{
			string filename = assembly.Location;
			string name = filename + ".mdb";

			return new MonoSymbolFile (name, assembly);
		}
#endif

		public static MonoSymbolFile ReadSymbolFile (string mdbFilename)
		{
			return new MonoSymbolFile (mdbFilename);
		}

		public int CompileUnitCount {
			get { return ot.CompileUnitCount; }
		}

		public int SourceCount {
			get { return ot.SourceCount; }
		}

		public int MethodCount {
			get { return ot.MethodCount; }
		}

		public int TypeCount {
			get { return ot.TypeCount; }
		}

		public int AnonymousScopeCount {
			get { return ot.AnonymousScopeCount; }
		}

		public int NamespaceCount {
			get { return last_namespace_index; }
		}

		public Guid Guid {
			get { return guid; }
		}

		public OffsetTable OffsetTable {
			get { return ot; }
		}

		internal int LineNumberCount = 0;
		internal int LocalCount = 0;
		internal int StringSize = 0;

		internal int LineNumberSize = 0;
		internal int ExtendedLineNumberSize = 0;

		public SourceFileEntry GetSourceFile (int index)
		{
			if ((index < 1) || (index > ot.SourceCount))
				throw new ArgumentException ();
			if (reader == null)
				throw new InvalidOperationException ();

			lock (this) {
				SourceFileEntry source;
				if (source_file_hash.TryGetValue (index, out source))
					return source;

				long old_pos = reader.BaseStream.Position;

				reader.BaseStream.Position = ot.SourceTableOffset +
					SourceFileEntry.Size * (index - 1);
				source = new SourceFileEntry (this, reader);
				source_file_hash.Add (index, source);

				reader.BaseStream.Position = old_pos;
				return source;
			}
		}

		public SourceFileEntry[] Sources {
			get {
				if (reader == null)
					throw new InvalidOperationException ();

				SourceFileEntry[] retval = new SourceFileEntry [SourceCount];
				for (int i = 0; i < SourceCount; i++)
					retval [i] = GetSourceFile (i + 1);
				return retval;
			}
		}

		public CompileUnitEntry GetCompileUnit (int index)
		{
			if ((index < 1) || (index > ot.CompileUnitCount))
				throw new ArgumentException ();
			if (reader == null)
				throw new InvalidOperationException ();

			lock (this) {
				CompileUnitEntry unit;
				if (compile_unit_hash.TryGetValue (index, out unit))
					return unit;

				long old_pos = reader.BaseStream.Position;

				reader.BaseStream.Position = ot.CompileUnitTableOffset +
					CompileUnitEntry.Size * (index - 1);
				unit = new CompileUnitEntry (this, reader);
				compile_unit_hash.Add (index, unit);

				reader.BaseStream.Position = old_pos;
				return unit;
			}
		}

		public CompileUnitEntry[] CompileUnits {
			get {
				if (reader == null)
					throw new InvalidOperationException ();

				CompileUnitEntry[] retval = new CompileUnitEntry [CompileUnitCount];
				for (int i = 0; i < CompileUnitCount; i++)
					retval [i] = GetCompileUnit (i + 1);
				return retval;
			}
		}

		void read_methods ()
		{
			lock (this) {
				if (method_token_hash != null)
					return;

				method_token_hash = new Dictionary<int, MethodEntry> ();
				method_list = new List<MethodEntry> ();

				long old_pos = reader.BaseStream.Position;
				reader.BaseStream.Position = ot.MethodTableOffset;

				for (int i = 0; i < MethodCount; i++) {
					MethodEntry entry = new MethodEntry (this, reader, i + 1);
					method_token_hash.Add (entry.Token, entry);
					method_list.Add (entry);
				}

				reader.BaseStream.Position = old_pos;
			}
		}

		public MethodEntry GetMethodByToken (int token)
		{
			if (reader == null)
				throw new InvalidOperationException ();

			lock (this) {
				read_methods ();
				MethodEntry me;
				method_token_hash.TryGetValue (token, out me);
				return me;
			}
		}

		public MethodEntry GetMethod (int index)
		{
			if ((index < 1) || (index > ot.MethodCount))
				throw new ArgumentException ();
			if (reader == null)
				throw new InvalidOperationException ();

			lock (this) {
				read_methods ();
				return (MethodEntry) method_list [index - 1];
			}
		}

		public MethodEntry[] Methods {
			get {
				if (reader == null)
					throw new InvalidOperationException ();

				lock (this) {
					read_methods ();
					MethodEntry[] retval = new MethodEntry [MethodCount];
					method_list.CopyTo (retval, 0);
					return retval;
				}
			}
		}

		public int FindSource (string file_name)
		{
			if (reader == null)
				throw new InvalidOperationException ();

			lock (this) {
				if (source_name_hash == null) {
					source_name_hash = new Dictionary<string, int> ();

					for (int i = 0; i < ot.SourceCount; i++) {
						SourceFileEntry source = GetSourceFile (i + 1);
						source_name_hash.Add (source.FileName, i);
					}
				}

				int value;
				if (!source_name_hash.TryGetValue (file_name, out value))
					return -1;
				return value;
			}
		}

		public AnonymousScopeEntry GetAnonymousScope (int id)
		{
			if (reader == null)
				throw new InvalidOperationException ();

			AnonymousScopeEntry scope;
			lock (this) {
				if (anonymous_scopes != null) {
					anonymous_scopes.TryGetValue (id, out scope);
					return scope;
				}

				anonymous_scopes = new Dictionary<int, AnonymousScopeEntry> ();
				reader.BaseStream.Position = ot.AnonymousScopeTableOffset;
				for (int i = 0; i < ot.AnonymousScopeCount; i++) {
					scope = new AnonymousScopeEntry (reader);
					anonymous_scopes.Add (scope.ID, scope);
				}

				return anonymous_scopes [id];
			}
		}

		internal MyBinaryReader BinaryReader {
			get {
				if (reader == null)
					throw new InvalidOperationException ();

				return reader;
			}
		}

		public void Dispose ()
		{
			Dispose (true);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				if (reader != null) {
					reader.Close ();
					reader = null;
				}
			}
		}
	}
}
