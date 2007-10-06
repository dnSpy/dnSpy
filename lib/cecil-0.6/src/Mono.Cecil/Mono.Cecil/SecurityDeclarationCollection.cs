//
// SecurityDeclarationCollection.cs
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

	public sealed class SecurityDeclarationCollection : IReflectionVisitable {

		IDictionary m_items;
		IHasSecurity m_container;

		public SecurityDeclaration this [int index] {
			get { return m_items [index] as SecurityDeclaration; }
			set { m_items [index] = value; }
		}

		public SecurityDeclaration this [SecurityAction action] {
			get { return m_items [action] as SecurityDeclaration; }
			set { m_items [action] = value; }
		}

		public IHasSecurity Container {
			get { return m_container; }
		}

		public int Count {
			get { return m_items.Count; }
		}

		public bool IsSynchronized {
			get { return false; }
		}

		public object SyncRoot {
			get { return this; }
		}

		public SecurityDeclarationCollection (IHasSecurity container)
		{
			m_container = container;
			m_items = new Hashtable ();
		}

		public void Add (SecurityDeclaration value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");

			// Each action can only be added once so...
			SecurityDeclaration current = (SecurityDeclaration) m_items[value.Action];
			if (current != null) {
				// ... further additions are transformed into unions
#if !CF_1_0 && !CF_2_0
                current.PermissionSet = current.PermissionSet.Union (value.PermissionSet);
#endif
			} else {
				m_items.Add (value.Action, value);
				SetHasSecurity (true);
			}
		}

		public void Clear ()
		{
			m_items.Clear ();
			SetHasSecurity (false);
		}

		public bool Contains (SecurityAction action)
		{
			return (m_items [action] != null);
		}

		public bool Contains (SecurityDeclaration value)
		{
			if (value == null)
				return (m_items.Count == 0);

			SecurityDeclaration item = (SecurityDeclaration) m_items[value.Action];
			if (item == null)
				return false;

#if !CF_1_0 && !CF_2_0
			return value.PermissionSet.IsSubsetOf (item.PermissionSet);
#else
            // XXX For CF, this concept does not exist--so always be true
            return true;
#endif
		}

		public void Remove (SecurityAction action)
		{
			m_items.Remove (action);
			SetHasSecurity (this.Count > 0);
		}

		public void CopyTo (Array ary, int index)
		{
			m_items.Values.CopyTo (ary, index);
		}

		public IEnumerator GetEnumerator ()
		{
			return m_items.Values.GetEnumerator ();
		}

		public void Accept (IReflectionVisitor visitor)
		{
			visitor.VisitSecurityDeclarationCollection (this);
		}

		private void SetHasSecurity (bool value)
		{
			TypeDefinition td = (m_container as TypeDefinition);
			if (td != null) {
				if (value)
					td.Attributes |= TypeAttributes.HasSecurity;
				else
					td.Attributes &= ~TypeAttributes.HasSecurity;
				return;
			}
			MethodDefinition md = (m_container as MethodDefinition);
			if (md != null) {
				if (value)
					md.Attributes |= MethodAttributes.HasSecurity;
				else
					md.Attributes &= ~MethodAttributes.HasSecurity;
			}
		}
	}
}
