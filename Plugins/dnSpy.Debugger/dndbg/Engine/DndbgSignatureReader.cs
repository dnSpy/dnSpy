/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dndbg.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dndbg.Engine {
	struct DebugSignatureReader {
		static DebugSignatureReader() {
			noOwnerModule = new ModuleDefUser();
			corLibTypes = new CorLibTypes(noOwnerModule);
		}
		static readonly ModuleDef noOwnerModule;
		static readonly CorLibTypes corLibTypes;

		internal static CorLibTypes CorLibTypes {
			get { return corLibTypes; }
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
				case Table.TypeDef:		return new TypeDefDndbg(mdi, rid);
				case Table.TypeRef:		return new TypeRefDndbg(mdi, rid);
				case Table.TypeSpec:	return new TypeSpecDndbg(mdi, rid, this);
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

		public static TypeDef CreateTypeDef(IMetaDataImport mdi, uint rid) {
			return new TypeDefDndbg(mdi, rid);
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

		public TypeDefDndbg(IMetaDataImport mdi, uint rid)
			: base(UTF8String.Empty) {
			this.mdi = mdi;
			this.rid = rid;
			InitializeName(MDAPI.GetTypeDefName(mdi, MDToken.Raw), out @namespace, out name);
		}

		internal static void InitializeName(string fullname, out UTF8String @namespace, out UTF8String name) {
			string s = fullname ?? string.Empty;
			int index = s.LastIndexOf('.');
			if (index < 0) {
				@namespace = UTF8String.Empty;
				name = s;
			}
			else {
				@namespace = s.Substring(0, index);
				name = s.Substring(index + 1);
			}
		}
	}

	sealed class TypeRefDndbg : TypeRefUser, IMetaDataImportProvider {
		public IMetaDataImport MetaDataImport {
			get { return mdi; }
		}
		readonly IMetaDataImport mdi;

		public TypeRefDndbg(IMetaDataImport mdi, uint rid)
			: base(null, UTF8String.Empty) {
			this.mdi = mdi;
			this.rid = rid;
			TypeDefDndbg.InitializeName(MDAPI.GetTypeRefName(mdi, MDToken.Raw), out @namespace, out name);
		}
	}

	sealed class TypeSpecDndbg : TypeSpecUser, IMetaDataImportProvider {
		public IMetaDataImport MetaDataImport {
			get { return mdi; }
		}
		readonly IMetaDataImport mdi;

		readonly ISignatureReaderHelper helper;

		public TypeSpecDndbg(IMetaDataImport mdi, uint rid, ISignatureReaderHelper helper)
			: base() {
			this.mdi = mdi;
			this.rid = rid;
			this.helper = helper;
		}

		protected override TypeSig GetTypeSigAndExtraData_NoLock(out byte[] extraData) {
			var sigData = MDAPI.GetTypeSpecSignatureBlob(mdi, MDToken.Raw);
			var sig = ReadTypeSignature(sigData, new GenericParamContext(), out extraData);
			if (sig != null)
				sig.Rid = rid;
			return sig;
		}

		TypeSig ReadTypeSignature(byte[] data, GenericParamContext gpContext, out byte[] extraData) {
			if (data == null) {
				extraData = null;
				return null;
			}

			return SignatureReader.ReadTypeSig(helper, new CorLibTypes(noOwnerModule), data, gpContext, out extraData);
		}
		static readonly ModuleDef noOwnerModule = new ModuleDefUser();
	}
}
