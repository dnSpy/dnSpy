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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IUnresolvedMethod" /> interface.
	/// </summary>
	[Serializable]
	public class DefaultUnresolvedMethod : AbstractUnresolvedMember, IUnresolvedMethod
	{
		IList<IUnresolvedAttribute> returnTypeAttributes;
		IList<IUnresolvedTypeParameter> typeParameters;
		IList<IUnresolvedParameter> parameters;
		IUnresolvedMember accessorOwner;
		
		protected override void FreezeInternal()
		{
			returnTypeAttributes = FreezableHelper.FreezeListAndElements(returnTypeAttributes);
			typeParameters = FreezableHelper.FreezeListAndElements(typeParameters);
			parameters = FreezableHelper.FreezeListAndElements(parameters);
			base.FreezeInternal();
		}
		
		public override object Clone()
		{
			var copy = (DefaultUnresolvedMethod)base.Clone();
			if (returnTypeAttributes != null)
				copy.returnTypeAttributes = new List<IUnresolvedAttribute>(returnTypeAttributes);
			if (typeParameters != null)
				copy.typeParameters = new List<IUnresolvedTypeParameter>(typeParameters);
			if (parameters != null)
				copy.parameters = new List<IUnresolvedParameter>(parameters);
			return copy;
		}
		
		public override void ApplyInterningProvider(InterningProvider provider)
		{
			base.ApplyInterningProvider(provider);
			if (provider != null) {
				returnTypeAttributes = provider.InternList(returnTypeAttributes);
				typeParameters = provider.InternList(typeParameters);
				parameters = provider.InternList(parameters);
			}
		}
		
		public DefaultUnresolvedMethod()
		{
			this.SymbolKind = SymbolKind.Method;
		}
		
		public DefaultUnresolvedMethod(IUnresolvedTypeDefinition declaringType, string name)
		{
			this.SymbolKind = SymbolKind.Method;
			this.DeclaringTypeDefinition = declaringType;
			this.Name = name;
			if (declaringType != null)
				this.UnresolvedFile = declaringType.UnresolvedFile;
		}
		
		public IList<IUnresolvedAttribute> ReturnTypeAttributes {
			get {
				if (returnTypeAttributes == null)
					returnTypeAttributes = new List<IUnresolvedAttribute>();
				return returnTypeAttributes;
			}
		}
		
		public IList<IUnresolvedTypeParameter> TypeParameters {
			get {
				if (typeParameters == null)
					typeParameters = new List<IUnresolvedTypeParameter>();
				return typeParameters;
			}
		}
		
		public bool IsExtensionMethod {
			get { return flags[FlagExtensionMethod]; }
			set {
				ThrowIfFrozen();
				flags[FlagExtensionMethod] = value;
			}
		}
		
		public bool IsConstructor {
			get { return this.SymbolKind == SymbolKind.Constructor; }
		}
		
		public bool IsDestructor {
			get { return this.SymbolKind == SymbolKind.Destructor; }
		}
		
		public bool IsOperator {
			get { return this.SymbolKind == SymbolKind.Operator; }
		}
		
		public bool IsPartial {
			get { return flags[FlagPartialMethod]; }
			set {
				ThrowIfFrozen();
				flags[FlagPartialMethod] = value;
			}
		}

		public bool IsAsync {
			get { return flags[FlagAsyncMethod]; }
			set {
				ThrowIfFrozen();
				flags[FlagAsyncMethod] = value;
			}
		}

		public bool HasBody {
			get { return flags[FlagHasBody]; }
			set {
				ThrowIfFrozen();
				flags[FlagHasBody] = value;
			}
		}
		
		[Obsolete]
		public bool IsPartialMethodDeclaration {
			get { return IsPartial && !HasBody; }
			set {
				if (value) {
					IsPartial = true;
					HasBody = false;
				} else if (!value && IsPartial && !HasBody) {
					IsPartial = false;
				}
			}
		}
		
		[Obsolete]
		public bool IsPartialMethodImplementation {
			get { return IsPartial && HasBody; }
			set {
				if (value) {
					IsPartial = true;
					HasBody = true;
				} else if (!value && IsPartial && HasBody) {
					IsPartial = false;
				}
			}
		}
		
		public IList<IUnresolvedParameter> Parameters {
			get {
				if (parameters == null)
					parameters = new List<IUnresolvedParameter>();
				return parameters;
			}
		}
		
		public IUnresolvedMember AccessorOwner {
			get { return accessorOwner; }
			set {
				ThrowIfFrozen();
				accessorOwner = value;
			}
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder("[");
			b.Append(SymbolKind.ToString());
			b.Append(' ');
			if (DeclaringTypeDefinition != null) {
				b.Append(DeclaringTypeDefinition.Name);
				b.Append('.');
			}
			b.Append(Name);
			b.Append('(');
			b.Append(string.Join(", ", this.Parameters));
			b.Append("):");
			b.Append(ReturnType.ToString());
			b.Append(']');
			return b.ToString();
		}
		
		public override IMember CreateResolved(ITypeResolveContext context)
		{
			return new DefaultResolvedMethod(this, context);
		}
		
		public override IMember Resolve(ITypeResolveContext context)
		{
			if (accessorOwner != null) {
				var owner = accessorOwner.Resolve(context);
				if (owner != null) {
					IProperty p = owner as IProperty;
					if (p != null) {
						if (p.CanGet && p.Getter.Name == this.Name)
							return p.Getter;
						if (p.CanSet && p.Setter.Name == this.Name)
							return p.Setter;
					}
					IEvent e = owner as IEvent;
					if (e != null) {
						if (e.CanAdd && e.AddAccessor.Name == this.Name)
							return e.AddAccessor;
						if (e.CanRemove && e.RemoveAccessor.Name == this.Name)
							return e.RemoveAccessor;
						if (e.CanInvoke && e.InvokeAccessor.Name == this.Name)
							return e.InvokeAccessor;
					}
				}
				return null;
			}
			
			ITypeReference interfaceTypeReference = null;
			if (this.IsExplicitInterfaceImplementation && this.ExplicitInterfaceImplementations.Count == 1)
				interfaceTypeReference = this.ExplicitInterfaceImplementations[0].DeclaringTypeReference;
			return Resolve(ExtendContextForType(context, this.DeclaringTypeDefinition),
			               this.SymbolKind, this.Name, interfaceTypeReference,
			               this.TypeParameters.Select(tp => tp.Name).ToList(),
			               this.Parameters.Select(p => p.Type).ToList());
		}
		
		IMethod IUnresolvedMethod.Resolve(ITypeResolveContext context)
		{
			return (IMethod)Resolve(context);
		}
		
		public static DefaultUnresolvedMethod CreateDefaultConstructor(IUnresolvedTypeDefinition typeDefinition)
		{
			if (typeDefinition == null)
				throw new ArgumentNullException("typeDefinition");
			DomRegion region = typeDefinition.Region;
			region = new DomRegion(region.FileName, region.BeginLine, region.BeginColumn); // remove endline/endcolumn
			return new DefaultUnresolvedMethod(typeDefinition, ".ctor") {
				SymbolKind = SymbolKind.Constructor,
				Accessibility = typeDefinition.IsAbstract ? Accessibility.Protected : Accessibility.Public,
				IsSynthetic = true,
				HasBody = true,
				Region = region,
				BodyRegion = region,
				ReturnType = KnownTypeReference.Void
			};
		}
		
		static readonly IUnresolvedMethod dummyConstructor = CreateDummyConstructor();
		
		/// <summary>
		/// Returns a dummy constructor instance:
		/// </summary>
		/// <returns>
		/// A public instance constructor with IsSynthetic=true and no declaring type.
		/// </returns>
		public static IUnresolvedMethod DummyConstructor {
			get { return dummyConstructor; }
		}
		
		static IUnresolvedMethod CreateDummyConstructor()
		{
			var m = new DefaultUnresolvedMethod {
				SymbolKind = SymbolKind.Constructor,
				Name = ".ctor",
				Accessibility = Accessibility.Public,
				IsSynthetic = true,
				ReturnType = KnownTypeReference.Void
			};
			m.Freeze();
			return m;
		}
	}
}
