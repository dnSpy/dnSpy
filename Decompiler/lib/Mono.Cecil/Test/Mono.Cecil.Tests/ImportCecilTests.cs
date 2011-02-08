using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SR = System.Reflection;
using System.Runtime.CompilerServices;

using Mono.Cecil.Cil;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class ImportCecilTests : BaseTestFixture {

		[Test]
		public void ImportStringByRef ()
		{
			var get_string = Compile<Func<string, string>> ((module, body) => {
				var type = module.Types [1];

				var method_by_ref = new MethodDefinition {
					Name = "ModifyString",
					IsPrivate = true,
					IsStatic = true,
				};

				type.Methods.Add (method_by_ref);

				method_by_ref.MethodReturnType.ReturnType = module.Import (typeof (void).ToDefinition ());

				method_by_ref.Parameters.Add (new ParameterDefinition (module.Import (typeof (string).ToDefinition ())));
				method_by_ref.Parameters.Add (new ParameterDefinition (module.Import (new ByReferenceType (typeof (string).ToDefinition ()))));

				var m_il = method_by_ref.Body.GetILProcessor ();
				m_il.Emit (OpCodes.Ldarg_1);
				m_il.Emit (OpCodes.Ldarg_0);
				m_il.Emit (OpCodes.Stind_Ref);
				m_il.Emit (OpCodes.Ret);

				var v_0 = new VariableDefinition (module.Import (typeof (string).ToDefinition ()));
				body.Variables.Add (v_0);

				var il = body.GetILProcessor ();
				il.Emit (OpCodes.Ldnull);
				il.Emit (OpCodes.Stloc, v_0);
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldloca, v_0);
				il.Emit (OpCodes.Call, method_by_ref);
				il.Emit (OpCodes.Ldloc_0);
				il.Emit (OpCodes.Ret);
			});

			Assert.AreEqual ("foo", get_string ("foo"));
		}

		[Test]
		public void ImportStringArray ()
		{
			var identity = Compile<Func<string [,], string [,]>> ((module, body) => {
				var il = body.GetILProcessor ();
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ret);
			});

			var array = new string [2, 2];

			Assert.AreEqual (array, identity (array));
		}

		[Test]
		public void ImportFieldStringEmpty ()
		{
			var get_empty = Compile<Func<string>> ((module, body) => {
				var il = body.GetILProcessor ();
				il.Emit (OpCodes.Ldsfld, module.Import (typeof (string).GetField ("Empty").ToDefinition ()));
				il.Emit (OpCodes.Ret);
			});

			Assert.AreEqual ("", get_empty ());
		}

		[Test]
		public void ImportStringConcat ()
		{
			var concat = Compile<Func<string, string, string>> ((module, body) => {
				var il = body.GetILProcessor ();
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldarg_1);
				il.Emit (OpCodes.Call, module.Import (typeof (string).GetMethod ("Concat", new [] { typeof (string), typeof (string) }).ToDefinition ()));
				il.Emit (OpCodes.Ret);
			});

			Assert.AreEqual ("FooBar", concat ("Foo", "Bar"));
		}

		public class Generic<T> {
			public T Field;

			public T Method (T t)
			{
				return t;
			}

			public TS GenericMethod<TS> (T t, TS s)
			{
				return s;
			}

			public Generic<TS> ComplexGenericMethod<TS> (T t, TS s)
			{
				return new Generic<TS> { Field = s };
			}
		}

		[Test]
		public void ImportGenericField ()
		{
			var get_field = Compile<Func<Generic<string>, string>> ((module, body) => {
				var generic_def = module.Import (typeof (Generic<>)).Resolve ();
				var field_def = generic_def.Fields.Where (f => f.Name == "Field").First ();

				var field_string = field_def.MakeGeneric (module.Import (typeof (string)));

				var field_ref = module.Import (field_string);

				var il = body.GetILProcessor ();
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldfld, field_ref);
				il.Emit (OpCodes.Ret);
			});

			var generic = new Generic<string> {
				Field = "foo",
			};

			Assert.AreEqual ("foo", get_field (generic));
		}

		[Test]
		public void ImportGenericMethod ()
		{
			var generic_identity = Compile<Func<Generic<int>, int, int>> ((module, body) => {
				var generic_def = module.Import (typeof (Generic<>)).Resolve ();
				var method_def = generic_def.Methods.Where (m => m.Name == "Method").First ();

				var method_int = method_def.MakeGeneric (module.Import (typeof (int)));
				var method_ref = module.Import (method_int);

				var il = body.GetILProcessor ();
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldarg_1);
				il.Emit (OpCodes.Callvirt, method_ref);
				il.Emit (OpCodes.Ret);
			});

			Assert.AreEqual (42, generic_identity (new Generic<int> (), 42));
		}

		[Test]
		public void ImportGenericMethodSpec ()
		{
			var gen_spec_id = Compile<Func<Generic<string>, int, int>> ((module, body) => {
				var generic_def = module.Import (typeof (Generic<>)).Resolve ();
				var method_def = generic_def.Methods.Where (m => m.Name == "GenericMethod").First ();

				var method_string = method_def.MakeGeneric (module.Import (typeof (string)));

				var method_instance = method_string.MakeGenericMethod (module.Import (typeof (int)));

				var method_ref = module.Import (method_instance);

				var il = body.GetILProcessor ();
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldnull);
				il.Emit (OpCodes.Ldarg_1);
				il.Emit (OpCodes.Callvirt, method_ref);
				il.Emit (OpCodes.Ret);
			});

			Assert.AreEqual (42, gen_spec_id (new Generic<string> (), 42));
		}

		[Test]
		public void ImportComplexGenericMethodSpec ()
		{
			var gen_spec_id = Compile<Func<Generic<string>, int, int>> ((module, body) => {
				var generic_def = module.Import (typeof (Generic<>)).Resolve ();
				var method_def = generic_def.Methods.Where (m => m.Name == "ComplexGenericMethod").First ();

				var method_string = method_def.MakeGeneric (module.Import (typeof (string)));
				var method_instance = method_string.MakeGenericMethod (module.Import (typeof (int)));
				var method_ref = module.Import (method_instance);

				var field_def = generic_def.Fields.Where (f => f.Name == "Field").First ();
				var field_int = field_def.MakeGeneric (module.Import (typeof (int)));
				var field_ref = module.Import (field_int);

				var il = body.GetILProcessor ();
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Ldnull);
				il.Emit (OpCodes.Ldarg_1);
				il.Emit (OpCodes.Callvirt, method_ref);
				il.Emit (OpCodes.Ldfld, field_ref);
				il.Emit (OpCodes.Ret);
			});

			Assert.AreEqual (42, gen_spec_id (new Generic<string> (), 42));
		}

		[Test]
		public void ImportMethodOnOpenGeneric ()
		{
			var generic = typeof (Generic<>).ToDefinition ();
			var module = ModuleDefinition.CreateModule ("foo", ModuleKind.Dll);

			var method = module.Import (generic.GetMethod ("Method"));

			Assert.AreEqual ("T Mono.Cecil.Tests.ImportCecilTests/Generic`1::Method(T)", method.FullName);
		}

		delegate void Emitter (ModuleDefinition module, MethodBody body);

		[MethodImpl (MethodImplOptions.NoInlining)]
		static TDelegate Compile<TDelegate> (Emitter emitter)
			where TDelegate : class
		{
			var name = GetTestCaseName ();

			var module = CreateTestModule<TDelegate> (name, emitter);
			var assembly = LoadTestModule (module);

			return CreateRunDelegate<TDelegate> (GetTestCase (name, assembly));
		}

		static TDelegate CreateRunDelegate<TDelegate> (Type type)
			where TDelegate : class
		{
			return (TDelegate) (object) Delegate.CreateDelegate (typeof (TDelegate), type.GetMethod ("Run"));
		}

		static Type GetTestCase (string name, SR.Assembly assembly)
		{
			return assembly.GetType (name);
		}

		static SR.Assembly LoadTestModule (ModuleDefinition module)
		{
			using (var stream = new MemoryStream ()) {
				module.Write (stream);
				File.WriteAllBytes (Path.Combine (Path.Combine (Path.GetTempPath (), "cecil"), module.Name + ".dll"), stream.ToArray ());
				return SR.Assembly.Load (stream.ToArray ());
			}
		}

		static ModuleDefinition CreateTestModule<TDelegate> (string name, Emitter emitter)
		{
			var module = CreateModule (name);

			var type = new TypeDefinition (
				"",
				name,
				TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract,
				module.Import (typeof (object)));

			module.Types.Add (type);

			var method = CreateMethod (type, typeof (TDelegate).GetMethod ("Invoke"));

			emitter (module, method.Body);

			return module;
		}

		static MethodDefinition CreateMethod (TypeDefinition type, SR.MethodInfo pattern)
		{
			var module = type.Module;

			var method = new MethodDefinition {
				Name = "Run",
				IsPublic = true,
				IsStatic = true,
			};

			type.Methods.Add (method);

			method.MethodReturnType.ReturnType = module.Import (pattern.ReturnType);

			foreach (var parameter_pattern in pattern.GetParameters ())
				method.Parameters.Add (new ParameterDefinition (module.Import (parameter_pattern.ParameterType)));

			return method;
		}

		static ModuleDefinition CreateModule (string name)
		{
			return ModuleDefinition.CreateModule (name, ModuleKind.Dll);
		}

		[MethodImpl (MethodImplOptions.NoInlining)]
		static string GetTestCaseName ()
		{
			var stack_trace = new StackTrace ();
			var stack_frame = stack_trace.GetFrame (2);

			return "ImportCecil_" + stack_frame.GetMethod ().Name;
		}
	}
}
