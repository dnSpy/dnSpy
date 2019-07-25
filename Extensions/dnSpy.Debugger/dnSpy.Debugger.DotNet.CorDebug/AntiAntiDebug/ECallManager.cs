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
using dnlib.IO;
using dnlib.PE;

namespace dnSpy.Debugger.DotNet.CorDebug.AntiAntiDebug {
	sealed class ECallManager {
		/// <summary>
		/// true if we found the CLR module (mscorwks/clr.dll/coreclr.dll)
		/// </summary>
		public bool FoundClrModule { get; }

		readonly Dictionary<string, ECFunc[]> classToFuncsDict = new Dictionary<string, ECFunc[]>(StringComparer.Ordinal);
		ulong clrDllBaseAddress;

		/// <summary>
		/// Constructor that can be used to test this class on some random clr file
		/// </summary>
		/// <param name="filename"></param>
		public ECallManager(string filename) {
			FoundClrModule = true;
			Initialize(filename);
		}

		public ECallManager(int pid, string clrPath) {
			using (var process = Process.GetProcessById(pid)) {
				FoundClrModule = false;
				foreach (ProcessModule? mod in process.Modules) {
					if (StringComparer.OrdinalIgnoreCase.Equals(clrPath, mod!.FileName)) {
						FoundClrModule = true;
						Initialize(mod.FileName);
						clrDllBaseAddress = (ulong)mod.BaseAddress.ToInt64();
						break;
					}
				}
			}
			Debug.Assert(FoundClrModule, $"Couldn't find {clrPath}");
		}

		void Initialize(string filename) {
			try {
				using (var reader = new ECallListReader(filename)) {
					foreach (var ecc in reader.List) {
						var fname = ecc.FullName;
						bool b = classToFuncsDict.ContainsKey(fname);
						Debug.Assert(!b);
						if (!b)
							classToFuncsDict[fname] = ecc.Functions;
					}
				}
			}
			catch {
			}
		}

		public bool FindFunc(string classFullName, string methodName, out ulong methodAddr) {
			methodAddr = 0;
			if (!classToFuncsDict.TryGetValue(classFullName, out var funcs))
				return false;
			foreach (var func in funcs) {
				if (func.Name == methodName) {
					methodAddr = clrDllBaseAddress + func.FunctionRVA;
					return true;
				}
			}
			return false;
		}
	}

	enum FCFuncFlag : ushort {
		EndOfArray = 0x01,
		HasSignature = 0x02,
		Unreferenced = 0x04, // Suppress unused fcall check
		QCall = 0x08, // QCall - mscorlib.dll to mscorwks.dll transition implemented as PInvoke
	}

	[DebuggerDisplay("{FullName}")]
	readonly struct ECClass {
		public readonly string Namespace;
		public readonly string Name;
		public readonly ECFunc[] Functions;
		public string FullName => string.IsNullOrEmpty(Namespace) ? Name : Namespace + "." + Name;

		public ECClass(string ns, string name, ECFunc[] funcs) {
			Namespace = ns;
			Name = name;
			Functions = funcs;
		}
	}

	// Unfortunately this enum is different from sscli20's CorInfoIntrinsics
	enum CorInfoIntrinsics : byte {
		Illegal = byte.MaxValue,
	}

	enum DynamicID : byte {
		FastAllocateString,
		CtorCharArrayManaged,
		CtorCharArrayStartLengthManaged,
		CtorCharCountManaged,
		CtorCharPtrManaged,
		CtorCharPtrStartLengthManaged,
		InternalGetCurrentThread,
		InvalidDynamicFCallId = byte.MaxValue,
	}

	[DebuggerDisplay("{FunctionRVA} {Name}")]
	readonly struct ECFunc {
		public readonly uint RecordRVA;
		public readonly uint Flags;
		public readonly uint FunctionRVA;
		public readonly string Name;
		public readonly uint MethodSigRVA;

		public bool HasSignature => MethodSigRVA != 0;
		public bool IsUnreferenced => (Flags & (uint)FCFuncFlag.Unreferenced) != 0;
		public bool IsQCall => (Flags & (uint)FCFuncFlag.QCall) != 0;
		public CorInfoIntrinsics IntrinsicID => (CorInfoIntrinsics)(Flags >> 16);
		public DynamicID DynamicID => (DynamicID)(Flags >> 24);

		public ECFunc(uint recRva, uint flags, uint methRva, string name, uint sigRva) {
			RecordRVA = recRva;
			Flags = flags;
			FunctionRVA = methRva;
			Name = name;
			MethodSigRVA = sigRva;
		}
	}

