//
// MethodSpecification.cs
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

	public abstract class MethodSpecification : MethodReference {

		MethodReference m_elementMethod;

		public MethodReference ElementMethod {
			get { return m_elementMethod; }
			set { m_elementMethod = value; }
		}

		public override string Name {
			get { return m_elementMethod.Name; }
			set { throw new InvalidOperationException (); }
		}

		public override MethodCallingConvention CallingConvention {
			get { return m_elementMethod.CallingConvention; }
			set { throw new InvalidOperationException (); }
		}

		public override bool HasThis {
			get { return m_elementMethod.HasThis; }
			set { throw new InvalidOperationException (); }
		}

		public override bool ExplicitThis {
			get { return m_elementMethod.ExplicitThis; }
			set { throw new InvalidOperationException (); }
		}

		public override MethodReturnType ReturnType {
			get { return m_elementMethod.ReturnType; }
			set { throw new InvalidOperationException (); }
		}

		public override TypeReference DeclaringType {
			get { return m_elementMethod.DeclaringType; }
			set { throw new InvalidOperationException (); }
		}

		public override ParameterDefinitionCollection Parameters {
			get { return m_elementMethod.Parameters; }
		}

		internal MethodSpecification (MethodReference elemMethod) : base (string.Empty)
		{
			m_elementMethod = elemMethod;
		}

		public override MethodReference GetOriginalMethod()
		{
			return m_elementMethod.GetOriginalMethod ();
		}
	}
}
