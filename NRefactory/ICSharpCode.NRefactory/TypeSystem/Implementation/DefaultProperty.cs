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
	/// Default implementation of <see cref="IProperty"/>.
	/// </summary>
	[Serializable]
	public class DefaultProperty : AbstractMember, IProperty
	{
		IAccessor getter, setter;
		IList<IParameter> parameters;
		
		protected override void FreezeInternal()
		{
			parameters = FreezeList(parameters);
			if (getter != null) getter.Freeze();
			if (setter != null) setter.Freeze();
			base.FreezeInternal();
		}
		
		public DefaultProperty(ITypeDefinition declaringTypeDefinition, string name)
			: base(declaringTypeDefinition, name, EntityType.Property)
		{
		}
		
		protected DefaultProperty(IProperty p) : base(p)
		{
			this.getter = p.Getter;
			this.setter = p.Setter;
			this.parameters = CopyList(p.Parameters);
		}
		
		public override void ApplyInterningProvider(IInterningProvider provider)
		{
			base.ApplyInterningProvider(provider);
			if (provider != null) {
				getter = provider.Intern(getter);
				setter = provider.Intern(setter);
				parameters = provider.InternList(parameters);
			}
		}
		
		public bool IsIndexer {
			get { return this.EntityType == EntityType.Indexer; }
		}
		
		public IList<IParameter> Parameters {
			get {
				if (parameters == null)
					parameters = new List<IParameter>();
				return parameters;
			}
		}
		
		public bool CanGet {
			get { return getter != null; }
		}
		
		public bool CanSet {
			get { return setter != null; }
		}
		
		public IAccessor Getter{
			get { return getter; }
			set {
				CheckBeforeMutation();
				getter = value;
			}
		}
		
		public IAccessor Setter {
			get { return setter; }
			set {
				CheckBeforeMutation();
				setter = value;
			}
		}
	}
}
