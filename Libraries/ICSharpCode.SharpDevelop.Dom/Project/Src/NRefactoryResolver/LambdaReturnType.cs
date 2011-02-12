// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.SharpDevelop.Dom.NRefactoryResolver
{
	public class LambdaReturnType : AnonymousMethodReturnType
	{
		NRefactoryResolver resolver;
		LambdaExpression lambdaExpression;
		List<Expression> returnExpressions = new List<Expression>();
		
		public override bool CanBeConvertedToExpressionTree {
			get { return lambdaExpression != null; }
		}
		
		internal LambdaReturnType(LambdaExpression expression, NRefactoryResolver resolver)
			: base(resolver.CompilationUnit)
		{
			this.resolver = resolver;
			this.lambdaExpression = expression;
			
			base.MethodParameters = new List<IParameter>();
			foreach (ParameterDeclarationExpression param in expression.Parameters) {
				base.MethodParameters.Add(NRefactoryASTConvertVisitor.CreateParameter(param, resolver.CallingMember as IMethod, resolver.CallingClass, resolver.CompilationUnit));
			}
			if (expression.ExpressionBody.IsNull)
				expression.StatementBody.AcceptVisitor(new ReturnStatementFinder(returnExpressions), null);
			else
				returnExpressions.Add(expression.ExpressionBody);
		}
		
		internal LambdaReturnType(AnonymousMethodExpression expression, NRefactoryResolver resolver)
			: base(resolver.CompilationUnit)
		{
			this.resolver = resolver;
			
			if (expression.HasParameterList) {
				base.MethodParameters = new List<IParameter>();
				foreach (ParameterDeclarationExpression param in expression.Parameters) {
					base.MethodParameters.Add(NRefactoryASTConvertVisitor.CreateParameter(param, resolver.CallingMember as IMethod, resolver.CallingClass, resolver.CompilationUnit));
				}
			}
			expression.Body.AcceptVisitor(new ReturnStatementFinder(returnExpressions), null);
		}
		
		sealed class ReturnStatementFinder : AbstractAstVisitor
		{
			List<Expression> returnExpressions;
			
			public ReturnStatementFinder(List<Expression> returnExpressions)
			{
				this.returnExpressions = returnExpressions;
			}
			
			public override object VisitReturnStatement(ReturnStatement returnStatement, object data)
			{
				returnExpressions.Add(returnStatement.Expression);
				return base.VisitReturnStatement(returnStatement, data);
			}
			
			public override object VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, object data)
			{
				return null;
			}
			
			public override object VisitLambdaExpression(LambdaExpression lambdaExpression, object data)
			{
				return null;
			}
		}
		
		public override IReturnType ResolveReturnType(IReturnType[] parameterTypes)
		{
			if (lambdaExpression == null)
				return ResolveReturnType();
			
			try {
				MemberLookupHelper.Log("LambdaReturnType: SetImplicitLambdaParameterTypes ", parameterTypes);
				resolver.SetImplicitLambdaParameterTypes(lambdaExpression, parameterTypes);
				return ResolveReturnType();
			} finally {
				resolver.UnsetImplicitLambdaParameterTypes(lambdaExpression);
			}
		}
		
		public override IReturnType ResolveReturnType()
		{
			MemberLookupHelper.Log("LambdaReturnType: ResolveReturnType");
			IReturnType result;
			if (returnExpressions.Count == 0)
				result = resolver.ProjectContent.SystemTypes.Void;
			else
				result = returnExpressions.Select(rt => resolver.ResolveInternal(rt, ExpressionContext.Default))
					.Select(rr => rr != null ? rr.ResolvedType : null)
					.Aggregate((rt1, rt2) => MemberLookupHelper.GetCommonType(resolver.ProjectContent, rt1, rt2));
			MemberLookupHelper.Log("LambdaReturnType: inferred " + result);
			return result;
		}
	}
}
