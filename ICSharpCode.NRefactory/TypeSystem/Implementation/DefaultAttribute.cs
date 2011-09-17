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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IAttribute"/>.
	/// </summary>
	[Serializable]
	public sealed class DefaultAttribute : AbstractFreezable, IAttribute, ISupportsInterning
	{
		ITypeReference attributeType;
		readonly ITypeReference[] constructorParameterTypes;
		DomRegion region;
		IList<IConstantValue> positionalArguments;
		IList<KeyValuePair<string, IConstantValue>> namedArguments;
		
		protected override void FreezeInternal()
		{
			positionalArguments = FreezeList(positionalArguments);
			
			if (namedArguments == null || namedArguments.Count == 0) {
				namedArguments = EmptyList<KeyValuePair<string, IConstantValue>>.Instance;
			} else {
				namedArguments = Array.AsReadOnly(namedArguments.ToArray());
				foreach (var pair in namedArguments) {
					pair.Value.Freeze();
				}
			}
			
			base.FreezeInternal();
		}
		
		public DefaultAttribute(ITypeReference attributeType, IEnumerable<ITypeReference> constructorParameterTypes)
		{
			if (attributeType == null)
				throw new ArgumentNullException("attributeType");
			this.attributeType = attributeType;
			this.constructorParameterTypes = constructorParameterTypes != null ? constructorParameterTypes.ToArray() : null;
		}
		
		public ITypeReference AttributeType {
			get { return attributeType; }
		}
		
		public ReadOnlyCollection<ITypeReference> ConstructorParameterTypes {
			get { return Array.AsReadOnly(constructorParameterTypes); }
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				CheckBeforeMutation();
				region = value;
			}
		}
		
		public IList<IConstantValue> PositionalArguments {
			get {
				if (positionalArguments == null)
					positionalArguments = new List<IConstantValue>();
				return positionalArguments;
			}
		}
		
		public IList<ResolveResult> GetPositionalArguments(ITypeResolveContext context)
		{
			return this.PositionalArguments.Select(a => a.Resolve(context)).ToList();
		}
		
		public IList<KeyValuePair<string, IConstantValue>> NamedArguments {
			get {
				if (namedArguments == null)
					namedArguments = new List<KeyValuePair<string, IConstantValue>>();
				return namedArguments;
			}
		}
		
		public IList<KeyValuePair<string, ResolveResult>> GetNamedArguments(ITypeResolveContext context)
		{
			return this.NamedArguments.Select(p => new KeyValuePair<string, ResolveResult>(p.Key, p.Value.Resolve(context))).ToList();
		}
		
		public IMethod ResolveConstructor(ITypeResolveContext context)
		{
			IType[] parameterTypes = null;
			if (constructorParameterTypes != null && constructorParameterTypes.Length > 0) {
				parameterTypes = new IType[constructorParameterTypes.Length];
				for (int i = 0; i < parameterTypes.Length; i++) {
					parameterTypes[i] = constructorParameterTypes[i].Resolve(context);
				}
			}
			IMethod bestMatch = null;
			foreach (IMethod ctor in attributeType.Resolve(context).GetConstructors(context)) {
				if (ctor.IsStatic)
					continue;
				if (parameterTypes == null) {
					if (ctor.Parameters.Count == 0)
						return ctor;
				} else if (ctor.Parameters.Count == parameterTypes.Length) {
					bestMatch = ctor;
					bool ok = true;
					for (int i = 0; i < parameterTypes.Length; i++) {
						if (ctor.Parameters[i].Type != parameterTypes[i]) {
							ok = false;
							break;
						}
					}
					if (ok)
						return ctor;
				}
			}
			return bestMatch;
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append('[');
			b.Append(attributeType.ToString());
			if (this.PositionalArguments.Count + this.NamedArguments.Count > 0) {
				b.Append('(');
				bool first = true;
				foreach (var element in this.PositionalArguments) {
					if (first) first = false; else b.Append(", ");
					b.Append(element.ToString());
				}
				foreach (var pair in this.NamedArguments) {
					if (first) first = false; else b.Append(", ");
					b.Append(pair.Key);
					b.Append('=');
					b.Append(pair.Value.ToString());
				}
				b.Append(')');
			}
			b.Append(']');
			return b.ToString();
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			attributeType = provider.Intern(attributeType);
			if (constructorParameterTypes != null) {
				for (int i = 0; i < constructorParameterTypes.Length; i++) {
					constructorParameterTypes[i] = provider.Intern(constructorParameterTypes[i]);
				}
			}
			positionalArguments = provider.InternList(positionalArguments);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return attributeType.GetHashCode() ^ (positionalArguments != null ? positionalArguments.GetHashCode() : 0) ^ (namedArguments != null ? namedArguments.GetHashCode() : 0) ^ region.GetHashCode();
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			DefaultAttribute a = other as DefaultAttribute;
			return a != null && attributeType == a.attributeType && positionalArguments == a.positionalArguments && namedArguments == a.namedArguments && region == a.region;
		}
		
		public void AddNamedArgument(string name, ITypeReference type, object value)
		{
			AddNamedArgument(name, new SimpleConstantValue(type, value));
		}
		
		public void AddNamedArgument(string name, IConstantValue value)
		{
			CheckBeforeMutation();
			this.NamedArguments.Add(new KeyValuePair<string, IConstantValue>(name, value));
		}
	}
}
