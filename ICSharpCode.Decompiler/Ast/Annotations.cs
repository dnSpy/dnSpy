using System;
using System.Collections.Generic;
using System.Diagnostics;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast
{
	public class TypeInformation
	{
		public readonly TypeReference InferredType;
		
		public TypeInformation(TypeReference inferredType)
		{
			this.InferredType = inferredType;
		}
	}
	
	public class LdTokenAnnotation {}
	
	/// <summary>
	/// Annotation that is applied to the body expression of an Expression.Lambda() call.
	/// </summary>
	public class ParameterDeclarationAnnotation
	{
		public readonly List<ParameterDeclaration> Parameters = new List<ParameterDeclaration>();
		
		public ParameterDeclarationAnnotation(ILExpression expr)
		{
			Debug.Assert(expr.Code == ILCode.ExpressionTreeParameterDeclarations);
			for (int i = 0; i < expr.Arguments.Count - 1; i++) {
				ILExpression p = expr.Arguments[i];
				// p looks like this:
				//   stloc(v, call(Expression::Parameter, call(Type::GetTypeFromHandle, ldtoken(...)), ldstr(...)))
				ILVariable v = (ILVariable)p.Operand;
				TypeReference typeRef = (TypeReference)p.Arguments[0].Arguments[0].Arguments[0].Operand;
				string name = (string)p.Arguments[0].Arguments[1].Operand;
				Parameters.Add(new ParameterDeclaration(AstBuilder.ConvertType(typeRef), name).WithAnnotation(v));
			}
		}
	}
	
	/// <summary>
	/// Annotation that is applied to a LambdaExpression that was produced by an expression tree.
	/// </summary>
	public class ExpressionTreeLambdaAnnotation
	{
	}
}
