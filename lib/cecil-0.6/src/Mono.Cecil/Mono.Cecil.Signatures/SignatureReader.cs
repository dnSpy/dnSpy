//
// SignatureReader.cs
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

namespace Mono.Cecil.Signatures {

	using System;
	using System.Collections;
	using System.IO;
	using System.Text;

	using Mono.Cecil;
	using Mono.Cecil.Metadata;

	internal sealed class SignatureReader : BaseSignatureVisitor {

		MetadataRoot m_root;
		ReflectionReader m_reflectReader;
		byte [] m_blobData;

		IDictionary m_signatures;

		IAssemblyResolver AssemblyResolver {
			get { return m_reflectReader.Module.Assembly.Resolver; }
		}

		public SignatureReader (MetadataRoot root, ReflectionReader reflectReader)
		{
			m_root = root;
			m_reflectReader = reflectReader;

			m_blobData = m_root.Streams.BlobHeap != null ? m_root.Streams.BlobHeap.Data : new byte [0];

			m_signatures = new Hashtable ();
		}

		public FieldSig GetFieldSig (uint index)
		{
			FieldSig f = m_signatures [index] as FieldSig;
			if (f == null) {
				f = new FieldSig (index);
				f.Accept (this);
				m_signatures [index] = f;
			}
			return f;
		}

		public PropertySig GetPropSig (uint index)
		{
			PropertySig p = m_signatures [index] as PropertySig;
			if (p == null) {
				p = new PropertySig (index);
				p.Accept (this);
				m_signatures [index] = p;
			}
			return p;
		}

		public MethodDefSig GetMethodDefSig (uint index)
		{
			MethodDefSig m = m_signatures [index] as MethodDefSig;
			if (m == null) {
				m = new MethodDefSig (index);
				m.Accept (this);
				m_signatures [index] = m;
			}
			return m;
		}

		public MethodRefSig GetMethodRefSig (uint index)
		{
			MethodRefSig m = m_signatures [index] as MethodRefSig;
			if (m == null) {
				m = new MethodRefSig (index);
				m.Accept (this);
				m_signatures [index] = m;
			}
			return m;
		}

		public TypeSpec GetTypeSpec (uint index)
		{
			TypeSpec ts = m_signatures [index] as TypeSpec;

			if (ts == null) {
				ts = ReadTypeSpec (m_blobData, (int) index);
				m_signatures [index] = ts;
			}

			return ts;
		}

		public MethodSpec GetMethodSpec (uint index)
		{
			MethodSpec ms = m_signatures [index] as MethodSpec;

			if (ms == null) {
				ms = ReadMethodSpec (m_blobData, (int) index);
				m_signatures [index] = ms;
			}

			return ms;
		}

		public LocalVarSig GetLocalVarSig (uint index)
		{
			LocalVarSig lv = m_signatures [index] as LocalVarSig;
			if (lv == null) {
				lv = new LocalVarSig (index);
				lv.Accept (this);
				m_signatures [index] = lv;
			}
			return lv;
		}

		public CustomAttrib GetCustomAttrib (uint index, MethodReference ctor)
		{
			return GetCustomAttrib (index, ctor, false);
		}

		public CustomAttrib GetCustomAttrib (uint index, MethodReference ctor, bool resolve)
		{
			return ReadCustomAttrib ((int) index, ctor, resolve);
		}

		public CustomAttrib GetCustomAttrib (byte [] data, MethodReference ctor)
		{
			return GetCustomAttrib (data, ctor, false);
		}

		public CustomAttrib GetCustomAttrib (byte [] data, MethodReference ctor, bool resolve)
		{
			BinaryReader br = new BinaryReader (new MemoryStream (data));
			return ReadCustomAttrib (br, data, ctor, resolve);
		}

		public Signature GetMemberRefSig (TokenType tt, uint index)
		{
			int start, callconv;
			Utilities.ReadCompressedInteger (m_blobData, (int) index, out start);
			callconv = m_blobData [start];
			if ((callconv & 0x5) == 0x5 || (callconv & 0x10) == 0x10) // vararg || generic?
				return GetMethodDefSig (index);
			if ((callconv & 0x6) != 0) // field ?
				return GetFieldSig (index);

			switch (tt) {
			case TokenType.TypeDef :
			case TokenType.TypeRef :
			case TokenType.TypeSpec :
				return GetMethodRefSig (index);
			case TokenType.ModuleRef :
			case TokenType.Method :
				return GetMethodDefSig (index);
			}
			return null;
		}

