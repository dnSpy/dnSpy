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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	public class ExpressionTreeConverter
	{
		#region static TryConvert method
		public static bool CouldBeExpressionTree(InvocationExpression expr)
		{
			if (expr != null && expr.Arguments.Count == 2) {
				MethodReference mr = expr.Annotation<MethodReference>();
				return mr != null && mr.Name == "Lambda" && mr.DeclaringType.FullName == "System.Linq.Expressions.Expression";
			}
			return false;
		}
		
		public static Expression TryConvert(DecompilerContext context, Expression expr)
		{
			Expression converted = new ExpressionTreeConverter(context).Convert(expr);
			if (converted != null) {
				converted.AddAnnotation(new ExpressionTreeLambdaAnnotation());
			}
			return converted;
		}
		#endregion
		
		readonly DecompilerContext context;
		Stack<LambdaExpression> activeLambdas = new Stack<LambdaExpression>();
		
		private ExpressionTreeConverter(DecompilerContext context)
		{
			this.context = context;
		}
		
		#region Main Convert method
		Expression Convert(Expression expr)
		{
			InvocationExpression invocation = expr as InvocationExpression;
			if (invocation != null) {
				MethodReference mr = invocation.Annotation<MethodReference>();
				if (mr != null && mr.DeclaringType.FullName == "System.Linq.Expressions.Expression") {
					switch (mr.Name) {
						case "Add":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.Add, false);
						case "AddChecked":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.Add, true);
						case "AddAssign":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.Add, false);
						case "AddAssignChecked":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.Add, true);
						case "And":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.BitwiseAnd);
						case "AndAlso":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.ConditionalAnd);
						case "AndAssign":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.BitwiseAnd);
						case "ArrayAccess":
						case "ArrayIndex":
						case "ArrayLength":
							return NotImplemented(invocation);
						case "Assign":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.Assign);
						case "Call":
							return NotImplemented(invocation);
						case "Coalesce":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.NullCoalescing);
						case "Condition":
							return NotImplemented(invocation);
						case "Constant":
							if (invocation.Arguments.Count >= 1)
								return invocation.Arguments.First().Clone();
							else
								return NotSupported(expr);
						case "Convert":
						case "ConvertChecked":
						case "Default":
							return NotImplemented(invocation);
						case "Divide":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.Divide);
						case "DivideAssign":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.Divide);
						case "Equal":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.Equality);
						case "ExclusiveOr":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.ExclusiveOr);
						case "ExclusiveOrAssign":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.ExclusiveOr);
						case "Field":
							return ConvertField(invocation);
						case "GreaterThan":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.GreaterThan);
						case "GreaterThanOrEqual":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.GreaterThanOrEqual);
						case "Lambda":
							return ConvertLambda(invocation);
						case "LeftShift":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.ShiftLeft);
						case "LeftShiftAssign":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.ShiftLeft);
						case "LessThan":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.LessThan);
						case "LessThanOrEqual":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.LessThanOrEqual);
						case "Modulo":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.Modulus);
						case "ModuloAssign":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.Modulus);
						case "Multiply":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.Multiply, false);
						case "MultiplyChecked":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.Multiply, true);
						case "MultiplyAssign":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.Multiply, false);
						case "MultiplyAssignChecked":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.Multiply, true);
						case "Negate":
						case "NegateChecked":
							return NotImplemented(invocation);
						case "New":
							return ConvertNewObject(invocation);
						case "Not":
							return NotImplemented(invocation);
						case "NotEqual":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.InEquality);
						case "OnesComplement":
							return NotImplemented(invocation);
						case "Or":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.BitwiseOr);
						case "OrAssign":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.BitwiseOr);
						case "OrElse":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.ConditionalOr);
						case "Quote":
							return NotImplemented(invocation);
						case "RightShift":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.ShiftRight);
						case "RightShiftAssign":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.ShiftRight);
						case "Subtract":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.Subtract, false);
						case "SubtractChecked":
							return ConvertBinaryOperator(invocation, BinaryOperatorType.Subtract, true);
						case "SubtractAssign":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.Subtract, false);
						case "SubtractAssignChecked":
							return ConvertAssignmentOperator(invocation, AssignmentOperatorType.Subtract, true);
						case "TypeAs":
							return ConvertTypeAs(invocation);
						case "TypeIs":
							return ConvertTypeIs(invocation);
					}
				}
			}
			IdentifierExpression ident = expr as IdentifierExpression;
			if (ident != null) {
				ILVariable v = ident.Annotation<ILVariable>();
				if (v != null) {
					foreach (LambdaExpression lambda in activeLambdas) {
						foreach (ParameterDeclaration p in lambda.Parameters) {
							if (p.Annotation<ILVariable>() == v)
								return new IdentifierExpression(p.Name).WithAnnotation(v);
						}
					}
				}
			}
			return NotSupported(expr);
		}
		
		Expression NotSupported(Expression expr)
		{
			Debug.WriteLine("Expression Tree Conversion Failed: '" + expr + "' is not supported");
			return null;
		}
		
		Expression NotImplemented(Expression expr)
		{
			return new IdentifierExpression("NotImplemented").Invoke(expr.Clone());
		}
		#endregion
		
		#region Convert Lambda
		static readonly Expression emptyArrayPattern = new ArrayCreateExpression {
			Type = new AnyNode(),
			Arguments = { new PrimitiveExpression(0) }
		};
		
		Expression ConvertLambda(InvocationExpression invocation)
		{
			if (invocation.Arguments.Count != 2)
				return NotSupported(invocation);
			LambdaExpression lambda = new LambdaExpression();
			Expression body = invocation.Arguments.First();
			ArrayCreateExpression parameterArray = invocation.Arguments.Last() as ArrayCreateExpression;
			if (parameterArray == null)
				return NotSupported(invocation);
			
			var annotation = body.Annotation<ParameterDeclarationAnnotation>();
			if (annotation != null) {
				lambda.Parameters.AddRange(annotation.Parameters);
			} else {
				// No parameter declaration annotation found.
				if (!emptyArrayPattern.IsMatch(parameterArray))
					return null;
			}
			
			activeLambdas.Push(lambda);
			Expression convertedBody = Convert(body);
			activeLambdas.Pop();
			if (convertedBody == null)
				return null;
			lambda.Body = convertedBody;
			return lambda;
		}
		#endregion
		
		#region Convert Field
		static readonly Expression getFieldFromHandlePattern =
			new TypePattern(typeof(FieldInfo)).ToType().Invoke("GetFieldFromHandle", new LdTokenPattern("field").ToExpression().Member("FieldHandle"));
		
		Expression ConvertField(InvocationExpression invocation)
		{
			if (invocation.Arguments.Count != 2)
				return NotSupported(invocation);
			
			Expression fieldInfoExpr = invocation.Arguments.ElementAt(1);
			Match m = getFieldFromHandlePattern.Match(fieldInfoExpr);
			if (!m.Success)
				return NotSupported(invocation);
			
			FieldReference fr = m.Get<AstNode>("field").Single().Annotation<FieldReference>();
			if (fr == null)
				return null;
			
			Expression target = Convert(invocation.Arguments.ElementAt(0));
			if (target == null)
				return null;
			
			return target.Member(fr.Name).WithAnnotation(fr);
		}
		#endregion
		
		#region Convert Binary Operator
		Expression ConvertBinaryOperator(InvocationExpression invocation, BinaryOperatorType op, bool? isChecked = null)
		{
			if (invocation.Arguments.Count != 2)
				return NotSupported(invocation);
			
			Expression left = Convert(invocation.Arguments.ElementAt(0));
			if (left == null)
				return null;
			Expression right = Convert(invocation.Arguments.ElementAt(1));
			if (right == null)
				return null;
			
			BinaryOperatorExpression boe = new BinaryOperatorExpression(left, op, right);
			if (isChecked != null)
				boe.AddAnnotation(isChecked.Value ? AddCheckedBlocks.CheckedAnnotation : AddCheckedBlocks.UncheckedAnnotation);
			return boe;
		}
		
		Expression ConvertAssignmentOperator(InvocationExpression invocation, AssignmentOperatorType op, bool? isChecked = null)
		{
			return NotImplemented(invocation);
		}
		#endregion
		
		#region Convert New Object
		static readonly Expression newObjectCtorPattern = new TypePattern(typeof(MethodBase)).ToType().Invoke
			(
				"GetMethodFromHandle",
				new LdTokenPattern("ctor").ToExpression().Member("MethodHandle"),
				new OptionalNode(new TypeOfExpression(new AnyNode("declaringType")).Member("TypeHandle"))
			).CastTo(new TypePattern(typeof(ConstructorInfo)));
		
		Expression ConvertNewObject(InvocationExpression invocation)
		{
			if (invocation.Arguments.Count < 1 || invocation.Arguments.Count > 3)
				return NotSupported(invocation);
			
			Match m = newObjectCtorPattern.Match(invocation.Arguments.First());
			if (!m.Success)
				return NotSupported(invocation);
			
			MethodReference ctor = m.Get<AstNode>("ctor").Single().Annotation<MethodReference>();
			if (ctor == null)
				return null;
			
			TypeReference declaringType;
			if (m.Has("declaringType")) {
				declaringType = m.Get<AstNode>("declaringType").Single().Annotation<TypeReference>();
			} else {
				declaringType = ctor.DeclaringType;
			}
			if (declaringType == null)
				return null;
			
			ObjectCreateExpression oce = new ObjectCreateExpression(AstBuilder.ConvertType(declaringType));
			if (invocation.Arguments.Count >= 2) {
				IList<Expression> arguments = ConvertExpressionsArray(invocation.Arguments.ElementAtOrDefault(1));
				if (arguments == null)
					return null;
				oce.Arguments.AddRange(arguments);
			}
			if (invocation.Arguments.Count >= 3 && declaringType.IsAnonymousType()) {
				MethodDefinition resolvedCtor = ctor.Resolve();
				if (resolvedCtor == null || resolvedCtor.Parameters.Count != oce.Arguments.Count)
					return null;
				AnonymousTypeCreateExpression atce = new AnonymousTypeCreateExpression();
				var arguments = oce.Arguments.ToArray();
				if (AstMethodBodyBuilder.CanInferAnonymousTypePropertyNamesFromArguments(arguments, resolvedCtor.Parameters)) {
					oce.Arguments.MoveTo(atce.Initializers);
				} else {
					for (int i = 0; i < resolvedCtor.Parameters.Count; i++) {
						atce.Initializers.Add(
							new NamedExpression {
								Identifier = resolvedCtor.Parameters[i].Name,
								Expression = arguments[i].Detach()
							});
					}
				}
				return atce;
			}
			
			return oce;
		}
		#endregion
		
		#region ConvertExpressionsArray
		static readonly Pattern expressionArrayPattern = new Choice {
			new ArrayCreateExpression {
				Type = new TypePattern(typeof(System.Linq.Expressions.Expression)),
				Arguments = { new PrimitiveExpression(0) }
			},
			new ArrayCreateExpression {
				Type = new TypePattern(typeof(System.Linq.Expressions.Expression)),
				AdditionalArraySpecifiers = { new ArraySpecifier() },
				Initializer = new ArrayInitializerExpression {
					Elements = { new Repeat(new AnyNode("elements")) }
				}
			}
		};
		
		IList<Expression> ConvertExpressionsArray(Expression arrayExpression)
		{
			Match m = expressionArrayPattern.Match(arrayExpression);
			if (m.Success) {
				List<Expression> result = new List<Expression>();
				foreach (Expression expr in m.Get<Expression>("elements")) {
					Expression converted = Convert(expr);
					if (converted == null)
						return null;
					result.Add(converted);
				}
				return result;
			}
			return null;
		}
		#endregion
		
		#region Convert TypeAs/TypeIs
		static readonly TypeOfPattern typeOfPattern = new TypeOfPattern("type");
		
		Expression ConvertTypeAs(InvocationExpression invocation)
		{
			if (invocation.Arguments.Count != 2)
				return null;
			Match m = typeOfPattern.Match(invocation.Arguments.ElementAt(1));
			if (m.Success) {
				Expression converted = Convert(invocation.Arguments.First());
				if (converted != null)
					return new AsExpression(converted, m.Get<AstType>("type").Single().Clone());
			}
			return null;
		}
		
		Expression ConvertTypeIs(InvocationExpression invocation)
		{
			if (invocation.Arguments.Count != 2)
				return null;
			Match m = typeOfPattern.Match(invocation.Arguments.ElementAt(1));
			if (m.Success) {
				Expression converted = Convert(invocation.Arguments.First());
				if (converted != null) {
					return new IsExpression {
						Expression = converted,
						Type = m.Get<AstType>("type").Single().Clone()
					};
				}
			}
			return null;
		}
		#endregion
	}
}
