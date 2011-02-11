// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Reflection;

using Debugger;
using Debugger.MetaData;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;
using ICSharpCode.NRefactory.Visitors;

namespace ICSharpCode.NRefactory.Ast
{
	public static class ExpressionExtensionMethods
	{
		public static Value Evaluate(this Expression expression, Process process)
		{
			return ExpressionEvaluator.Evaluate(expression, process);
		}
		
		static M SetStaticType<M>(this M expr, DebugType type) where M: INode
		{
			expr.UserData = type;
			return expr;
		}
		
		public static DebugType GetStaticType(this INode expr)
		{
			return expr.UserData as DebugType;
		}
		
		public static Expression Parenthesize(this Expression expr)
		{
			if (expr is IdentifierExpression ||
			    expr is MemberReferenceExpression ||
			    expr is IndexerExpression ||
			    expr is ParenthesizedExpression ||
			    expr is PrimitiveExpression)
				return expr;
			return new ParenthesizedExpression(expr);
		}
		
		public static Expression CastTo(this Expression expresion, DebugType castTo)
		{
			// No need to cast
			if (expresion.GetStaticType() == castTo)
				return expresion;
			if (expresion is PrimitiveExpression) {
				object val = ((PrimitiveExpression)expresion).Value;
				if (val != null && val.GetType().FullName == castTo.FullName)
					return expresion;
			}
			return new CastExpression(castTo.GetTypeReference(), expresion.Parenthesize(), CastType.Cast);
		}
		
		public static Expression GetExpression(this DebugLocalVariableInfo locVar)
		{
			return new IdentifierExpression(locVar.Name).SetStaticType((DebugType)locVar.LocalType);
		}
		
		public static Expression GetExpression(this DebugParameterInfo par)
		{
			return new IdentifierExpression(par.Name).SetStaticType((DebugType)par.ParameterType);
		}
		
		public static UnaryOperatorExpression AppendDereference(this Expression expression)
		{
			return new UnaryOperatorExpression(new ParenthesizedExpression(expression), UnaryOperatorType.Dereference);
		}
		
		public static IndexerExpression AppendIndexer(this Expression expression, params int[] indices)
		{
			IndexerExpression indexerExpr = new IndexerExpression(Parenthesize(expression), new List<Expression>());
			foreach(int index in indices) {
				indexerExpr.Indexes.Add(new PrimitiveExpression(index));
			}
			DebugType staticType = expression.GetStaticType();
			if (staticType != null && staticType.IsArray)
				indexerExpr.SetStaticType((DebugType)staticType.GetElementType());
			if (staticType != null && staticType.FullNameWithoutGenericArguments == typeof(List<>).FullName)
				indexerExpr.SetStaticType((DebugType)staticType.GetGenericArguments()[0]);
			return indexerExpr;
		}
		
		public static Expression AppendMemberReference(this Expression expresion, IDebugMemberInfo memberInfo, params Expression[] args)
		{
			Expression target;
			if (memberInfo.IsStatic) {
				target = new TypeReferenceExpression(
					memberInfo.DeclaringType.GetTypeReference()
				);
			} else {
				target = expresion.CastTo((DebugType)memberInfo.DeclaringType);
			}
			
			if (memberInfo is DebugFieldInfo) {
				if (args.Length > 0)
					throw new DebuggerException("No arguments expected for a field");
				return new MemberReferenceExpression(target, memberInfo.Name).SetStaticType(memberInfo.MemberType);
			}
			
			if (memberInfo is MethodInfo) {
				return new InvocationExpression(
					new MemberReferenceExpression(target, memberInfo.Name),
					AddExplicitTypes((MethodInfo)memberInfo, args)
				).SetStaticType(memberInfo.MemberType);
			}
			
			if (memberInfo is PropertyInfo) {
				PropertyInfo propInfo = (PropertyInfo)memberInfo;
				if (args.Length > 0) {
					if (memberInfo.Name != "Item")
						throw new DebuggerException("Arguments expected only for the Item property");
					return new IndexerExpression(
						target,
						AddExplicitTypes(propInfo.GetGetMethod() ?? propInfo.GetSetMethod(), args)
					).SetStaticType(memberInfo.MemberType);
				} else {
					return new MemberReferenceExpression(target, memberInfo.Name).SetStaticType(memberInfo.MemberType);
				}
			}
			
			throw new DebuggerException("Unknown member type " + memberInfo.GetType().FullName);
		}
		
