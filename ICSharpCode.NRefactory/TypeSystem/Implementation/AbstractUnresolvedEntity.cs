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
		EntityType entityType;
		Accessibility accessibility;
		internal BitVector16 flags;
		
		// flags for AbstractUnresolvedEntity:
		internal const ushort FlagFrozen    = 0x0001;
		internal const ushort FlagSealed    = 0x0002;
		internal const ushort FlagAbstract  = 0x0004;
		internal const ushort FlagShadowing = 0x0008;
		internal const ushort FlagSynthetic = 0x0010;
		internal const ushort FlagStatic    = 0x0020;
		// flags for DefaultUnresolvedTypeDefinition
		internal const ushort FlagAddDefaultConstructorIfRequired = 0x0040;
		internal const ushort FlagHasExtensionMethods = 0x0080;
		internal const ushort FlagHasNoExtensionMethods = 0x0100;
		// flags for AbstractUnresolvedMember:
		internal const ushort FlagExplicitInterfaceImplementation = 0x0040;
		internal const ushort FlagVirtual = 0x0080;
		internal const ushort FlagOverride = 0x0100;
		// flags for DefaultField:
		internal const ushort FlagFieldIsReadOnly = 0x1000;
		internal const ushort FlagFieldIsVolatile = 0x2000;
		// flags for DefaultMethod:
		internal const ushort FlagExtensionMethod = 0x1000;
		
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
		
		public virtual void ApplyInterningProvider(IInterningProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");
			ThrowIfFrozen();
			name = provider.Intern(name);
			attributes = provider.InternList(attributes);
			if (rareFields != null)
				rareFields.ApplyInterningProvider(provider);
		}
		
		[Serializable]
		internal class RareFields
		{
			internal DomRegion region;
			internal DomRegion bodyRegion;
			internal IParsedFile parsedFile;
			
			protected internal virtual void FreezeInternal()
			{
			}
			
			public virtual void ApplyInterningProvider(IInterningProvider provider)
			{
			}
		}
		
		protected void ThrowIfFrozen()
		{
			FreezableHelper.ThrowIfFrozen(this);
		}
		
		public EntityType EntityType {
			get { return entityType; }
			set {
				ThrowIfFrozen();
				entityType = value;
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
		
		public IParsedFile ParsedFile {
			get { return rareFields != null ? rareFields.parsedFile : null; }
			set {
				if (value != null || rareFields != null)
					WriteRareFields().parsedFile = value;
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
					throw new ArgumentNullException();
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
