//
// BaseReflectionVisitor.cs
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

	public abstract class BaseReflectionVisitor : IReflectionVisitor {

		public virtual void VisitModuleDefinition (ModuleDefinition module)
		{
		}

		public virtual void VisitTypeDefinitionCollection (TypeDefinitionCollection types)
		{
		}

		public virtual void VisitTypeDefinition (TypeDefinition type)
		{
		}

		public virtual void VisitTypeReferenceCollection (TypeReferenceCollection refs)
		{
		}

		public virtual void VisitTypeReference (TypeReference type)
		{
		}

		public virtual void VisitMemberReferenceCollection (MemberReferenceCollection members)
		{
		}

		public virtual void VisitMemberReference (MemberReference member)
		{
		}

		public virtual void VisitInterfaceCollection (InterfaceCollection interfaces)
		{
		}

		public virtual void VisitInterface (TypeReference interf)
		{
		}

		public virtual void VisitExternTypeCollection (ExternTypeCollection externs)
		{
		}

		public virtual void VisitExternType (TypeReference externType)
		{
		}

		public virtual void VisitOverrideCollection (OverrideCollection meth)
		{
		}

		public virtual void VisitOverride (MethodReference ov)
		{
		}

		public virtual void VisitNestedTypeCollection (NestedTypeCollection nestedTypes)
		{
		}

		public virtual void VisitNestedType (TypeDefinition nestedType)
		{
		}

		public virtual void VisitParameterDefinitionCollection (ParameterDefinitionCollection parameters)
		{
		}

		public virtual void VisitParameterDefinition (ParameterDefinition parameter)
		{
		}

		public virtual void VisitMethodDefinitionCollection (MethodDefinitionCollection methods)
		{
		}

		public virtual void VisitMethodDefinition (MethodDefinition method)
		{
		}

		public virtual void VisitConstructorCollection (ConstructorCollection ctors)
		{
		}

		public virtual void VisitConstructor (MethodDefinition ctor)
		{
		}

		public virtual void VisitPInvokeInfo (PInvokeInfo pinvk)
		{
		}

		public virtual void VisitEventDefinitionCollection (EventDefinitionCollection events)
		{
		}

		public virtual void VisitEventDefinition (EventDefinition evt)
		{
		}

		public virtual void VisitFieldDefinitionCollection (FieldDefinitionCollection fields)
		{
		}

		public virtual void VisitFieldDefinition (FieldDefinition field)
		{
		}

		public virtual void VisitPropertyDefinitionCollection (PropertyDefinitionCollection properties)
		{
		}

		public virtual void VisitPropertyDefinition (PropertyDefinition property)
		{
		}

		public virtual void VisitSecurityDeclarationCollection (SecurityDeclarationCollection secDecls)
		{
		}

		public virtual void VisitSecurityDeclaration (SecurityDeclaration secDecl)
		{
		}

		public virtual void VisitCustomAttributeCollection (CustomAttributeCollection customAttrs)
		{
		}

		public virtual void VisitCustomAttribute (CustomAttribute customAttr)
		{
		}

		public virtual void VisitGenericParameterCollection (GenericParameterCollection genparams)
		{
		}

		public virtual void VisitGenericParameter (GenericParameter genparam)
		{
		}

		public virtual void VisitMarshalSpec (MarshalSpec marshalSpec)
		{
		}

		public virtual void TerminateModuleDefinition (ModuleDefinition module)
		{
		}

		protected void VisitCollection (ICollection coll)
		{
			if (coll.Count == 0)
				return;

			foreach (IReflectionVisitable visitable in coll)
				visitable.Accept (this);
		}
	}
}
