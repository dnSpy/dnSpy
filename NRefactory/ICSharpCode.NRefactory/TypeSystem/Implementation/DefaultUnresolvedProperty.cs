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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IUnresolvedProperty"/>.
	/// </summary>
	[Serializable]
	public class DefaultUnresolvedProperty : AbstractUnresolvedMember, IUnresolvedProperty
	{
		IUnresolvedAccessor getter, setter;
		IList<IUnresolvedParameter> parameters;
		
		protected override void FreezeInternal()
		{
			parameters = FreezableHelper.FreezeListAndElements(parameters);
			FreezableHelper.Freeze(getter);
			FreezableHelper.Freeze(setter);
			base.FreezeInternal();
		}
		
		public override void ApplyInterningProvider(IInterningProvider provider)
		{
			base.ApplyInterningProvider(provider);
			getter = provider.Intern(getter);
			setter = provider.Intern(setter);
			parameters = provider.InternList(parameters);
		}
		
		public DefaultUnresolvedProperty()
		{
			this.EntityType = EntityType.Property;
		}
		
		public DefaultUnresolvedProperty(IUnresolvedTypeDefinition declaringType, string name)
		{
			this.EntityType = EntityType.Property;
			this.DeclaringTypeDefinition = declaringType;
			this.Name = name;
			if (declaringType != null)
				this.ParsedFile = declaringType.ParsedFile;
		}
		
		public bool IsIndexer {
			get { return this.EntityType == EntityType.Indexer; }
		}
		
		public IList<IUnresolvedParameter> Parameters {
			get {
				if (parameters == null)
					parameters = new List<IUnresolvedParameter>();
				return parameters;
			}
		}
		
		public bool CanGet {
			get { return getter != null; }
		}
		
		public bool CanSet {
			get { return setter != null; }
		}
		
		public IUnresolvedAccessor Getter {
			get { return getter; }
			set {
				ThrowIfFrozen();
				getter = value;
			}
		}
		
		public IUnresolvedAccessor Setter {
			get { return setter; }
			set {
				ThrowIfFrozen();
				setter = value;
			}
		}
		
		public override IMember CreateResolved(ITypeResolveContext context)
		{
			return new DefaultResolvedProperty(this, context);
		}
	}
}
