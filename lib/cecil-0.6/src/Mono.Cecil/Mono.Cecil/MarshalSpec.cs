//
// MarshalDesc.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

namespace Mono.Cecil {

	using System;

	public class MarshalSpec {

		NativeType m_natIntr;
		IHasMarshalSpec m_container;

		public NativeType NativeIntrinsic {
			get { return m_natIntr; }
			set { m_natIntr = value; }
		}

		public IHasMarshalSpec Container {
			get { return m_container; }
			set { m_container = value; }
		}

		public MarshalSpec (NativeType natIntr, IHasMarshalSpec container)
		{
			m_natIntr = natIntr;
			m_container = container;
		}

		public virtual void Accept (IReflectionVisitor visitor)
		{
			visitor.VisitMarshalSpec (this);
		}
	}

	public sealed class ArrayMarshalSpec : MarshalSpec {

		NativeType m_elemType;
		int m_paramNum;
		int m_elemMult;
		int m_numElem;

		public NativeType ElemType {
			get { return m_elemType; }
			set { m_elemType = value; }
		}

		public int ParamNum {
			get { return m_paramNum; }
			set { m_paramNum = value; }
		}

		public int ElemMult {
			get { return m_elemMult; }
			set { m_elemMult = value; }
		}

		public int NumElem {
			get { return m_numElem; }
			set { m_numElem = value; }
		}

		public ArrayMarshalSpec (IHasMarshalSpec container) : base (NativeType.ARRAY, container)
		{
		}
	}

	public sealed class CustomMarshalerSpec : MarshalSpec {

		Guid m_guid;
		string m_unmanagedType;
		string m_managedType;
		string m_cookie;

		public Guid Guid {
			get { return m_guid; }
			set { m_guid = value; }
		}

		public String UnmanagedType {
			get { return m_unmanagedType; }
			set { m_unmanagedType = value; }
		}

		public string ManagedType {
			get { return m_managedType; }
			set { m_managedType = value; }
		}

		public string Cookie {
			get { return m_cookie; }
			set { m_cookie = value; }
		}

		public CustomMarshalerSpec (IHasMarshalSpec container) : base (NativeType.CUSTOMMARSHALER, container)
		{
		}
	}

	public sealed class SafeArraySpec : MarshalSpec {

		private VariantType m_elemType;

		public VariantType ElemType {
			get { return m_elemType; }
			set { m_elemType = value; }
		}

		public SafeArraySpec (IHasMarshalSpec container) : base (NativeType.SAFEARRAY, container)
		{
		}
	}

	public sealed class FixedArraySpec : MarshalSpec {

		private int m_numElem;
		private NativeType m_elemType;

		public int NumElem {
			get { return m_numElem; }
			set { m_numElem = value; }
		}

		public NativeType ElemType {
			get { return m_elemType; }
			set { m_elemType = value; }
		}

		public FixedArraySpec (IHasMarshalSpec container) : base (NativeType.FIXEDARRAY, container)
		{
		}
	}

	public sealed class FixedSysStringSpec : MarshalSpec {

		private int m_size;

		public int Size {
			get { return m_size; }
			set { m_size = value; }
		}

		public FixedSysStringSpec (IHasMarshalSpec container) : base (NativeType.FIXEDSYSSTRING, container)
		{
		}
	}
}
