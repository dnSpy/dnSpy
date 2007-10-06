//
// MethodReturnType.cs
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

	using System.Reflection;

	using Mono.Cecil.Metadata;

	public sealed class MethodReturnType : ICustomAttributeProvider, IHasMarshalSpec, IHasConstant {

		MethodReference m_method;
		ParameterDefinition m_param;

		TypeReference m_returnType;

		public MethodReference Method {
			get { return m_method; }
			set { m_method = value; }
		}

		public TypeReference ReturnType {
			get { return m_returnType; }
			set { m_returnType = value; }
		}

		internal ParameterDefinition Parameter {
			get { return m_param; }
			set { m_param = value; }
		}

		public MetadataToken MetadataToken {
			get { return m_param.MetadataToken; }
			set { m_param.MetadataToken = value; }
		}

		public CustomAttributeCollection CustomAttributes {
			get {
				if (m_param == null) {
					m_param = new ParameterDefinition (
						string.Empty, 0, (ParameterAttributes) 0, m_returnType);
					m_param.Method = m_method;
				}

				return m_param.CustomAttributes;
			}
		}

		public bool HasConstant {
			get {
				if (m_param == null)
					return false;

				return m_param.HasConstant;
			}
		}

		public object Constant {
			get {
				if (m_param == null)
					return null;

				return m_param.Constant;
			}
			set {
				m_param.Constant = value;
			}
		}

		public MarshalSpec MarshalSpec {
			get {
				if (m_param == null)
					return null;

				return m_param.MarshalSpec;
			}
			set { m_param.MarshalSpec = value; }
		}

		public MethodReturnType (TypeReference retType)
		{
			m_returnType = retType;
		}
	}
}
