// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpDevelop.Dom.VBNet
{
	public class VBNetAmbience :  AbstractAmbience
	{
		public static IDictionary<string, string> TypeConversionTable {
			get { return ICSharpCode.NRefactory.Ast.TypeReference.PrimitiveTypesVBReverse; }
		}
		
		string GetModifier(IEntity decoration)
		{
			StringBuilder builder = new StringBuilder();
			
			if (IncludeHtmlMarkup) {
				builder.Append("<i>");
			}
			
			if (decoration.IsStatic) {
				builder.Append("Shared ");
			}
			if (decoration.IsAbstract) {
				builder.Append("MustOverride ");
			} else if (decoration.IsSealed) {
				builder.Append("NotOverridable ");
			}
			if (decoration.IsVirtual) {
				builder.Append("Overridable ");
			} else if (decoration.IsOverride) {
				builder.Append("Overrides ");
			}
			if (decoration.IsNew) {
				builder.Append("Shadows ");
			}
			
			if (IncludeHtmlMarkup) {
				builder.Append("</i>");
			}
			
			return builder.ToString();
		}
		
		public override string ConvertAccessibility(ModifierEnum accessibility)
		{
			StringBuilder builder = new StringBuilder();
			if (ShowAccessibility) {
				if ((accessibility & ModifierEnum.Public) == ModifierEnum.Public) {
					builder.Append("Public");
				} else if ((accessibility & ModifierEnum.Private) == ModifierEnum.Private) {
					builder.Append("Private");
				} else if ((accessibility & (ModifierEnum.Protected | ModifierEnum.Internal)) == (ModifierEnum.Protected | ModifierEnum.Internal)) {
					builder.Append("Protected Friend");
				} else if ((accessibility & ModifierEnum.Internal) == ModifierEnum.Internal) {
					builder.Append("Friend");
				} else if ((accessibility & ModifierEnum.Protected) == ModifierEnum.Protected) {
					builder.Append("Protected");
				}
				builder.Append(' ');
			}
			return builder.ToString();
		}
		
		public override string Convert(IClass c)
		{
			CheckThread();
			
			StringBuilder builder = new StringBuilder();
			
			builder.Append(ConvertAccessibility(c.Modifiers));
			
			if (IncludeHtmlMarkup) {
				builder.Append("<i>");
			}
			
			if (ShowModifiers) {
				if (c.IsSealed) {
					if (c.ClassType == ClassType.Class) {
						builder.Append("NotInheritable ");
					}
				} else if (c.IsAbstract && c.ClassType != ClassType.Interface) {
					builder.Append("MustInherit ");
				}
			}
			
			if (IncludeHtmlMarkup) {
				builder.Append("</i>");
			}
			
			if (ShowDefinitionKeyWord) {
				switch (c.ClassType) {
					case ClassType.Delegate:
						builder.Append("Delegate ");
						if (ShowReturnType) {
							foreach (IMethod m in c.Methods) {
								if (m.Name != "Invoke") {
									continue;
								}
								
								if (m.ReturnType == null || m.ReturnType.FullyQualifiedName == "System.Void") {
									builder.Append("Sub");
								} else {
									builder.Append("Function");
								}
							}
						}
						break;
					case ClassType.Class:
						builder.Append("Class");
						break;
					case ClassType.Module:
						builder.Append("Module");
						break;
					case ClassType.Struct:
						builder.Append("Structure");
						break;
					case ClassType.Interface:
						builder.Append("Interface");
						break;
					case ClassType.Enum:
						builder.Append("Enum");
						break;
				}
				builder.Append(' ');
			}
			
			AppendClassNameWithTypeParameters(builder, c, UseFullyQualifiedMemberNames, true, null);
			
			if (ShowParameterList && c.ClassType == ClassType.Delegate) {
				builder.Append("(");
				if (IncludeHtmlMarkup) builder.Append("<br>");
				
				foreach (IMethod m in c.Methods) {
					if (m.Name != "Invoke") continue;
					
					for (int i = 0; i < m.Parameters.Count; ++i) {
						if (IncludeHtmlMarkup) builder.Append("&nbsp;&nbsp;&nbsp;");
						
						builder.Append(Convert(m.Parameters[i]));
						if (i + 1 < m.Parameters.Count) builder.Append(", ");

						if (IncludeHtmlMarkup) builder.Append("<br>");
					}
				}

				builder.Append(")");
			}
			if (ShowReturnType && c.ClassType == ClassType.Delegate) {
				foreach (IMethod m in c.Methods) {
					if (m.Name != "Invoke") continue;
					
					if (m.ReturnType == null || m.ReturnType.FullyQualifiedName == "System.Void") {
					} else {
						if (ShowReturnType) {
							builder.Append(" As ");
							builder.Append(Convert(m.ReturnType));
						}
					}
				}
			} else if (ShowInheritanceList && c.ClassType != ClassType.Delegate) {
				if (c.BaseTypes.Count > 0) {
					builder.Append(" Inherits ");
					for (int i = 0; i < c.BaseTypes.Count; ++i) {
						builder.Append(c.BaseTypes[i]);
						if (i + 1 < c.BaseTypes.Count) {
							builder.Append(", ");
						}
					}
				}
			}
			
			return builder.ToString();
		}
		
		void AppendTypeNameForFullyQualifiedMemberName(StringBuilder builder, IReturnType declaringType)
		{
			if (UseFullyQualifiedMemberNames && declaringType != null) {
				AppendReturnType(builder, declaringType, true);
				builder.Append('.');
			}
		}
		
		void AppendClassNameWithTypeParameters(StringBuilder builder, IClass c, bool fullyQualified, bool isConvertingClassName, IList<IReturnType> typeArguments)
		{
			if (isConvertingClassName && IncludeHtmlMarkup) {
				builder.Append("<b>");
			}
			if (fullyQualified) {
				if (c.DeclaringType != null) {
					AppendClassNameWithTypeParameters(builder, c.DeclaringType, fullyQualified, false, typeArguments);
					builder.Append('.');
					builder.Append(c.Name);
				} else {
					builder.Append(c.FullyQualifiedName);
				}
			} else {
				builder.Append(c.Name);
			}
			if (isConvertingClassName && IncludeHtmlMarkup) {
				builder.Append("</b>");
			}
			// skip type parameters that belong to declaring types (in DOM, inner classes repeat type parameters from outer classes)
			int skippedTypeParameterCount = c.DeclaringType != null ? c.DeclaringType.TypeParameters.Count : 0;
			// show type parameters for classes only if ShowTypeParameterList is set; but always show them in other cases.
			if ((ShowTypeParameterList || !isConvertingClassName) && c.TypeParameters.Count > skippedTypeParameterCount) {
				builder.Append("(Of ");
				for (int i = skippedTypeParameterCount; i < c.TypeParameters.Count; ++i) {
					if (i > skippedTypeParameterCount)
						builder.Append(", ");
					if (typeArguments != null && i < typeArguments.Count)
						AppendReturnType(builder, typeArguments[i], false);
					else
						builder.Append(ConvertTypeParameter(c.TypeParameters[i]));
				}
				builder.Append(')');
			}
		}
		
		public override string ConvertEnd(IClass c)
		{
			if (c == null)
				throw new ArgumentNullException("c");
			
			StringBuilder builder = new StringBuilder();
			
			builder.Append("End ");
			
			switch (c.ClassType) {
				case ClassType.Delegate:
					builder.Append("Delegate");
					break;
				case ClassType.Class:
					builder.Append("Class");
					break;
				case ClassType.Module:
					builder.Append("Module");
					break;
				case ClassType.Struct:
					builder.Append("Structure");
					break;
				case ClassType.Interface:
					builder.Append("Interface");
					break;
				case ClassType.Enum:
					builder.Append("Enum");
					break;
			}
			
			return builder.ToString();
		}
		
		public override string Convert(IField field)
		{
			CheckThread();
			
			if (field == null)
				throw new ArgumentNullException("field");
			
			StringBuilder builder = new StringBuilder();
			
			builder.Append(ConvertAccessibility(field.Modifiers));
			
			if (IncludeHtmlMarkup) {
				builder.Append("<i>");
			}
			
			if (ShowModifiers) {
				if (field.IsConst) {
					builder.Append("Const ");
				} else if (field.IsStatic) {
					builder.Append("Shared ");
				}
			}
			
			if (IncludeHtmlMarkup) {
				builder.Append("</i>");
			}
			
			AppendTypeNameForFullyQualifiedMemberName(builder, field.DeclaringTypeReference);
			
			if (IncludeHtmlMarkup) {
				builder.Append("<b>");
			}
			
			builder.Append(field.Name);
			
			if (IncludeHtmlMarkup) {
				builder.Append("</b>");
			}
			
			if (field.ReturnType != null && ShowReturnType) {
				builder.Append(" As ");
				builder.Append(Convert(field.ReturnType));
			}
			
			return builder.ToString();
		}
		
		public override string Convert(IProperty property)
		{
			CheckThread();
			
			StringBuilder builder = new StringBuilder();
			
			builder.Append(ConvertAccessibility(property.Modifiers));
			
			if (ShowModifiers) {
				builder.Append(GetModifier(property));
				
				if (property.IsIndexer) {
					builder.Append("Default ");
				}
				
				if (property.CanGet && !property.CanSet) {
					builder.Append("ReadOnly ");
				}
				if (property.CanSet && !property.CanGet) {
					builder.Append("WriteOnly ");
				}
			}
			
			if (ShowDefinitionKeyWord) {
				builder.Append("Property ");
			}
			
			AppendTypeNameForFullyQualifiedMemberName(builder, property.DeclaringTypeReference);
			if (IncludeHtmlMarkup) {
				builder.Append("<b>");
			}
			
			builder.Append(property.Name);
			
			if (IncludeHtmlMarkup) {
				builder.Append("</b>");
			}
			
			if (ShowParameterList && property.Parameters.Count > 0) {
				builder.Append("(");
				if (IncludeHtmlMarkup) builder.Append("<br>");
				
				for (int i = 0; i < property.Parameters.Count; ++i) {
					if (IncludeHtmlMarkup) builder.Append("&nbsp;&nbsp;&nbsp;");
					builder.Append(Convert(property.Parameters[i]));
					if (i + 1 < property.Parameters.Count) {
						builder.Append(", ");
					}
					if (IncludeHtmlMarkup) builder.Append("<br>");
				}
				
				builder.Append(')');
			}
			
			if (property.ReturnType != null && ShowReturnType) {
				builder.Append(" As ");
				builder.Append(Convert(property.ReturnType));
			}
			
			return builder.ToString();
		}
		
		public override string Convert(IEvent e)
		{
			CheckThread();
			
			if (e == null)
				throw new ArgumentNullException("e");
			
			StringBuilder builder = new StringBuilder();
			
			builder.Append(ConvertAccessibility(e.Modifiers));
			
			if (ShowModifiers) {
				builder.Append(GetModifier(e));
			}
			
			if (ShowDefinitionKeyWord) {
				builder.Append("Event ");
			}
			
			AppendTypeNameForFullyQualifiedMemberName(builder, e.DeclaringTypeReference);
			
			if (IncludeHtmlMarkup) {
				builder.Append("<b>");
			}
			
			builder.Append(e.Name);
			
			if (IncludeHtmlMarkup) {
				builder.Append("</b>");
			}
			
			if (e.ReturnType != null && ShowReturnType) {
				builder.Append(" As ");
				builder.Append(Convert(e.ReturnType));
			}
			
			return builder.ToString();
		}
		
		public override string Convert(IMethod m)
		{
			CheckThread();
			
			StringBuilder builder = new StringBuilder();
			if (ShowModifiers && m.IsExtensionMethod) {
				builder.Append("<Extension> ");
			}
			
			builder.Append(ConvertAccessibility(m.Modifiers)); // show visibility
			
			if (ShowModifiers) {
				builder.Append(GetModifier(m));
			}
			if (ShowDefinitionKeyWord) {
				if (m.ReturnType == null || m.ReturnType.FullyQualifiedName == "System.Void") {
					builder.Append("Sub ");
				} else {
					builder.Append("Function ");
				}
			}

			AppendTypeNameForFullyQualifiedMemberName(builder, m.DeclaringTypeReference);
			
			if (IncludeHtmlMarkup) {
				builder.Append("<b>");
			}
			
			builder.Append(m.IsConstructor ? "New" : m.Name);
			
			if (IncludeHtmlMarkup) {
				builder.Append("</b>");
			}
			
			if (ShowTypeParameterList && m.TypeParameters.Count > 0) {
				builder.Append("(Of ");
				for (int i = 0; i < m.TypeParameters.Count; ++i) {
					if (i > 0) builder.Append(", ");
					builder.Append(ConvertTypeParameter(m.TypeParameters[i]));
				}
				builder.Append(')');
			}
			
			if (ShowParameterList) {
				builder.Append("(");
				if (IncludeHtmlMarkup) builder.Append("<br>");

				for (int i = 0; i < m.Parameters.Count; ++i) {
					if (IncludeHtmlMarkup) builder.Append("&nbsp;&nbsp;&nbsp;");
					builder.Append(Convert(m.Parameters[i]));
					if (i + 1 < m.Parameters.Count) {
						builder.Append(", ");
					}
					if (IncludeHtmlMarkup) builder.Append("<br>");
				}
				
				builder.Append(')');
			}
			
			if (ShowReturnType && m.ReturnType != null && m.ReturnType.FullyQualifiedName != "System.Void") {
				builder.Append(" As ");
				builder.Append(Convert(m.ReturnType));
			}
			
			return builder.ToString();
		}
		
		string ConvertTypeParameter(ITypeParameter tp)
		{
			if (tp.BoundTo != null)
				return Convert(tp.BoundTo);
			else
				return tp.Name;
		}
		
		public override string ConvertEnd(IMethod m)
		{
			if (m == null)
				throw new ArgumentNullException("m");
			
			if (m.ReturnType == null || m.ReturnType.FullyQualifiedName == "System.Void") {
				return "End Sub";
			} else {
				return "End Function";
			}
		}
		
		public override string Convert(IReturnType returnType)
		{
			CheckThread();
			
			if (returnType == null) {
				return String.Empty;
			}
			
			returnType = returnType.GetDirectReturnType();
			
			StringBuilder builder = new StringBuilder();
			
			AppendReturnType(builder, returnType, false);
			
			return builder.ToString();
		}
		
		void AppendReturnType(StringBuilder builder, IReturnType returnType, bool forceFullyQualifiedName)
		{
			IReturnType arrayReturnType = returnType;
			returnType = GetElementType(returnType);
			
			if (returnType == null)
				return;
			
			string fullName = returnType.FullyQualifiedName;
			string shortName;
			bool isConstructedType = returnType.IsConstructedReturnType;
			if (fullName != null && !isConstructedType && TypeConversionTable.TryGetValue(fullName, out shortName)) {
				builder.Append(shortName);
			} else {
				IClass c = returnType.GetUnderlyingClass();
				
				if (c != null) {
					IList<IReturnType> ta = isConstructedType ? returnType.CastToConstructedReturnType().TypeArguments : null;
					AppendClassNameWithTypeParameters(builder, c, forceFullyQualifiedName || UseFullyQualifiedTypeNames, false, ta);
				} else {
					if (UseFullyQualifiedTypeNames || forceFullyQualifiedName) {
						builder.Append(fullName);
					} else {
						builder.Append(returnType.Name);
					}
					if (isConstructedType) {
						builder.Append("(Of ");
						IList<IReturnType> ta = returnType.CastToConstructedReturnType().TypeArguments;
						for (int i = 0; i < ta.Count; ++i) {
							if (i > 0) builder.Append(", ");
							AppendReturnType(builder, ta[i], false);
						}
						builder.Append(')');
					}
				}
			}
			
			UnpackArrayType(builder, arrayReturnType);
		}
		
		static IReturnType GetElementType(IReturnType potentialArrayType)
		{
			if (potentialArrayType == null)
				return null;
			ArrayReturnType result;
			while ((result = potentialArrayType.CastToArrayReturnType()) != null) {
				potentialArrayType = result.ArrayElementType;
			}
			return potentialArrayType;
		}
		
		static void UnpackArrayType(StringBuilder builder, IReturnType returnType)
		{
			if (returnType.IsArrayReturnType) {
				builder.Append('(');
				int dimensions = returnType.CastToArrayReturnType().ArrayDimensions;
				for (int i = 1; i < dimensions; ++i) {
					builder.Append(',');
				}
				builder.Append(')');
				UnpackArrayType(builder, returnType.CastToArrayReturnType().ArrayElementType);
			}
		}
		
		public override string Convert(IParameter param)
		{
			CheckThread();
			
			if (param == null)
				throw new ArgumentNullException("param");
			
			StringBuilder builder = new StringBuilder();
			if (IncludeHtmlMarkup) {
				builder.Append("<i>");
			}
			
			if (param.IsOptional) {
				builder.Append("Optional ");
			}
			if (param.IsRef || param.IsOut) {
				builder.Append("ByRef ");
			} else if (param.IsParams) {
				builder.Append("ParamArray ");
			}
			if (IncludeHtmlMarkup) {
				builder.Append("</i>");
			}
			
			if (ShowParameterNames) {
				builder.Append(param.Name);
				builder.Append(" As ");
			}

			builder.Append(Convert(param.ReturnType));
			
			return builder.ToString();
		}

		public override string WrapAttribute(string attribute)
		{
			return "<" + attribute + ">";
		}
		
		public override string WrapComment(string comment)
		{
			return "' " + comment;
		}
		
		public override string GetIntrinsicTypeName(string dotNetTypeName)
		{
			string shortName;
			if (TypeConversionTable.TryGetValue(dotNetTypeName, out shortName)) {
				return shortName;
			}
			return dotNetTypeName;
		}
	}
	
}
