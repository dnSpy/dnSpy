//
// MethodReference.cs
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

namespace Mono.Cecil {

	using System.Text;

	public class MethodReference : MemberReference, IMethodSignature, IGenericParameterProvider {

		ParameterDefinitionCollection m_parameters;
		MethodReturnType m_returnType;

		bool m_hasThis;
		bool m_explicitThis;
		MethodCallingConvention m_callConv;
		GenericParameterCollection m_genparams;

		public virtual bool HasThis {
			get { return m_hasThis; }
			set { m_hasThis = value; }
		}

		public virtual bool ExplicitThis {
			get { return m_explicitThis; }
			set { m_explicitThis = value; }
		}

		public virtual MethodCallingConvention CallingConvention {
			get { return m_callConv; }
			set { m_callConv = value; }
		}

		public virtual ParameterDefinitionCollection Parameters {
			get {
				if (m_parameters == null)
					m_parameters = new ParameterDefinitionCollection (this);
				return m_parameters;
			}
		}

		public GenericParameterCollection GenericParameters {
			get {
				if (m_genparams == null)
					m_genparams = new GenericParameterCollection (this);
				return m_genparams;
			}
		}

		public virtual MethodReturnType ReturnType {
			get { return m_returnType;}
			set { m_returnType = value; }
		}

		internal MethodReference (string name, bool hasThis,
			bool explicitThis, MethodCallingConvention callConv) : this (name)
		{
			m_parameters = new ParameterDefinitionCollection (this);
			m_hasThis = hasThis;
			m_explicitThis = explicitThis;
			m_callConv = callConv;
		}

		internal MethodReference (string name) : base (name)
		{
			m_returnType = new MethodReturnType (null);
		}

		public MethodReference (string name,
			TypeReference declaringType, TypeReference returnType,
			bool hasThis, bool explicitThis, MethodCallingConvention callConv) :
			this (name, hasThis, explicitThis, callConv)
		{
			this.DeclaringType = declaringType;
			this.ReturnType.ReturnType = returnType;
		}

		public virtual MethodReference GetOriginalMethod ()
		{
			return this;
		}

		public int GetSentinel ()
		{
			for (int i = 0; i < Parameters.Count; i++)
				if (Parameters [i].ParameterType is SentinelType)
					return i;

			return -1;
		}

		public override string ToString ()
		{
			int sentinel = GetSentinel ();

			StringBuilder sb = new StringBuilder ();
			sb.Append (m_returnType.ReturnType.FullName);
			sb.Append (" ");
			sb.Append (base.ToString ());
			sb.Append ("(");
			for (int i = 0; i < this.Parameters.Count; i++) {
				if (i > 0)
					sb.Append (",");

				if (i == sentinel)
					sb.Append ("...,");

				sb.Append (this.Parameters [i].ParameterType.FullName);
			}
			sb.Append (")");
			return sb.ToString ();
		}
	}
}
