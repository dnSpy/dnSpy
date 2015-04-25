// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Globalization;

using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IUnresolvedTypeParameter"/>.
	/// </summary>
	[Serializable]
	public class DefaultUnresolvedTypeParameter : IUnresolvedTypeParameter, IFreezable
	{
		readonly int index;
		IList<IUnresolvedAttribute> attributes;
		IList<ITypeReference> constraints;
		string name;
		DomRegion region;
		
		SymbolKind ownerType;
		VarianceModifier variance;
		BitVector16 flags;
		const ushort FlagFrozen                       = 0x0001;
		const ushort FlagReferenceTypeConstraint      = 0x0002;
		const ushort FlagValueTypeConstraint          = 0x0004;
		const ushort FlagDefaultConstructorConstraint = 0x0008;
		
		public void Freeze()
		{
			if (!flags[FlagFrozen]) {
				FreezeInternal();
				flags[FlagFrozen] = true;
			}
		}
		
		protected virtual void FreezeInternal()
		{
			attributes = FreezableHelper.FreezeListAndElements(attributes);
			constraints = FreezableHelper.FreezeList(constraints);
		}
		
		public DefaultUnresolvedTypeParameter(SymbolKind ownerType, int index, string name = null)
		{
			this.ownerType = ownerType;
			this.index = index;
			this.name = name ?? ((ownerType == SymbolKind.Method ? "!!" : "!") + index.ToString(CultureInfo.InvariantCulture));
		}
		
		public SymbolKind OwnerType {
			get { return ownerType; }
		}
		
		public int Index {
			get { return index; }
		}
		
		public bool IsFrozen {
			get { return flags[FlagFrozen]; }
		}
		
		public string Name {
			get { return name; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				name = value;
			}
		}
		
		string INamedElement.FullName {
			get { return name; }
		}
		
		string INamedElement.Namespace {
			get { return string.Empty; }
		}
		
		string INamedElement.ReflectionName {
			get {
				if (ownerType == SymbolKind.Method)
					return "``" + index.ToString(CultureInfo.InvariantCulture);
				else
					return "`" + index.ToString(CultureInfo.InvariantCulture);
			}
		}
		
		public IList<IUnresolvedAttribute> Attributes {
			get {
				if (attributes == null)
					attributes = new List<IUnresolvedAttribute>();
				return attributes;
			}
		}
		
		public IList<ITypeReference> Constraints {
			get {
				if (constraints == null)
					constraints = new List<ITypeReference>();
				return constraints;
			}
		}
		
		public VarianceModifier Variance {
			get { return variance; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				variance = value;
			}
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				region = value;
			}
		}
		
		public bool HasDefaultConstructorConstraint {
			get { return flags[FlagDefaultConstructorConstraint]; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				flags[FlagDefaultConstructorConstraint] = value;
			}
		}
		
		public bool HasReferenceTypeConstraint {
			get { return flags[FlagReferenceTypeConstraint]; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				flags[FlagReferenceTypeConstraint] = value;
			}
		}
		
		public bool HasValueTypeConstraint {
			get { return flags[FlagValueTypeConstraint]; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				flags[FlagValueTypeConstraint] = value;
			}
		}
		
		/// <summary>
		/// Uses the specified interning provider to intern
		/// strings and lists in this entity.
		/// This method does not test arbitrary objects to see if they implement ISupportsInterning;
		/// instead we assume that those are interned immediately when they are created (before they are added to this entity).
		/// </summary>
		public virtual void ApplyInterningProvider(InterningProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");
			FreezableHelper.ThrowIfFrozen(this);
			name = provider.Intern(name);
			attributes = provider.InternList(attributes);
			constraints = provider.InternList(constraints);
		}
		
		public virtual ITypeParameter CreateResolvedTypeParameter(ITypeResolveContext context)
		{
			IEntity owner = null;
			if (this.OwnerType == SymbolKind.Method) {
				owner = context.CurrentMember as IMethod;
			} else if (this.OwnerType == SymbolKind.TypeDefinition) {
				owner = context.CurrentTypeDefinition;
			}
			if (owner == null)
				throw new InvalidOperationException("Could not determine the type parameter's owner.");
			return new DefaultTypeParameter(
				owner, index, name, variance,
				this.Attributes.CreateResolvedAttributes(context), this.Region,
				this.HasValueTypeConstraint, this.HasReferenceTypeConstraint, this.HasDefaultConstructorConstraint, this.Constraints.Resolve(context)
			);
		}
	}
}
