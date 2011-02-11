// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents an unknown member.
	/// </summary>
	public class UnknownMemberResolveResult : ResolveResult
	{
		readonly IType targetType;
		readonly string memberName;
		readonly ReadOnlyCollection<IType> typeArguments;
		
		public UnknownMemberResolveResult(IType targetType, string memberName, IEnumerable<IType> typeArguments) : base(SharedTypes.UnknownType)
		{
			if (targetType == null)
				throw new ArgumentNullException("targetType");
			this.targetType = targetType;
			this.memberName = memberName;
			this.typeArguments = new ReadOnlyCollection<IType>(typeArguments.ToArray());
		}
		
		/// <summary>
		/// The type on which the method is being called.
		/// </summary>
		public IType TargetType {
			get { return targetType; }
		}
		
		public string MemberName {
			get { return memberName; }
		}
		
		public ReadOnlyCollection<IType> TypeArguments {
			get { return typeArguments; }
		}
		
		public override bool IsError {
			get { return true; }
		}
		
		public override string ToString()
		{
			return string.Format("[{0} {1}.{2}]", GetType().Name, targetType, memberName);
		}
	}
	
	/// <summary>
	/// Represents an unknown method.
	/// </summary>
	public class UnknownMethodResolveResult : UnknownMemberResolveResult
	{
		readonly ReadOnlyCollection<IParameter> parameters;
		
		public UnknownMethodResolveResult(IType targetType, string methodName, IEnumerable<IType> typeArguments, IEnumerable<IParameter> parameters) 
			: base(targetType, methodName, typeArguments)
		{
			this.parameters = new ReadOnlyCollection<IParameter>(parameters.ToArray());
		}
		
		public ReadOnlyCollection<IParameter> Parameters {
			get { return parameters; }
		}
	}
	
	/// <summary>
	/// Represents an unknown identifier.
	/// </summary>
	public class UnknownIdentifierResolveResult : ResolveResult
	{
		readonly string identifier;
		
		public UnknownIdentifierResolveResult(string identifier)
			: base(SharedTypes.UnknownType)
		{
			this.identifier = identifier;
		}
		
		public string Identifier {
			get { return identifier; }
		}
		
		public override bool IsError {
			get { return true; }
		}
		
		public override string ToString()
		{
			return string.Format("[{0} {1}]", GetType().Name, identifier);
		}
	}
}