	struct ECallListReader : IDisposable {
		readonly PEImage peImage;
		DataReader reader;
		readonly bool is32bit;
		readonly uint ptrSize;
		readonly uint endRva;
		readonly List<ECClass> list;
		TableFormat? tableFormat;

		public List<ECClass> List => list;

		enum TableFormat {
			// .NET 2.0 to ???
			V1,
			// .NET 3.x and later
			V2,
		}

		public ECallListReader(string filename) {
			peImage = new PEImage(filename);
			reader = peImage.CreateReader();
			is32bit = peImage.ImageNTHeaders.OptionalHeader.Magic == 0x010B;
			ptrSize = is32bit ? 4U : 8;
			var last = peImage.ImageSectionHeaders[peImage.ImageSectionHeaders.Count - 1];
			endRva = (uint)last.VirtualAddress + last.VirtualSize;
			list = new List<ECClass>();
			tableFormat = null;
			Read();
		}

		ulong ReadPtr(long pos) {
			reader.Position = (uint)pos;
			return is32bit ? reader.ReadUInt32() : reader.ReadUInt64();
		}

		uint? ReadRva(long pos) {
			ulong ptr = ReadPtr(pos);
			ulong b = peImage.ImageNTHeaders.OptionalHeader.ImageBase;
			if (ptr == 0)
				return 0;
			if (ptr < b)
				return null;
			ptr -= b;
			return ptr >= endRva ? (uint?)null : (uint)ptr;
		}

		ImageSectionHeader? FindSection(string name) {
			foreach (var sect in peImage.ImageSectionHeaders) {
				if (sect.DisplayName == name)
					return sect;
			}
			return null;
		}

		void Read() {
			// Refs: coreclr/src/vm/{ecalllist.h,mscorlib.cpp,ecall.h,ecall.cpp}

			long pos = 0;
			long end = reader.Length - (3 * ptrSize - 1);

			List<ECClass> eccList = new List<ECClass>();
			for (; pos <= end; pos += ptrSize) {
				tableFormat = null;
				var ecc = ReadECClass(pos, true);
				if (ecc is null)
					continue;
				for (long pos2 = pos; pos2 <= end; pos2 += 3 * ptrSize) {
					ecc = ReadECClass(pos2, false);
					if (ecc is null)
						break;
					eccList.Add(ecc.Value);
				}
				if (eccList.Count >= 20)
					break;
				eccList.Clear();
			}

			list.AddRange(eccList);
		}

		ECClass? ReadECClass(long pos, bool first) {
			if (pos + ptrSize * 3 > reader.Length)
				return null;

			var name = ReadAsciizIdPtr(pos);
			if (name is null)
				return null;
			var ns = ReadAsciizIdPtr(pos + ptrSize);
			if (ns is null)
				return null;
			var funcs = ReadECFuncs(ReadRva(pos + ptrSize * 2), first);
			if (funcs is null)
				return null;

			return new ECClass(ns, name, funcs);
		}

