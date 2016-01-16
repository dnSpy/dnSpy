using System.Collections.Generic;
using System.Diagnostics;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using dnlib.DotNet;

namespace ICSharpCode.Decompiler.Ast {
	public class TypeInformation
	{
		public readonly TypeSig InferredType;
		public readonly TypeSig ExpectedType;
		
		public TypeInformation(TypeSig inferredType, TypeSig expectedType)
		{
			this.InferredType = inferredType;
			this.ExpectedType = expectedType;
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
				ITypeDefOrRef typeRef = (ITypeDefOrRef)p.Arguments[0].Arguments[0].Arguments[0].Operand;
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
