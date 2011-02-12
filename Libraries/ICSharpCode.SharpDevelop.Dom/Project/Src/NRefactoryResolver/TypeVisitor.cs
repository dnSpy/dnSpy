// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

// created on 22.08.2003 at 19:02

using System;
using System.Collections.Generic;
using System.Text;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace ICSharpCode.SharpDevelop.Dom.NRefactoryResolver
{
	// TODO: Rename this class, the visitor functionality was moved to ResolveVisitor
	public static class TypeVisitor
	{
		[Flags]
		public enum ReturnTypeOptions
		{
			None = 0,
			Lazy = 1,
			BaseTypeReference = 2
		}
		
		public static IReturnType CreateReturnType(TypeReference reference, NRefactoryResolver resolver)
		{
			return CreateReturnType(reference,
			                        resolver.CallingClass, resolver.CallingMember,
			                        resolver.CaretLine, resolver.CaretColumn,
			                        resolver.ProjectContent, ReturnTypeOptions.None);
		}
		
		public static IReturnType CreateReturnType(TypeReference reference, IClass callingClass,
		                                           IMember callingMember, int caretLine, int caretColumn,
		                                           IProjectContent projectContent,
		                                           ReturnTypeOptions options)
		{
			if (reference == null) return null;
			if (reference.IsNull) return null;
			if (reference is InnerClassTypeReference) {
				reference = ((InnerClassTypeReference)reference).CombineToNormalTypeReference();
			}
			
			bool useLazyReturnType = (options & ReturnTypeOptions.Lazy) == ReturnTypeOptions.Lazy;
			bool isBaseTypeReference = (options & ReturnTypeOptions.BaseTypeReference) == ReturnTypeOptions.BaseTypeReference;
			
			LanguageProperties languageProperties = projectContent.Language;
			IReturnType t = null;
			if (callingClass != null && !reference.IsGlobal) {
				foreach (ITypeParameter tp in callingClass.TypeParameters) {
					if (languageProperties.NameComparer.Equals(tp.Name, reference.Type)) {
						t = new GenericReturnType(tp);
						break;
					}
				}
				IMethod callingMethod = callingMember as IMethod;
				if (t == null && callingMethod != null) {
					foreach (ITypeParameter tp in callingMethod.TypeParameters) {
						if (languageProperties.NameComparer.Equals(tp.Name, reference.Type)) {
							t = new GenericReturnType(tp);
							break;
						}
					}
				}
			}
			if (t == null && reference.Type == "dynamic") {
				t = new DynamicReturnType(projectContent);
			}
			if (t == null) {
				int typeParameterCount = reference.GenericTypes.Count;
				if (reference.IsKeyword) {
					// keyword-type like void, int, string etc.
					IClass c = projectContent.GetClass(reference.Type, typeParameterCount);
					if (c != null)
						t = c.DefaultReturnType;
					else
						t = new GetClassReturnType(projectContent, reference.Type, typeParameterCount);
				} else {
					if (useLazyReturnType || isBaseTypeReference) {
						if (reference.IsGlobal) {
							t = new GetClassReturnType(projectContent, reference.Type, typeParameterCount);
						} else if (callingClass != null) {
							SearchClassReturnType scrt = new SearchClassReturnType(projectContent, callingClass, caretLine, caretColumn, reference.Type, typeParameterCount);
							if (isBaseTypeReference)
								scrt.LookForInnerClassesInDeclaringClass = false;
							t = scrt;
						}
					} else {
						IClass c;
						if (reference.IsGlobal) {
							c = projectContent.GetClass(reference.Type, typeParameterCount);
							t = (c != null) ? c.DefaultReturnType : null;
						} else if (callingClass != null) {
							t = projectContent.SearchType(new SearchTypeRequest(reference.Type, typeParameterCount, callingClass, caretLine, caretColumn)).Result;
						}
						if (t == null) {
							return null;
						}
					}
				}
			}
			if (reference.GenericTypes.Count > 0) {
				IReturnType[] para = new IReturnType[reference.GenericTypes.Count];
				for (int i = 0; i < reference.GenericTypes.Count; ++i) {
					para[i] = CreateReturnType(reference.GenericTypes[i], callingClass, callingMember, caretLine, caretColumn, projectContent, options);
				}
				t = new ConstructedReturnType(t, para);
			}
			for (int i = 0; i < reference.PointerNestingLevel; i++) {
				t = new PointerReturnType(t);
			}
			return WrapArray(projectContent, t, reference);
		}
		
		static IReturnType WrapArray(IProjectContent pc, IReturnType t, TypeReference reference)
		{
			if (reference.IsArrayType) {
				for (int i = reference.RankSpecifier.Length - 1; i >= 0; --i) {
					int dimensions = reference.RankSpecifier[i] + 1;
					if (dimensions > 0) {
						t = new ArrayReturnType(pc, t, dimensions);
					}
				}
			}
			return t;
		}
	}
}
