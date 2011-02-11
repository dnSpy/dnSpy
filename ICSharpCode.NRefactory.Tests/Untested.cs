// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory
{
	// This file contains unit tests from SharpDevelop 4.0 which are not (yet) ported to NRefactory
	
	/*
	 
	 class ReflectionOrCecilLayerTests {
		[Test]
		public void ParameterComparisonTest()
		{
			DefaultParameter p1 = new DefaultParameter("a", mscorlib.GetClass("System.String", 0).DefaultReturnType, DomRegion.Empty);
			DefaultParameter p2 = new DefaultParameter("b", new GetClassReturnType(mscorlib, "System.String", 0), DomRegion.Empty);
			IList<IParameter> a1 = new List<IParameter>();
			IList<IParameter> a2 = new List<IParameter>();
			a1.Add(p1);
			a2.Add(p2);
			Assert.AreEqual(0, DiffUtility.Compare(a1, a2));
		}
		
		[Test]
		public void GenericDocumentationTagNamesTest()
		{
			DefaultClass c = (DefaultClass)mscorlib.GetClass("System.Collections.Generic.List", 1);
			Assert.AreEqual("T:System.Collections.Generic.List`1",
			                c.DocumentationTag);
			Assert.AreEqual("M:System.Collections.Generic.List`1.Add(`0)",
			                GetMethod(c, "Add").DocumentationTag);
			Assert.AreEqual("M:System.Collections.Generic.List`1.AddRange(System.Collections.Generic.IEnumerable{`0})",
			                GetMethod(c, "AddRange").DocumentationTag);
			Assert.AreEqual("M:System.Collections.Generic.List`1.ConvertAll``1(System.Converter{`0,``0})",
			                GetMethod(c, "ConvertAll").DocumentationTag);
		}
	 }
	 
	 */
}
