// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Debugger;
using Debugger.MetaData;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Visitors;

namespace ICSharpCode.NRefactory.Ast
{
	public static class ExpressionExtensionMethods
	{
		public static Value Evaluate(this Expression expression, Process process)
		{
			return ExpressionEvaluator.Evaluate(expression, process);
		}
		
		static M SetStaticType<M>(this M expr, DebugType type) where M: AstNode
		{
			expr.AddAnnotation(type);
			return expr;
		}
		
		public static DebugType GetStaticType(this AstNode expr)
		{
			return expr.Annotation<DebugType>();
		}
		
		public static Expression Parenthesize(this Expression expr)
		{
			if (expr is IdentifierExpression ||
			    expr is MemberReferenceExpression ||
			    expr is IndexerExpression ||
			    expr is ParenthesizedExpression ||
			    expr is PrimitiveExpression)
				return expr;
			return new ParenthesizedExpression() { Expression = expr };
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
			return new CastExpression() { Expression = expresion.Clone().Parenthesize(), Type = castTo.GetTypeReference() };
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
			return new UnaryOperatorExpression(UnaryOperatorType.Dereference, new ParenthesizedExpression() { Expression = expression });
		}
		
		public static IndexerExpression AppendIndexer(this Expression expression, params int[] indices)
		{
			var args = new List<Expression>();
			foreach(int index in indices) {
				args.Add(new PrimitiveExpression(index));
			}
			IndexerExpression indexerExpr = expression.Clone().Indexer(args);
			
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
				target = new TypeReferenceExpression() { Type = memberInfo.DeclaringType.GetTypeReference() };
			} else {
				target = CastTo(expresion, (DebugType)memberInfo.DeclaringType);
			}
			
			if (memberInfo is DebugFieldInfo) {
				if (args.Length > 0)
					throw new DebuggerException("No arguments expected for a field");
				
				var mre = new MemberReferenceExpression() { Target = target, MemberName = memberInfo.Name };
				return mre.SetStaticType(memberInfo.MemberType);
			}
			
			if (memberInfo is MethodInfo) {
				var mre = new MemberReferenceExpression() { Target = target, MemberName = memberInfo.Name };
				var ie = new InvocationExpression() { 
					Target = mre/*, 
					Arguments = AddExplicitTypes((MethodInfo)memberInfo, args) */
				};
				
				return ie.SetStaticType(memberInfo.MemberType);
			}
			