		static List<Expression> AddExplicitTypes(MethodInfo method, Expression[] args)
		{
			if (args.Length != method.GetParameters().Length)
				throw new DebuggerException("Incorrect number of arguments");
			List<Expression> typedArgs = new List<Expression>(args.Length);
			for(int i = 0; i < args.Length; i++) {
				typedArgs.Add(args[i].CastTo((DebugType)method.GetParameters()[i].ParameterType));
			}
			return typedArgs;
		}
		
		public static bool Is<T>(this Type type)
		{
			return type.FullName == typeof(T).FullName;
		}
		
		public static bool CanPromoteTo(this Type type, Type toType)
		{
			return ((DebugType)type).CanImplicitelyConvertTo(toType);
		}
		
		public static string PrettyPrint(this INode code)
		{
			if (code == null) return string.Empty;
			CSharpOutputVisitor csOutVisitor = new CSharpOutputVisitor();
			code.AcceptVisitor(csOutVisitor, null);
			return csOutVisitor.Text;
		}
		
		public static TypeReference GetTypeReference(this Type type)
		{
			List<int> arrayRanks = new List<int>();
			while(type.IsArray) {
				// C# uses reverse array order
				arrayRanks.Add(type.GetArrayRank() - 1);
				type = type.GetElementType();
			}
			
			int pointerNest = 0;
			while(type.IsPointer) {
				pointerNest++;
				type = type.GetElementType();
			}
			
			if (type.IsArray)
				throw new DebuggerException("C# does not support pointers to arrays");
			
			string name = type.Name;
			if (name.IndexOf('`') != -1)
				name = name.Substring(0, name.IndexOf('`'));
			if (!string.IsNullOrEmpty(type.Namespace))
				name = type.Namespace + "." + name;
			
			List<Type> genArgs = new List<Type>();
			// This inludes the generic arguments of the outter types
			genArgs.AddRange(type.GetGenericArguments());
			if (type.DeclaringType != null)
				genArgs.RemoveRange(0, type.DeclaringType.GetGenericArguments().Length);
			List<TypeReference> genTypeRefs = new List<TypeReference>();
			foreach(Type genArg in genArgs) {
				genTypeRefs.Add(genArg.GetTypeReference());
			}
			
			if (type.DeclaringType != null) {
				TypeReference outterRef = type.DeclaringType.GetTypeReference();
				InnerClassTypeReference innerRef = new InnerClassTypeReference(outterRef, name, genTypeRefs);
				innerRef.PointerNestingLevel = pointerNest;
				innerRef.RankSpecifier = arrayRanks.ToArray();
				return innerRef.SetStaticType((DebugType)type);
			} else {
				return new TypeReference(name, pointerNest, arrayRanks.ToArray(), genTypeRefs).SetStaticType((DebugType)type);
			}
		}
		
		/// <summary>
		/// Converts tree into nested TypeReference/InnerClassTypeReference.
		/// Dotted names are split into separate nodes.
		/// It does not normalize generic arguments.
		/// </summary>
		static TypeReference NormalizeTypeReference(this INode expr)
		{
			if (expr is IdentifierExpression) {
				return new TypeReference(
					((IdentifierExpression)expr).Identifier,
					((IdentifierExpression)expr).TypeArguments
				);
			} else if (expr is MemberReferenceExpression) {
				TypeReference outter = NormalizeTypeReference(((MemberReferenceExpression)expr).TargetObject);
				return new InnerClassTypeReference(
					outter,
					((MemberReferenceExpression)expr).MemberName,
					((MemberReferenceExpression)expr).TypeArguments
				);
			} else if (expr is TypeReferenceExpression) {
				return NormalizeTypeReference(((TypeReferenceExpression)expr).TypeReference);
			} else if (expr is InnerClassTypeReference) { // Frist - it is also TypeReference
				InnerClassTypeReference typeRef = (InnerClassTypeReference)expr;
				string[] names = typeRef.Type.Split('.');
				TypeReference newRef = NormalizeTypeReference(typeRef.BaseType);
				foreach(string name in names) {
					newRef = new InnerClassTypeReference(newRef, name, new List<TypeReference>());
				}
				newRef.GenericTypes.AddRange(typeRef.GenericTypes);
				newRef.PointerNestingLevel = typeRef.PointerNestingLevel;
				newRef.RankSpecifier = typeRef.RankSpecifier;
				return newRef;
			} else if (expr is TypeReference) {
				TypeReference typeRef = (TypeReference)expr;
				string[] names = typeRef.Type.Split('.');
				if (names.Length == 1)
					return typeRef;
				TypeReference newRef = null;
				foreach(string name in names) {
					if (newRef == null) {
						newRef = new TypeReference(name, new List<TypeReference>());
					} else {
						newRef = new InnerClassTypeReference(newRef, name, new List<TypeReference>());
					}
				}
				newRef.GenericTypes.AddRange(typeRef.GenericTypes);
				newRef.PointerNestingLevel = typeRef.PointerNestingLevel;
				newRef.RankSpecifier = typeRef.RankSpecifier;
				return newRef;
			} else {
				throw new EvaluateException(expr, "Type expected. {0} seen.", expr.GetType().FullName);
			}
		}
		
