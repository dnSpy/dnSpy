using System;
using System.Linq;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class TypeParserTests : BaseTestFixture {

		[Test]
		public void SimpleStringReference ()
		{
			var module = GetCurrentModule ();
			var corlib = module.TypeSystem.Corlib;

			const string fullname = "System.String";

			var type = TypeParser.ParseType (module, fullname);
			Assert.IsNotNull (type);
			Assert.AreEqual (corlib, type.Scope);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("System", type.Namespace);
			Assert.AreEqual ("String", type.Name);
			Assert.AreEqual (MetadataType.String, type.MetadataType);
			Assert.IsFalse (type.IsValueType);
			Assert.IsInstanceOf (typeof (TypeReference), type);
		}

		[Test]
		public void SimpleInt32Reference ()
		{
			var module = GetCurrentModule ();
			var corlib = module.TypeSystem.Corlib;

			const string fullname = "System.Int32";

			var type = TypeParser.ParseType (module, fullname);
			Assert.IsNotNull (type);
			Assert.AreEqual (corlib, type.Scope);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("System", type.Namespace);
			Assert.AreEqual ("Int32", type.Name);
			Assert.AreEqual (MetadataType.Int32, type.MetadataType);
			Assert.IsTrue (type.IsValueType);
			Assert.IsInstanceOf (typeof (TypeReference), type);
		}

		[Test]
		public void SimpleTypeDefinition ()
		{
			var module = GetCurrentModule ();

			const string fullname = "Mono.Cecil.Tests.TypeParserTests";

			var type = TypeParser.ParseType (module, fullname);
			Assert.IsNotNull (type);
			Assert.AreEqual (module, type.Scope);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("Mono.Cecil.Tests", type.Namespace);
			Assert.AreEqual ("TypeParserTests", type.Name);
			Assert.IsInstanceOf (typeof (TypeDefinition), type);
		}

		[Test]
		public void ByRefTypeReference ()
		{
			var module = GetCurrentModule ();
			var corlib = module.TypeSystem.Corlib;

			const string fullname = "System.String&";

			var type = TypeParser.ParseType (module, fullname);

			Assert.IsInstanceOf (typeof (ByReferenceType), type);

			type = ((ByReferenceType) type).ElementType;

			Assert.IsNotNull (type);
			Assert.AreEqual (corlib, type.Scope);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("System", type.Namespace);
			Assert.AreEqual ("String", type.Name);
			Assert.IsInstanceOf (typeof (TypeReference), type);
		}

		[Test]
		public void FullyQualifiedTypeReference ()
		{
			var module = GetCurrentModule ();
			var cecil = module.AssemblyReferences.Where (reference => reference.Name == "Mono.Cecil").First ();

			var fullname = "Mono.Cecil.TypeDefinition, " + cecil.FullName;

			var type = TypeParser.ParseType (module, fullname);
			Assert.IsNotNull (type);
			Assert.AreEqual (cecil, type.Scope);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("Mono.Cecil", type.Namespace);
			Assert.AreEqual ("TypeDefinition", type.Name);
			Assert.IsInstanceOf (typeof (TypeReference), type);
		}

		[Test]
		public void OpenGenericType ()
		{
			var module = GetCurrentModule ();
			var corlib = module.TypeSystem.Corlib;

			const string fullname = "System.Collections.Generic.Dictionary`2";

			var type = TypeParser.ParseType (module, fullname);
			Assert.IsNotNull (type);
			Assert.AreEqual (corlib, type.Scope);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("System.Collections.Generic", type.Namespace);
			Assert.AreEqual ("Dictionary`2", type.Name);
			Assert.IsInstanceOf (typeof (TypeReference), type);
			Assert.AreEqual (2, type.GenericParameters.Count);
		}

		public class ID {}

		[Test]
		public void SimpleNestedType ()
		{
			var module = GetCurrentModule ();

			const string fullname = "Mono.Cecil.Tests.TypeParserTests+ID";

			var type = TypeParser.ParseType (module, fullname);

			Assert.IsNotNull (type);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual (module, type.Scope);
			Assert.AreEqual ("", type.Namespace);
			Assert.AreEqual ("ID", type.Name);

			Assert.AreEqual ("Mono.Cecil.Tests.TypeParserTests/ID", type.FullName);
			Assert.AreEqual (fullname, TypeParser.ToParseable (type));
		}

		[Test]
		public void TripleNestedTypeWithScope ()
		{
			var module = GetCurrentModule ();

			const string fullname = "Bingo.Foo`1+Bar`1+Baz`1, Bingo";

			var type = TypeParser.ParseType (module, fullname);

			Assert.AreEqual ("Bingo.Foo`1+Bar`1+Baz`1, Bingo, Culture=neutral, PublicKeyToken=null", TypeParser.ToParseable (type));

			Assert.IsNotNull (type);
			Assert.AreEqual ("Bingo", type.Scope.Name);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("", type.Namespace);
			Assert.AreEqual ("Baz`1", type.Name);
			Assert.IsInstanceOf (typeof (TypeReference), type);
			Assert.AreEqual (1, type.GenericParameters.Count);

			type = type.DeclaringType;

			Assert.IsNotNull (type);
			Assert.AreEqual ("Bingo", type.Scope.Name);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("", type.Namespace);
			Assert.AreEqual ("Bar`1", type.Name);
			Assert.IsInstanceOf (typeof (TypeReference), type);
			Assert.AreEqual (1, type.GenericParameters.Count);

			type = type.DeclaringType;

			Assert.IsNotNull (type);
			Assert.AreEqual ("Bingo", type.Scope.Name);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("Bingo", type.Namespace);
			Assert.AreEqual ("Foo`1", type.Name);
			Assert.IsInstanceOf (typeof (TypeReference), type);
			Assert.AreEqual (1, type.GenericParameters.Count);
		}

		[Test]
		public void Vector ()
		{
			var module = GetCurrentModule ();

			const string fullname = "Bingo.Gazonk[], Bingo";

			var type = TypeParser.ParseType (module, fullname);

			Assert.AreEqual ("Bingo.Gazonk[], Bingo, Culture=neutral, PublicKeyToken=null", TypeParser.ToParseable (type));

			var array = type as ArrayType;
			Assert.IsNotNull (array);
			Assert.AreEqual (1, array.Rank);
			Assert.IsTrue (array.IsVector);

			type = array.ElementType;

			Assert.IsNotNull (type);
			Assert.AreEqual ("Bingo", type.Scope.Name);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("Bingo", type.Namespace);
			Assert.AreEqual ("Gazonk", type.Name);
			Assert.IsInstanceOf (typeof (TypeReference), type);
		}

		[Test]
		public void ThreeDimensionalArray ()
		{
			var module = GetCurrentModule ();

			const string fullname = "Bingo.Gazonk[,,], Bingo";

			var type = TypeParser.ParseType (module, fullname);

			var array = type as ArrayType;
			Assert.IsNotNull (array);
			Assert.AreEqual (3, array.Rank);
			Assert.IsFalse (array.IsVector);

			type = array.ElementType;

			Assert.IsNotNull (type);
			Assert.AreEqual ("Bingo", type.Scope.Name);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("Bingo", type.Namespace);
			Assert.AreEqual ("Gazonk", type.Name);
			Assert.IsInstanceOf (typeof (TypeReference), type);
		}

		[Test]
		public void GenericInstanceExternArguments ()
		{
			var module = GetCurrentModule ();

			var fullname = string.Format ("System.Collections.Generic.Dictionary`2[[System.Int32, {0}],[System.String, {0}]]",
				typeof (object).Assembly.FullName);

			var type = TypeParser.ParseType (module, fullname);

			Assert.AreEqual (fullname, TypeParser.ToParseable (type));

			var instance = type as GenericInstanceType;
			Assert.IsNotNull (instance);
			Assert.AreEqual (2, instance.GenericArguments.Count);
			Assert.AreEqual ("mscorlib", type.Scope.Name);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("System.Collections.Generic", type.Namespace);
			Assert.AreEqual ("Dictionary`2", type.Name);

			type = instance.ElementType;

			Assert.AreEqual (2, type.GenericParameters.Count);

			var argument = instance.GenericArguments [0];
			Assert.AreEqual ("mscorlib", argument.Scope.Name);
			Assert.AreEqual (module, argument.Module);
			Assert.AreEqual ("System", argument.Namespace);
			Assert.AreEqual ("Int32", argument.Name);

			argument = instance.GenericArguments [1];
			Assert.AreEqual ("mscorlib", argument.Scope.Name);
			Assert.AreEqual (module, argument.Module);
			Assert.AreEqual ("System", argument.Namespace);
			Assert.AreEqual ("String", argument.Name);
		}

		[Test]
		public void GenericInstanceMixedArguments ()
		{
			var module = GetCurrentModule ();

			var fullname = string.Format ("System.Collections.Generic.Dictionary`2[Mono.Cecil.Tests.TypeParserTests,[System.String, {0}]]",
				typeof (object).Assembly.FullName);

			var type = TypeParser.ParseType (module, fullname);

			var instance = type as GenericInstanceType;
			Assert.IsNotNull (instance);
			Assert.AreEqual (2, instance.GenericArguments.Count);
			Assert.AreEqual ("mscorlib", type.Scope.Name);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("System.Collections.Generic", type.Namespace);
			Assert.AreEqual ("Dictionary`2", type.Name);

			type = instance.ElementType;

			Assert.AreEqual (2, type.GenericParameters.Count);

			var argument = instance.GenericArguments [0];
			Assert.IsInstanceOf (typeof (TypeDefinition), argument);
			Assert.AreEqual (module, argument.Module);
			Assert.AreEqual ("Mono.Cecil.Tests", argument.Namespace);
			Assert.AreEqual ("TypeParserTests", argument.Name);

			argument = instance.GenericArguments [1];
			Assert.AreEqual ("mscorlib", argument.Scope.Name);
			Assert.AreEqual (module, argument.Module);
			Assert.AreEqual ("System", argument.Namespace);
			Assert.AreEqual ("String", argument.Name);
		}

		public class Foo<TX, TY> {
		}

		public class Bar {}

		[Test]
		public void GenericInstanceTwoNonFqArguments ()
		{
			var module = GetCurrentModule ();

			var fullname = string.Format ("System.Collections.Generic.Dictionary`2[Mono.Cecil.Tests.TypeParserTests+Bar,Mono.Cecil.Tests.TypeParserTests+Bar], {0}", typeof (object).Assembly.FullName);

			var type = TypeParser.ParseType (module, fullname);

			var instance = type as GenericInstanceType;
			Assert.IsNotNull (instance);
			Assert.AreEqual (2, instance.GenericArguments.Count);
			Assert.AreEqual ("mscorlib", type.Scope.Name);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("System.Collections.Generic", type.Namespace);
			Assert.AreEqual ("Dictionary`2", type.Name);

			type = instance.ElementType;

			Assert.AreEqual (2, type.GenericParameters.Count);

			var argument = instance.GenericArguments [0];
			Assert.AreEqual (module, argument.Module);
			Assert.AreEqual ("", argument.Namespace);
			Assert.AreEqual ("Bar", argument.Name);
			Assert.IsInstanceOf (typeof (TypeDefinition), argument);

			argument = instance.GenericArguments [1];
			Assert.AreEqual (module, argument.Module);
			Assert.AreEqual ("", argument.Namespace);
			Assert.AreEqual ("Bar", argument.Name);
			Assert.IsInstanceOf (typeof (TypeDefinition), argument);
		}

		[Test]
		public void ComplexGenericInstanceMixedArguments ()
		{
			var module = GetCurrentModule ();

			var fullname = string.Format ("System.Collections.Generic.Dictionary`2[[System.String, {0}],Mono.Cecil.Tests.TypeParserTests+Foo`2[Mono.Cecil.Tests.TypeParserTests,[System.Int32, {0}]]]",
				typeof (object).Assembly.FullName);

			var type = TypeParser.ParseType (module, fullname);

			var instance = type as GenericInstanceType;
			Assert.IsNotNull (instance);
			Assert.AreEqual (2, instance.GenericArguments.Count);
			Assert.AreEqual ("mscorlib", type.Scope.Name);
			Assert.AreEqual (module, type.Module);
			Assert.AreEqual ("System.Collections.Generic", type.Namespace);
			Assert.AreEqual ("Dictionary`2", type.Name);

			type = instance.ElementType;

			Assert.AreEqual (2, type.GenericParameters.Count);

			var argument = instance.GenericArguments [0];
			Assert.AreEqual ("mscorlib", argument.Scope.Name);
			Assert.AreEqual (module, argument.Module);
			Assert.AreEqual ("System", argument.Namespace);
			Assert.AreEqual ("String", argument.Name);

			argument = instance.GenericArguments [1];

			instance = argument as GenericInstanceType;
			Assert.IsNotNull (instance);
			Assert.AreEqual (2, instance.GenericArguments.Count);
			Assert.AreEqual (module, instance.Module);
			Assert.AreEqual ("Mono.Cecil.Tests.TypeParserTests/Foo`2", instance.ElementType.FullName);
			Assert.IsInstanceOf (typeof (TypeDefinition), instance.ElementType);

			argument = instance.GenericArguments [0];
			Assert.AreEqual (module, argument.Module);
			Assert.AreEqual ("Mono.Cecil.Tests", argument.Namespace);
			Assert.AreEqual ("TypeParserTests", argument.Name);
			Assert.IsInstanceOf (typeof (TypeDefinition), argument);

			argument = instance.GenericArguments [1];
			Assert.AreEqual ("mscorlib", argument.Scope.Name);
			Assert.AreEqual (module, argument.Module);
			Assert.AreEqual ("System", argument.Namespace);
			Assert.AreEqual ("Int32", argument.Name);
		}
	}
}
