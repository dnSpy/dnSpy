//
// TypeSpecification.cs
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

	public abstract class TypeSpecification : TypeReference {

		private TypeReference m_elementType;

		public override string Name {
			get { return m_elementType.Name; }
			set { throw new NotSupportedException (); }
		}

		public override string Namespace {
			get { return m_elementType.Namespace; }
			set { throw new NotSupportedException (); }
		}

		public override bool IsValueType {
			get { return m_elementType.IsValueType; }
			set { throw new InvalidOperationException (); }
		}

		public override IMetadataScope Scope {
			get { return m_elementType.Scope; }
		}

		public override ModuleDefinition Module {
			get { return m_elementType.Module; }
			set { throw new InvalidOperationException (); }
		}

		public TypeReference ElementType {
			get { return m_elementType; }
			set { m_elementType = value; }
		}

		public override string FullName {
			get { return m_elementType.FullName; }
		}

		internal TypeSpecification (TypeReference elementType) : base (string.Empty, string.Empty)
		{
			m_elementType = elementType;
		}

		public override TypeReference GetOriginalType ()
		{
			return m_elementType.GetOriginalType ();
		}
	}
}
