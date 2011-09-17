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
	/// Default implementation of <see cref="IMethod" /> interface.
	/// </summary>
	[Serializable]
	public class DefaultMethod : AbstractMember, IMethod
	{
		IList<IAttribute> returnTypeAttributes;
		IList<ITypeParameter> typeParameters;
		IList<IParameter> parameters;
		
		const ushort FlagExtensionMethod = 0x1000;
		
		protected override void FreezeInternal()
		{
			returnTypeAttributes = FreezeList(returnTypeAttributes);
			typeParameters = FreezeList(typeParameters);
			parameters = FreezeList(parameters);
			base.FreezeInternal();
		}
		
		public DefaultMethod(ITypeDefinition declaringTypeDefinition, string name)
			: base(declaringTypeDefinition, name, EntityType.Method)
		{
		}
		
		/// <summary>
		/// Copy constructor
		/// </summary>
		protected DefaultMethod(IMethod method) : base(method)
		{
			returnTypeAttributes = CopyList(method.ReturnTypeAttributes);
			typeParameters = CopyList(method.TypeParameters);
			parameters = CopyList(method.Parameters);
			this.IsExtensionMethod = method.IsExtensionMethod;
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
		
		public IList<IAttribute> ReturnTypeAttributes {
			get {
				if (returnTypeAttributes == null)
					returnTypeAttributes = new List<IAttribute>();
				return returnTypeAttributes;
			}
		}
		
		public IList<ITypeParameter> TypeParameters {
			get {
				if (typeParameters == null)
					typeParameters = new List<ITypeParameter>();
				return typeParameters;
			}
		}
		
		public bool IsExtensionMethod {
			get { return flags[FlagExtensionMethod]; }
			set {
				CheckBeforeMutation();
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
		
		public IList<IParameter> Parameters {
			get {
				if (parameters == null)
					parameters = new List<IParameter>();
				return parameters;
			}
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder("[");
			b.Append(EntityType.ToString());
			b.Append(' ');
			b.Append(DeclaringType.Name);
			b.Append('.');
			b.Append(Name);
			b.Append('(');
			var p = this.Parameters;
			for (int i = 0; i < p.Count; i++) {
				if (i > 0) b.Append(", ");
				if (p[i] == null)
					b.Append("null");
				else
					b.Append(p[i].ToString());
			}
			b.Append("):");
			b.Append(ReturnType.ToString());
			b.Append(']');
			return b.ToString();
		}
		
		public static DefaultMethod CreateDefaultConstructor(ITypeDefinition typeDefinition)
		{
			DomRegion region = new DomRegion(typeDefinition.Region.FileName, typeDefinition.Region.BeginLine, typeDefinition.Region.BeginColumn);
			return new DefaultMethod(typeDefinition, ".ctor") {
				EntityType = EntityType.Constructor,
				Accessibility = typeDefinition.IsAbstract ? Accessibility.Protected : Accessibility.Public,
				IsSynthetic = true,
				Region = region,
				ReturnType = typeDefinition
			};
		}
	}
}