		public MarshalSig GetMarshalSig (uint index)
		{
			MarshalSig ms = m_signatures [index] as MarshalSig;
			if (ms == null) {
				byte [] data = m_root.Streams.BlobHeap.Read (index);
				ms = ReadMarshalSig (data);
				m_signatures [index] = ms;
			}
			return ms;
		}

		public MethodSig GetStandAloneMethodSig (uint index)
		{
			byte [] data = m_root.Streams.BlobHeap.Read (index);
			int start;
			if ((data [0] & 0x5) > 0) {
				MethodRefSig mrs = new MethodRefSig (index);
				ReadMethodRefSig (mrs, data, 0, out start);
				return mrs;
			} else {
				MethodDefSig mds = new MethodDefSig (index);
				ReadMethodDefSig (mds, data, 0, out start);
				return mds;
			}
		}

		public override void VisitMethodDefSig (MethodDefSig methodDef)
		{
			int start;
			ReadMethodDefSig (methodDef, m_root.Streams.BlobHeap.Read (methodDef.BlobIndex), 0, out start);
		}

		public override void VisitMethodRefSig (MethodRefSig methodRef)
		{
			int start;
			ReadMethodRefSig (methodRef, m_root.Streams.BlobHeap.Read (methodRef.BlobIndex), 0, out start);
		}

		public override void VisitFieldSig (FieldSig field)
		{
			int start;
			Utilities.ReadCompressedInteger (m_blobData, (int) field.BlobIndex, out start);
			field.CallingConvention = m_blobData [start];
			field.Field = (field.CallingConvention & 0x6) != 0;
			field.CustomMods = ReadCustomMods (m_blobData, start + 1, out start);
			field.Type = ReadType (m_blobData, start, out start);
		}

		public override void VisitPropertySig (PropertySig property)
		{
			int start;
			Utilities.ReadCompressedInteger (m_blobData, (int) property.BlobIndex, out start);
			property.CallingConvention = m_blobData [start];
			property.Property = (property.CallingConvention & 0x8) != 0;
			property.ParamCount = Utilities.ReadCompressedInteger (m_blobData, start + 1, out start);
			property.CustomMods = ReadCustomMods (m_blobData, start, out start);
			property.Type = ReadType (m_blobData, start, out start);
			property.Parameters = ReadParameters (property.ParamCount, m_blobData, start);
		}

		public override void VisitLocalVarSig (LocalVarSig localvar)
		{
			int start;
			Utilities.ReadCompressedInteger (m_blobData, (int) localvar.BlobIndex, out start);
			localvar.CallingConvention = m_blobData [start];
			localvar.Local = (localvar.CallingConvention & 0x7) != 0;
			localvar.Count = Utilities.ReadCompressedInteger (m_blobData, start + 1, out start);
			localvar.LocalVariables = ReadLocalVariables (localvar.Count, m_blobData, start);
		}

		void ReadMethodDefSig (MethodDefSig methodDef, byte [] data, int pos, out int start)
		{
			methodDef.CallingConvention = data [pos];
			start = pos + 1;
			methodDef.HasThis = (methodDef.CallingConvention & 0x20) != 0;
			methodDef.ExplicitThis = (methodDef.CallingConvention & 0x40) != 0;
			if ((methodDef.CallingConvention & 0x5) != 0)
				methodDef.MethCallConv |= MethodCallingConvention.VarArg;
			else if ((methodDef.CallingConvention & 0x10) != 0) {
				methodDef.MethCallConv |= MethodCallingConvention.Generic;
				methodDef.GenericParameterCount = Utilities.ReadCompressedInteger (data, start, out start);
			} else
				methodDef.MethCallConv |= MethodCallingConvention.Default;

			methodDef.ParamCount = Utilities.ReadCompressedInteger (data, start, out start);
			methodDef.RetType = ReadRetType (data, start, out start);
			int sentpos;
			methodDef.Parameters = ReadParameters (methodDef.ParamCount, data, start, out sentpos);
			methodDef.Sentinel = sentpos;
		}

