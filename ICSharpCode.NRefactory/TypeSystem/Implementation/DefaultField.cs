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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IField"/>.
	/// </summary>
	[Serializable]
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
