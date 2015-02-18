using System;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace Debugger.MetaData {
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
			public static readonly SignatureReaderHelper Instance = new SignatureReaderHelper();

			public ITypeDefOrRef ResolveTypeDefOrRef(uint codedToken, GenericParamContext gpContext) {
				uint token;
				if (!CodedToken.TypeDefOrRef.Decode(codedToken, out token))
					return null;
				uint rid = MDToken.ToRID(token);
				switch (MDToken.ToTable(token)) {
				case Table.TypeDef:		return new TypeDefUser(UTF8String.Empty) { Rid = rid };
				case Table.TypeRef:		return new TypeRefUser(null, UTF8String.Empty) { Rid = rid };
				case Table.TypeSpec:	return new TypeSpecUser() { Rid = rid };
				}
				return null;
			}

			public TypeSig ConvertRTInternalAddress(IntPtr address) {
				return null;
			}
		}

		public TypeSig ReadTypeSignature(byte[] data) {
			return SignatureReader.ReadTypeSig(SignatureReaderHelper.Instance, corLibTypes, data);
		}

		public CallingConventionSig ReadSignature(byte[] data) {
			return SignatureReader.ReadSig(SignatureReaderHelper.Instance, corLibTypes, data);
		}
	}
}
