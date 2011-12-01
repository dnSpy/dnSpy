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
		
		public override void ApplyInterningProvider(IInterningProvider provider)
		{
			base.ApplyInterningProvider(provider);
			constantValue = provider.Intern(constantValue);
		}
		
		public DefaultUnresolvedField()
		{
			this.EntityType = EntityType.Field;
		}
		
		public DefaultUnresolvedField(IUnresolvedTypeDefinition declaringType, string name)
		{
			this.EntityType = EntityType.Field;
			this.DeclaringTypeDefinition = declaringType;
			this.Name = name;
			if (declaringType != null)
				this.ParsedFile = declaringType.ParsedFile;
		}
		
		public bool IsConst {
			get { return constantValue != null; }
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
	}
}