		static string GetNameWithArgCounts(TypeReference typeRef)
		{
			string name = typeRef.Type;
			if (typeRef.GenericTypes.Count > 0)
				name += "`" + typeRef.GenericTypes.Count.ToString();
			if (typeRef is InnerClassTypeReference) {
				return GetNameWithArgCounts(((InnerClassTypeReference)typeRef).BaseType) + "." + name;
			} else {
				return name;
			}
		}
		
		public static DebugType ResolveType(this INode expr, Debugger.AppDomain appDomain)
		{
			if (expr is TypeReference && expr.GetStaticType() != null)
				return expr.GetStaticType();
			if (expr is TypeReferenceExpression && ((TypeReferenceExpression)expr).TypeReference.GetStaticType() != null)
				return ((TypeReferenceExpression)expr).TypeReference.GetStaticType();
			
			appDomain.Process.TraceMessage("Resolving {0}", expr.PrettyPrint());
			
			TypeReference typeRef = NormalizeTypeReference(expr);
			
			List<TypeReference> genTypeRefs;
			if (typeRef is InnerClassTypeReference) {
				genTypeRefs = ((InnerClassTypeReference)typeRef).CombineToNormalTypeReference().GenericTypes;
			} else {
				genTypeRefs = typeRef.GenericTypes;
			}
			
			List<DebugType> genArgs = new List<DebugType>();
			foreach(TypeReference genTypeRef in genTypeRefs) {
				genArgs.Add(ResolveType(genTypeRef, appDomain));
			}
			
			return ResolveTypeInternal(typeRef, genArgs.ToArray(), appDomain);
		}
		
		/// <summary>
		/// For performance this is separate method.
		/// 'genArgs' should hold type for each generic parameter in 'typeRef'.
		/// </summary>
		static DebugType ResolveTypeInternal(TypeReference typeRef, DebugType[] genArgs, Debugger.AppDomain appDomain)
		{
			DebugType type = null;
			
			// Try to construct non-nested type
			// If there are generic types up in the tree, it must be nested type
			if (genArgs.Length == typeRef.GenericTypes.Count) {
				string name = GetNameWithArgCounts(typeRef);
				type = DebugType.CreateFromNameOrNull(appDomain, name, null, genArgs);
			}
			
			// Try to construct nested type
			if (type == null && typeRef is InnerClassTypeReference) {
				DebugType[] outterGenArgs = genArgs;
				// Do not pass our generic arguments to outter type
				Array.Resize(ref outterGenArgs, genArgs.Length - typeRef.GenericTypes.Count);
				
				DebugType outter = ResolveTypeInternal(((InnerClassTypeReference)typeRef).BaseType, outterGenArgs, appDomain);
				string nestedName = typeRef.GenericTypes.Count == 0 ? typeRef.Type : typeRef.Type + "`" + typeRef.GenericTypes.Count;
				type = DebugType.CreateFromNameOrNull(appDomain, nestedName, outter, genArgs);
			}
			
			if (type == null)
				throw new GetValueException("Can not resolve " + typeRef.PrettyPrint());
			
			for(int i = 0; i < typeRef.PointerNestingLevel; i++) {
				type = (DebugType)type.MakePointerType();
			}
			if (typeRef.RankSpecifier != null) {
				for(int i = typeRef.RankSpecifier.Length - 1; i >= 0; i--) {
					type = (DebugType)type.MakeArrayType(typeRef.RankSpecifier[i] + 1);
				}
			}
			return type;
		}
	}
}
