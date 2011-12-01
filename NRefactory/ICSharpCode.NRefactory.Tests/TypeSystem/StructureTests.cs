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
using System.Reflection;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem
{
	[TestFixture]
	public class StructureTests
	{
		[Test]
		public void ClassesThatSupportInterningAreSealed()
		{
			foreach (Type type in typeof(ISupportsInterning).Assembly.GetTypes()) {
				if (typeof(ISupportsInterning).IsAssignableFrom(type) && !type.IsInterface) {
					Assert.IsTrue(type.IsSealed, type.FullName);
				}
			}
		}
		
		[Test]
		public void System_TypeCode_corresponds_with_KnownTypeCode()
		{
			foreach (TypeCode typeCode in Enum.GetValues(typeof(System.TypeCode))) {
				if (typeCode == TypeCode.Empty)
					Assert.AreEqual("None", ((KnownTypeCode)typeCode).ToString());
				else
					Assert.AreEqual(typeCode.ToString(), ((KnownTypeCode)typeCode).ToString());
			}
		}
		
		[Test]
		public void KnownTypeReference_Get_returns_correct_KnownType()
		{
			foreach (KnownTypeCode typeCode in Enum.GetValues(typeof(KnownTypeCode))) {
				if (typeCode == KnownTypeCode.None) {
					Assert.IsNull(KnownTypeReference.Get(KnownTypeCode.None));
				} else {
					Assert.AreEqual(typeCode, KnownTypeReference.Get(typeCode).KnownTypeCode);
				}
			}
		}
		
		[Test]
		public void KnownTypeReference_has_static_fields_for_KnownTypes()
		{
			foreach (KnownTypeCode typeCode in Enum.GetValues(typeof(KnownTypeCode))) {
				if (typeCode == KnownTypeCode.None)
					continue;
				FieldInfo field = typeof(KnownTypeReference).GetField(typeCode.ToString());
				Assert.IsNotNull(field, "Missing field for " + typeCode.ToString());
				KnownTypeReference ktr = (KnownTypeReference)field.GetValue(null);
				Assert.AreEqual(typeCode, ktr.KnownTypeCode);
			}
		}
	}
}
