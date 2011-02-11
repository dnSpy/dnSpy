// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents the result of resolving an expression.
	/// </summary>
	public class ResolveResult
	{
		IType type;
		
		public ResolveResult(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			this.type = type;
		}
		
		public IType Type {
			get { return type; }
		}
		
		public virtual bool IsCompileTimeConstant {
			get { return false; }
		}
		
		public virtual object ConstantValue {
			get { return null; }
		}
		
		public virtual bool IsError {
			get { return false; }
		}
		
		public override string ToString()
		{
			return "[" + GetType().Name + " " + type + "]";
		}
	}
}
