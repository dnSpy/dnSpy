//
// IGenericInstanceMethod.cs
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

	using System.Text;

	public sealed class GenericInstanceMethod : MethodSpecification, IGenericInstance {

		private GenericArgumentCollection m_genArgs;

		public GenericArgumentCollection GenericArguments {
			get {
				if (m_genArgs == null)
					m_genArgs = new GenericArgumentCollection (this);
				return m_genArgs;
			}
		}

		public GenericInstanceMethod (MethodReference elemMethod) : base (elemMethod)
		{
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			MethodReference meth = this.ElementMethod;
			sb.Append (meth.ReturnType.ReturnType.FullName);
			sb.Append (" ");
			sb.Append (meth.DeclaringType.FullName);
			sb.Append ("::");
			sb.Append (meth.Name);
			sb.Append ("<");
			for (int i = 0; i < this.GenericArguments.Count; i++) {
				if (i > 0)
					sb.Append (",");
				sb.Append (this.GenericArguments [i].FullName);
			}
			sb.Append (">");
			sb.Append ("(");
			for (int i = 0; i < meth.Parameters.Count; i++) {
				sb.Append (meth.Parameters [i].ParameterType.FullName);
				if (i < meth.Parameters.Count - 1)
					sb.Append (",");
			}
			sb.Append (")");
			return sb.ToString ();
		}
	}
}
