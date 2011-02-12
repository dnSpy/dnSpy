// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// ResolveResult representing a compile-time constant.
	/// </summary>
	public class ConstantResolveResult : ResolveResult
	{
		object constantValue;
		
		public ConstantResolveResult(IType type, object constantValue) : base(type)
		{
			this.constantValue = constantValue;
		}
		
		public override bool IsCompileTimeConstant {
			get { return true; }
		}
		
		public override object ConstantValue {
			get { return constantValue; }
		}
		
		public override string ToString()
		{
			return string.Format("[{0} {1} = {2}]", GetType().Name, this.Type, constantValue);
		}
	}
}
