// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IField"/>.
	/// </summary>
	public class DefaultField : AbstractMember, IField
	{
		IConstantValue constantValue;
		
		const ushort FlagIsReadOnly = 0x1000;
		const ushort FlagIsVolatile = 0x2000;
		
		protected override void FreezeInternal()
		{
			if (constantValue != null)
				constantValue.Freeze();
			base.FreezeInternal();
		}
		
		public DefaultField(ITypeDefinition declaringTypeDefinition, string name)
			: base(declaringTypeDefinition, name, EntityType.Field)
		{
		}
		
		protected DefaultField(IField f) : base(f)
		{
			this.constantValue = f.ConstantValue;
			this.IsReadOnly = f.IsReadOnly;
			this.IsVolatile = f.IsVolatile;
		}
		
		public override void ApplyInterningProvider(IInterningProvider provider)
		{
			base.ApplyInterningProvider(provider);
			if (provider != null)
				constantValue = provider.Intern(constantValue);
		}
		
		DomRegion IVariable.DeclarationRegion {
			get {
				return Region;
			}
		}
		
		public bool IsConst {
			get { return constantValue != null; }
		}
		
		public bool IsReadOnly {
			get { return flags[FlagIsReadOnly]; }
			set {
				CheckBeforeMutation();
				flags[FlagIsReadOnly] = value;
			}
		}
		
		public bool IsVolatile {
			get { return flags[FlagIsVolatile]; }
			set {
				CheckBeforeMutation();
				flags[FlagIsVolatile] = value;
			}
		}
		
		public IConstantValue ConstantValue {
			get { return constantValue; }
			set {
				CheckBeforeMutation();
				constantValue = value;
			}
		}
		
		ITypeReference IVariable.Type {
			get { return this.ReturnType; }
		}
	}
}
