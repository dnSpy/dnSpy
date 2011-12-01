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
using System.Threading;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public sealed class DummyTypeParameter : AbstractType, ITypeParameter
	{
		static ITypeParameter[] methodTypeParameters = { new DummyTypeParameter(EntityType.Method, 0) };
		static ITypeParameter[] classTypeParameters = { new DummyTypeParameter(EntityType.TypeDefinition, 0) };
		
		public static ITypeParameter GetMethodTypeParameter(int index)
		{
			return GetTypeParameter(ref methodTypeParameters, EntityType.Method, index);
		}
		
		public static ITypeParameter GetClassTypeParameter(int index)
		{
			return GetTypeParameter(ref classTypeParameters, EntityType.TypeDefinition, index);
		}
		
		static ITypeParameter GetTypeParameter(ref ITypeParameter[] typeParameters, EntityType entityType, int index)
		{
			ITypeParameter[] tps = typeParameters;
			while (index >= tps.Length) {
				// We don't have a normal type parameter for this index, so we need to extend our array.
				// Because the array can be used concurrently from multiple threads, we have to use
				// Interlocked.CompareExchange.
				ITypeParameter[] newTps = new ITypeParameter[index + 1];
				tps.CopyTo(newTps, 0);
				for (int i = tps.Length; i < newTps.Length; i++) {
					newTps[i] = new DummyTypeParameter(entityType, i);
				}
				ITypeParameter[] oldTps = Interlocked.CompareExchange(ref typeParameters, newTps, tps);
				if (oldTps == tps) {
					// exchange successful
					tps = newTps;
				} else {
					// exchange not successful
					tps = oldTps;
				}
			}
			return tps[index];
		}
		
		readonly EntityType ownerType;
		readonly int index;
		
		private DummyTypeParameter(EntityType ownerType, int index)
		{
			this.ownerType = ownerType;
			this.index = index;
		}
		
		public override string Name {
			get { return "!" + index; }
		}
		
		public override bool? IsReferenceType {
			get { return null; }
		}
		
		public override TypeKind Kind {
			get { return TypeKind.TypeParameter; }
		}
		
		public override ITypeReference ToTypeReference()
		{
			return new TypeParameterReference(ownerType, index);
		}
		
		public int Index {
			get { return index; }
		}
		
		IList<IAttribute> ITypeParameter.Attributes {
			get { return EmptyList<IAttribute>.Instance; }
		}
		
		EntityType ITypeParameter.OwnerType {
			get { return ownerType; }
		}
		
		VarianceModifier ITypeParameter.Variance {
			get { return VarianceModifier.Invariant; }
		}
		
		DomRegion ITypeParameter.Region {
			get { return DomRegion.Empty; }
		}
		
		IEntity ITypeParameter.Owner {
			get { return null; }
		}
		
		IType ITypeParameter.EffectiveBaseClass {
			get { return SpecialType.UnknownType; }
		}
		
		IList<IType> ITypeParameter.EffectiveInterfaceSet {
			get { return EmptyList<IType>.Instance; }
		}
		
		bool ITypeParameter.HasDefaultConstructorConstraint {
			get { return false; }
		}
		
		bool ITypeParameter.HasReferenceTypeConstraint {
			get { return false; }
		}
		
		bool ITypeParameter.HasValueTypeConstraint {
			get { return false; }
		}
	}
}
