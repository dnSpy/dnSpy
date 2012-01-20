using System;
using System.IO;
using System.Reflection;

using NUnit.Core;
using NUnit.Core.Extensibility;

using Mono.Cecil.Cil;

namespace Mono.Cecil.Tests {

	public abstract class TestCecilAttribute : Attribute {

		bool verify = true;
		Type symbol_reader_provider;
		Type symbol_writer_provider;

		public bool Verify {
			get { return verify; }
			set { verify = value; }
		}

		public Type SymbolReaderProvider {
			get { return symbol_reader_provider; }
			set { symbol_reader_provider = value; }
		}

		public Type SymbolWriterProvider {
			get { return symbol_writer_provider; }
			set { symbol_writer_provider = value; }
		}

		public abstract string GetModuleLocation (Assembly assembly);
	}

	[AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
	public sealed class TestModuleAttribute : TestCecilAttribute {

		readonly string module;

		public string Module {
			get { return module; }
		}

		public TestModuleAttribute (string assembly)
		{
			this.module = assembly;
		}

		public override string GetModuleLocation (Assembly assembly)
		{
			return BaseTestFixture.GetAssemblyResourcePath (module, assembly);
		}
	}

	[AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
	public sealed class TestCSharpAttribute : TestCecilAttribute {

		readonly string file;

		public string File {
			get { return file; }
		}

		public TestCSharpAttribute (string file)
		{
			this.file = file;
		}

		public override string GetModuleLocation (Assembly assembly)
		{
			return CompilationService.CompileResource (
				BaseTestFixture.GetCSharpResourcePath (file, assembly));
		}
	}

	[AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
	public sealed class TestILAttribute : TestCecilAttribute {

		readonly string file;

		public string File {
			get { return file; }
		}

		public TestILAttribute (string file)
		{
			this.file = file;
		}

		public override string GetModuleLocation (Assembly assembly)
		{
			return CompilationService.CompileResource (
				BaseTestFixture.GetILResourcePath (file, assembly));
		}
	}

	class CecilTestCase : NUnitTestMethod {

		readonly TestCecilAttribute attribute;
		readonly TestCaseType type;

		public CecilTestCase (MethodInfo method, TestCecilAttribute attribute, TestCaseType type)
			: base (method)
		{
			this.TestName.Name = type.ToString ();
			this.TestName.FullName = method.DeclaringType.FullName + "." + method.Name + "." + type;
			this.attribute = attribute;
			this.type = type;
		}

		ModuleDefinition GetModule ()
		{
			var location = attribute.GetModuleLocation (this.Method.DeclaringType.Assembly);

			var parameters = new ReaderParameters {
				SymbolReaderProvider = GetSymbolReaderProvider (attribute),
			};

			switch (type) {
			case TestCaseType.ReadImmediate:
				parameters.ReadingMode = ReadingMode.Immediate;
				return ModuleDefinition.ReadModule (location, parameters);
			case TestCaseType.ReadDeferred:
				parameters.ReadingMode = ReadingMode.Deferred;
				return ModuleDefinition.ReadModule (location, parameters);
			case TestCaseType.WriteFromImmediate:
				parameters.ReadingMode = ReadingMode.Immediate;
				return RoundTrip (location, parameters, "cecil-irt");
			case TestCaseType.WriteFromDeferred:
				parameters.ReadingMode = ReadingMode.Deferred;
				return RoundTrip (location, parameters, "cecil-drt");
			default:
				return null;
			}
		}

		static ISymbolReaderProvider GetSymbolReaderProvider (TestCecilAttribute attribute)
		{
			if (attribute.SymbolReaderProvider == null)
				return null;

			return (ISymbolReaderProvider) Activator.CreateInstance (attribute.SymbolReaderProvider);
		}

		static ISymbolWriterProvider GetSymbolWriterProvider (TestCecilAttribute attribute)
		{
			if (attribute.SymbolReaderProvider == null)
				return null;

			return (ISymbolWriterProvider) Activator.CreateInstance (attribute.SymbolWriterProvider);
		}

		ModuleDefinition RoundTrip (string location, ReaderParameters reader_parameters, string folder)
		{
			var module = ModuleDefinition.ReadModule (location, reader_parameters);
			var rt_folder = Path.Combine (Path.GetTempPath (), folder);
			if (!Directory.Exists (rt_folder))
				Directory.CreateDirectory (rt_folder);
			var rt_module = Path.Combine (rt_folder, Path.GetFileName (location));

			var writer_parameters = new WriterParameters {
				SymbolWriterProvider = GetSymbolWriterProvider (attribute),
			};

			Reflect.InvokeMethod (Method, Fixture, new object [] { module });

			module.Write (rt_module, writer_parameters);

			if (attribute.Verify)
				CompilationService.Verify (rt_module);

			return ModuleDefinition.ReadModule (rt_module, reader_parameters);
		}

		public override TestResult RunTest ()
		{
			var result = new TestResult (TestName);
			var module = GetModule ();
			if (module == null)
				return result;

			Reflect.InvokeMethod (Method, Fixture, new object [] { module });

			result.Success ();
			return result;
		}
	}

	class CecilTestSuite : TestSuite {

		public CecilTestSuite (MethodInfo method)
			: base (method.DeclaringType.FullName, method.Name)
		{
		}

		public override TestResult Run (EventListener listener, ITestFilter filter)
		{
			if (this.Parent != null)
				this.Fixture = this.Parent.Fixture;

			return base.Run (listener, filter);
		}

		protected override void DoOneTimeSetUp (TestResult suiteResult)
		{
		}

		protected override void DoOneTimeTearDown (TestResult suiteResult)
		{
		}
	}

	enum TestCaseType {
		ReadImmediate,
		ReadDeferred,
		WriteFromImmediate,
		WriteFromDeferred,
	}

	static class CecilTestFactory {

		public static CecilTestSuite CreateTestSuite (MethodInfo method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");

			var suite = new CecilTestSuite (method);

			NUnitFramework.ApplyCommonAttributes (method, suite);
			PopulateTestSuite (method, suite);

			return suite;
		}

		static void PopulateTestSuite (MethodInfo method, CecilTestSuite suite)
		{
			var attribute = GetTestCecilAttribute (method);
			if (attribute == null)
				throw new ArgumentException ();

			foreach (var value in Enum.GetValues (typeof (TestCaseType))) {
				var test = CreateTestCase (method, attribute, (TestCaseType) value);
				if (test != null)
					suite.Add (test);
			}
		}

		static CecilTestCase CreateTestCase (MethodInfo method, TestCecilAttribute attribute, TestCaseType type)
		{
			return new CecilTestCase (method, attribute, type);
		}

		static TestCecilAttribute GetTestCecilAttribute (MethodInfo method)
		{
			foreach (var attribute in method.GetCustomAttributes (false)) {
				var test = attribute as TestCecilAttribute;
				if (test != null)
					return test;
			}

			return null;
		}
	}

	[NUnitAddin]
	public class CecilTestAddin : IAddin, ITestCaseBuilder {

		public bool Install (IExtensionHost host)
		{
			if (host == null)
				throw new ArgumentNullException ("host");

			var builders = host.GetExtensionPoint ("TestCaseBuilders");
			if (builders == null)
				return false;

			builders.Install (this);
			return true;
		}

		public Test BuildFrom (MethodInfo method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");

			return CecilTestFactory.CreateTestSuite (method);
		}

		public bool CanBuildFrom (MethodInfo method)
		{
			if (method == null)
				return false;

			return IsCecilTestMethod (method);
		}

		static bool IsCecilTestMethod (MethodInfo method)
		{
			return Reflect.HasAttribute (method, typeof (TestModuleAttribute).FullName, false)
				|| Reflect.HasAttribute (method, typeof (TestILAttribute).FullName, false)
				|| Reflect.HasAttribute (method, typeof (TestCSharpAttribute).FullName, false);
		}
	}
}
