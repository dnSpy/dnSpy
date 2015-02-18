//
// SignatureWriter.cs
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
	using System.Text;

	using Mono.Cecil;
	using Mono.Cecil.Binary;
	using Mono.Cecil.Metadata;

	internal sealed class SignatureWriter : BaseSignatureVisitor {

		MetadataWriter m_mdWriter;
		MemoryBinaryWriter m_sigWriter;

		public SignatureWriter (MetadataWriter mdWriter)
		{
			m_mdWriter = mdWriter;
			m_sigWriter = new MemoryBinaryWriter ();
		}

		uint GetPointer ()
		{
			return m_mdWriter.AddBlob (m_sigWriter.ToArray ());
		}

		public uint AddMethodDefSig (MethodDefSig methSig)
		{
			return AddSignature (methSig);
		}

		public uint AddMethodRefSig (MethodRefSig methSig)
		{
			return AddSignature (methSig);
		}

		public uint AddPropertySig (PropertySig ps)
		{
			return AddSignature (ps);
		}

		public uint AddFieldSig (FieldSig fSig)
		{
			return AddSignature (fSig);
		}

		public uint AddLocalVarSig (LocalVarSig lvs)
		{
			return AddSignature (lvs);
		}

		uint AddSignature (Signature s)
		{
			m_sigWriter.Empty ();
			s.Accept (this);
			return GetPointer ();
		}

		public uint AddTypeSpec (TypeSpec ts)
		{
			m_sigWriter.Empty ();
			Write (ts);
			return GetPointer ();
		}

		public uint AddMethodSpec (MethodSpec ms)
		{
			m_sigWriter.Empty ();
			Write (ms);
			return GetPointer ();
		}

		public uint AddMarshalSig (MarshalSig ms)
		{
			m_sigWriter.Empty ();
			Write (ms);
			return GetPointer ();
		}

		public uint AddCustomAttribute (CustomAttrib ca, MethodReference ctor)
		{
			CompressCustomAttribute (ca, ctor, m_sigWriter);
			return GetPointer ();
		}

		public byte [] CompressCustomAttribute (CustomAttrib ca, MethodReference ctor)
		{
			MemoryBinaryWriter writer = new MemoryBinaryWriter ();
			CompressCustomAttribute (ca, ctor, writer);
			return writer.ToArray ();
		}

		public byte [] CompressFieldSig (FieldSig field)
		{
			m_sigWriter.Empty ();
			VisitFieldSig (field);
			return m_sigWriter.ToArray ();
		}

		public byte [] CompressLocalVar (LocalVarSig.LocalVariable var)
		{
			m_sigWriter.Empty ();
			Write (var);
			return m_sigWriter.ToArray ();
		}

		void CompressCustomAttribute (CustomAttrib ca, MethodReference ctor, MemoryBinaryWriter writer)
		{
			m_sigWriter.Empty ();
			Write (ca, ctor, writer);
		}

		public override void VisitMethodDefSig (MethodDefSig methodDef)
		{
			m_sigWriter.Write (methodDef.CallingConvention);
			if (methodDef.GenericParameterCount > 0)
				Write (methodDef.GenericParameterCount);
			Write (methodDef.ParamCount);
			Write (methodDef.RetType);
			Write (methodDef.Parameters, methodDef.Sentinel);
		}

		public override void VisitMethodRefSig (MethodRefSig methodRef)
		{
			m_sigWriter.Write (methodRef.CallingConvention);
			Write (methodRef.ParamCount);
			Write (methodRef.RetType);
			Write (methodRef.Parameters, methodRef.Sentinel);
		}

		public override void VisitFieldSig (FieldSig field)
		{
			m_sigWriter.Write (field.CallingConvention);
			Write (field.CustomMods);
			Write (field.Type);
		}

		public override void VisitPropertySig (PropertySig property)
		{
			m_sigWriter.Write (property.CallingConvention);
			Write (property.ParamCount);
			Write (property.CustomMods);
			Write (property.Type);
			Write (property.Parameters);
		}

		public override void VisitLocalVarSig (LocalVarSig localvar)
		{
			m_sigWriter.Write (localvar.CallingConvention);
			Write (localvar.Count);
			Write (localvar.LocalVariables);
		}

		void Write (LocalVarSig.LocalVariable [] vars)
		{
			foreach (LocalVarSig.LocalVariable var in vars)
				Write (var);
		}

		void Write (LocalVarSig.LocalVariable var)
		{
			Write (var.CustomMods);
			if ((var.Constraint & Constraint.Pinned) != 0)
				Write (ElementType.Pinned);
			if (var.ByRef)
				Write (ElementType.ByRef);
			Write (var.Type);
		}

		void Write (RetType retType)
		{
			Write (retType.CustomMods);
			if (retType.Void)
				Write (ElementType.Void);
			else if (retType.TypedByRef)
				Write (ElementType.TypedByRef);
			else if (retType.ByRef) {
				Write (ElementType.ByRef);
				Write (retType.Type);
			} else
				Write (retType.Type);
		}

		void Write (Param [] parameters, int sentinel)
		{
			for (int i = 0; i < parameters.Length; i++) {
				if (i == sentinel)
					Write (ElementType.Sentinel);

				Write (parameters [i]);
			}
		}

		void Write (Param [] parameters)
		{
			foreach (Param p in parameters)
				Write (p);
		}

		void Write (ElementType et)
		{
			Write ((int) et);
		}

		void Write (SigType t)
		{
			Write ((int) t.ElementType);

			switch (t.ElementType) {
			case ElementType.ValueType :
				Write ((int) Utilities.CompressMetadataToken (
						CodedIndex.TypeDefOrRef, ((VALUETYPE) t).Type));
				break;
			case ElementType.Class :
				Write ((int) Utilities.CompressMetadataToken (
						CodedIndex.TypeDefOrRef, ((CLASS) t).Type));
				break;
			case ElementType.Ptr :
				PTR p = (PTR) t;
				if (p.Void)
					Write (ElementType.Void);
				else {
					Write (p.CustomMods);
					Write (p.PtrType);
				}
				break;
			case ElementType.FnPtr :
				FNPTR fp = (FNPTR) t;
				if (fp.Method is MethodRefSig)
					(fp.Method as MethodRefSig).Accept (this);
				else
					(fp.Method as MethodDefSig).Accept (this);
				break;
			case ElementType.Array :
				ARRAY ary = (ARRAY) t;
				Write (ary.CustomMods);
				ArrayShape shape = ary.Shape;
				Write (ary.Type);
				Write (shape.Rank);
				Write (shape.NumSizes);
				foreach (int size in shape.Sizes)
					Write (size);
				Write (shape.NumLoBounds);
				foreach (int loBound in shape.LoBounds)
					Write (loBound);
				break;
			case ElementType.SzArray :
				SZARRAY sa = (SZARRAY) t;
				Write (sa.CustomMods);
				Write (sa.Type);
				break;
			case ElementType.Var :
				Write (((VAR) t).Index);
				break;
			case ElementType.MVar :
				Write (((MVAR) t).Index);
				break;
			case ElementType.GenericInst :
				GENERICINST gi = t as GENERICINST;
				Write (gi.ValueType ? ElementType.ValueType : ElementType.Class);
				Write ((int) Utilities.CompressMetadataToken (
						CodedIndex.TypeDefOrRef, gi.Type));
				Write (gi.Signature);
				break;
			}
		}

		void Write (TypeSpec ts)
		{
			Write (ts.CustomMods);
			Write (ts.Type);
		}

		void Write (MethodSpec ms)
		{
			Write (0x0a);
			Write (ms.Signature);
		}

		void Write (GenericInstSignature gis)
		{
			Write (gis.Arity);
			for (int i = 0; i < gis.Arity; i++)
				Write (gis.Types [i]);
		}

		void Write (GenericArg arg)
		{
			Write (arg.CustomMods);
			Write (arg.Type);
		}

		void Write (Param p)
		{
			Write (p.CustomMods);
			if (p.TypedByRef)
				Write (ElementType.TypedByRef);
			else if (p.ByRef) {
				Write (ElementType.ByRef);
				Write (p.Type);
			} else
				Write (p.Type);
		}

		void Write (CustomMod [] customMods)
		{
			foreach (CustomMod cm in customMods)
				Write (cm);
		}

		void Write (CustomMod cm)
		{
			switch (cm.CMOD) {
			case CustomMod.CMODType.OPT :
				Write (ElementType.CModOpt);
				break;
			case CustomMod.CMODType.REQD :
				Write (ElementType.CModReqD);
				break;
			}

			Write ((int) Utilities.CompressMetadataToken (
					CodedIndex.TypeDefOrRef, cm.TypeDefOrRef));
		}

		void Write (MarshalSig ms)
		{
			Write ((int) ms.NativeInstrinsic);
			switch (ms.NativeInstrinsic) {
			case NativeType.ARRAY :
				MarshalSig.Array ar = (MarshalSig.Array) ms.Spec;
				Write ((int) ar.ArrayElemType);
				if (ar.ParamNum != -1)
					Write (ar.ParamNum);
				if (ar.NumElem != -1)
					Write (ar.NumElem);
				if (ar.ElemMult != -1)
					Write (ar.ElemMult);
				break;
			case NativeType.CUSTOMMARSHALER :
				MarshalSig.CustomMarshaler cm = (MarshalSig.CustomMarshaler) ms.Spec;
				Write (cm.Guid);
				Write (cm.UnmanagedType);
				Write (cm.ManagedType);
				Write (cm.Cookie);
				break;
			case NativeType.FIXEDARRAY :
				MarshalSig.FixedArray fa = (MarshalSig.FixedArray) ms.Spec;
				Write (fa.NumElem);
				if (fa.ArrayElemType != NativeType.NONE)
					Write ((int) fa.ArrayElemType);
				break;
			case NativeType.SAFEARRAY :
				Write ((int) ((MarshalSig.SafeArray) ms.Spec).ArrayElemType);
				break;
			case NativeType.FIXEDSYSSTRING :
				Write (((MarshalSig.FixedSysString) ms.Spec).Size);
				break;
			}
		}

		void Write (CustomAttrib ca, MethodReference ctor, MemoryBinaryWriter writer)
		{
			if (ca == null)
				return;

			if (ca.Prolog != CustomAttrib.StdProlog)
				return;

			writer.Write (ca.Prolog);

			for (int i = 0; i < ctor.Parameters.Count; i++)
				Write (ca.FixedArgs [i], writer);

			writer.Write (ca.NumNamed);

			for (int i = 0; i < ca.NumNamed; i++)
				Write (ca.NamedArgs [i], writer);
		}

		void Write (CustomAttrib.FixedArg fa, MemoryBinaryWriter writer)
		{
			if (fa.SzArray)
				writer.Write (fa.NumElem);

			foreach (CustomAttrib.Elem elem in fa.Elems)
				Write (elem, writer);
		}

		void Write (CustomAttrib.NamedArg na, MemoryBinaryWriter writer)
		{
			if (na.Field)
				writer.Write ((byte) 0x53);
			else if (na.Property)
				writer.Write ((byte) 0x54);
			else
				throw new MetadataFormatException ("Unknown kind of namedarg");

			if (na.FixedArg.SzArray)
				writer.Write ((byte) ElementType.SzArray);

			if (na.FieldOrPropType == ElementType.Object)
				writer.Write ((byte) ElementType.Boxed);
			else
				writer.Write ((byte) na.FieldOrPropType);

			if (na.FieldOrPropType == ElementType.Enum)
				Write (na.FixedArg.Elems [0].ElemType.FullName);

			Write (na.FieldOrPropName);

			Write (na.FixedArg, writer);
		}

		void Write (CustomAttrib.Elem elem, MemoryBinaryWriter writer) // TODO
		{
			if (elem.String)
				elem.FieldOrPropType = ElementType.String;
			else if (elem.Type)
				elem.FieldOrPropType = ElementType.Type;
			else if (elem.BoxedValueType)
				Write (elem.FieldOrPropType);

			switch (elem.FieldOrPropType) {
			case ElementType.Boolean :
				writer.Write ((byte) ((bool) elem.Value ? 1 : 0));
				break;
			case ElementType.Char :
				writer.Write ((ushort) (char) elem.Value);
				break;
			case ElementType.R4 :
				writer.Write ((float) elem.Value);
				break;
			case ElementType.R8 :
				writer.Write ((double) elem.Value);
				break;
			case ElementType.I1 :
				writer.Write ((sbyte) elem.Value);
				break;
			case ElementType.I2 :
				writer.Write ((short) elem.Value);
				break;
			case ElementType.I4 :
				writer.Write ((int) elem.Value);
				break;
			case ElementType.I8 :
				writer.Write ((long) elem.Value);
				break;
			case ElementType.U1 :
				writer.Write ((byte) elem.Value);
				break;
			case ElementType.U2 :
				writer.Write ((ushort) elem.Value);
				break;
			case ElementType.U4 :
				writer.Write ((uint) elem.Value);
				break;
			case ElementType.U8 :
				writer.Write ((long) elem.Value);
				break;
			case ElementType.String :
			case ElementType.Type :
				string s = elem.Value as string;
				if (s == null)
					writer.Write ((byte) 0xff);
				else if (s.Length == 0)
					writer.Write ((byte) 0x00);
				else
					Write (s);
				break;
			case ElementType.Object :
				if (elem.Value != null)
					throw new NotSupportedException ("Unknown state");
				writer.Write ((byte) 0xff);
				break;
			default :
				throw new NotImplementedException ("WriteElem " + elem.FieldOrPropType.ToString ());
			}
		}

		void Write (string s)
		{
			byte [] str = Encoding.UTF8.GetBytes (s);
			Write (str.Length);
			m_sigWriter.Write (str);
		}

		void Write (int i)
		{
			Utilities.WriteCompressedInteger (m_sigWriter, i);
		}
	}
}
