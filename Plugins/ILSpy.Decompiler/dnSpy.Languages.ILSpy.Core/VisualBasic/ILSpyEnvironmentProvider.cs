// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnSpy.Decompiler.Shared;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.VB.Visitors;

namespace dnSpy.Languages.ILSpy.VisualBasic {
	sealed class ILSpyEnvironmentProvider : IEnvironmentProvider {
		public string RootNamespace => "";

		readonly StringBuilder sb;

		public ILSpyEnvironmentProvider(StringBuilder sb = null) {
			this.sb = sb ?? new StringBuilder();
		}

		public string GetTypeNameForAttribute(ICSharpCode.NRefactory.CSharp.Attribute attribute) {
			var mr = attribute.Type.Annotations
				.OfType<IMemberRef>()
				.FirstOrDefault();
			return mr == null ? string.Empty : mr.FullName;
		}

		/*
var annotation = type.Annotation<TypeReference>();
if (annotation == null )
	return null;

IEntity current = null;
if (entity != null) {
	var typeInfo = entity.Annotation<TypeReference>();
	current = loader.ReadTypeReference(typeInfo).Resolve(context).GetDefinition();
}

return loader.ReadTypeReference(annotation, entity: current).Resolve(context);*/
		public ICSharpCode.NRefactory.TypeSystem.IType ResolveType(ICSharpCode.NRefactory.VB.Ast.AstType type, ICSharpCode.NRefactory.VB.Ast.TypeDeclaration entity = null) => SpecialType.UnknownType;

		public TypeKind GetTypeKindForAstType(ICSharpCode.NRefactory.CSharp.AstType type) {
			var annotation = type.Annotation<ITypeDefOrRef>();
			if (annotation == null)
				return TypeKind.Unknown;

			var definition = annotation.ResolveTypeDef();
			if (definition == null)
				return TypeKind.Unknown;
			if (definition.IsClass)
				return TypeKind.Class;
			if (definition.IsInterface)
				return TypeKind.Interface;
			if (definition.IsEnum)
				return TypeKind.Enum;
			if (definition.IsValueType)
				return TypeKind.Struct;

			return TypeKind.Unknown;
		}

		public TypeCode ResolveExpression(ICSharpCode.NRefactory.CSharp.Expression expression) {
			var annotation = expression.Annotations.OfType<TypeInformation>().FirstOrDefault();

			if (annotation == null || annotation.InferredType == null)
				return TypeCode.Object;

			var definition = annotation.InferredType.ScopeType.ResolveTypeDef();

			if (definition == null)
				return TypeCode.Object;

			switch (definition.FullName) {
			case "System.String":
				return TypeCode.String;
			default:
				break;
			}

			return TypeCode.Object;
		}

		public bool? IsReferenceType(ICSharpCode.NRefactory.CSharp.Expression expression) {
			if (expression is ICSharpCode.NRefactory.CSharp.NullReferenceExpression)
				return true;

			var annotation = expression.Annotations.OfType<TypeInformation>().FirstOrDefault();

			if (annotation == null || annotation.InferredType == null)
				return null;

			var definition = annotation.InferredType.ScopeType.ResolveTypeDef();

			if (definition == null)
				return null;

			return !definition.IsValueType;
		}

		public IEnumerable<ICSharpCode.NRefactory.VB.Ast.InterfaceMemberSpecifier> CreateMemberSpecifiersForInterfaces(IEnumerable<ICSharpCode.NRefactory.VB.Ast.AstType> interfaces) {
			foreach (var type in interfaces) {
				var def = type.Annotation<ITypeDefOrRef>().ResolveTypeDef();
				if (def == null)
					continue;
				foreach (var method in def.Methods.Where(m => !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))) {
					yield return ICSharpCode.NRefactory.VB.Ast.InterfaceMemberSpecifier.CreateWithColor((ICSharpCode.NRefactory.VB.Ast.AstType)type.Clone(), method.Name, TextTokenKindUtils.GetTextTokenKind(method));
				}

				foreach (var property in def.Properties) {
					yield return ICSharpCode.NRefactory.VB.Ast.InterfaceMemberSpecifier.CreateWithColor((ICSharpCode.NRefactory.VB.Ast.AstType)type.Clone(), property.Name, TextTokenKindUtils.GetTextTokenKind(property));
				}
			}
		}

		public bool HasEvent(ICSharpCode.NRefactory.VB.Ast.Expression expression) => expression.Annotation<EventDef>() != null;

		public bool IsMethodGroup(ICSharpCode.NRefactory.CSharp.Expression expression) {
			var annotation = expression.Annotation<MethodDef>();
			if (annotation == null)
				return false;
			return expression.Annotation<PropertyDef>() == null && expression.Annotation<EventDef>() == null;
		}

		public ICSharpCode.NRefactory.CSharp.ParameterDeclaration[] GetParametersForProperty(ICSharpCode.NRefactory.CSharp.PropertyDeclaration property) {
			var propInfo = property.Annotation<PropertyDef>();

			if (propInfo == null)
				return new ICSharpCode.NRefactory.CSharp.ParameterDeclaration[0];

			sb.Clear();
			var getMethod = propInfo.GetMethod;
			if (getMethod != null)
				return getMethod.Parameters.Where(p => p.IsNormalMethodParameter).Select(p => new ICSharpCode.NRefactory.CSharp.ParameterDeclaration(AstBuilder.ConvertType(p.Type, sb), p.Name, GetModifiers(p))).ToArray();
			var setMethod = propInfo.SetMethod;
			if (setMethod != null) {
				var ps = setMethod.Parameters.Where(p => p.IsNormalMethodParameter).ToArray();
				if (ps.Length > 1)
					return ps.Take(ps.Length - 1).Select(p => new ICSharpCode.NRefactory.CSharp.ParameterDeclaration(AstBuilder.ConvertType(p.Type, sb), p.Name, GetModifiers(p))).ToArray();
			}

			return new ICSharpCode.NRefactory.CSharp.ParameterDeclaration[0];
		}

		ICSharpCode.NRefactory.CSharp.ParameterModifier GetModifiers(Parameter p) {
			var pd = p.ParamDef;
			if (pd != null) {
				if (pd.IsOut && pd.IsIn)
					return ICSharpCode.NRefactory.CSharp.ParameterModifier.Ref;
				if (pd.IsOut)
					return ICSharpCode.NRefactory.CSharp.ParameterModifier.Out;
			}

			return ICSharpCode.NRefactory.CSharp.ParameterModifier.None;
		}
	}
}