		void ReadMethodRefSig (MethodRefSig methodRef, byte [] data, int pos, out int start)
		{
			methodRef.CallingConvention = data [pos];
			start = pos + 1;
			methodRef.HasThis = (methodRef.CallingConvention & 0x20) != 0;
			methodRef.ExplicitThis = (methodRef.CallingConvention & 0x40) != 0;
			if ((methodRef.CallingConvention & 0x1) != 0)
				methodRef.MethCallConv |= MethodCallingConvention.C;
			else if ((methodRef.CallingConvention & 0x2) != 0)
				methodRef.MethCallConv |= MethodCallingConvention.StdCall;
			else if ((methodRef.CallingConvention & 0x3) != 0)
				methodRef.MethCallConv |= MethodCallingConvention.ThisCall;
			else if ((methodRef.CallingConvention & 0x4) != 0)
				methodRef.MethCallConv |= MethodCallingConvention.FastCall;
			else if ((methodRef.CallingConvention & 0x5) != 0)
				methodRef.MethCallConv |= MethodCallingConvention.VarArg;
			else
				methodRef.MethCallConv |= MethodCallingConvention.Default;
			methodRef.ParamCount = Utilities.ReadCompressedInteger (data, start, out start);
			methodRef.RetType = ReadRetType (data, start, out start);
			int sentpos;
			methodRef.Parameters = ReadParameters (methodRef.ParamCount, data, start, out sentpos);
			methodRef.Sentinel = sentpos;
		}

		LocalVarSig.LocalVariable [] ReadLocalVariables (int length, byte [] data, int pos)
		{
			int start = pos;
			LocalVarSig.LocalVariable [] types = new LocalVarSig.LocalVariable [length];
			for (int i = 0; i < length; i++)
				types [i] = ReadLocalVariable (data, start, out start);
			return types;
		}

		LocalVarSig.LocalVariable ReadLocalVariable (byte [] data, int pos, out int start)
		{
			start = pos;
			LocalVarSig.LocalVariable lv = new LocalVarSig.LocalVariable ();
			lv.ByRef = false;
			int cursor;
			while (true) {
				lv.CustomMods = ReadCustomMods (data, start, out start);
				cursor = start;
				int current = Utilities.ReadCompressedInteger (data, start, out start);
				if (current == (int) ElementType.Pinned) // the only possible constraint
					lv.Constraint |= Constraint.Pinned;
				else if (current == (int) ElementType.ByRef) {
					lv.ByRef = true;

					if (lv.CustomMods == null || lv.CustomMods.Length == 0)
						lv.CustomMods = ReadCustomMods (data, start, out start);
				} else {
					lv.Type = ReadType (data, cursor, out start);
					break;
				}
			}
			return lv;
		}

		TypeSpec ReadTypeSpec (byte [] data, int pos)
		{
			int start = pos;
			Utilities.ReadCompressedInteger (data, start, out start);
			TypeSpec ts = new TypeSpec ();
			ts.CustomMods = ReadCustomMods (data, start, out start);
			ts.Type = ReadType (data, start, out start);
			return ts;
		}

		MethodSpec ReadMethodSpec (byte [] data, int pos)
		{
			int start = pos;

			Utilities.ReadCompressedInteger (data, start, out start);
			if (Utilities.ReadCompressedInteger (data, start, out start) != 0x0a)
				throw new ReflectionException ("Invalid MethodSpec signature");

			return new MethodSpec (ReadGenericInstSignature (data, start, out start));
		}

		RetType ReadRetType (byte [] data, int pos, out int start)
		{
			RetType rt = new RetType ();
			start = pos;
			rt.CustomMods = ReadCustomMods (data, start, out start);
			int curs = start;
			ElementType flag = (ElementType) Utilities.ReadCompressedInteger (data, start, out start);
			switch (flag) {
			case ElementType.Void :
				rt.ByRef = rt.TypedByRef = false;
				rt.Void = true;
				break;
			case ElementType.TypedByRef :
				rt.ByRef = rt.Void = false;
				rt.TypedByRef = true;
				break;
			case ElementType.ByRef :
				rt.TypedByRef = rt.Void = false;
				rt.ByRef = true;

				if (rt.CustomMods == null || rt.CustomMods.Length == 0)
					rt.CustomMods = ReadCustomMods (data, start, out start);

				rt.Type = ReadType (data, start, out start);
				break;
			default :
				rt.TypedByRef = rt.Void = rt.ByRef = false;
				rt.Type = ReadType (data, curs, out start);
				break;
			}
			return rt;
		}

