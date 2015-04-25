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
using System.Text;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Base class for <see cref="IUnresolvedEntity"/> implementations.
	/// </summary>
	[Serializable]
	public abstract class AbstractUnresolvedEntity : IUnresolvedEntity, IFreezable
	{
		// possible optimizations to reduce the memory usage of AbstractUnresolvedEntity:
		// - store regions in more compact form (e.g. assume both file names are identical; use ushort for columns)
		
		IUnresolvedTypeDefinition declaringTypeDefinition;
		
		string name = string.Empty;
		IList<IUnresolvedAttribute> attributes;
		internal RareFields rareFields;
		
		// 1 byte per enum + 2 bytes for flags
		SymbolKind symbolKind;
		Accessibility accessibility;
		internal BitVector16 flags;
		
		// flags for AbstractUnresolvedEntity:
		internal const ushort FlagFrozen    = 0x0001;
		internal const ushort FlagSealed    = 0x0002;
		internal const ushort FlagAbstract  = 0x0004;
		internal const ushort FlagShadowing = 0x0008;
		internal const ushort FlagSynthetic = 0x0010;
		internal const ushort FlagStatic    = 0x0020;
		// flags for DefaultUnresolvedTypeDefinition/LazyCecilTypeDefinition
		internal const ushort FlagAddDefaultConstructorIfRequired = 0x0040;
		internal const ushort FlagHasExtensionMethods = 0x0080;
		internal const ushort FlagHasNoExtensionMethods = 0x0100;
		internal const ushort FlagPartialTypeDefinition = 0x0200;
		// flags for AbstractUnresolvedMember:
		internal const ushort FlagExplicitInterfaceImplementation = 0x0040;
		internal const ushort FlagVirtual = 0x0080;
		internal const ushort FlagOverride = 0x0100;
		// flags for DefaultField:
		internal const ushort FlagFieldIsReadOnly = 0x1000;
		internal const ushort FlagFieldIsVolatile = 0x2000;
		internal const ushort FlagFieldIsFixedSize = 0x4000;
		// flags for DefaultMethod:
		internal const ushort FlagExtensionMethod = 0x1000;
		internal const ushort FlagPartialMethod = 0x2000;
		internal const ushort FlagHasBody = 0x4000;
		internal const ushort FlagAsyncMethod = 0x8000;
		
		public bool IsFrozen {
			get { return flags[FlagFrozen]; }
		}
		
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
			if (rareFields != null)
				rareFields.FreezeInternal();
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
			ThrowIfFrozen();
			name = provider.Intern(name);
			attributes = provider.InternList(attributes);
			if (rareFields != null)
				rareFields.ApplyInterningProvider(provider);
		}
		
		/// <summary>
		/// Creates a shallow clone of this entity.
		/// Collections (e.g. a type's member list) will be cloned as well, but the elements
		/// of said list will not be.
		/// If this instance is frozen, the clone will be unfrozen.
		/// </summary>
		public virtual object Clone()
		{
			var copy = (AbstractUnresolvedEntity)MemberwiseClone();
			copy.flags[FlagFrozen] = false;
			if (attributes != null)
				copy.attributes = new List<IUnresolvedAttribute>(attributes);
			if (rareFields != null)
				copy.rareFields = (RareFields)rareFields.Clone();
			return copy;
		}
		
		[Serializable]
		internal class RareFields
		{
			internal DomRegion region;
			internal DomRegion bodyRegion;
			internal IUnresolvedFile unresolvedFile;
			
			protected internal virtual void FreezeInternal()
			{
			}
			
			public virtual void ApplyInterningProvider(InterningProvider provider)
			{
			}
			
			public virtual object Clone()
			{
				return MemberwiseClone();
			}
		}
		
		protected void ThrowIfFrozen()
		{
			FreezableHelper.ThrowIfFrozen(this);
		}
		
		public SymbolKind SymbolKind {
			get { return symbolKind; }
			set {
				ThrowIfFrozen();
				symbolKind = value;
			}
		}
		
		internal virtual RareFields WriteRareFields()
		{
			ThrowIfFrozen();
			if (rareFields == null) rareFields = new RareFields();
			return rareFields;
		}
		
		public DomRegion Region {
			get { return rareFields != null ? rareFields.region : DomRegion.Empty; }
			set {
				if (value != DomRegion.Empty || rareFields != null)
					WriteRareFields().region = value;
			}
		}
		
		public DomRegion BodyRegion {
			get { return rareFields != null ? rareFields.bodyRegion : DomRegion.Empty; }
			set {
				if (value != DomRegion.Empty || rareFields != null)
					WriteRareFields().bodyRegion = value;
			}
		}
		
		public IUnresolvedFile UnresolvedFile {
			get { return rareFields != null ? rareFields.unresolvedFile : null; }
			set {
				if (value != null || rareFields != null)
					WriteRareFields().unresolvedFile = value;
			}
		}
		
		public IUnresolvedTypeDefinition DeclaringTypeDefinition {
			get { return declaringTypeDefinition; }
			set {
				ThrowIfFrozen();
				declaringTypeDefinition = value;
			}
		}
		
		public IList<IUnresolvedAttribute> Attributes {
			get {
				if (attributes == null)
					attributes = new List<IUnresolvedAttribute>();
				return attributes;
			}
		}
		
		public string Name {
			get { return name; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				ThrowIfFrozen();
				name = value;
			}
		}
		
		public virtual string FullName {
			get {
				if (declaringTypeDefinition != null)
					return declaringTypeDefinition.FullName + "." + name;
				else if (!string.IsNullOrEmpty(this.Namespace))
					return this.Namespace + "." + name;
				else
					return name;
			}
		}
		
		public virtual string Namespace {
			get {
				if (declaringTypeDefinition != null)
					return declaringTypeDefinition.Namespace;
				else
					return string.Empty;
			}
			set {
				throw new NotSupportedException();
			}
		}
		
		public virtual string ReflectionName {
			get {
				if (declaringTypeDefinition != null)
					return declaringTypeDefinition.ReflectionName + "." + name;
				else
					return name;
			}
		}
		
		public Accessibility Accessibility {
			get { return accessibility; }
			set {
				ThrowIfFrozen();
				accessibility = value;
			}
		}
		
		public bool IsStatic {
			get { return flags[FlagStatic]; }
			set {
				ThrowIfFrozen();
				flags[FlagStatic] = value;
			}
		}
		
		public bool IsAbstract {
			get { return flags[FlagAbstract]; }
			set {
				ThrowIfFrozen();
				flags[FlagAbstract] = value;
			}
		}
		
		public bool IsSealed {
			get { return flags[FlagSealed]; }
			set {
				ThrowIfFrozen();
				flags[FlagSealed] = value;
			}
		}
		
		public bool IsShadowing {
			get { return flags[FlagShadowing]; }
			set {
				ThrowIfFrozen();
				flags[FlagShadowing] = value;
			}
		}
		
		public bool IsSynthetic {
			get { return flags[FlagSynthetic]; }
			set {
				ThrowIfFrozen();
				flags[FlagSynthetic] = value;
			}
		}
		
		bool IHasAccessibility.IsPrivate {
			get { return accessibility == Accessibility.Private; }
		}
		
		bool IHasAccessibility.IsPublic {
			get { return accessibility == Accessibility.Public; }
		}
		
		bool IHasAccessibility.IsProtected {
			get { return accessibility == Accessibility.Protected; }
		}
		
		bool IHasAccessibility.IsInternal {
			get { return accessibility == Accessibility.Internal; }
		}
		
		bool IHasAccessibility.IsProtectedOrInternal {
			get { return accessibility == Accessibility.ProtectedOrInternal; }
		}
		
		bool IHasAccessibility.IsProtectedAndInternal {
			get { return accessibility == Accessibility.ProtectedAndInternal; }
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder("[");
			b.Append(GetType().Name);
			b.Append(' ');
			if (this.DeclaringTypeDefinition != null) {
				b.Append(this.DeclaringTypeDefinition.Name);
				b.Append('.');
			}
			b.Append(this.Name);
			b.Append(']');
			return b.ToString();
		}
	}
}
