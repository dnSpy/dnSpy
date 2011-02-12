// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ICSharpCode.SharpDevelop.Dom.CSharp
{
	public class CSharpAmbience : AbstractAmbience
	{
		public static IDictionary<string, string> TypeConversionTable {
			get { return ICSharpCode.NRefactory.Ast.TypeReference.PrimitiveTypesCSharpReverse; }
		}
		
		bool ModifierIsSet(ModifierEnum modifier, ModifierEnum query)
		{
			return (modifier & query) == query;
		}
		
		public override string ConvertAccessibility(ModifierEnum accessibility)
		{
			if (ShowAccessibility) {
				if (ModifierIsSet(accessibility, ModifierEnum.Public)) {
					return "public ";
				} else if (ModifierIsSet(accessibility, ModifierEnum.Private)) {
					return "private ";
				} else if (ModifierIsSet(accessibility, ModifierEnum.ProtectedAndInternal)) {
					return "protected internal ";
				} else if (ModifierIsSet(accessibility, ModifierEnum.Internal)) {
					return "internal ";
				} else if (ModifierIsSet(accessibility, ModifierEnum.Protected)) {
					return "protected ";
				}
			}
			
			return string.Empty;
		}
		
		string GetModifier(IEntity decoration)
		{
			string ret = "";
			
			if (IncludeHtmlMarkup) {
				ret += "<i>";
			}
			
			if (decoration.IsStatic) {
				ret += "static ";
			} else if (decoration.IsSealed) {
				ret += "sealed ";
			} else if (decoration.IsVirtual) {
				ret += "virtual ";
			} else if (decoration.IsOverride) {
				ret += "override ";
			} else if (decoration.IsNew) {
				ret += "new ";
			}
			
			if (IncludeHtmlMarkup) {
				ret += "</i>";
			}
			
			return ret;
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
				if (c.IsStatic) {
					builder.Append("static ");
				} else if (c.IsSealed) {
					switch (c.ClassType) {
						case ClassType.Delegate:
						case ClassType.Struct:
						case ClassType.Enum:
							break;
							
						default:
							builder.Append("sealed ");
							break;
					}
				} else if (c.IsAbstract && c.ClassType != ClassType.Interface) {
					builder.Append("abstract ");
				}
				#if DEBUG
				if (c.HasCompoundClass)
					builder.Append("multiple_parts ");
				if (c is CompoundClass) {
					builder.Append("compound{");
					builder.Append(string.Join(",", (c as CompoundClass).Parts.Select(p => p.CompilationUnit.FileName).ToArray()));
					builder.Append("} ");
				}
				#endif
			}
			
			if (IncludeHtmlMarkup) {
				builder.Append("</i>");
			}
			
			if (ShowDefinitionKeyWord) {
				switch (c.ClassType) {
					case ClassType.Delegate:
						builder.Append("delegate");
						break;
					case ClassType.Class:
					case ClassType.Module:
						builder.Append("class");
						break;
					case ClassType.Struct:
						builder.Append("struct");
						break;
					case ClassType.Interface:
						builder.Append("interface");
						break;
					case ClassType.Enum:
						builder.Append("enum");
						break;
				}
				builder.Append(' ');
			}
			if (ShowReturnType && c.ClassType == ClassType.Delegate) {
				foreach(IMethod m in c.Methods) {
					if (m.Name != "Invoke") continue;
					
					builder.Append(Convert(m.ReturnType));
					builder.Append(' ');
				}
			}
			
			AppendClassNameWithTypeParameters(builder, c, UseFullyQualifiedMemberNames, true, null);
			
			if (ShowParameterList && c.ClassType == ClassType.Delegate) {
				builder.Append(" (");
				if (IncludeHtmlMarkup) builder.Append("<br>");
				
				foreach(IMethod m in c.Methods) {
					if (m.Name != "Invoke") continue;
					
					for (int i = 0; i < m.Parameters.Count; ++i) {
						if (IncludeHtmlMarkup) builder.Append("&nbsp;&nbsp;&nbsp;");
						
						builder.Append(Convert(m.Parameters[i]));
						if (i + 1 < m.Parameters.Count) builder.Append(", ");
						
						if (IncludeHtmlMarkup) builder.Append("<br>");
					}
				}
				builder.Append(')');
				
			} else if (ShowInheritanceList) {
				if (c.BaseTypes.Count > 0) {
					builder.Append(" : ");
					for (int i = 0; i < c.BaseTypes.Count; ++i) {
						builder.Append(c.BaseTypes[i]);
						if (i + 1 < c.BaseTypes.Count) {
							builder.Append(", ");
						}
					}
				}
			}
			
			if (IncludeBody) {
				builder.Append("\n{");
			}
			
			return builder.ToString();
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
				builder.Append('<');
				for (int i = skippedTypeParameterCount; i < c.TypeParameters.Count; ++i) {
					if (i > skippedTypeParameterCount)
						builder.Append(", ");
					if (typeArguments != null && i < typeArguments.Count)
						AppendReturnType(builder, typeArguments[i], false);
					else
						builder.Append(ConvertTypeParameter(c.TypeParameters[i]));
				}
				builder.Append('>');
			}
		}
		
		public override string ConvertEnd(IClass c)
		{
			return "}";
		}
		
		public override string Convert(IField field)
		{
			CheckThread();
			
			StringBuilder builder = new StringBuilder();
			
			builder.Append(ConvertAccessibility(field.Modifiers));
			
			if (IncludeHtmlMarkup) {
				builder.Append("<i>");
			}
			
			if (ShowModifiers) {
				if (field.IsConst) {
					builder.Append("const ");
				} else if (field.IsStatic) {
					builder.Append("static ");
				}
				
				if (field.IsNew) {
					builder.Append("new ");
				}
				if (field.IsReadonly) {
					builder.Append("readonly ");
				}
				if ((field.Modifiers & ModifierEnum.Volatile) == ModifierEnum.Volatile) {
					builder.Append("volatile ");
				}
			}
			
			if (IncludeHtmlMarkup) {
				builder.Append("</i>");
			}
			
			if (field.ReturnType != null && ShowReturnType) {
				builder.Append(Convert(field.ReturnType));
				builder.Append(' ');
			}
			
			AppendTypeNameForFullyQualifiedMemberName(builder, field.DeclaringTypeReference);
			
			if (IncludeHtmlMarkup) {
				builder.Append("<b>");
			}
			
			builder.Append(field.Name);
			
			if (IncludeHtmlMarkup) {
				builder.Append("</b>");
			}
			
			if (IncludeBody) builder.Append(";");
			
			return builder.ToString();
		}
		
		public override string Convert(IProperty property)
		{
			CheckThread();
			
			StringBuilder builder = new StringBuilder();
			
			builder.Append(ConvertAccessibility(property.Modifiers));
			
			if (ShowModifiers) {
				builder.Append(GetModifier(property));
			}
			
			if (property.ReturnType != null && ShowReturnType) {
				builder.Append(Convert(property.ReturnType));
				builder.Append(' ');
			}
			
			AppendTypeNameForFullyQualifiedMemberName(builder, property.DeclaringTypeReference);
			
			if (property.IsIndexer) {
				builder.Append("this");
			} else {
				if (IncludeHtmlMarkup) {
					builder.Append("<b>");
				}
				builder.Append(property.Name);
				if (IncludeHtmlMarkup) {
					builder.Append("</b>");
				}
			}
			
			if (property.Parameters.Count > 0 && ShowParameterList) {
				builder.Append(property.IsIndexer ? '[' : '(');
				if (IncludeHtmlMarkup) builder.Append("<br>");
				
				for (int i = 0; i < property.Parameters.Count; ++i) {
					if (IncludeHtmlMarkup) builder.Append("&nbsp;&nbsp;&nbsp;");
					builder.Append(Convert(property.Parameters[i]));
					if (i + 1 < property.Parameters.Count) {
						builder.Append(", ");
					}
					if (IncludeHtmlMarkup) builder.Append("<br>");
				}
				
				builder.Append(property.IsIndexer ? ']' : ')');
			}
			
			if (IncludeBody) {
				builder.Append(" { ");
				
				if (property.CanGet) {
					builder.Append("get; ");
				}
				if (property.CanSet) {
					builder.Append("set; ");
				}
				
				builder.Append(" } ");
			}
			
			return builder.ToString();
		}
		
		public override string Convert(IEvent e)
		{
			CheckThread();
			
			StringBuilder builder = new StringBuilder();
			
			builder.Append(ConvertAccessibility(e.Modifiers));
			
			if (ShowModifiers) {
				builder.Append(GetModifier(e));
			}
			
			if (ShowDefinitionKeyWord) {
				builder.Append("event ");
			}
			
			if (e.ReturnType != null && ShowReturnType) {
				builder.Append(Convert(e.ReturnType));
				builder.Append(' ');
			}
			
			AppendTypeNameForFullyQualifiedMemberName(builder, e.DeclaringTypeReference);
			
			if (IncludeHtmlMarkup) {
				builder.Append("<b>");
			}
			
			builder.Append(e.Name);
			
			if (IncludeHtmlMarkup) {
				builder.Append("</b>");
			}
			
			if (IncludeBody) builder.Append(";");
			
			return builder.ToString();
		}
		
		public override string Convert(IMethod m)
		{
			CheckThread();
			
			StringBuilder builder = new StringBuilder();
			builder.Append(ConvertAccessibility(m.Modifiers));
			
			if (ShowModifiers) {
				builder.Append(GetModifier(m));
			}
			
			if (!m.IsConstructor && m.ReturnType != null && ShowReturnType) {
				builder.Append(Convert(m.ReturnType));
				builder.Append(' ');
			}
			
			AppendTypeNameForFullyQualifiedMemberName(builder, m.DeclaringTypeReference);
			
			if (IncludeHtmlMarkup) {
				builder.Append("<b>");
			}
			
			if (m.IsConstructor && m.DeclaringType != null) {
				builder.Append(m.DeclaringType.Name);
			} else {
				builder.Append(m.Name);
			}
			
			if (IncludeHtmlMarkup) {
				builder.Append("</b>");
			}
			
			if (ShowTypeParameterList && m.TypeParameters.Count > 0) {
				builder.Append('<');
				for (int i = 0; i < m.TypeParameters.Count; ++i) {
					if (i > 0) builder.Append(", ");
					builder.Append(ConvertTypeParameter(m.TypeParameters[i]));
				}
				builder.Append('>');
			}
			
			if (ShowParameterList) {
				builder.Append("(");
				if (IncludeHtmlMarkup) builder.Append("<br>");
				
				if (m.IsExtensionMethod) builder.Append("this ");
				
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
			
			if (IncludeBody) {
				if (m.DeclaringType != null) {
					if (m.DeclaringType.ClassType == ClassType.Interface) {
						builder.Append(";");
					} else {
						builder.Append(" {");
					}
				} else {
					builder.Append(" {");
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
		
		string ConvertTypeParameter(ITypeParameter tp)
		{
			if (tp.BoundTo != null)
				return Convert(tp.BoundTo);
			else
				return tp.Name;
		}
		
		public override string ConvertEnd(IMethod m)
		{
			return "}";
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
						builder.Append('<');
						IList<IReturnType> ta = returnType.CastToConstructedReturnType().TypeArguments;
						for (int i = 0; i < ta.Count; ++i) {
							if (i > 0) builder.Append(", ");
							AppendReturnType(builder, ta[i], false);
						}
						builder.Append('>');
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
				builder.Append('[');
				int dimensions = returnType.CastToArrayReturnType().ArrayDimensions;
				for (int i = 1; i < dimensions; ++i) {
					builder.Append(',');
				}
				builder.Append(']');
				UnpackArrayType(builder, returnType.CastToArrayReturnType().ArrayElementType);
			}
		}
		
		public override string Convert(IParameter param)
		{
			CheckThread();
			
			StringBuilder builder = new StringBuilder();
			
			if (IncludeHtmlMarkup) {
				builder.Append("<i>");
			}
			
			if (param.IsRef) {
				builder.Append("ref ");
			} else if (param.IsOut) {
				builder.Append("out ");
			} else if (param.IsParams) {
				builder.Append("params ");
			}
			
			if (IncludeHtmlMarkup) {
				builder.Append("</i>");
			}
			
			builder.Append(Convert(param.ReturnType));
			
			if (ShowParameterNames) {
				builder.Append(' ');
				builder.Append(param.Name);
			}
			return builder.ToString();
		}
		
		public override string WrapAttribute(string attribute)
		{
			return "[" + attribute + "]";
		}
		
		public override string WrapComment(string comment)
		{
			return "// " + comment;
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
