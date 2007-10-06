//
// ParameterReference.cs
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

	using System.Collections;

	using Mono.Cecil.Metadata;

	public abstract class ParameterReference : IMetadataTokenProvider, IAnnotationProvider, IReflectionVisitable {

		string m_name;
		int m_sequence;
		TypeReference m_paramType;
		MetadataToken m_token;
		IDictionary m_annotations;

		public string Name {
			get { return m_name; }
			set { m_name = value; }
		}

		public int Sequence {
			get { return m_sequence; }
			set { m_sequence = value; }
		}

		public TypeReference ParameterType {
			get { return m_paramType; }
			set { m_paramType = value; }
		}

		public MetadataToken MetadataToken {
			get { return m_token; }
			set { m_token = value; }
		}

		IDictionary IAnnotationProvider.Annotations {
			get {
				if (m_annotations == null)
					m_annotations = new Hashtable ();
				return m_annotations;
			}
		}

		public ParameterReference (string name, int sequence, TypeReference parameterType)
		{
			m_name = name;
			m_sequence = sequence;
			m_paramType = parameterType;
		}

		public override string ToString ()
		{
			if (m_name != null && m_name.Length > 0)
				return m_name;

			return string.Concat ("A_", m_sequence);
		}

		public abstract void Accept (IReflectionVisitor visitor);
	}
}
