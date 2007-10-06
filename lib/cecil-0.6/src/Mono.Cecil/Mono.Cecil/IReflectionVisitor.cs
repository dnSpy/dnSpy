//
// IReflectionVisitor.cs
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

	public interface IReflectionVisitor {

		void VisitModuleDefinition (ModuleDefinition module);
		void VisitTypeDefinitionCollection (TypeDefinitionCollection types);
		void VisitTypeDefinition (TypeDefinition type);
		void VisitTypeReferenceCollection (TypeReferenceCollection refs);
		void VisitTypeReference (TypeReference type);
		void VisitMemberReferenceCollection (MemberReferenceCollection members);
		void VisitMemberReference (MemberReference member);
		void VisitInterfaceCollection (InterfaceCollection interfaces);
		void VisitInterface (TypeReference interf);
		void VisitExternTypeCollection (ExternTypeCollection externs);
		void VisitExternType (TypeReference externType);
		void VisitOverrideCollection (OverrideCollection meth);
		void VisitOverride (MethodReference ov);
		void VisitNestedTypeCollection (NestedTypeCollection nestedTypes);
		void VisitNestedType (TypeDefinition nestedType);
		void VisitParameterDefinitionCollection (ParameterDefinitionCollection parameters);
		void VisitParameterDefinition (ParameterDefinition parameter);
		void VisitMethodDefinitionCollection (MethodDefinitionCollection methods);
		void VisitMethodDefinition (MethodDefinition method);
		void VisitConstructorCollection (ConstructorCollection ctors);
		void VisitConstructor (MethodDefinition ctor);
		void VisitPInvokeInfo (PInvokeInfo pinvk);
		void VisitEventDefinitionCollection (EventDefinitionCollection events);
		void VisitEventDefinition (EventDefinition evt);
		void VisitFieldDefinitionCollection (FieldDefinitionCollection fields);
		void VisitFieldDefinition (FieldDefinition field);
		void VisitPropertyDefinitionCollection (PropertyDefinitionCollection properties);
		void VisitPropertyDefinition (PropertyDefinition property);
		void VisitSecurityDeclarationCollection (SecurityDeclarationCollection secDecls);
		void VisitSecurityDeclaration (SecurityDeclaration secDecl);
		void VisitCustomAttributeCollection (CustomAttributeCollection customAttrs);
		void VisitCustomAttribute (CustomAttribute customAttr);
		void VisitGenericParameterCollection (GenericParameterCollection genparams);
		void VisitGenericParameter (GenericParameter genparam);
		void VisitMarshalSpec (MarshalSpec marshalSpec);

		void TerminateModuleDefinition (ModuleDefinition module);
	}
}
