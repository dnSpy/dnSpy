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
				
				var mre = new MemberReferenceExpression() { Target = target.Clone(), MemberName = memberInfo.Name };
				return mre.SetStaticType(memberInfo.MemberType);
			}
			
			if (memberInfo is MethodInfo) {
				var mre = new MemberReferenceExpression() { Target = target, MemberName = memberInfo.Name };
				var ie = new InvocationExpression() { Target = mre.Clone() };
				ie.Arguments.AddRange(AddExplicitTypes((MethodInfo)memberInfo, args));
				
				return ie.SetStaticType(memberInfo.MemberType);
			}
			
			if (memberInfo is PropertyInfo) {
				PropertyInfo propInfo = (PropertyInfo)memberInfo;
				if (args.Length > 0) {
					if (memberInfo.Name != "Item")
						throw new DebuggerException("Arguments expected only for the Item property");
					var ie = new IndexerExpression() { Target = target.Clone() };
					ie.Arguments.AddRange(AddExplicitTypes(propInfo.GetGetMethod() ?? propInfo.GetSetMethod(), args));
					
					return ie.SetStaticType(memberInfo.MemberType);
				} else {
					return (new MemberReferenceExpression() {
					        	Target = target.Clone(),
					        	MemberName = memberInfo.Name }).SetStaticType(memberInfo.MemberType);
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
				CSharpOutputVisitor csOutVisitor = new CSharpOutputVisitor(sw, new CSharpFormattingOptions());
				code.AcceptVisitor(csOutVisitor, null);
				return sw.ToString();
			}
		}
		
		public static AstType GetTypeReference(this Type type)
		{
			return new SimpleType(type.Name).SetStaticType((DebugType)type);
		}
		
		public static DebugType ResolveType(this AstNode expr, Debugger.AppDomain appDomain)
		{
			var result = expr.GetStaticType();
			if (result != null)
				return result;
			
			return DebugType.CreateFromType(appDomain, expr.Annotation<Type>());
		}
	}
}
