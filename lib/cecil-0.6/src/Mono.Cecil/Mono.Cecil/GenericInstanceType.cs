//
// GenericInstanceType.cs
//
// Author:
//	Martin Baulig  <martin@ximian.com>
//  Jb Evain  <jbevain@gmail.com>
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

	public sealed class GenericInstanceType : TypeSpecification, IGenericInstance {

		private GenericArgumentCollection m_genArgs;

		public GenericArgumentCollection GenericArguments {
			get {
				if (m_genArgs == null)
					m_genArgs = new GenericArgumentCollection (this);
				return m_genArgs;
			}
		}

		public override bool IsValueType {
			get { return m_isValueType; }
			set { m_isValueType = value; }
		}

		public override string FullName {
			get {
				StringBuilder sb = new StringBuilder ();
				sb.Append (base.FullName);
				sb.Append ("<");
				for (int i = 0; i < this.GenericArguments.Count; i++) {
					if (i > 0)
						sb.Append (",");
					sb.Append (this.GenericArguments [i].FullName);
				}
				sb.Append (">");
				return sb.ToString ();
			}
		}

		public GenericInstanceType (TypeReference elementType) : base (elementType)
		{
		}
	}
}
