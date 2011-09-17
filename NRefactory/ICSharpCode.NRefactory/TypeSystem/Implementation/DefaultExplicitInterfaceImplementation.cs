// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation for IExplicitInterfaceImplementation.
	/// </summary>
	[Serializable]
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