			if (memberInfo is PropertyInfo) {
				PropertyInfo propInfo = (PropertyInfo)memberInfo;
				if (args.Length > 0) {
					if (memberInfo.Name != "Item")
						throw new DebuggerException("Arguments expected only for the Item property");
					return (new IndexerExpression() { 
					        	Target = target/*, 
					        	Arguments = AddExplicitTypes(propInfo.GetGetMethod() ?? propInfo.GetSetMethod()
					        	                             , args) */
					        }
					       ).SetStaticType(memberInfo.MemberType);
				} else {
					return (new MemberReferenceExpression() { Target = target, MemberName = memberInfo.Name }).SetStaticType(memberInfo.MemberType);
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
				typedArgs.Add(CastTo(args[i], (DebugType)method.GetParameters()[i].ParameterType));
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
		
		public static string PrettyPrint(this AstNode code)
		{
			if (code == null) return string.Empty;

			using (var sw = new StringWriter())
			{
				OutputVisitor csOutVisitor = new OutputVisitor(sw, new CSharpFormattingPolicy());
				code.AcceptVisitor(csOutVisitor, null);
				return sw.ToString();
			}
		}
		
		public static AstType GetTypeReference(this Type type)
		{
			return new SimpleType(type.Name).SetStaticType((DebugType)type);
			
//			List<int> arrayRanks = new List<int>();
//			while(type.IsArray) {
//				// C# uses reverse array order
//				arrayRanks.Add(type.GetArrayRank() - 1);
//				type = type.GetElementType();
//			}
//			
//			int pointerNest = 0;
//			while(type.IsPointer) {
//				pointerNest++;
//				type = type.GetElementType();
//			}
//			
//			if (type.IsArray)
//				throw new DebuggerException("C# does not support pointers to arrays");
//			
//			string name = type.Name;
//			if (name.IndexOf('`') != -1)
//				name = name.Substring(0, name.IndexOf('`'));
//			if (!string.IsNullOrEmpty(type.Namespace))
//				name = type.Namespace + "." + name;
//			
//			List<Type> genArgs = new List<Type>();
//			// This inludes the generic arguments of the outter types
//			genArgs.AddRange(type.GetGenericArguments());
//			if (type.DeclaringType != null)
//				genArgs.RemoveRange(0, type.DeclaringType.GetGenericArguments().Length);
//			List<AstType> genTypeRefs = new List<AstType>();
//			foreach(Type genArg in genArgs) {
//				genTypeRefs.Add(genArg.GetTypeReference());
//			}
//			
//			if (type.DeclaringType != null) {
//				var outterRef = type.DeclaringType.GetTypeReference();
//				var innerRef = new ComposedType() {
//					PointerRank = pointerNest,
//					//ArraySpecifiers = arrayRanks.ConvertAll(r => new ArraySpecifier(r)),
////					BaseType = new MemberType() {
////						Target = outterRef, MemberName = name, TypeArguments = genTypeRefs }
//				};
//				
//				return innerRef.SetStaticType((DebugType)type);
//			} else {
//				return (new ComposedType() {
//				        	PointerRank = pointerNest,
//				        	//ArraySpecifiers = arrayRanks.ConvertAll(r => new ArraySpecifier(r)),
//				        	BaseType = new SimpleType() {
//				        		Identifier = name,
//				        		/*TypeArguments = genTypeRefs*/ }}).SetStaticType((DebugType)type);
//			}
		}
		
		/// <summary>
		/// Converts tree into nested TypeReference/InnerClassTypeReference.
		/// Dotted names are split into separate nodes.
		/// It does not normalize generic arguments.
		/// </summary>
//		static AstType NormalizeTypeReference(this AstNode expr)
//		{
//			if (expr is IdentifierExpression) {
//				return new SimpleType() {
//					Identifier = ((IdentifierExpression)expr).Identifier/*,
//					TypeArguments = ((IdentifierExpression)expr).TypeArguments*/
//				};
//			} else if (expr is MemberReferenceExpression) {
//				var outter = NormalizeTypeReference(((MemberReferenceExpression)expr).Target);
//				return new MemberType() { Target = outter,
//					MemberName = ((MemberReferenceExpression)expr).MemberName/*,
//					TypeArguments = ((MemberReferenceExpression)expr).TypeArguments*/ };
//			} else if (expr is TypeReferenceExpression) {
//				return NormalizeTypeReference(((TypeReferenceExpression)expr).Type);
//			} else if (expr is ComposedType) { // Frist - it is also TypeReference
//				var typeRef = (ComposedType)expr;
//				string[] names = null;
//				if (typeRef.BaseType is SimpleType)
//					names = (((SimpleType)typeRef.BaseType)).Identifier.Split('.');
//				else
//					names = (((MemberType)typeRef.BaseType)).MemberName.Split('.');
//				
//				var newRef = NormalizeTypeReference(typeRef.BaseType) as ComposedType;
//				foreach(string name in names) {
//					newRef = new ComposedType() {
//						BaseType = new SimpleType() { Identifier = name/*, TypeArguments = new List<AstType>() */}
//					};
//				}
//				//(((MemberType)newRef).TypeArguments as List<AstType>).AddRange(typeRef.TypeArguments);
//				newRef.PointerRank = typeRef.PointerRank;
//				//newRef.ArraySpecifiers = typeRef.ArraySpecifiers;
//				return newRef;
//			} else if (expr is SimpleType) {
//				var typeRef = (SimpleType)expr;
//				string[] names = typeRef.Identifier.Split('.');
//				if (names.Length == 1)
//					return typeRef;
//				AstType newRef = null;
//				foreach(string name in names) {
//					if (newRef == null) {
//						newRef = new SimpleType() { Identifier = name/*, TypeArguments = new List<AstType>()*/ };
//					} else {
//						newRef = new MemberType() { Target = newRef, MemberName = name/*, TypeArguments = new List<AstType>() */};
//					}
//				}
//				//((List<AstType>)newRef.TypeArguments).AddRange(typeRef.TypeArguments);
//				//newRef.PointerNestingLevel = typeRef.PointerNestingLevel;
//				//newRef.RankSpecifier = typeRef.RankSpecifier;
//				return newRef;
//			} else if (expr is PrimitiveType) {
//				return (PrimitiveType)expr;
//			} else {
//				throw new EvaluateException(expr, "Type expected. {0} seen.", expr.GetType().FullName);
//			}
//		}
//		
//		static string GetNameWithArgCounts(AstType typeRef)
//		{
//			string name = string.Empty;
//			
//			if (typeRef is SimpleType)
//			{
//				name = ((SimpleType)typeRef).Identifier;
//				if (((SimpleType)typeRef).TypeArguments.Count() > 0)
//					name += "`" + ((SimpleType)typeRef).TypeArguments.Count().ToString();
//			}
//			
//			if (typeRef is MemberType) {
//				name = ((MemberType)typeRef).MemberName;
//				return GetNameWithArgCounts(((MemberType)typeRef).Target) + "." + name;
//			} else {
//				return name;
//			}
//		}
//		
		public static DebugType ResolveType(this AstNode expr, Debugger.AppDomain appDomain)
		{
			var result = expr.GetStaticType();
			if (result != null)
				return result;
			
			return DebugType.CreateFromType(appDomain, expr.Annotation<Type>());
			
//			if (expr is AstType && expr.GetStaticType() != null)
//				return expr.GetStaticType();
//			if (expr is TypeReferenceExpression && ((TypeReferenceExpression)expr).Type.GetStaticType() != null)
//				return ((TypeReferenceExpression)expr).Type.GetStaticType();
//			
//			appDomain.Process.TraceMessage("Resolving {0}", expr.PrettyPrint());
//			
//			var typeRef = NormalizeTypeReference(expr);
//			
//			List<AstType> genTypeRefs = null;
//			if (typeRef is MemberType) {
//				//FIXME genTypeRefs = ((MemberType)typeRef).CombineToNormalTypeReference().TypeArguments as List<AstType>;
//			} else {
//				if (typeRef is SimpleType) {
//					//genTypeRefs = ((SimpleType)typeRef).TypeArguments as List<AstType>;
//				}
//			}
//			
//			List<DebugType> genArgs = new List<DebugType>();
//			foreach(var genTypeRef in genTypeRefs) {
//				genArgs.Add(ResolveType(genTypeRef, appDomain));
//			}
//			
//			return ResolveTypeInternal(typeRef, genArgs.ToArray(), appDomain);
		}
		
		/// <summary>
		/// For performance this is separate method.
		/// 'genArgs' should hold type for each generic parameter in 'typeRef'.
		/// </summary>
//		static DebugType ResolveTypeInternal(AstType typeRef, DebugType[] genArgs, Debugger.AppDomain appDomain)
//		{
//			DebugType type = null;
//			
//			if (typeRef is SimpleType) {
//				// Try to construct non-nested type
//				// If there are generic types up in the tree, it must be nested type
//				var simple = (SimpleType)typeRef;
//				
//				if (genArgs.Length == simple.TypeArguments.Count()) {
//					string name = GetNameWithArgCounts(simple);
//					type = DebugType.CreateFromNameOrNull(appDomain, name, null, genArgs);
//				}
//			}
//			// Try to construct nested type
//			if (type == null && typeRef is MemberType) {
//				var member = (MemberType)typeRef;
//				DebugType[] outterGenArgs = genArgs;
//				// Do not pass our generic arguments to outter type
//				Array.Resize(ref outterGenArgs, genArgs.Length - member.TypeArguments.Count());
//				
//				DebugType outter = ResolveTypeInternal(member.Target, outterGenArgs, appDomain);
//				string nestedName = member.TypeArguments.Count() == 0 ?
//					member.MemberName : member.MemberName + "`" + member.TypeArguments.Count();
//				type = DebugType.CreateFromNameOrNull(appDomain, nestedName, outter, genArgs);
//			}
//			
//			if (type == null)
//				throw new GetValueException("Can not resolve " + typeRef.PrettyPrint());
//			
//			if (typeRef is ComposedType) {
//				
//				for(int i = 0; i < ((ComposedType)typeRef).PointerRank; i++) {
//					type = (DebugType)type.MakePointerType();
//				}
//				if (((ComposedType)typeRef).ArraySpecifiers != null) {
//					var enumerator = ((ComposedType)typeRef).ArraySpecifiers.Reverse().GetEnumerator();
//						while (enumerator.MoveNext()) {
//						type = (DebugType)type.MakeArrayType(enumerator.Current.Dimensions + 1);
//					}
//				}
//			}
//			return type;
//		}
	}
}
