// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class OverloadResolutionTests
	{
		readonly ITypeResolveContext context = CecilLoaderTests.Mscorlib;
		readonly DefaultTypeDefinition dummyClass = new DefaultTypeDefinition(CecilLoaderTests.Mscorlib, string.Empty, "DummyClass");
		
		ResolveResult[] MakeArgumentList(params Type[] argumentTypes)
		{
			return argumentTypes.Select(t => new ResolveResult(t.ToTypeReference().Resolve(context))).ToArray();
		}
		
		DefaultMethod MakeMethod(params object[] parameterTypesOrDefaultValues)
		{
			DefaultMethod m = new DefaultMethod(dummyClass, "Method");
			foreach (var typeOrDefaultValue in parameterTypesOrDefaultValues) {
				Type type = typeOrDefaultValue as Type;
				if (type != null)
					m.Parameters.Add(new DefaultParameter(type.ToTypeReference(), string.Empty));
				else if (Type.GetTypeCode(typeOrDefaultValue.GetType()) > TypeCode.Object)
					m.Parameters.Add(new DefaultParameter(typeOrDefaultValue.GetType().ToTypeReference(), string.Empty) {
					                 	DefaultValue = new SimpleConstantValue(typeOrDefaultValue.GetType().ToTypeReference(), typeOrDefaultValue)
					                 });
				else
					throw new ArgumentException(typeOrDefaultValue.ToString());
			}
			return m;
		}
		
		DefaultMethod MakeParamsMethod(params object[] parameterTypesOrDefaultValues)
		{
			DefaultMethod m = MakeMethod(parameterTypesOrDefaultValues);
			((DefaultParameter)m.Parameters.Last()).IsParams = true;
			return m;
		}
		
		DefaultParameter MakeOptionalParameter(IType type, string name)
		{
			return new DefaultParameter(type, name) {
				DefaultValue = new SimpleConstantValue(type, null)
			};
		}
		
		[Test]
		public void PreferIntOverUInt()
		{
			OverloadResolution r = new OverloadResolution(context, MakeArgumentList(typeof(ushort)));
			var c1 = MakeMethod(typeof(int));
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(c1));
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(MakeMethod(typeof(uint))));
			Assert.IsFalse(r.IsAmbiguous);
			Assert.AreSame(c1, r.BestCandidate);
		}
		
		[Test]
		public void NullableIntAndNullableUIntIsAmbiguous()
		{
			OverloadResolution r = new OverloadResolution(context, MakeArgumentList(typeof(ushort?)));
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(MakeMethod(typeof(int?))));
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(MakeMethod(typeof(uint?))));
			Assert.AreEqual(OverloadResolutionErrors.AmbiguousMatch, r.BestCandidateErrors);
			
			// then adding a matching overload solves the ambiguity:
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(MakeMethod(typeof(ushort?))));
			Assert.AreEqual(OverloadResolutionErrors.None, r.BestCandidateErrors);
			Assert.IsNull(r.BestCandidateAmbiguousWith);
		}
		
		[Test]
		public void ParamsMethodMatchesEmptyArgumentList()
		{
			OverloadResolution r = new OverloadResolution(context, MakeArgumentList());
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(MakeParamsMethod(typeof(int[]))));
			Assert.IsTrue(r.BestCandidateIsExpandedForm);
		}
		
		[Test]
		public void ParamsMethodMatchesOneArgumentInExpandedForm()
		{
			OverloadResolution r = new OverloadResolution(context, MakeArgumentList(typeof(int)));
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(MakeParamsMethod(typeof(int[]))));
			Assert.IsTrue(r.BestCandidateIsExpandedForm);
		}
		
		[Test]
		public void ParamsMethodMatchesInUnexpandedForm()
		{
			OverloadResolution r = new OverloadResolution(context, MakeArgumentList(typeof(int[])));
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(MakeParamsMethod(typeof(int[]))));
			Assert.IsFalse(r.BestCandidateIsExpandedForm);
		}
		
		[Test]
		public void LessArgumentsPassedToParamsIsBetter()
		{
			OverloadResolution r = new OverloadResolution(context, MakeArgumentList(typeof(int), typeof(int), typeof(int)));
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(MakeParamsMethod(typeof(int[]))));
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(MakeParamsMethod(typeof(int), typeof(int[]))));
			Assert.IsFalse(r.IsAmbiguous);
			Assert.AreEqual(2, r.BestCandidate.Parameters.Count);
		}
		
		[Test]
		public void CallInvalidParamsDeclaration()
		{
			OverloadResolution r = new OverloadResolution(context, MakeArgumentList(typeof(int[,])));
			Assert.AreEqual(OverloadResolutionErrors.ArgumentTypeMismatch, r.AddCandidate(MakeParamsMethod(typeof(int))));
			Assert.IsFalse(r.BestCandidateIsExpandedForm);
		}
		
		[Test]
		public void PreferMethodWithoutOptionalParameters()
		{
			var m1 = MakeMethod();
			var m2 = MakeMethod(1);
			
			OverloadResolution r = new OverloadResolution(context, MakeArgumentList());
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(m1));
			Assert.AreEqual(OverloadResolutionErrors.None, r.AddCandidate(m2));
			Assert.IsFalse(r.IsAmbiguous);
			Assert.AreSame(m1, r.BestCandidate);
		}
		
		[Test]
		public void SkeetEvilOverloadResolution()
		{
			// http://msmvps.com/blogs/jon_skeet/archive/2010/11/02/evil-code-overload-resolution-workaround.aspx
			
			// static void Foo<T>(T? ignored = default(T?)) where T : struct
			var m1 = MakeMethod();
			m1.TypeParameters.Add(new DefaultTypeParameter(EntityType.Method, 0, "T") { HasValueTypeConstraint = true });
			m1.Parameters.Add(MakeOptionalParameter(
				NullableType.Create(m1.TypeParameters[0], context),
				"ignored"
			));
			
			// class ClassConstraint<T> where T : class {}
			DefaultTypeDefinition classConstraint = new DefaultTypeDefinition(dummyClass, "ClassConstraint");
			classConstraint.TypeParameters.Add(new DefaultTypeParameter(EntityType.TypeDefinition, 0, "T") { HasReferenceTypeConstraint = true });
			
			// static void Foo<T>(ClassConstraint<T> ignored = default(ClassConstraint<T>))
			// where T : class
			var m2 = MakeMethod();
			m2.TypeParameters.Add(new DefaultTypeParameter(EntityType.Method, 0, "T") { HasReferenceTypeConstraint = true });
			m2.Parameters.Add(MakeOptionalParameter(
				new ParameterizedType(classConstraint, new[] { m2.TypeParameters[0] }),
				"ignored"
			));
			
			// static void Foo<T>()
			var m3 = MakeMethod();
			m3.TypeParameters.Add(new DefaultTypeParameter(EntityType.Method, 0, "T"));
			
			// Call: Foo<int>();
			OverloadResolution o;
			o = new OverloadResolution(context, new ResolveResult[0], typeArguments: new[] { typeof(int).ToTypeReference().Resolve(context) });
			Assert.AreEqual(OverloadResolutionErrors.None, o.AddCandidate(m1));
			Assert.AreEqual(OverloadResolutionErrors.ConstructedTypeDoesNotSatisfyConstraint, o.AddCandidate(m2));
			Assert.AreSame(m1, o.BestCandidate);
			
			// Call: Foo<string>();
			o = new OverloadResolution(context, new ResolveResult[0], typeArguments: new[] { typeof(string).ToTypeReference().Resolve(context) });
			Assert.AreEqual(OverloadResolutionErrors.ConstructedTypeDoesNotSatisfyConstraint, o.AddCandidate(m1));
			Assert.AreEqual(OverloadResolutionErrors.None, o.AddCandidate(m2));
			Assert.AreSame(m2, o.BestCandidate);
			
			// Call: Foo<int?>();
			o = new OverloadResolution(context, new ResolveResult[0], typeArguments: new[] { typeof(int?).ToTypeReference().Resolve(context) });
			Assert.AreEqual(OverloadResolutionErrors.ConstructedTypeDoesNotSatisfyConstraint, o.AddCandidate(m1));
			Assert.AreEqual(OverloadResolutionErrors.ConstructedTypeDoesNotSatisfyConstraint, o.AddCandidate(m2));
			Assert.AreEqual(OverloadResolutionErrors.None, o.AddCandidate(m3));
			Assert.AreSame(m3, o.BestCandidate);
		}
	}
}
