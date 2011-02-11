// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IProperty"/>.
	/// </summary>
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
