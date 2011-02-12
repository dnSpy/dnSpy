// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation for IExplicitInterfaceImplementation.
	/// </summary>
	public sealed class DefaultExplicitInterfaceImplementation : Immutable, IExplicitInterfaceImplementation, ISupportsInterning
	{
		public ITypeReference InterfaceType { get; private set; }
		public string MemberName { get; private set; }
		
		public DefaultExplicitInterfaceImplementation(ITypeReference interfaceType, string memberName)
		{
			if (interfaceType == null)
				throw new ArgumentNullException("interfaceType");
			if (memberName == null)
				throw new ArgumentNullException("memberName");
			this.InterfaceType = interfaceType;
			this.MemberName = memberName;
		}
		
		public override string ToString()
		{
			return InterfaceType + "." + MemberName;
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			InterfaceType = provider.Intern(InterfaceType);
			MemberName = provider.Intern(MemberName);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return InterfaceType.GetHashCode() ^ MemberName.GetHashCode();
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			DefaultExplicitInterfaceImplementation o = other as DefaultExplicitInterfaceImplementation;
			return InterfaceType == o.InterfaceType && MemberName == o.MemberName;
		}
	}
}
