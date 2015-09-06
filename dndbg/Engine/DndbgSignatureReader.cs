/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Diagnostics;
using dndbg.Engine.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dndbg.Engine {
	struct DebugSignatureReader {
		static readonly CorLibTypes corLibTypes = new CorLibTypes();

		sealed class CorLibTypes : ICorLibTypes {
			static readonly ITypeDefOrRef dummyRef = new TypeDefUser(UTF8String.Empty);
			static readonly CorLibTypeSig sigVoid = new CorLibTypeSig(dummyRef, ElementType.Void);
			static readonly CorLibTypeSig sigBoolean = new CorLibTypeSig(dummyRef, ElementType.Boolean);
			static readonly CorLibTypeSig sigChar = new CorLibTypeSig(dummyRef, ElementType.Char);
			static readonly CorLibTypeSig sigI1 = new CorLibTypeSig(dummyRef, ElementType.I1);
			static readonly CorLibTypeSig sigU1 = new CorLibTypeSig(dummyRef, ElementType.U1);
			static readonly CorLibTypeSig sigI2 = new CorLibTypeSig(dummyRef, ElementType.I2);
			static readonly CorLibTypeSig sigU2 = new CorLibTypeSig(dummyRef, ElementType.U2);
			static readonly CorLibTypeSig sigI4 = new CorLibTypeSig(dummyRef, ElementType.I4);
			static readonly CorLibTypeSig sigU4 = new CorLibTypeSig(dummyRef, ElementType.U4);
			static readonly CorLibTypeSig sigI8 = new CorLibTypeSig(dummyRef, ElementType.I8);
			static readonly CorLibTypeSig sigU8 = new CorLibTypeSig(dummyRef, ElementType.U8);
			static readonly CorLibTypeSig sigR4 = new CorLibTypeSig(dummyRef, ElementType.R4);
			static readonly CorLibTypeSig sigR8 = new CorLibTypeSig(dummyRef, ElementType.R8);
			static readonly CorLibTypeSig sigString = new CorLibTypeSig(dummyRef, ElementType.String);
			static readonly CorLibTypeSig sigTypedByRef = new CorLibTypeSig(dummyRef, ElementType.TypedByRef);
			static readonly CorLibTypeSig sigI = new CorLibTypeSig(dummyRef, ElementType.I);
			static readonly CorLibTypeSig sigU = new CorLibTypeSig(dummyRef, ElementType.U);
			static readonly CorLibTypeSig sigObject = new CorLibTypeSig(dummyRef, ElementType.Object);

			public CorLibTypeSig Void {
				get { return sigVoid; }
			}

			public CorLibTypeSig Boolean {
				get { return sigBoolean; }
			}

			public CorLibTypeSig Char {
				get { return sigChar; }
			}

			public CorLibTypeSig SByte {
				get { return sigI1; }
			}

			public CorLibTypeSig Byte {
				get { return sigU1; }
			}

			public CorLibTypeSig Int16 {
				get { return sigI2; }
			}

			public CorLibTypeSig UInt16 {
				get { return sigU2; }
			}

			public CorLibTypeSig Int32 {
				get { return sigI4; }
			}

			public CorLibTypeSig UInt32 {
				get { return sigU4; }
			}

			public CorLibTypeSig Int64 {
				get { return sigI8; }
			}

			public CorLibTypeSig UInt64 {
				get { return sigU8; }
			}

			public CorLibTypeSig Single {
				get { return sigR4; }
			}

			public CorLibTypeSig Double {
				get { return sigR8; }
			}

			public CorLibTypeSig String {
				get { return sigString; }
			}

			public CorLibTypeSig TypedReference {
				get { return sigTypedByRef; }
			}

			public CorLibTypeSig IntPtr {
				get { return sigI; }
			}

			public CorLibTypeSig UIntPtr {
				get { return sigU; }
			}

			public CorLibTypeSig Object {
				get { return sigObject; }
			}

			public AssemblyRef AssemblyRef {
				get { throw new NotImplementedException(); }
			}

			public TypeRef GetTypeRef(string @namespace, string name) {
				throw new NotImplementedException();
			}
		}

		sealed class SignatureReaderHelper : ISignatureReaderHelper {
			readonly IMetaDataImport mdi;

			public SignatureReaderHelper(IMetaDataImport mdi) {
				Debug.Assert(mdi != null);
				if (mdi == null)
					throw new ArgumentNullException();
				this.mdi = mdi;
			}

			public ITypeDefOrRef ResolveTypeDefOrRef(uint codedToken, GenericParamContext gpContext) {
				uint token;
				if (!CodedToken.TypeDefOrRef.Decode(codedToken, out token))
					return null;
				uint rid = MDToken.ToRID(token);
				switch (MDToken.ToTable(token)) {
				case Table.TypeDef:		return new TypeDefDndbg(mdi) { Rid = rid };
				case Table.TypeRef:		return new TypeRefDndbg(mdi) { Rid = rid };
				case Table.TypeSpec:	return new TypeSpecDndbg(mdi) { Rid = rid };
				}
				return null;
			}

			public TypeSig ConvertRTInternalAddress(IntPtr address) {
				return null;
			}
		}

		public TypeSig ReadTypeSignature(IMetaDataImport mdi, byte[] data) {
			return SignatureReader.ReadTypeSig(new SignatureReaderHelper(mdi), corLibTypes, data);
		}

		public CallingConventionSig ReadSignature(IMetaDataImport mdi, byte[] data) {
			return SignatureReader.ReadSig(new SignatureReaderHelper(mdi), corLibTypes, data);
		}
	}

	interface IMetaDataImportProvider : IMDTokenProvider {
		IMetaDataImport MetaDataImport { get; }
	}

	sealed class TypeDefDndbg : TypeDefUser, IMetaDataImportProvider {
		public IMetaDataImport MetaDataImport {
			get { return mdi; }
		}
		readonly IMetaDataImport mdi;

		public TypeDefDndbg(IMetaDataImport mdi)
			: base(UTF8String.Empty) {
			this.mdi = mdi;
		}
	}

	sealed class TypeRefDndbg : TypeRefUser, IMetaDataImportProvider {
		public IMetaDataImport MetaDataImport {
			get { return mdi; }
		}
		readonly IMetaDataImport mdi;

		public TypeRefDndbg(IMetaDataImport mdi)
			: base(null, UTF8String.Empty) {
			this.mdi = mdi;
		}
	}

	sealed class TypeSpecDndbg : TypeSpecUser, IMetaDataImportProvider {
		public IMetaDataImport MetaDataImport {
			get { return mdi; }
		}
		readonly IMetaDataImport mdi;

		public TypeSpecDndbg(IMetaDataImport mdi)
			: base() {
			this.mdi = mdi;
		}
	}
}
