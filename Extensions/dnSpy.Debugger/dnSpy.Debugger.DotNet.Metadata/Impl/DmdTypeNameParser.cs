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
using System.IO;
using DMD = dnlib.DotNet;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	[Serializable]
	sealed class TypeNameParserException : Exception {
		public TypeNameParserException(string message) : base(message) { }
		public TypeNameParserException(string message, Exception innerException) : base(message, innerException) { }
	}

	interface ITypeDefResolver {
		DmdTypeDef GetTypeDef(IDmdAssemblyName assemblyName, List<string> typeNames);
	}

	struct DmdTypeNameParser : IDisposable {
		readonly DmdModule ownerModule;
		readonly ITypeDefResolver typeDefResolver;
		readonly IList<DmdType> genericTypeArguments;
		readonly StringReader reader;
		const int MAX_RECURSION_COUNT = 100;
		int recursionCounter;

		public static DmdType ParseThrow(DmdModule ownerModule, string typeFullName) =>
			ParseThrow(ownerModule, typeFullName, null);

		public static DmdType ParseThrow(DmdModule ownerModule, string typeFullName, IList<DmdType> genericTypeArguments) {
			using (var parser = new DmdTypeNameParser(ownerModule, typeFullName, genericTypeArguments))
				return parser.ParseCore();
		}

		public static DmdType Parse(DmdModule ownerModule, string typeFullName) =>
			Parse(ownerModule, typeFullName, null);

		public static DmdType Parse(DmdModule ownerModule, string typeFullName, IList<DmdType> genericTypeArguments) {
			try {
				return ParseThrow(ownerModule, typeFullName, genericTypeArguments);
			}
			catch (TypeNameParserException) {
				return null;
			}
		}

		public static DmdType ParseThrow(ITypeDefResolver typeDefResolver, string typeFullName) =>
			ParseThrow(typeDefResolver, typeFullName, null);

		public static DmdType ParseThrow(ITypeDefResolver typeDefResolver, string typeFullName, IList<DmdType> genericTypeArguments) {
			using (var parser = new DmdTypeNameParser(typeDefResolver, typeFullName, genericTypeArguments))
				return parser.ParseCore();
		}

		public static DmdType Parse(ITypeDefResolver typeDefResolver, string typeFullName) =>
			Parse(typeDefResolver, typeFullName, null);

		public static DmdType Parse(ITypeDefResolver typeDefResolver, string typeFullName, IList<DmdType> genericTypeArguments) {
			try {
				return ParseThrow(typeDefResolver, typeFullName, genericTypeArguments);
			}
			catch (TypeNameParserException) {
				return null;
			}
		}

		DmdTypeNameParser(DmdModule ownerModule, string typeFullName, IList<DmdType> genericTypeArguments) {
			this.ownerModule = ownerModule;
			typeDefResolver = null;
			reader = new StringReader(typeFullName ?? string.Empty);
			this.genericTypeArguments = genericTypeArguments ?? Array.Empty<DmdType>();
			recursionCounter = 0;
		}

		DmdTypeNameParser(ITypeDefResolver typeDefResolver, string typeFullName, IList<DmdType> genericTypeArguments) {
			ownerModule = null;
			this.typeDefResolver = typeDefResolver;
			reader = new StringReader(typeFullName ?? string.Empty);
			this.genericTypeArguments = genericTypeArguments ?? Array.Empty<DmdType>();
			recursionCounter = 0;
		}

		void IncrementRecursionCounter() {
			if (recursionCounter >= MAX_RECURSION_COUNT)
				throw new TypeNameParserException("Stack overflow");
			recursionCounter++;
		}
		void DecrementRecursionCounter() => recursionCounter--;

		public void Dispose() => reader?.Dispose();

		abstract class TSpec {
			public readonly DMD.ElementType etype;
			protected TSpec(DMD.ElementType etype) => this.etype = etype;
		}

		sealed class SZArraySpec : TSpec {
			public static readonly SZArraySpec Instance = new SZArraySpec();
			SZArraySpec() : base(DMD.ElementType.SZArray) { }
		}

		sealed class ArraySpec : TSpec {
			public int rank;
			public readonly IList<int> sizes = new List<int>();
			public readonly IList<int> lowerBounds = new List<int>();
			public ArraySpec() : base(DMD.ElementType.Array) { }
		}

		sealed class GenericInstSpec : TSpec {
			public readonly List<DmdType> args = new List<DmdType>();
			public GenericInstSpec() : base(DMD.ElementType.GenericInst) { }
		}

		sealed class ByRefSpec : TSpec {
			public static readonly ByRefSpec Instance = new ByRefSpec();
			ByRefSpec() : base(DMD.ElementType.ByRef) { }
		}

		sealed class PtrSpec : TSpec {
			public static readonly PtrSpec Instance = new PtrSpec();
			PtrSpec() : base(DMD.ElementType.Ptr) { }
		}

		DmdType ReadGenericSig() {
			Verify(ReadChar() == '!', "Expected '!'");
			IList<DmdType> types;
			if (PeekChar() == '!') {
				ReadChar();
				types = Array.Empty<DmdType>();
			}
			else
				types = genericTypeArguments;
			uint index = ReadUInt32();
			return index < (uint)types.Count ? types[(int)index] : throw new TypeNameParserException("Invalid generic type index");
		}

		DmdType CreateTypeSig(IList<TSpec> tspecs, DmdType currentType) {
			foreach (var tspec in tspecs) {
				switch (tspec.etype) {
				case DMD.ElementType.SZArray:
					currentType = currentType.MakeArrayType();
					break;

				case DMD.ElementType.Array:
					var arraySpec = (ArraySpec)tspec;
					currentType = currentType.MakeArrayType(arraySpec.rank, arraySpec.sizes, arraySpec.lowerBounds);
					break;

				case DMD.ElementType.GenericInst:
					var ginstSpec = (GenericInstSpec)tspec;
					currentType = currentType.MakeGenericType(ginstSpec.args.ToArray());
					break;

				case DMD.ElementType.ByRef:
					currentType = currentType.MakeByRefType();
					break;

				case DMD.ElementType.Ptr:
					currentType = currentType.MakePointerType();
					break;

				default:
					Verify(false, "Unknown TSpec");
					break;
				}
			}
			return currentType;
		}

		DmdParsedTypeRef CreateTypeRef(List<string> typeNames) {
			if (typeNames.Count == 0)
				throw new InvalidOperationException();
			DmdParsedTypeRef typeRef = null;
			for (int i = 0; i < typeNames.Count; i++) {
				var newTypeRef = CreateTypeRefNoAssembly(typeNames[i], typeRef);
				typeRef = newTypeRef;
			}
			return typeRef;
		}

		List<string> ReadTypeRefAndNestedNoAssembly(char nestedChar) {
			var typeNames = new List<string>();
			while (true) {
				typeNames.Add(ReadId(false));
				SkipWhite();
				if (PeekChar() != nestedChar)
					break;
				ReadChar();
			}
			return typeNames;
		}

		DmdParsedTypeRef CreateTypeRefNoAssembly(string fullName, DmdParsedTypeRef declaringTypeRef) {
			DmdTypeUtilities.SplitFullName(fullName, out string ns, out string name);
			return new DmdParsedTypeRef(ownerModule, declaringTypeRef, DmdTypeScope.Invalid, ns, name, null);
		}

		IDmdAssemblyName FindAssemblyRef(DmdParsedTypeRef nonNestedTypeRef) {
			IDmdAssemblyName asmRef = null;
			if ((object)nonNestedTypeRef != null)
				asmRef = FindAssemblyRefCore(nonNestedTypeRef);
			return asmRef ?? ownerModule.Assembly.GetName();
		}

		IDmdAssemblyName FindAssemblyRefCore(DmdParsedTypeRef nonNestedTypeRef) {
			var modAsm = (DmdAssemblyImpl)ownerModule.Assembly;
			var type = modAsm.GetType(nonNestedTypeRef, ignoreCase: false);
			if ((object)type != null)
				return modAsm.GetName();

			var corLibAsm = (DmdAssemblyImpl)ownerModule.AppDomain.CorLib;
			if (corLibAsm != null) {
				type = corLibAsm.GetType(nonNestedTypeRef, ignoreCase: false);
				if ((object)type != null)
					return corLibAsm.GetName();
			}

			return modAsm.GetName();
		}

		static void Verify(bool b, string msg) {
			if (!b)
				throw new TypeNameParserException(msg);
		}

		void SkipWhite() {
			while (true) {
				int next = PeekChar();
				if (next == -1)
					break;
				if (!char.IsWhiteSpace((char)next))
					break;
				ReadChar();
			}
		}

		uint ReadUInt32() {
			SkipWhite();
			bool readInt = false;
			uint val = 0;
			while (true) {
				int c = PeekChar();
				if (c == -1 || !(c >= '0' && c <= '9'))
					break;
				ReadChar();
				uint newVal = val * 10 + (uint)(c - '0');
				Verify(newVal >= val, "Integer overflow");
				val = newVal;
				readInt = true;
			}
			Verify(readInt, "Expected an integer");
			return val;
		}

		int ReadInt32() {
			SkipWhite();

			bool isSigned = false;
			if (PeekChar() == '-') {
				isSigned = true;
				ReadChar();
			}

			uint val = ReadUInt32();
			if (isSigned) {
				Verify(val <= (uint)int.MaxValue + 1, "Integer overflow");
				return -(int)val;
			}
			else {
				Verify(val <= (uint)int.MaxValue, "Integer overflow");
				return (int)val;
			}
		}

		string ReadId() => ReadId(true);

		string ReadId(bool ignoreWhiteSpace) {
			SkipWhite();
			var sb = ObjectPools.AllocStringBuilder();
			int c;
			while ((c = GetIdChar(ignoreWhiteSpace)) != -1)
				sb.Append((char)c);
			Verify(sb.Length > 0, "Expected an id");
			return ObjectPools.FreeAndToString(ref sb);
		}

		int PeekChar() => reader.Peek();
		int ReadChar() => reader.Read();

		DmdType ParseCore() {
			try {
				var type = ReadType(true);
				SkipWhite();
				Verify(PeekChar() == -1, "Extra input after type name");
				return type;
			}
			catch (TypeNameParserException) {
				throw;
			}
			catch (Exception ex) {
				throw new TypeNameParserException("Could not parse type name", ex);
			}
		}

		DmdType ReadType(bool readAssemblyReference) {
			IncrementRecursionCounter();
			DmdType result;

			SkipWhite();
			if (PeekChar() == '!') {
				var currentSig = ReadGenericSig();
				var tspecs = ReadTSpecs();
				ReadOptionalAssemblyRef();
				result = CreateTypeSig(tspecs, currentSig);
			}
			else {
				var typeNames = ReadTypeRefAndNestedNoAssembly('+');
				var tspecs = ReadTSpecs();

				IDmdAssemblyName asmRef;
				if (typeDefResolver == null) {
					Debug.Assert(ownerModule != null);
					var typeRef = CreateTypeRef(typeNames);
					var nonNestedTypeRef = (DmdParsedTypeRef)DmdTypeUtilities.GetNonNestedType(typeRef);
					if (readAssemblyReference)
						asmRef = ReadOptionalAssemblyRef() ?? FindAssemblyRef(nonNestedTypeRef);
					else
						asmRef = FindAssemblyRef(nonNestedTypeRef);
					nonNestedTypeRef.SetTypeScope(new DmdTypeScope(asmRef ?? new DmdReadOnlyAssemblyName(string.Empty)));
					result = Resolve(asmRef, typeRef) ?? typeRef;
				}
				else {
					asmRef = readAssemblyReference ? ReadOptionalAssemblyRef() : null;
					result = typeDefResolver.GetTypeDef(asmRef, typeNames);
					if ((object)result == null)
						throw new TypeNameParserException("Couldn't find the type def");
				}

				if (tspecs.Count != 0)
					result = CreateTypeSig(tspecs, result);
			}

			DecrementRecursionCounter();
			return result;
		}

		DmdType Resolve(IDmdAssemblyName asmRef, DmdType typeRef) {
			var asm = ownerModule.Assembly;
			var asmName = asm.GetName();
			if (!DmdMemberInfoEqualityComparer.DefaultOther.Equals(asmRef, asmName))
				return null;
			var td = typeRef.ResolveNoThrow();
			return td?.Module == ownerModule ? td : null;
		}

		DmdReadOnlyAssemblyName ReadOptionalAssemblyRef() {
			SkipWhite();
			if (PeekChar() == ',') {
				ReadChar();
				ReadAssemblyRef(out var name, out var version, out var cultureName, out var flags, out var publicKey, out var publicKeyToken, out var hashAlgorithm);
				return new DmdReadOnlyAssemblyName(name, version, cultureName, flags, publicKey, publicKeyToken, hashAlgorithm);
			}
			return null;
		}

		IList<TSpec> ReadTSpecs() {
			var tspecs = new List<TSpec>();
			while (true) {
				SkipWhite();
				switch (PeekChar()) {
				case '[':	// SZArray, Array, or GenericInst
					ReadChar();
					SkipWhite();
					var peeked = PeekChar();
					if (peeked == ']') {
						// SZ array
						Verify(ReadChar() == ']', "Expected ']'");
						tspecs.Add(SZArraySpec.Instance);
					}
					else if (peeked == '*' || peeked == ',' || peeked == '-' || char.IsDigit((char)peeked)) {
						// Array

						var arraySpec = new ArraySpec();
						arraySpec.rank = 0;
						while (true) {
							SkipWhite();
							int c = PeekChar();
							if (c == '*')
								ReadChar();
							else if (c == ',' || c == ']') {
							}
							else if (c == '-' || char.IsDigit((char)c)) {
								int lower = ReadInt32();
								uint? size;
								SkipWhite();
								Verify(ReadChar() == '.', "Expected '.'");
								Verify(ReadChar() == '.', "Expected '.'");
								if (PeekChar() == '.') {
									ReadChar();
									size = null;
								}
								else {
									SkipWhite();
									if (PeekChar() == '-') {
										int upper = ReadInt32();
										Verify(upper >= lower, "upper < lower");
										size = (uint)(upper - lower + 1);
										Verify(size.Value != 0 && size.Value <= 0x1FFFFFFF, "Invalid size");
									}
									else {
										uint upper = ReadUInt32();
										long lsize = (long)upper - (long)lower + 1;
										Verify(lsize > 0 && lsize <= 0x1FFFFFFF, "Invalid size");
										size = (uint)lsize;
									}
								}
								if (arraySpec.lowerBounds.Count == arraySpec.rank)
									arraySpec.lowerBounds.Add(lower);
								if (size.HasValue && arraySpec.sizes.Count == arraySpec.rank)
									arraySpec.sizes.Add((int)size.Value);
							}
							else
								Verify(false, "Unknown char");

							arraySpec.rank++;
							SkipWhite();
							if (PeekChar() != ',')
								break;
							ReadChar();
						}

						Verify(ReadChar() == ']', "Expected ']'");
						tspecs.Add(arraySpec);
					}
					else {
						// Generic args

						var ginstSpec = new GenericInstSpec();
						while (true) {
							SkipWhite();
							peeked = PeekChar();
							bool needSeperators = peeked == '[';
							if (peeked == ']')
								break;
							Verify(!needSeperators || ReadChar() == '[', "Expected '['");
							ginstSpec.args.Add(ReadType(needSeperators));
							SkipWhite();
							Verify(!needSeperators || ReadChar() == ']', "Expected ']'");
							SkipWhite();
							if (PeekChar() != ',')
								break;
							ReadChar();
						}

						Verify(ReadChar() == ']', "Expected ']'");
						tspecs.Add(ginstSpec);
					}
					break;

				case '&':	// ByRef
					ReadChar();
					tspecs.Add(ByRefSpec.Instance);
					break;

				case '*':	// Ptr
					ReadChar();
					tspecs.Add(PtrSpec.Instance);
					break;

				default:
					return tspecs;
				}
			}
		}

		public static void ParseAssemblyName(string asmFullName, out string name, out Version version, out string cultureName, out DmdAssemblyNameFlags flags, out byte[] publicKey, out byte[] publicKeyToken, out DmdAssemblyHashAlgorithm hashAlgorithm) {
			if (asmFullName == null)
				throw new ArgumentNullException(nameof(asmFullName));
			try {
				using (var parser = new DmdTypeNameParser((DmdModule)null, asmFullName, null))
					parser.ReadAssemblyRef(out name, out version, out cultureName, out flags, out publicKey, out publicKeyToken, out hashAlgorithm);
				return;
			}
			catch {
			}
			name = null;
			version = null;
			cultureName = null;
			flags = 0;
			publicKey = null;
			publicKeyToken = null;
			hashAlgorithm = 0;
		}

		void ReadAssemblyRef(out string name, out Version version, out string cultureName, out DmdAssemblyNameFlags flags, out byte[] publicKey, out byte[] publicKeyToken, out DmdAssemblyHashAlgorithm hashAlgorithm) {
			name = ReadAssemblyNameId();
			version = null;
			cultureName = null;
			flags = 0;
			publicKey = null;
			publicKeyToken = null;
			hashAlgorithm = DmdAssemblyHashAlgorithm.None;
			SkipWhite();
			if (PeekChar() != ',')
				return;
			ReadChar();

			while (true) {
				SkipWhite();
				int c = PeekChar();
				if (c == -1 || c == ']')
					break;
				if (c == ',') {
					ReadChar();
					continue;
				}

				string key = ReadId();
				SkipWhite();
				if (PeekChar() != '=')
					continue;
				ReadChar();
				string value = ReadId();

				switch (key.ToUpperInvariant()) {
				case "VERSION":
					if (!Version.TryParse(value, out version))
						version = null;
					break;

				case "CONTENTTYPE":
					if (StringComparer.OrdinalIgnoreCase.Equals(value, "WindowsRuntime"))
						flags = (flags & ~DmdAssemblyNameFlags.ContentType_Mask) | DmdAssemblyNameFlags.ContentType_WindowsRuntime;
					else
						flags = (flags & ~DmdAssemblyNameFlags.ContentType_Mask) | DmdAssemblyNameFlags.ContentType_Default;
					break;

				case "RETARGETABLE":
					if (StringComparer.OrdinalIgnoreCase.Equals(value, "Yes"))
						flags |= DmdAssemblyNameFlags.Retargetable;
					else
						flags &= ~DmdAssemblyNameFlags.Retargetable;
					break;

				case "PUBLICKEY":
					flags |= DmdAssemblyNameFlags.PublicKey;
					if (StringComparer.OrdinalIgnoreCase.Equals(value, "null") ||
						StringComparer.OrdinalIgnoreCase.Equals(value, "neutral"))
						publicKey = Array.Empty<byte>();
					else
						publicKey = HexUtils.ParseBytes(value);
					break;

				case "PUBLICKEYTOKEN":
					if (StringComparer.OrdinalIgnoreCase.Equals(value, "null") ||
						StringComparer.OrdinalIgnoreCase.Equals(value, "neutral"))
						publicKeyToken = Array.Empty<byte>();
					else
						publicKeyToken = HexUtils.ParseBytes(value);
					break;

				case "CULTURE":
				case "LANGUAGE":
					if (StringComparer.OrdinalIgnoreCase.Equals(value, "neutral"))
						cultureName = string.Empty;
					else
						cultureName = value;
					break;
				}
			}
		}

		string ReadAssemblyNameId() {
			SkipWhite();
			var sb = ObjectPools.AllocStringBuilder();
			int c;
			while ((c = GetAsmNameChar()) != -1)
				sb.Append((char)c);
			var name = ObjectPools.FreeAndToString(ref sb).Trim();
			Verify(name.Length > 0, "Expected an assembly name");
			return name;
		}

		int GetAsmNameChar() {
			int c = PeekChar();
			if (c == -1)
				return -1;
			switch (c) {
			case '\\':
				ReadChar();
				return ReadChar();

			case ']':
			case ',':
				return -1;

			default:
				return ReadChar();
			}
		}

		int GetIdChar(bool ignoreWhiteSpace) {
			int c = PeekChar();
			if (c == -1)
				return -1;
			if (ignoreWhiteSpace && char.IsWhiteSpace((char)c))
				return -1;
			switch (c) {
			case '\\':
				ReadChar();
				return ReadChar();

			case ',':
			case '+':
			case '&':
			case '*':
			case '[':
			case ']':
			case '=':
				return -1;

			default:
				return ReadChar();
			}
		}
	}
}
