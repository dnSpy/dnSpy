// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.SharpDevelop.Dom.NRefactoryResolver
{
	/// <summary>
	/// Description of LambdaParameterReturnType.
	/// </summary>
	public class LambdaParameterReturnType : ProxyReturnType
	{
		LambdaExpression lambda;
		int parameterIndex;
		string parameterName;
		NRefactoryResolver resolver;
		
		public LambdaParameterReturnType(LambdaExpression lambda, string name, NRefactoryResolver resolver)
		{
			if (lambda == null)
				throw new ArgumentNullException("lambda");
			if (name == null)
				throw new ArgumentNullException("name");
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			this.lambda = lambda;
			this.parameterName = name;
			this.parameterIndex = lambda.Parameters.FindIndex(p => p.ParameterName == name);
			this.resolver = resolver;
			if (parameterIndex < 0)
				throw new ArgumentException("there is no lambda parameter with that name");
		}
		
		IReturnType cachedType;
		
		public override IReturnType BaseType {
			get {
				NRefactoryResolver resolver = this.resolver;
				LambdaExpression lambda = this.lambda;
				if (resolver == null || lambda == null)
					return cachedType;
				
				this.resolver = null;
				this.lambda = null;
				MemberLookupHelper.Log("Resolving " + this);
				IReturnType rt = resolver.GetExpectedTypeFromContext(lambda);
				MemberLookupHelper.Log("Resolving " + this + ", got delegate type " + rt);
				IMethod sig = CSharp.TypeInference.GetDelegateOrExpressionTreeSignature(rt, true);
				if (sig != null && parameterIndex < sig.Parameters.Count) {
					MemberLookupHelper.Log("Resolving " + this + ", got type " + rt);
					return cachedType = sig.Parameters[parameterIndex].ReturnType;
				}
				return null;
			}
		}
		
		public override string ToString()
		{
			return "[LambdaParameterReturnType: " + parameterName +
				(resolver != null ? " (not yet resolved)" : " (" + cachedType + ")")
				+ "]";
		}
	}
}
