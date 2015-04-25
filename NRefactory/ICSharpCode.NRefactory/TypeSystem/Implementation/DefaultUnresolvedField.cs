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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IUnresolvedField"/>.
	/// </summary>
	[Serializable]
	public class DefaultUnresolvedField : AbstractUnresolvedMember, IUnresolvedField
	{
		IConstantValue constantValue;
		
		protected override void FreezeInternal()
		{
			FreezableHelper.Freeze(constantValue);
			base.FreezeInternal();
		}
		
		public DefaultUnresolvedField()
		{
			this.SymbolKind = SymbolKind.Field;
		}
		
		public DefaultUnresolvedField(IUnresolvedTypeDefinition declaringType, string name)
		{
			this.SymbolKind = SymbolKind.Field;
			this.DeclaringTypeDefinition = declaringType;
			this.Name = name;
			if (declaringType != null)
				this.UnresolvedFile = declaringType.UnresolvedFile;
		}
		
		public bool IsConst {
			get { return constantValue != null && !IsFixed; }
		}
		
		public bool IsReadOnly {
			get { return flags[FlagFieldIsReadOnly]; }
			set {
				ThrowIfFrozen();
				flags[FlagFieldIsReadOnly] = value;
			}
		}
		
		public bool IsVolatile {
			get { return flags[FlagFieldIsVolatile]; }
			set {
				ThrowIfFrozen();
				flags[FlagFieldIsVolatile] = value;
			}
		}

		public bool IsFixed {
			get { return flags[FlagFieldIsFixedSize]; }
			set {
				ThrowIfFrozen();
				flags[FlagFieldIsFixedSize] = value;
			}
		}
		
		public IConstantValue ConstantValue {
			get { return constantValue; }
			set {
				ThrowIfFrozen();
				constantValue = value;
			}
		}
		
		public override IMember CreateResolved(ITypeResolveContext context)
		{
			return new DefaultResolvedField(this, context);
		}
		
		IField IUnresolvedField.Resolve(ITypeResolveContext context)
		{
			return (IField)Resolve(context);
		}
	}
}
