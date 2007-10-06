//
// ModuleReference.cs
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

	using Mono.Cecil;
	using Mono.Cecil.Metadata;

	public class ModuleReference : IMetadataScope, IAnnotationProvider, IReflectionStructureVisitable {

		string m_name;
		MetadataToken m_token;
		IDictionary m_annotations;

		public string Name {
			get { return m_name; }
			set { m_name = value; }
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

		public ModuleReference (string name)
		{
			m_name = name;
		}

		public virtual void Accept (IReflectionStructureVisitor visitor)
		{
			visitor.VisitModuleReference (this);
		}
	}
}