		ECFunc[]? ReadECFuncs(uint? rva, bool first) {
			if (rva is null || rva.Value == 0)
				return null;
			var funcs = new List<ECFunc>();

			var pos = (long)peImage.ToFileOffset((RVA)rva.Value);
			if (tableFormat is null)
				InitializeTableFormat(pos);
			if (tableFormat is null)
				return null;
			var tblSize = tableFormat == TableFormat.V1 ? 5 * ptrSize : 3 * ptrSize;
			for (;;) {
				if (pos + ptrSize > reader.Length)
					return null;
				ulong flags = ReadPtr(pos);
				if ((flags & (ulong)FCFuncFlag.EndOfArray) != 0)
					break;
				bool hasSig = (flags & (ulong)FCFuncFlag.HasSignature) != 0;
				uint size = tblSize + (hasSig ? ptrSize : 0);
				if (pos + size > reader.Length)
					return null;

				uint? methRva;
				string? name;
				if (tableFormat == TableFormat.V1) {
					methRva = ReadRva(pos + ptrSize * 1);
					ulong nullPtr1 = ReadPtr(pos + ptrSize * 2);
					ulong nullPtr2 = ReadPtr(pos + ptrSize * 3);
					name = ReadAsciizIdPtr(pos + ptrSize * 4);
					if (nullPtr1 != 0 || nullPtr2 != 0)
						return null;
				}
				else {
					Debug.Assert(tableFormat == TableFormat.V2);
					methRva = ReadRva(pos + ptrSize * 1);
					name = ReadAsciizIdPtr(pos + ptrSize * 2);
				}
				if (name is null || methRva is null)
					return null;
				if (methRva.Value != 0 && !IsCodeRva(methRva.Value))
					return null;
				uint sigRva = 0;
				if (hasSig) {
					var srva = ReadRva(pos + tblSize);
					if (srva is null || srva.Value == 0)
						return null;
					sigRva = srva.Value;
				}
				uint recRva = (uint)peImage.ToRVA((FileOffset)pos);
				funcs.Add(new ECFunc(recRva, (uint)flags, methRva.Value, name, sigRva));
				pos += size;
            }

			// A zero length array is allowed (eg. clr.dll 4.6.96.0) so we can't return null if we find one
			return funcs.ToArray();
		}

		void InitializeTableFormat(long pos) {
			if (pos + ptrSize > reader.Length)
				return;
			ulong flags = ReadPtr(pos);
			if ((flags & (ulong)FCFuncFlag.EndOfArray) != 0)
				return;

			bool hasSig = (flags & (ulong)FCFuncFlag.HasSignature) != 0;

			if (pos + ptrSize * (5 + (hasSig ? 1 : 0)) < reader.Length) {
				uint? methRva = ReadRva(pos + ptrSize * 1);
				ulong nullPtr1 = ReadPtr(pos + ptrSize * 2);
				ulong nullPtr2 = ReadPtr(pos + ptrSize * 3);
				var name = ReadAsciizIdPtr(pos + ptrSize * 4);
				if (nullPtr1 == 0 && nullPtr2 == 0 && !(name is null) && !(methRva is null) && (methRva.Value == 0 || IsCodeRva(methRva.Value))) {
					tableFormat = TableFormat.V1;
					return;
				}
			}


			if (pos + ptrSize * (3 + (hasSig ? 1 : 0)) < reader.Length) {
				uint? methRva = ReadRva(pos + ptrSize * 1);
				var name = ReadAsciizIdPtr(pos + ptrSize * 2);
				if (!(name is null) && !(methRva is null) && (methRva.Value == 0 || IsCodeRva(methRva.Value))) {
					tableFormat = TableFormat.V2;
					return;
				}
			}
		}

		bool IsCodeRva(uint rva) {
			if (rva == 0)
				return false;
			var textSect = FindSection(".text");
			if (textSect is null)
				return false;
			return (uint)textSect.VirtualAddress <= rva && rva < (uint)textSect.VirtualAddress + Math.Max(textSect.VirtualSize, textSect.SizeOfRawData);
		}

		string? ReadAsciizIdPtr(long pos) => ReadAsciizId(ReadRva(pos));

		string? ReadAsciizId(uint? rva) {
			if (rva is null || rva.Value == 0)
				return null;
			reader.Position = (uint)peImage.ToFileOffset((RVA)rva.Value);
			var bytes = reader.TryReadBytesUntil(0);
			const int MIN_ID_LEN = 2;
			const int MAX_ID_LEN = 256;
			if (bytes is null || bytes.Length < MIN_ID_LEN || bytes.Length > MAX_ID_LEN)
				return null;
			foreach (var b in bytes) {
				var ch = (char)b;
				if (!(('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z') || ('0' <= ch && ch <= '9') || ch == '_' || ch == '.'))
					return null;
			}
			var s = Encoding.ASCII.GetString(bytes);
			if (char.IsNumber(s[0]))
				return null;
			return s;
		}

		public void Dispose() => peImage?.Dispose();
	}
}
