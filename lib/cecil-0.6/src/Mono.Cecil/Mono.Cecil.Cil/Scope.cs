//
// Scope.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
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

	public class Scope : IScopeProvider, IVariableDefinitionProvider, ICodeVisitable {

		Instruction m_start;
		Instruction m_end;

		Scope m_parent;
		ScopeCollection m_scopes;

		VariableDefinitionCollection m_variables;

		public Instruction Start {
			get { return m_start; }
			set { m_start = value; }
		}

		public Instruction End {
			get { return m_end; }
			set { m_end = value; }
		}

		public Scope Parent {
			get { return m_parent; }
			set { m_parent = value; }
		}

		public ScopeCollection Scopes {
			get {
				if (m_scopes == null)
					m_scopes = new ScopeCollection (this);

				return m_scopes;
			}
		}

		public VariableDefinitionCollection Variables {
			get {
				if (m_variables == null)
					m_variables = new VariableDefinitionCollection (this);

				return m_variables;
			}
		}

		public void Accept (ICodeVisitor visitor)
		{
			visitor.VisitScope (this);
		}
	}
}
