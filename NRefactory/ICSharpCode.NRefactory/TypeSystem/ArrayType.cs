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
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents an array type.
	/// </summary>
	[Serializable]
	public sealed class ArrayType : TypeWithElementType, ISupportsInterning
	{
		readonly int dimensions;
		
		public ArrayType(IType elementType, int dimensions = 1) : base(elementType)
		{
			if (dimensions <= 0)
				throw new ArgumentOutOfRangeException("dimensions", dimensions, "dimensions must be positive");
			this.dimensions = dimensions;
		}
		
		public override TypeKind Kind {
			get { return TypeKind.Array; }
		}
		
		public int Dimensions {
			get { return dimensions; }
		}
		
		public override string NameSuffix {
			get {
				return "[" + new string(',', dimensions-1) + "]";
			}
		}
		
		public override bool? IsReferenceType(ITypeResolveContext context)
		{
			return true;
		}
		
		public override int GetHashCode()
		{
			return unchecked(elementType.GetHashCode() * 71681 + dimensions);
		}
		
		public override bool Equals(IType other)
		{
			ArrayType a = other as ArrayType;
			return a != null && elementType.Equals(a.elementType) && a.dimensions == dimensions;
		}
		
		static readonly GetClassTypeReference systemArray = new GetClassTypeReference("System", "Array", 0);
		static readonly GetClassTypeReference listInterface = new GetClassTypeReference("System.Collections.Generic", "IList", 1);
		
		public override IEnumerable<IType> GetBaseTypes(ITypeResolveContext context)
		{
			List<IType> baseTypes = new List<IType>();
			IType t = systemArray.Resolve(context);
			if (t != SharedTypes.UnknownType)
				baseTypes.Add(t);
			if (dimensions == 1) { // single-dimensional arrays implement IList<T>
				ITypeDefinition def = listInterface.Resolve(context) as ITypeDefinition;
				if (def != null)
					baseTypes.Add(new ParameterizedType(def, new[] { elementType }));
			}
			return baseTypes;
		}
		
		public override IEnumerable<IMethod> GetMethods(ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return systemArray.Resolve(context).GetMethods(context, filter, options);
		}
		
		static readonly DefaultParameter indexerParam = new DefaultParameter(KnownTypeReference.Int32, string.Empty);
		
		public override IEnumerable<IProperty> GetProperties(ITypeResolveContext context, Predicate<IProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			ITypeDefinition arrayDef = systemArray.Resolve(context) as ITypeDefinition;
			if (arrayDef != null) {
				if ((options & GetMemberOptions.IgnoreInheritedMembers) == 0) {
					foreach (IProperty p in arrayDef.GetProperties(context, filter, options)) {
						yield return p;
					}
				}
				DefaultProperty indexer = new DefaultProperty(arrayDef, "Items") {
					EntityType = EntityType.Indexer,
					ReturnType = elementType,
					Accessibility = Accessibility.Public,
					Getter = DefaultAccessor.GetFromAccessibility(Accessibility.Public),
					Setter = DefaultAccessor.GetFromAccessibility(Accessibility.Public),
					IsSynthetic = true
				};
				for (int i = 0; i < dimensions; i++) {
					indexer.Parameters.Add(indexerParam);
				}
				indexer.Freeze();
				if (filter == null || filter(indexer)) {
					yield return indexer;
				}
			}
		}
		
		// Events, Fields: System.Array doesn't have any; so we can use the AbstractType default implementation
		// that simply returns an empty list
		
		public override IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitArrayType(this);
		}
		
		public override IType VisitChildren(TypeVisitor visitor)
		{
			IType e = elementType.AcceptVisitor(visitor);
			if (e == elementType)
				return this;
			else
				return new ArrayType(e, dimensions);
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			elementType = provider.Intern(elementType);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return elementType.GetHashCode() ^ dimensions;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ArrayType o = other as ArrayType;
			return o != null && elementType == o.elementType && dimensions == o.dimensions;
		}
	}
	
	[Serializable]
	public sealed class ArrayTypeReference : ITypeReference, ISupportsInterning
	{
		ITypeReference elementType;
		int dimensions;
		
		public ArrayTypeReference(ITypeReference elementType, int dimensions = 1)
		{
			if (elementType == null)
				throw new ArgumentNullException("elementType");
			if (dimensions <= 0)
				throw new ArgumentOutOfRangeException("dimensions", dimensions, "dimensions must be positive");
			this.elementType = elementType;
			this.dimensions = dimensions;
		}
		
		public ITypeReference ElementType {
			get { return elementType; }
		}
		
		public int Dimensions {
			get { return dimensions; }
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			return new ArrayType(elementType.Resolve(context), dimensions);
		}
		
		public override string ToString()
		{
			return elementType.ToString() + "[" + new string(',', dimensions - 1) + "]";
		}
		
		public static ITypeReference Create(ITypeReference elementType, int dimensions)
		{
			if (elementType is IType)
				return new ArrayType((IType)elementType, dimensions);
			else
				return new ArrayTypeReference(elementType, dimensions);
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			elementType = provider.Intern(elementType);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return elementType.GetHashCode() ^ dimensions;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			ArrayTypeReference o = other as ArrayTypeReference;
			return o != null && elementType == o.elementType && dimensions == o.dimensions;
		}
	}
}