		Param [] ReadParameters (int length, byte [] data, int pos)
		{
			Param [] ret = new Param [length];
			int start = pos;
			for (int i = 0; i < length; i++)
				ret [i] = ReadParameter (data, start, out start);
			return ret;
		}

		Param [] ReadParameters (int length, byte [] data, int pos, out int sentinelpos)
		{
			Param [] ret = new Param [length];
			int start = pos;
			sentinelpos = -1;

			for (int i = 0; i < length; i++) {
				int curs = start;
				int flag = Utilities.ReadCompressedInteger (data, start, out start);

				if (flag == (int) ElementType.Sentinel) {
					sentinelpos = i;
					curs = start;
				}

				ret [i] = ReadParameter (data, curs, out start);
			}

			return ret;
		}

		Param ReadParameter (byte [] data, int pos, out int start)
		{
			Param p = new Param ();
			start = pos;

			p.CustomMods = ReadCustomMods (data, start, out start);
			int curs = start;
			ElementType flag = (ElementType) Utilities.ReadCompressedInteger (data, start, out start);
			switch (flag) {
			case ElementType.TypedByRef :
				p.TypedByRef = true;
				p.ByRef = false;
				break;
			case ElementType.ByRef :
				p.TypedByRef = false;
				p.ByRef = true;

				if (p.CustomMods == null || p.CustomMods.Length == 0)
					p.CustomMods = ReadCustomMods (data, start, out start);

				p.Type = ReadType (data, start, out start);
				break;
			default :
				p.TypedByRef = false;
				p.ByRef = false;
				p.Type = ReadType (data, curs, out start);
				break;
			}
			return p;
		}

		SigType ReadType (byte [] data, int pos, out int start)
		{
			start = pos;
			ElementType element = (ElementType) Utilities.ReadCompressedInteger (data, start, out start);
			switch (element) {
			case ElementType.ValueType :
				VALUETYPE vt = new VALUETYPE ();
				vt.Type = Utilities.GetMetadataToken(CodedIndex.TypeDefOrRef,
					(uint) Utilities.ReadCompressedInteger (data, start, out start));
				return vt;
			case ElementType.Class :
				CLASS c = new CLASS ();
				c.Type = Utilities.GetMetadataToken (CodedIndex.TypeDefOrRef,
					(uint) Utilities.ReadCompressedInteger (data, start, out start));
				return c;
			case ElementType.Ptr :
				PTR p = new PTR ();
				int buf = start;
				int flag = Utilities.ReadCompressedInteger (data, start, out start);
				p.Void = flag == (int) ElementType.Void;
				if (p.Void)
					return p;
				start = buf;
				p.CustomMods = ReadCustomMods (data, start, out start);
				p.PtrType = ReadType (data, start, out start);
				return p;
			case ElementType.FnPtr :
				FNPTR fp = new FNPTR ();
				if ((data [start] & 0x5) != 0) {
					MethodRefSig mr = new MethodRefSig ((uint) start);
					ReadMethodRefSig (mr, data, start, out start);
					fp.Method = mr;
				} else {
					MethodDefSig md = new MethodDefSig ((uint) start);
					ReadMethodDefSig (md, data, start, out start);
					fp.Method = md;
				}
				return fp;
			case ElementType.Array :
				ARRAY ary = new ARRAY ();
				ary.CustomMods = ReadCustomMods (data, start, out start);
				ArrayShape shape = new ArrayShape ();
				ary.Type = ReadType (data, start, out start);
				shape.Rank = Utilities.ReadCompressedInteger (data, start, out start);
				shape.NumSizes = Utilities.ReadCompressedInteger (data, start, out start);
				shape.Sizes = new int [shape.NumSizes];
				for (int i = 0; i < shape.NumSizes; i++)
					shape.Sizes [i] = Utilities.ReadCompressedInteger (data, start, out start);
				shape.NumLoBounds = Utilities.ReadCompressedInteger (data, start, out start);
				shape.LoBounds = new int [shape.NumLoBounds];
				for (int i = 0; i < shape.NumLoBounds; i++)
					shape.LoBounds [i] = Utilities.ReadCompressedInteger (data, start, out start);
				ary.Shape = shape;
				return ary;
			case ElementType.SzArray :
				SZARRAY sa = new SZARRAY ();
				sa.CustomMods = ReadCustomMods (data, start, out start);
				sa.Type = ReadType (data, start, out start);
				return sa;
			case ElementType.Var:
				return new VAR (Utilities.ReadCompressedInteger (data, start, out start));
			case ElementType.MVar:
				return new MVAR (Utilities.ReadCompressedInteger (data, start, out start));
			case ElementType.GenericInst:
				GENERICINST ginst = new GENERICINST ();

				ginst.ValueType = ((ElementType) Utilities.ReadCompressedInteger (
					data, start, out start)) == ElementType.ValueType;

				ginst.Type = Utilities.GetMetadataToken (CodedIndex.TypeDefOrRef,
					(uint) Utilities.ReadCompressedInteger (data, start, out start));

				ginst.Signature = ReadGenericInstSignature (data, start, out start);

				return ginst;
			default :
				return new SigType (element);
			}
		}

