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
		
		protected override void FreezeInternal()
		{
			returnTypeAttributes = FreezableHelper.FreezeListAndElements(returnTypeAttributes);
			typeParameters = FreezableHelper.FreezeListAndElements(typeParameters);
			parameters = FreezableHelper.FreezeListAndElements(parameters);
			base.FreezeInternal();
		}
		
		public override void ApplyInterningProvider(IInterningProvider provider)
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
			this.EntityType = EntityType.Method;
		}
		
		public DefaultUnresolvedMethod(IUnresolvedTypeDefinition declaringType, string name)
		{
			this.EntityType = EntityType.Method;
			this.DeclaringTypeDefinition = declaringType;
			this.Name = name;
			if (declaringType != null)
				this.ParsedFile = declaringType.ParsedFile;
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
			get { return this.EntityType == EntityType.Constructor; }
		}
		
		public bool IsDestructor {
			get { return this.EntityType == EntityType.Destructor; }
		}
		
		public bool IsOperator {
			get { return this.EntityType == EntityType.Operator; }
		}
		
		public bool IsPartialMethodDeclaration {
			get { return flags[FlagPartialMethodDeclaration]; }
			set {
				ThrowIfFrozen();
				flags[FlagPartialMethodDeclaration] = value;
			}
		}
		
		public bool IsPartialMethodImplementation {
			get { return flags[FlagPartialMethodImplemenation]; }
			set {
				ThrowIfFrozen();
				flags[FlagPartialMethodImplemenation] = value;
			}
		}
		
		public IList<IUnresolvedParameter> Parameters {
			get {
				if (parameters == null)
					parameters = new List<IUnresolvedParameter>();
				return parameters;
			}
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder("[");
			b.Append(EntityType.ToString());
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
		
		public static DefaultUnresolvedMethod CreateDefaultConstructor(IUnresolvedTypeDefinition typeDefinition)
		{
			if (typeDefinition == null)
				throw new ArgumentNullException("typeDefinition");
			DomRegion region = typeDefinition.Region;
			region = new DomRegion(region.FileName, region.BeginLine, region.BeginColumn); // remove endline/endcolumn
			return new DefaultUnresolvedMethod(typeDefinition, ".ctor") {
				EntityType = EntityType.Constructor,
				Accessibility = typeDefinition.IsAbstract ? Accessibility.Protected : Accessibility.Public,
				IsSynthetic = true,
				Region = region,
				ReturnType = KnownTypeReference.Void
			};
		}
	}
}
