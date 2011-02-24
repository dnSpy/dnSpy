// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using Mono.Cecil;

namespace Decompiler
{
	public interface IVariablePattern
	{
		bool MatchVariable(ILVariable v);
	}
	
	public class StoreToGenerated : ILExpression, IVariablePattern
	{
		public ILExpression LastMatch;
		
		public ILVariable LastVariable {
			get {
				return LastMatch != null ? LastMatch.Operand as ILVariable : null;
			}
		}
		
		public StoreToGenerated(ILExpression arg) : base(ILCode.Pattern, null, arg)
		{
		}
		
		public override bool Match(ILNode other)
		{
			ILExpression expr = other as ILExpression;
			if (expr != null && expr.Code == ILCode.Stloc && ((ILVariable)expr.Operand).IsGenerated && Match(this.Arguments, expr.Arguments)) {
				this.LastMatch = expr;
				return true;
			} else {
				return false;
			}
		}
		
		bool IVariablePattern.MatchVariable(ILVariable v)
		{
			return v == LastMatch.Operand;
		}
	}
	
	public class LoadFromVariable : ILExpression
	{
		IVariablePattern v;
		
		public LoadFromVariable(IVariablePattern v) : base(ILCode.Pattern, null)
		{
			this.v = v;
		}
		
		public override bool Match(ILNode other)
		{
			ILExpression expr = other as ILExpression;
			return expr != null && expr.Code == ILCode.Ldloc && v.MatchVariable(expr.Operand as ILVariable);
		}
	}
	
	public class AnyILExpression : ILExpression
	{
		public ILExpression LastMatch;
		
		public AnyILExpression() : base(ILCode.Pattern, null)
		{
		}
		
		public override bool Match(ILNode other)
		{
			if (other is ILExpression) {
				LastMatch = (ILExpression)other;
				return true;
			} else {
				return false;
			}
		}
	}
	
	public class ILCall : ILExpression
	{
		string fullClassName;
		string methodName;
		
		public ILCall(string fullClassName, string methodName, params ILExpression[] args) : base(ILCode.Pattern, null, args)
		{
			this.fullClassName = fullClassName;
			this.methodName = methodName;
		}
		
		public override bool Match(ILNode other)
		{
			ILExpression expr = other as ILExpression;
			if (expr != null && expr.Code == ILCode.Call) {
				MethodReference r = (MethodReference)expr.Operand;
				if (r.Name == methodName && r.DeclaringType.FullName == fullClassName)
					return Match(this.Arguments, expr.Arguments);
			}
			return false;
		}
	}
}