		GenericInstSignature ReadGenericInstSignature (byte [] data, int pos, out int start)
		{
			start = pos;
			GenericInstSignature gis = new GenericInstSignature ();
			gis.Arity = Utilities.ReadCompressedInteger (data, start, out start);
			gis.Types = new GenericArg [gis.Arity];
			for (int i = 0; i < gis.Arity; i++)
				gis.Types [i] = ReadGenericArg (data, start, out start);

			return gis;
		}

		GenericArg ReadGenericArg (byte[] data, int pos, out int start)
		{
			start = pos;
			CustomMod [] mods = ReadCustomMods (data, start, out start);
			GenericArg arg = new GenericArg (ReadType (data, start, out start));
			arg.CustomMods = mods;
			return arg;
		}

		CustomMod [] ReadCustomMods (byte [] data, int pos, out int start)
		{
			ArrayList cmods = new ArrayList ();
			start = pos;
			while (true) {
				int buf = start;
				ElementType flag = (ElementType) Utilities.ReadCompressedInteger (data, start, out start);
				start = buf;
				if (!((flag == ElementType.CModOpt) || (flag == ElementType.CModReqD)))
					break;
				cmods.Add (ReadCustomMod (data, start, out start));
			}
			return cmods.ToArray (typeof (CustomMod)) as CustomMod [];
		}

		CustomMod ReadCustomMod (byte [] data, int pos, out int start)
		{
			CustomMod cm = new CustomMod ();
			start = pos;
			ElementType cmod = (ElementType) Utilities.ReadCompressedInteger (data, start, out start);
			if (cmod == ElementType.CModOpt)
				cm.CMOD = CustomMod.CMODType.OPT;
			else if (cmod == ElementType.CModReqD)
				cm.CMOD = CustomMod.CMODType.REQD;
			else
				cm.CMOD = CustomMod.CMODType.None;
			cm.TypeDefOrRef = Utilities.GetMetadataToken (CodedIndex.TypeDefOrRef,
				(uint) Utilities.ReadCompressedInteger (data, start, out start));
			return cm;
		}

		CustomAttrib ReadCustomAttrib (int pos, MethodReference ctor, bool resolve)
		{
			int start, length = Utilities.ReadCompressedInteger (m_blobData, pos, out start);
			byte [] data = new byte [length];
			Buffer.BlockCopy (m_blobData, start, data, 0, length);
			try {
				return ReadCustomAttrib (new BinaryReader (
					new MemoryStream (data)), data, ctor, resolve);
			} catch {
				CustomAttrib ca = new CustomAttrib (ctor);
				ca.Read = false;
				return ca;
			}
		}

		CustomAttrib ReadCustomAttrib (BinaryReader br, byte [] data, MethodReference ctor, bool resolve)
		{
			CustomAttrib ca = new CustomAttrib (ctor);
			if (data.Length == 0) {
				ca.FixedArgs = new CustomAttrib.FixedArg [0];
				ca.NamedArgs = new CustomAttrib.NamedArg [0];
				return ca;
			}

			bool read = true;

			ca.Prolog = br.ReadUInt16 ();
			if (ca.Prolog != CustomAttrib.StdProlog)
				throw new MetadataFormatException ("Non standard prolog for custom attribute");

			ca.FixedArgs = new CustomAttrib.FixedArg [ctor.Parameters.Count];
			for (int i = 0; i < ca.FixedArgs.Length && read; i++)
				ca.FixedArgs [i] = ReadFixedArg (data, br,
					ctor.Parameters [i].ParameterType, ref read, resolve);

			if (br.BaseStream.Position == br.BaseStream.Length)
				read = false;

			if (!read) {
				ca.Read = read;
				return ca;
			}

			ca.NumNamed = br.ReadUInt16 ();
			ca.NamedArgs = new CustomAttrib.NamedArg [ca.NumNamed];
			for (int i = 0; i < ca.NumNamed && read; i++)
				ca.NamedArgs [i] = ReadNamedArg (data, br, ref read, resolve);

			ca.Read = read;
			return ca;
		}

