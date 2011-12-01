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
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem
{
	[TestFixture]
	public class TypeParameterTests
	{
		[Test]
		public void TypeParameterDerivingFromOtherTypeParameterDoesNotInheritReferenceConstraint()
		{
			// class C<T, U> where T : class where U : T
			var c = new DefaultUnresolvedTypeDefinition(string.Empty, "C");
			c.TypeParameters.Add(new DefaultUnresolvedTypeParameter(EntityType.TypeDefinition, 0, "T") { HasReferenceTypeConstraint = true });
			c.TypeParameters.Add(new DefaultUnresolvedTypeParameter(EntityType.TypeDefinition, 1, "U") {
			                     	Constraints = { new TypeParameterReference(EntityType.TypeDefinition, 0) }
			                     });
			
			ITypeDefinition resolvedC = new SimpleCompilation(CecilLoaderTests.Mscorlib).MainAssembly.GetTypeDefinition(c);
			// At runtime, we might have T=System.ValueType and U=int, so C# can't inherit the 'class' constraint
			// from one type parameter to another.
			Assert.AreEqual(true, resolvedC.TypeParameters[0].IsReferenceType);
			Assert.IsNull(resolvedC.TypeParameters[1].IsReferenceType);
		}
		
		[Test]
		public void ValueTypeParameterDerivingFromReferenceTypeParameter()
		{
			// class C<T, U> where T : class where U : struct, T
			var c = new DefaultUnresolvedTypeDefinition(string.Empty, "C");
			c.TypeParameters.Add(new DefaultUnresolvedTypeParameter(EntityType.TypeDefinition, 0, "T") { HasReferenceTypeConstraint = true });
			c.TypeParameters.Add(new DefaultUnresolvedTypeParameter(EntityType.TypeDefinition, 1, "U") {
			                     	HasValueTypeConstraint = true,
			                     	Constraints = { new TypeParameterReference(EntityType.TypeDefinition, 0) }
			                     });
			
			ITypeDefinition resolvedC = new SimpleCompilation(CecilLoaderTests.Mscorlib).MainAssembly.GetTypeDefinition(c);
			// At runtime, we might have T=System.ValueType and U=int, so C# can't inherit the 'class' constraint
			// from one type parameter to another.
			Assert.AreEqual(true, resolvedC.TypeParameters[0].IsReferenceType);
			Assert.AreEqual(false, resolvedC.TypeParameters[1].IsReferenceType);
		}
		
		[Test]
		public void TypeParameterDerivingFromOtherTypeParameterInheritsEffectiveBaseClass()
		{
			// class C<T, U> where T : List<string> where U : T
			var c = new DefaultUnresolvedTypeDefinition(string.Empty, "C");
			c.TypeParameters.Add(new DefaultUnresolvedTypeParameter(EntityType.TypeDefinition, 0, "T") {
			                     	Constraints = { typeof(List<string>).ToTypeReference() }
			                     });
			c.TypeParameters.Add(new DefaultUnresolvedTypeParameter(EntityType.TypeDefinition, 1, "U") {
			                     	Constraints = { new TypeParameterReference(EntityType.TypeDefinition, 0) }
			                     });
			
			ITypeDefinition resolvedC = new SimpleCompilation(CecilLoaderTests.Mscorlib).MainAssembly.GetTypeDefinition(c);
			Assert.AreEqual(true, resolvedC.TypeParameters[0].IsReferenceType);
			Assert.AreEqual(true, resolvedC.TypeParameters[1].IsReferenceType);
			Assert.AreEqual("System.Collections.Generic.List`1[[System.String]]", resolvedC.TypeParameters[0].EffectiveBaseClass.ReflectionName);
			Assert.AreEqual("System.Collections.Generic.List`1[[System.String]]", resolvedC.TypeParameters[1].EffectiveBaseClass.ReflectionName);
		}
	}
}
