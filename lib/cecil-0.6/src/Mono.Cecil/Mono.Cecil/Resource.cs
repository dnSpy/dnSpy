//
// Resource.cs
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

	public abstract class Resource : IAnnotationProvider, IReflectionStructureVisitable {

		string m_name;
		ManifestResourceAttributes m_attributes;
		IDictionary m_annotations;

		public string Name {
			get { return m_name; }
			set { m_name = value; }
		}

		public ManifestResourceAttributes Flags {
			get { return m_attributes; }
			set { m_attributes = value; }
		}

		IDictionary IAnnotationProvider.Annotations {
			get {
				if (m_annotations == null)
					m_annotations = new Hashtable ();
				return m_annotations;
			}
		}

		#region ManifestResourceAttributes

		public bool IsPublic {
			get { return (m_attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Public; }
			set {
				if (value) {
					m_attributes &= ~ManifestResourceAttributes.VisibilityMask;
					m_attributes |= ManifestResourceAttributes.Public;
				} else
					m_attributes &= ~(ManifestResourceAttributes.VisibilityMask & ManifestResourceAttributes.Public);
			}
		}

		public bool IsPrivate {
			get { return (m_attributes & ManifestResourceAttributes.VisibilityMask) == ManifestResourceAttributes.Private; }
			set {
				if (value) {
					m_attributes &= ~ManifestResourceAttributes.VisibilityMask;
					m_attributes |= ManifestResourceAttributes.Private;
				} else
					m_attributes &= ~(ManifestResourceAttributes.VisibilityMask & ManifestResourceAttributes.Private);
			}
		}

		#endregion

		internal Resource (string name, ManifestResourceAttributes attributes)
		{
			m_name = name;
			m_attributes = attributes;
		}

		public abstract void Accept (IReflectionStructureVisitor visitor);
	}
}