		CustomAttrib.FixedArg ReadFixedArg (byte [] data, BinaryReader br,
			TypeReference param, ref bool read, bool resolve)
		{
			CustomAttrib.FixedArg fa = new CustomAttrib.FixedArg ();
			if (param is ArrayType) {
				param = ((ArrayType) param).ElementType;
				fa.SzArray = true;
				fa.NumElem = br.ReadUInt32 ();

				if (fa.NumElem == 0 || fa.NumElem == 0xffffffff) {
					fa.Elems = new CustomAttrib.Elem [0];
					fa.NumElem = 0;
					return fa;
				}

				fa.Elems = new CustomAttrib.Elem [fa.NumElem];
				for (int i = 0; i < fa.NumElem; i++)
					fa.Elems [i] = ReadElem (data, br, param, ref read, resolve);
			} else
				fa.Elems = new CustomAttrib.Elem [] { ReadElem (data, br, param, ref read, resolve) };

			return fa;
		}

		TypeReference CreateEnumTypeReference (string enumName)
		{
			string asmName = null;
			int asmStart = enumName.IndexOf (',');
			if (asmStart != -1) {
				asmName = enumName.Substring (asmStart + 1);
				enumName = enumName.Substring (0, asmStart);
			}
			// Inner class style is reflection style.
			enumName = enumName.Replace ('+', '/');
			AssemblyNameReference asm;
			if (asmName == null) {
				// If no assembly is given then the ECMA standard says the
				// assembly is either the current one or mscorlib.
				if (m_reflectReader.Module.Types.Contains (enumName))
					return m_reflectReader.Module.Types [enumName];

				asm = m_reflectReader.Corlib;
			} else
				asm = AssemblyNameReference.Parse (asmName);

			string [] outers = enumName.Split ('/');
			string outerfullname = outers [0];
			string ns = null;
			int nsIndex = outerfullname.LastIndexOf ('.');
			if (nsIndex != -1)
				ns = outerfullname.Substring (0, nsIndex);
			string name = outerfullname.Substring (nsIndex + 1);
			TypeReference decType = new TypeReference (name, ns, asm);
			for (int i = 1; i < outers.Length; i++) {
				TypeReference t = new TypeReference (outers [i], null, asm);
				t.DeclaringType = decType;
				decType = t;
			}
			decType.IsValueType = true;

			return decType;
		}

		TypeReference ReadTypeReference (byte [] data, BinaryReader br, out ElementType elemType)
		{
			bool array = false;
			elemType = (ElementType) br.ReadByte ();
			if (elemType == ElementType.SzArray) {
				elemType = (ElementType) br.ReadByte ();
				array = true;
			}

			TypeReference res;
			if (elemType == ElementType.Enum)
				res = CreateEnumTypeReference (ReadUTF8String (data, br));
			else
				res = TypeReferenceFromElemType (elemType);

			if (array)
				res = new ArrayType (res);

			return res;
		}

