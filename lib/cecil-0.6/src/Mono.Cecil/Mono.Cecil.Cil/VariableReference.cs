//
// VariableReference.cs
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

namespace Mono.Cecil.Cil {

	public abstract class VariableReference : ICodeVisitable {

		string m_name;
		int m_index;
		TypeReference m_variableType;

		public string Name {
			get { return m_name; }
			set { m_name = value; }
		}

		public int Index {
			get { return m_index; }
			set { m_index = value; }
		}

		public TypeReference VariableType {
			get { return m_variableType; }
			set { m_variableType = value; }
		}

		public VariableReference (TypeReference variableType)
		{
			m_variableType = variableType;
		}

		public VariableReference (string name, int index, TypeReference variableType) : this (variableType)
		{
			m_name = name;
			m_index = index;
		}

		public override string ToString ()
		{
			if (m_name != null && m_name.Length > 0)
				return m_name;

			return string.Concat ("V_", m_index);
		}

		public abstract void Accept (ICodeVisitor visitor);
	}
}
