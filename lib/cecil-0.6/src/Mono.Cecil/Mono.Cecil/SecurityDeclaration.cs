//
// SecurityDeclaration.cs
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
	using System.Collections;
	using System.Security;

	public sealed class SecurityDeclaration : IRequireResolving, IAnnotationProvider, IReflectionVisitable {

		SecurityAction m_action;
		IDictionary m_annotations;

#if !CF_1_0 && !CF_2_0
		PermissionSet m_permSet;
#endif

		bool m_resolved;
		byte [] m_blob;

		public SecurityAction Action {
			get { return m_action; }
			set { m_action = value; }
		}

#if !CF_1_0 && !CF_2_0
		public PermissionSet PermissionSet {
			get { return m_permSet; }
			set { m_permSet = value; }
		}
#endif

		public bool Resolved {
			get { return m_resolved; }
			set { m_resolved = value; }
		}

		public byte [] Blob {
			get { return m_blob; }
			set { m_blob = value; }
		}

		IDictionary IAnnotationProvider.Annotations {
			get {
				if (m_annotations == null)
					m_annotations = new Hashtable ();
				return m_annotations;
			}
		}

		public SecurityDeclaration (SecurityAction action)
		{
			m_action = action;
			m_resolved = true;
		}

		public SecurityDeclaration Clone ()
		{
			return Clone (this);
		}

		internal static SecurityDeclaration Clone (SecurityDeclaration sec)
		{
			SecurityDeclaration sd = new SecurityDeclaration (sec.Action);
			if (!sec.Resolved) {
				sd.Resolved = false;
				sd.Blob = sec.Blob;
				return sd;
			}

#if !CF_1_0 && !CF_2_0
            sd.PermissionSet = sec.PermissionSet.Copy ();
#endif
			return sd;
		}

		public bool Resolve ()
		{
			throw new NotImplementedException ();
		}

		public void Accept (IReflectionVisitor visitor)
		{
			visitor.VisitSecurityDeclaration (this);
		}
	}
}

