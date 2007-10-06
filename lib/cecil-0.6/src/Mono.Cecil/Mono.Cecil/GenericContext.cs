//
// GenericContext.cs
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

	public class GenericContext {

		TypeReference m_type;
		MethodReference m_method;

		public TypeReference Type {
			get { return m_type; }
			set { m_type = value; }
		}

		public MethodReference Method {
			get { return m_method; }
			set { m_method = value; }
		}

		public bool AllowCreation {
			get { return m_type != null && m_type.GetType () == typeof (TypeReference); }
		}

		public bool Null {
			get { return m_type == null && m_method == null; }
		}

		public GenericContext ()
		{
		}

		public GenericContext (TypeReference type, MethodReference meth)
		{
			m_type = type;
			m_method = meth;
		}

		public GenericContext (IGenericParameterProvider provider)
		{
			if (provider is TypeReference)
				m_type = provider as TypeReference;
			else if (provider is MethodReference) {
				MethodReference meth = provider as MethodReference;
				m_method = meth;
				m_type = meth.DeclaringType;
			}
		}

		internal void CheckProvider (IGenericParameterProvider provider, int count)
		{
			if (!AllowCreation)
				return;

			for (int i = provider.GenericParameters.Count; i < count; i++)
				provider.GenericParameters.Add (new GenericParameter (i, provider));
		}

		public GenericContext Clone ()
		{
			GenericContext ctx = new GenericContext ();
			ctx.Type = m_type;
			ctx.Method = m_method;
			return ctx;
		}
	}
}
