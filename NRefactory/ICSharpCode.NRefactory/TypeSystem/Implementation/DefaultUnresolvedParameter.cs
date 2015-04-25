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
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation for IUnresolvedParameter.
	/// </summary>
	[Serializable]
	public sealed class DefaultUnresolvedParameter : IUnresolvedParameter, IFreezable, ISupportsInterning
	{
		string name = string.Empty;
		ITypeReference type = SpecialType.UnknownType;
		IList<IUnresolvedAttribute> attributes;
		IConstantValue defaultValue;
		DomRegion region;
		byte flags;
		
		public DefaultUnresolvedParameter()
		{
		}
		
		public DefaultUnresolvedParameter(ITypeReference type, string name)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (name == null)
				throw new ArgumentNullException("name");
			this.type = type;
			this.name = name;
		}
		
		void FreezeInternal()
		{
			attributes = FreezableHelper.FreezeListAndElements(attributes);
			FreezableHelper.Freeze(defaultValue);
		}
		
		public string Name {
			get { return name; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				FreezableHelper.ThrowIfFrozen(this);
				name = value;
			}
		}
		
		public ITypeReference Type {
			get { return type; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				FreezableHelper.ThrowIfFrozen(this);
				type = value;
			}
		}
		
		public IList<IUnresolvedAttribute> Attributes {
			get {
				if (attributes == null)
					attributes = new List<IUnresolvedAttribute>();
				return attributes;
			}
		}
		
		public IConstantValue DefaultValue {
			get { return defaultValue; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				defaultValue = value;
			}
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				region = value;
			}
		}
		
		bool HasFlag(byte flag)
		{
			return (this.flags & flag) != 0;
		}
		void SetFlag(byte flag, bool value)
		{
			FreezableHelper.ThrowIfFrozen(this);
			if (value)
				this.flags |= flag;
			else
				this.flags &= unchecked((byte)~flag);
		}
		
		public bool IsFrozen {
			get { return HasFlag(1); }
		}
		
		public void Freeze()
		{
			if (!this.IsFrozen) {
				FreezeInternal();
				this.flags |= 1;
			}
		}
		
		public bool IsRef {
			get { return HasFlag(2); }
			set { SetFlag(2, value); }
		}
		
		public bool IsOut {
			get { return HasFlag(4); }
			set { SetFlag(4, value); }
		}
		
		public bool IsParams {
			get { return HasFlag(8); }
			set { SetFlag(8, value); }
		}
		
		public bool IsOptional {
			get { return this.DefaultValue != null; }
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				int hashCode = 1919191 ^ (flags & ~1);
				hashCode *= 31;
				hashCode += type.GetHashCode();
				hashCode *= 31;
				hashCode += name.GetHashCode();
				if (attributes != null) {
					foreach (var attr in attributes)
						hashCode ^= attr.GetHashCode ();
				}
				if (defaultValue != null)
					hashCode ^= defaultValue.GetHashCode ();
				return hashCode;
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			// compare everything except for the IsFrozen flag
			DefaultUnresolvedParameter p = other as DefaultUnresolvedParameter;
			return p != null && type == p.type && name == p.name &&
				defaultValue == p.defaultValue && region == p.region && (flags & ~1) == (p.flags & ~1) && ListEquals(attributes, p.attributes);
		}
		
		static bool ListEquals(IList<IUnresolvedAttribute> list1, IList<IUnresolvedAttribute> list2)
		{
			return (list1 ?? EmptyList<IUnresolvedAttribute>.Instance).SequenceEqual(list2 ?? EmptyList<IUnresolvedAttribute>.Instance);
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			if (IsRef)
				b.Append("ref ");
			if (IsOut)
				b.Append("out ");
			if (IsParams)
				b.Append("params ");
			b.Append(name);
			b.Append(':');
			b.Append(type.ToString());
			if (defaultValue != null) {
				b.Append(" = ");
				b.Append(defaultValue.ToString());
			}
			return b.ToString();
		}

		static bool IsOptionalAttribute (IType attributeType)
		{
			return attributeType.Name == "OptionalAttribute" && attributeType.Namespace == "System.Runtime.InteropServices";
		}
		
		public IParameter CreateResolvedParameter(ITypeResolveContext context)
		{
			Freeze();
			if (defaultValue != null) {
				return new ResolvedParameterWithDefaultValue(defaultValue, context) {
					Type = type.Resolve(context),
					Name = name,
					Region = region,
					Attributes = attributes.CreateResolvedAttributes(context),
					IsRef = this.IsRef,
					IsOut = this.IsOut,
					IsParams = this.IsParams
				};
			} else {
				var owner = context.CurrentMember as IParameterizedMember;
				var resolvedAttributes = attributes.CreateResolvedAttributes (context);
				bool isOptional = resolvedAttributes != null && resolvedAttributes.Any (a => IsOptionalAttribute (a.AttributeType));
				return new DefaultParameter (type.Resolve (context), name, owner, region,
				                             resolvedAttributes, IsRef, IsOut, IsParams, isOptional);
			}
		}
		
		sealed class ResolvedParameterWithDefaultValue : IParameter
		{
			readonly IConstantValue defaultValue;
			readonly ITypeResolveContext context;
			
			public ResolvedParameterWithDefaultValue(IConstantValue defaultValue, ITypeResolveContext context)
			{
				this.defaultValue = defaultValue;
				this.context = context;
			}
			
			SymbolKind ISymbol.SymbolKind { get { return SymbolKind.Parameter; } }
			public IParameterizedMember Owner { get { return context.CurrentMember as IParameterizedMember; } }
			public IType Type { get; internal set; }
			public string Name { get; internal set; }
			public DomRegion Region { get; internal set; }
			public IList<IAttribute> Attributes { get; internal set; }
			public bool IsRef { get; internal set; }
			public bool IsOut { get; internal set; }
			public bool IsParams { get; internal set; }
			public bool IsOptional { get { return true; } }
			bool IVariable.IsConst { get { return false; } }
			
			ResolveResult resolvedDefaultValue;
			
			public object ConstantValue {
				get {
					ResolveResult rr = LazyInit.VolatileRead(ref this.resolvedDefaultValue);
					if (rr == null) {
						rr = defaultValue.Resolve(context);
						LazyInit.GetOrSet(ref this.resolvedDefaultValue, rr);
					}
					return rr.ConstantValue;
				}
			}
			
			public override string ToString()
			{
				return DefaultParameter.ToString(this);
			}

			public ISymbolReference ToReference()
			{
				if (Owner == null)
					return new ParameterReference(Type.ToTypeReference(), Name, Region, IsRef, IsOut, IsParams, true, ConstantValue);
				return new OwnedParameterReference(Owner.ToReference(), Owner.Parameters.IndexOf(this));
			}
		}
	}
}
