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
using System.Collections.Generic;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Base class for <see cref="IUnresolvedMember"/> implementations.
	/// </summary>
	[Serializable]
	public abstract class AbstractUnresolvedMember : AbstractUnresolvedEntity, IUnresolvedMember
	{
		ITypeReference returnType = SpecialType.UnknownType;
		IList<IMemberReference> interfaceImplementations;
		
		public override void ApplyInterningProvider(IInterningProvider provider)
		{
			base.ApplyInterningProvider(provider);
			returnType = provider.Intern(returnType);
			interfaceImplementations = provider.InternList(interfaceImplementations);
		}
		
		/*
		[Serializable]
		internal new class RareFields : AbstractUnresolvedEntity.RareFields
		{
			internal IList<IMemberReference> interfaceImplementations;
			
			public override void ApplyInterningProvider(IInterningProvider provider)
			{
				base.ApplyInterningProvider(provider);
				interfaceImplementations = provider.InternList(interfaceImplementations);
			}
			
			protected internal override void FreezeInternal()
			{
				interfaceImplementations = FreezableHelper.FreezeListAndElements(interfaceImplementations);
				base.FreezeInternal();
			}
		}
		
		internal override AbstractUnresolvedEntity.RareFields WriteRareFields()
		{
			ThrowIfFrozen();
			if (rareFields == null) rareFields = new RareFields();
			return rareFields;
		}*/
		
		public ITypeReference ReturnType {
			get { return returnType; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				ThrowIfFrozen();
				returnType = value;
			}
		}
		
		public bool IsExplicitInterfaceImplementation {
			get { return flags[FlagExplicitInterfaceImplementation]; }
			set {
				ThrowIfFrozen();
				flags[FlagExplicitInterfaceImplementation] = value;
			}
		}
		
		/*
		public IList<IMemberReference> InterfaceImplementations {
			get {
				RareFields rareFields = (RareFields)this.rareFields;
				if (rareFields == null || rareFields.interfaceImplementations == null) {
					rareFields = (RareFields)WriteRareFields();
					return rareFields.interfaceImplementations = new List<IMemberReference>();
				}
				return rareFields.interfaceImplementations;
			}
		}*/
		
		public IList<IMemberReference> ExplicitInterfaceImplementations {
			get {
				if (interfaceImplementations == null)
					interfaceImplementations = new List<IMemberReference>();
				return interfaceImplementations;
			}
		}
		
		public bool IsVirtual {
			get { return flags[FlagVirtual]; }
			set {
				ThrowIfFrozen();
				flags[FlagVirtual] = value;
			}
		}
		
		public bool IsOverride {
			get { return flags[FlagOverride]; }
			set {
				ThrowIfFrozen();
				flags[FlagOverride] = value;
			}
		}
		
		public bool IsOverridable {
			get {
				// override or virtual or abstract but not sealed
				return (flags.Data & (FlagOverride | FlagVirtual | FlagAbstract)) != 0 && !this.IsSealed;
			}
		}
		
		public abstract IMember CreateResolved(ITypeResolveContext context);
	}
}
