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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IParameter"/>.
	/// </summary>
	public sealed class DefaultParameter : IParameter
	{
		readonly IType type;
		readonly string name;
		readonly DomRegion region;
		readonly IList<IAttribute> attributes;
		readonly bool isRef, isOut, isParams, isOptional;
		readonly object defaultValue;
		readonly IParameterizedMember owner;
		
		public DefaultParameter(IType type, string name)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (name == null)
				throw new ArgumentNullException("name");
			this.type = type;
			this.name = name;
		}
		
		public DefaultParameter(IType type, string name, IParameterizedMember owner = null, DomRegion region = default(DomRegion), IList<IAttribute> attributes = null,
		                        bool isRef = false, bool isOut = false, bool isParams = false, bool isOptional = false, object defaultValue = null)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (name == null)
				throw new ArgumentNullException("name");
			this.type = type;
			this.name = name;
			this.owner = owner;
			this.region = region;
			this.attributes = attributes;
			this.isRef = isRef;
			this.isOut = isOut;
			this.isParams = isParams;
			this.isOptional = isOptional;
			this.defaultValue = defaultValue;
		}
		
		SymbolKind ISymbol.SymbolKind {
			get { return SymbolKind.Parameter; }
		}
		
		public IParameterizedMember Owner {
			get { return owner; }
		}
		
		public IList<IAttribute> Attributes {
			get { return attributes; }
		}
		
		public bool IsRef {
			get { return isRef; }
		}
		
		public bool IsOut {
			get { return isOut; }
		}
		
		public bool IsParams {
			get { return isParams; }
		}
		
		public bool IsOptional {
			get { return isOptional; }
		}
		
		public string Name {
			get { return name; }
		}
		
		public DomRegion Region {
			get { return region; }
		}
		
		public IType Type {
			get { return type; }
		}
		
		bool IVariable.IsConst {
			get { return false; }
		}
		
		public object ConstantValue {
			get { return defaultValue; }
		}
		
		public override string ToString()
		{
			return ToString(this);
		}
		
		public static string ToString(IParameter parameter)
		{
			StringBuilder b = new StringBuilder();
			if (parameter.IsRef)
				b.Append("ref ");
			if (parameter.IsOut)
				b.Append("out ");
			if (parameter.IsParams)
				b.Append("params ");
			b.Append(parameter.Name);
			b.Append(':');
			b.Append(parameter.Type.ReflectionName);
			if (parameter.IsOptional) {
				b.Append(" = ");
				if (parameter.ConstantValue != null)
					b.Append(parameter.ConstantValue.ToString());
				else
					b.Append("null");
			}
			return b.ToString();
		}

		public ISymbolReference ToReference()
		{
			if (owner == null)
				return new ParameterReference(type.ToTypeReference(), name, region, isRef, isOut, isParams, isOptional, defaultValue);
			return new OwnedParameterReference(owner.ToReference(), owner.Parameters.IndexOf(this));
		}
	}
	
	sealed class OwnedParameterReference : ISymbolReference
	{
		readonly IMemberReference memberReference;
		readonly int index;
		
		public OwnedParameterReference(IMemberReference member, int index)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			this.memberReference = member;
			this.index = index;
		}
		
		public ISymbol Resolve(ITypeResolveContext context)
		{
			IParameterizedMember member = memberReference.Resolve(context) as IParameterizedMember;
			if (member != null && index >= 0 && index < member.Parameters.Count)
				return member.Parameters[index];
			else
				return null;
		}
	}
	
	public sealed class ParameterReference : ISymbolReference
	{
		readonly ITypeReference type;
		readonly string name;
		readonly DomRegion region;
		readonly bool isRef, isOut, isParams, isOptional;
		readonly object defaultValue;
		
		public ParameterReference(ITypeReference type, string name, DomRegion region, bool isRef, bool isOut, bool isParams, bool isOptional, object defaultValue)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (name == null)
				throw new ArgumentNullException("name");
			this.type = type;
			this.name = name;
			this.region = region;
			this.isRef = isRef;
			this.isOut = isOut;
			this.isParams = isParams;
			this.isOptional = isOptional;
			this.defaultValue = defaultValue;
		}

		public ISymbol Resolve(ITypeResolveContext context)
		{
			return new DefaultParameter(type.Resolve(context), name, region: region, isRef: isRef, isOut: isOut, isParams: isParams, isOptional: isOptional, defaultValue: defaultValue);
		}
	}
}
