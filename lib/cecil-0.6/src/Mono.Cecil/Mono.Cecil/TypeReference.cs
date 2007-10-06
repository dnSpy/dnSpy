//
// TypeReference.cs
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

	public class TypeReference : MemberReference, IGenericParameterProvider, ICustomAttributeProvider {

		string m_namespace;
		bool m_fullNameDiscarded;
		string m_fullName;
		protected bool m_isValueType;
		IMetadataScope m_scope;
		ModuleDefinition m_module;

		CustomAttributeCollection m_customAttrs;
		GenericParameterCollection m_genparams;

		public override string Name {
			get { return base.Name; }
			set {
				base.Name = value;
				m_fullNameDiscarded = true;
			}
		}

		public virtual string Namespace {
			get { return m_namespace; }
			set {
				m_namespace = value;
				m_fullNameDiscarded = true;
			}
		}

		public virtual bool IsValueType {
			get { return m_isValueType; }
			set { m_isValueType = value; }
		}

		public virtual ModuleDefinition Module {
			get { return m_module; }
			set { m_module = value; }
		}

		public CustomAttributeCollection CustomAttributes {
			get {
				if (m_customAttrs == null)
					m_customAttrs = new CustomAttributeCollection (this);

				return m_customAttrs;
			}
		}

		public GenericParameterCollection GenericParameters {
			get {
				if (m_genparams == null)
					m_genparams = new GenericParameterCollection (this);
				return m_genparams;
			}
		}

		public virtual IMetadataScope Scope {
			get {
				if (this.DeclaringType != null)
					return this.DeclaringType.Scope;

				return m_scope;
			}
		}

		public virtual string FullName {
			get {
				if (m_fullName != null && !m_fullNameDiscarded)
					return m_fullName;

				if (this.DeclaringType != null)
					return string.Concat (this.DeclaringType.FullName, "/", this.Name);

				if (m_namespace == null || m_namespace.Length == 0)
					return this.Name;

				m_fullName = string.Concat (m_namespace, ".", this.Name);
				m_fullNameDiscarded = false;
				return m_fullName;
			}
		}

		protected TypeReference (string name, string ns) : base (name)
		{
			m_namespace = ns;
			m_fullNameDiscarded = false;
		}

		internal TypeReference (string name, string ns, IMetadataScope scope) : this (name, ns)
		{
			m_scope = scope;
		}

		public TypeReference (string name, string ns, IMetadataScope scope, bool valueType) :
			this (name, ns, scope)
		{
			this.IsValueType = valueType;
		}

		public virtual TypeReference GetOriginalType ()
		{
			return this;
		}

		internal void AttachToScope (IMetadataScope scope)
		{
			m_scope = scope;
		}

		public override void Accept (IReflectionVisitor visitor)
		{
			visitor.VisitTypeReference (this);
		}

		public override string ToString ()
		{
			return this.FullName;
		}
	}
}