		TypeReference TypeReferenceFromElemType (ElementType elemType)
		{
			switch (elemType) {
			case ElementType.Boxed :
				return m_reflectReader.SearchCoreType (Constants.Object);
			case ElementType.String :
				return m_reflectReader.SearchCoreType (Constants.String);
			case ElementType.Type :
				return m_reflectReader.SearchCoreType (Constants.Type);
			case ElementType.Boolean :
				return m_reflectReader.SearchCoreType (Constants.Boolean);
			case ElementType.Char :
				return m_reflectReader.SearchCoreType (Constants.Char);
			case ElementType.R4 :
				return m_reflectReader.SearchCoreType (Constants.Single);
			case ElementType.R8 :
				return m_reflectReader.SearchCoreType (Constants.Double);
			case ElementType.I1 :
				return m_reflectReader.SearchCoreType (Constants.SByte);
			case ElementType.I2 :
				return m_reflectReader.SearchCoreType (Constants.Int16);
			case ElementType.I4 :
				return m_reflectReader.SearchCoreType (Constants.Int32);
			case ElementType.I8 :
				return m_reflectReader.SearchCoreType (Constants.Int64);
			case ElementType.U1 :
				return m_reflectReader.SearchCoreType (Constants.Byte);
			case ElementType.U2 :
				return m_reflectReader.SearchCoreType (Constants.UInt16);
			case ElementType.U4 :
				return m_reflectReader.SearchCoreType (Constants.UInt32);
			case ElementType.U8 :
				return m_reflectReader.SearchCoreType (Constants.UInt64);
			default :
				throw new MetadataFormatException ("Non valid type in CustomAttrib.Elem: 0x{0}",
					((byte) elemType).ToString("x2"));
			}
		}

		internal CustomAttrib.NamedArg ReadNamedArg (byte [] data, BinaryReader br, ref bool read, bool resolve)
		{
			CustomAttrib.NamedArg na = new CustomAttrib.NamedArg ();
			byte kind = br.ReadByte ();
			if (kind == 0x53) { // field
				na.Field = true;
				na.Property = false;
			} else if (kind == 0x54) { // property
				na.Field = false;
				na.Property = true;
			} else
				throw new MetadataFormatException ("Wrong kind of namedarg found: 0x" + kind.ToString("x2"));

			TypeReference elemType = ReadTypeReference (data, br, out na.FieldOrPropType);
			na.FieldOrPropName = ReadUTF8String (data, br);
			na.FixedArg = ReadFixedArg (data, br, elemType, ref read, resolve);

			return na;
		}

		CustomAttrib.Elem ReadElem (byte [] data, BinaryReader br, TypeReference elemType, ref bool read, bool resolve)
		{
			CustomAttrib.Elem elem = new CustomAttrib.Elem ();

			string elemName = elemType.FullName;

			if (elemName == Constants.Object) {
				elemType = ReadTypeReference (data, br, out elem.FieldOrPropType);
				if (elemType is ArrayType) {
					read = false; // Don't know how to represent arrays as an object value.
					return elem;
				} else if (elemType.FullName == Constants.Object)
					throw new MetadataFormatException ("Non valid type in CustomAttrib.Elem after boxed prefix: 0x{0}",
						((byte) elem.FieldOrPropType).ToString("x2"));

				elem = ReadElem (data, br, elemType, ref read, resolve);
				elem.String = elem.Simple = elem.Type = false;
				elem.BoxedValueType = true;
				return elem;
			}

			elem.ElemType = elemType;

			if (elemName == Constants.Type || elemName == Constants.String) {
				switch (elemType.FullName) {
				case Constants.String:
					elem.String = true;
					elem.BoxedValueType = elem.Simple = elem.Type = false;
					break;
				case Constants.Type:
					elem.Type = true;
					elem.BoxedValueType = elem.Simple = elem.String = false;
					break;
				}

				if (data [br.BaseStream.Position] == 0xff) { // null
					elem.Value = null;
					br.BaseStream.Position++;
				} else {
					elem.Value = ReadUTF8String (data, br);
				}
				return elem;
			}

			elem.String = elem.Type = elem.BoxedValueType = false;
			if (!ReadSimpleValue (br, ref elem, elem.ElemType)) {
				if (!resolve) { // until enums writing is implemented
					read = false;
					return elem;
				}
				TypeReference typeRef = GetEnumUnderlyingType (elem.ElemType, resolve);
				if (typeRef == null || !ReadSimpleValue (br, ref elem, typeRef))
					read = false;
			}

			return elem;
		}

		TypeReference GetEnumUnderlyingType (TypeReference enumType, bool resolve)
		{
			TypeDefinition type = enumType as TypeDefinition;
			if (type == null && resolve && AssemblyResolver != null) {
				if (enumType.Scope is ModuleDefinition)
					throw new NotSupportedException ();

				AssemblyDefinition asm = AssemblyResolver.Resolve (
					((AssemblyNameReference) enumType.Scope).FullName);
				type = asm.MainModule.Types [enumType.FullName];
			}

			if (type != null && type.IsEnum)
				return type.Fields.GetField ("value__").FieldType;

			return null;
		}

