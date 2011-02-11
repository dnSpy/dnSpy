// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents a group of methods.
	/// </summary>
	public class MethodGroupResolveResult : ResolveResult
	{
		readonly ReadOnlyCollection<IMethod> methods;
		readonly ReadOnlyCollection<IType> typeArguments;
		readonly IType targetType;
		readonly string methodName;
		
		/// <summary>
		/// List of extension methods, used to avoid re-calculating it in ResolveInvocation() when it was already
		/// calculated by ResolveMemberAccess().
		/// </summary>
		internal List<List<IMethod>> ExtensionMethods;
		
		public MethodGroupResolveResult(IType targetType, string methodName, IList<IMethod> methods, IList<IType> typeArguments) : base(SharedTypes.UnknownType)
		{
			if (targetType == null)
				throw new ArgumentNullException("targetType");
			if (methods == null)
				throw new ArgumentNullException("methods");
			this.targetType = targetType;
			this.methodName = methodName;
			this.methods = new ReadOnlyCollection<IMethod>(methods);
			this.typeArguments = typeArguments != null ? new ReadOnlyCollection<IType>(typeArguments) : EmptyList<IType>.Instance;
		}
		
		public IType TargetType {
			get { return targetType; }
		}
		
		public string MethodName {
			get { return methodName; }
		}
		
		public ReadOnlyCollection<IMethod> Methods {
			get { return methods; }
		}
		
		public ReadOnlyCollection<IType> TypeArguments {
			get { return typeArguments; }
		}
		
		public override string ToString()
		{
			return string.Format("[{0} with {1} method(s)]", GetType().Name, methods.Count);
		}
	}
}
