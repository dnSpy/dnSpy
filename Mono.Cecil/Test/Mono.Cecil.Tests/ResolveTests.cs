using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class ResolveTests : BaseTestFixture {

		[Test]
		public void StringEmpty ()
		{
			var string_empty = GetReference<Func<string>, FieldReference> (
				() => string.Empty);

			Assert.AreEqual ("System.String System.String::Empty", string_empty.FullName);

			var definition = string_empty.Resolve ();

			Assert.IsNotNull (definition);

			Assert.AreEqual ("System.String System.String::Empty", definition.FullName);
			Assert.AreEqual ("mscorlib", definition.Module.Assembly.Name.Name);
		}

		delegate string GetSubstring (string str, int start, int length);

		[Test]
		public void StringSubstring ()
		{
			var string_substring = GetReference<GetSubstring, MethodReference> (
				(s, start, length) => s.Substring (start, length));

			var definition = string_substring.Resolve ();

			Assert.IsNotNull (definition);

			Assert.AreEqual ("System.String System.String::Substring(System.Int32,System.Int32)", definition.FullName);
			Assert.AreEqual ("mscorlib", definition.Module.Assembly.Name.Name);
		}

		[Test]
		public void StringLength ()
		{
			var string_length = GetReference<Func<string, int>, MethodReference> (s => s.Length);

			var definition = string_length.Resolve ();

			Assert.IsNotNull (definition);

			Assert.AreEqual ("get_Length", definition.Name);
			Assert.AreEqual ("System.String", definition.DeclaringType.FullName);
			Assert.AreEqual ("mscorlib", definition.Module.Assembly.Name.Name);
		}

		[Test]
		public void ListOfStringAdd ()
		{
			var list_add = GetReference<Action<List<string>>, MethodReference> (
				list => list.Add ("coucou"));

			Assert.AreEqual ("System.Void System.Collections.Generic.List`1<System.String>::Add(!0)", list_add.FullName);

			var definition = list_add.Resolve ();

			Assert.IsNotNull (definition);

			Assert.AreEqual ("System.Void System.Collections.Generic.List`1::Add(T)", definition.FullName);
			Assert.AreEqual ("mscorlib", definition.Module.Assembly.Name.Name);
		}

		[Test]
		public void DictionaryOfStringTypeDefinitionTryGetValue ()
		{
			var try_get_value = GetReference<Func<Dictionary<string, TypeDefinition>, string, bool>, MethodReference> (
				(d, s) => {
					TypeDefinition type;
					return d.TryGetValue (s, out type);
				});

			Assert.AreEqual ("System.Boolean System.Collections.Generic.Dictionary`2<System.String,Mono.Cecil.TypeDefinition>::TryGetValue(!0,!1&)",
				try_get_value.FullName);

			var definition = try_get_value.Resolve ();

			Assert.IsNotNull (definition);

			Assert.AreEqual ("System.Boolean System.Collections.Generic.Dictionary`2::TryGetValue(TKey,TValue&)", definition.FullName);
			Assert.AreEqual ("mscorlib", definition.Module.Assembly.Name.Name);
		}

		class CustomResolver : DefaultAssemblyResolver {

			public void Register (AssemblyDefinition assembly)
			{
				this.RegisterAssembly (assembly);
				this.AddSearchDirectory (Path.GetDirectoryName (assembly.MainModule.FullyQualifiedName));
			}
		}

		[Test]
		public void ExportedTypeFromModule ()
		{
			var resolver = new CustomResolver ();
			var parameters = new ReaderParameters { AssemblyResolver = resolver };
			var mma = GetResourceModule ("mma.exe", parameters);

			resolver.Register (mma.Assembly);

			var current_module = GetCurrentModule (parameters);
			var reference = new TypeReference ("Module.A", "Foo", current_module, AssemblyNameReference.Parse (mma.Assembly.FullName), false);

			var definition = reference.Resolve ();
			Assert.IsNotNull (definition);
			Assert.AreEqual ("Module.A.Foo", definition.FullName);
		}

		[Test]
		public void TypeForwarder ()
		{
			var resolver = new CustomResolver ();
			var parameters = new ReaderParameters { AssemblyResolver = resolver };

			var types = ModuleDefinition.ReadModule (
				CompilationService.CompileResource (GetCSharpResourcePath ("CustomAttributes.cs", typeof (ResolveTests).Assembly)),
				parameters);

			resolver.Register (types.Assembly);

			var current_module = GetCurrentModule (parameters);
			var reference = new TypeReference ("System.Diagnostics", "DebuggableAttribute", current_module, AssemblyNameReference.Parse (types.Assembly.FullName), false);

			var definition = reference.Resolve ();
			Assert.IsNotNull (definition);
			Assert.AreEqual ("System.Diagnostics.DebuggableAttribute", definition.FullName);
			Assert.AreEqual ("mscorlib", definition.Module.Assembly.Name.Name);
		}

		[Test]
		public void NestedTypeForwarder ()
		{
			var resolver = new CustomResolver ();
			var parameters = new ReaderParameters { AssemblyResolver = resolver };

			var types = ModuleDefinition.ReadModule (
				CompilationService.CompileResource (GetCSharpResourcePath ("CustomAttributes.cs", typeof (ResolveTests).Assembly)),
				parameters);

			resolver.Register (types.Assembly);

			var current_module = GetCurrentModule (parameters);
			var reference = new TypeReference ("", "DebuggingModes", current_module, null, true);
			reference.DeclaringType = new TypeReference ("System.Diagnostics", "DebuggableAttribute", current_module, AssemblyNameReference.Parse (types.Assembly.FullName), false);

			var definition = reference.Resolve ();
			Assert.IsNotNull (definition);
			Assert.AreEqual ("System.Diagnostics.DebuggableAttribute/DebuggingModes", definition.FullName);
			Assert.AreEqual ("mscorlib", definition.Module.Assembly.Name.Name);
		}

		[Test]
		public void RectangularArrayResolveGetMethod ()
		{
			var get_a_b = GetReference<Func<int[,], int>, MethodReference> (matrix => matrix [2, 2]);

			Assert.AreEqual ("Get", get_a_b.Name);
			Assert.IsNotNull (get_a_b.Module);
			Assert.IsNull (get_a_b.Resolve ());
		}

		[Test]
		public void ResolveFunctionPointer ()
		{
			var module = GetResourceModule ("cppcli.dll");
			var global = module.GetType ("<Module>");
			var field = global.GetField ("__onexitbegin_app_domain");

			var type = field.FieldType as PointerType;
			Assert.IsNotNull(type);

			var fnptr = type.ElementType as FunctionPointerType;
			Assert.IsNotNull (fnptr);

			Assert.IsNull (fnptr.Resolve ());
		}

		[Test]
		public void ResolveGenericParameter ()
		{
			var collection = typeof (Mono.Collections.Generic.Collection<>).ToDefinition ();
			var parameter = collection.GenericParameters [0];

			Assert.IsNotNull (parameter);

			Assert.IsNull (parameter.Resolve ());
		}

		[Test]
		public void ResolveNullVersionAssembly ()
		{
			var reference = AssemblyNameReference.Parse ("System.Core");
			reference.Version = null;

			var resolver = new DefaultAssemblyResolver ();
			Assert.IsNotNull (resolver.Resolve (reference));
		}

		static TRet GetReference<TDel, TRet> (TDel code)
		{
			var @delegate = code as Delegate;
			if (@delegate == null)
				throw new InvalidOperationException ();

			var reference = (TRet) GetReturnee (GetMethodFromDelegate (@delegate));

			Assert.IsNotNull (reference);

			return reference;
		}

		static object GetReturnee (MethodDefinition method)
		{
			Assert.IsTrue (method.HasBody);

			var instruction = method.Body.Instructions [method.Body.Instructions.Count - 1];

			Assert.IsNotNull (instruction);

			while (instruction != null) {
				var opcode = instruction.OpCode;
				switch (opcode.OperandType) {
				case OperandType.InlineField:
				case OperandType.InlineTok:
				case OperandType.InlineType:
				case OperandType.InlineMethod:
					return instruction.Operand;
				default:
					instruction = instruction.Previous;
					break;
				}
			}

			throw new InvalidOperationException ();
		}

		static MethodDefinition GetMethodFromDelegate (Delegate @delegate)
		{
			var method = @delegate.Method;
			var type = (TypeDefinition) TypeParser.ParseType (GetCurrentModule (), method.DeclaringType.FullName);

			Assert.IsNotNull (type);

			return type.Methods.Where (m => m.Name == method.Name).First ();
		}
	}
}