		bool ReadSimpleValue (BinaryReader br, ref CustomAttrib.Elem elem, TypeReference type)
		{
			switch (type.FullName) {
			case Constants.Boolean :
				elem.Value = br.ReadByte () == 1;
				break;
			case Constants.Char :
				elem.Value = (char) br.ReadUInt16 ();
				break;
			case Constants.Single :
				elem.Value = br.ReadSingle ();
				break;
			case Constants.Double :
				elem.Value = br.ReadDouble ();
				break;
			case Constants.Byte :
				elem.Value = br.ReadByte ();
				break;
			case Constants.Int16 :
				elem.Value = br.ReadInt16 ();
				break;
			case Constants.Int32 :
				elem.Value = br.ReadInt32 ();
				break;
			case Constants.Int64 :
				elem.Value = br.ReadInt64 ();
				break;
			case Constants.SByte :
				elem.Value = br.ReadSByte ();
				break;
			case Constants.UInt16 :
				elem.Value = br.ReadUInt16 ();
				break;
			case Constants.UInt32 :
				elem.Value = br.ReadUInt32 ();
				break;
			case Constants.UInt64 :
				elem.Value = br.ReadUInt64 ();
				break;
			default : // enum
				return false;
			}
			elem.Simple = true;
			return true;
		}

		MarshalSig ReadMarshalSig (byte [] data)
		{
			int start;
			MarshalSig ms = new MarshalSig ((NativeType) Utilities.ReadCompressedInteger (data, 0, out start));
			switch (ms.NativeInstrinsic) {
			case NativeType.ARRAY:
				MarshalSig.Array ar = new MarshalSig.Array ();
				ar.ArrayElemType = (NativeType) Utilities.ReadCompressedInteger (data, start, out start);
				if (start < data.Length)
					ar.ParamNum = Utilities.ReadCompressedInteger (data, start, out start);
				if (start < data.Length)
					ar.NumElem = Utilities.ReadCompressedInteger (data, start, out start);
				if (start < data.Length)
					ar.ElemMult = Utilities.ReadCompressedInteger (data, start, out start);
				ms.Spec = ar;
				break;
			case NativeType.CUSTOMMARSHALER:
				MarshalSig.CustomMarshaler cm = new MarshalSig.CustomMarshaler ();
				cm.Guid = ReadUTF8String (data, start, out start);
				cm.UnmanagedType = ReadUTF8String (data, start, out start);
				cm.ManagedType = ReadUTF8String (data, start, out start);
				cm.Cookie = ReadUTF8String (data, start, out start);
				ms.Spec = cm;
				break;
			case NativeType.FIXEDARRAY:
				MarshalSig.FixedArray fa = new MarshalSig.FixedArray ();
				fa.NumElem = Utilities.ReadCompressedInteger (data, start, out start);
				if (start < data.Length)
					fa.ArrayElemType = (NativeType) Utilities.ReadCompressedInteger (data, start, out start);
				ms.Spec = fa;
				break;
			case NativeType.SAFEARRAY:
				MarshalSig.SafeArray sa = new MarshalSig.SafeArray ();
				if (start < data.Length)
					sa.ArrayElemType = (VariantType) Utilities.ReadCompressedInteger (data, start, out start);
				ms.Spec = sa;
				break;
			case NativeType.FIXEDSYSSTRING:
				MarshalSig.FixedSysString fss = new MarshalSig.FixedSysString ();
				if (start < data.Length)
					fss.Size = Utilities.ReadCompressedInteger (data, start, out start);
				ms.Spec = fss;
				break;
			}
			return ms;
		}

		static internal string ReadUTF8String (byte [] data, BinaryReader br)
		{
			int start = (int)br.BaseStream.Position;
			string val = ReadUTF8String (data, start, out start);
			br.BaseStream.Position = start;
			return val;
		}

		static internal string ReadUTF8String (byte [] data, int pos, out int start)
		{
			int length = Utilities.ReadCompressedInteger (data, pos, out start);
			pos = start;
			start += length;
			// COMPACT FRAMEWORK NOTE: Encoding.GetString (byte[]) is not supported.
			return Encoding.UTF8.GetString (data, pos, length);
		}
	}
}
