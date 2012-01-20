using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Mono.Cecil;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class ModuleTests : BaseTestFixture {

		[Test]
		public void CreateModuleEscapesAssemblyName ()
		{
			var module = ModuleDefinition.CreateModule ("Test.dll", ModuleKind.Dll);
			Assert.AreEqual ("Test", module.Assembly.Name.Name);

			module = ModuleDefinition.CreateModule ("Test.exe", ModuleKind.Console);
			Assert.AreEqual ("Test", module.Assembly.Name.Name);
		}

		[TestModule ("hello.exe")]
		public void SingleModule (ModuleDefinition module)
		{
			var assembly = module.Assembly;

			Assert.AreEqual (1, assembly.Modules.Count);
			Assert.IsNotNull (assembly.MainModule);
		}

		[TestModule ("hello.exe")]
		public void EntryPoint (ModuleDefinition module)
		{
			var entry_point = module.EntryPoint;
			Assert.IsNotNull (entry_point);

			Assert.AreEqual ("System.Void Program::Main()", entry_point.ToString ());
		}

		[TestModule ("mma.exe")]
		public void MultiModules (ModuleDefinition module)
		{
			var assembly = module.Assembly;

			Assert.AreEqual (3, assembly.Modules.Count);

			Assert.AreEqual ("mma.exe", assembly.Modules [0].Name);
			Assert.AreEqual (ModuleKind.Console, assembly.Modules [0].Kind);

			Assert.AreEqual ("moda.netmodule", assembly.Modules [1].Name);
			Assert.AreEqual ("eedb4721-6c3e-4d9a-be30-49021121dd92", assembly.Modules [1].Mvid.ToString ());
			Assert.AreEqual (ModuleKind.NetModule, assembly.Modules [1].Kind);

			Assert.AreEqual ("modb.netmodule", assembly.Modules [2].Name);
			Assert.AreEqual ("46c5c577-11b2-4ea0-bb3c-3c71f1331dd0", assembly.Modules [2].Mvid.ToString ());
			Assert.AreEqual (ModuleKind.NetModule, assembly.Modules [2].Kind);
		}

		[TestModule ("hello.exe")]
		public void ModuleInformation (ModuleDefinition module)
		{
			Assert.IsNotNull (module);

			Assert.AreEqual ("hello.exe", module.Name);
			Assert.AreEqual (new Guid ("C3BC2BD3-2576-4D00-A80E-465B5632415F"), module.Mvid);
		}

		[TestModule ("hello.exe")]
		public void AssemblyReferences (ModuleDefinition module)
		{
			Assert.AreEqual (1, module.AssemblyReferences.Count);

			var reference = module.AssemblyReferences [0];

			Assert.AreEqual ("mscorlib", reference.Name);
			Assert.AreEqual (new Version (2, 0, 0, 0), reference.Version);
			Assert.AreEqual (new byte [] { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 }, reference.PublicKeyToken);
		}

		[TestModule ("pinvoke.exe")]
		public void ModuleReferences (ModuleDefinition module)
		{
			Assert.AreEqual (2, module.ModuleReferences.Count);
			Assert.AreEqual ("kernel32.dll", module.ModuleReferences [0].Name);
			Assert.AreEqual ("shell32.dll", module.ModuleReferences [1].Name);
		}

		[TestModule ("hello.exe")]
		public void Types (ModuleDefinition module)
		{
			Assert.AreEqual (2, module.Types.Count);
			Assert.AreEqual ("<Module>", module.Types [0].FullName);
			Assert.AreEqual ("<Module>", module.GetType ("<Module>").FullName);
			Assert.AreEqual ("Program", module.Types [1].FullName);
			Assert.AreEqual ("Program", module.GetType ("Program").FullName);
		}

		[TestModule ("libres.dll")]
		public void LinkedResource (ModuleDefinition module)
		{
			var resource = module.Resources.Where (res => res.Name == "linked.txt").First () as LinkedResource;
			Assert.IsNotNull (resource);

			Assert.AreEqual ("linked.txt", resource.Name);
			Assert.AreEqual ("linked.txt", resource.File);
			Assert.AreEqual (ResourceType.Linked, resource.ResourceType);
			Assert.IsTrue (resource.IsPublic);
		}

		[TestModule ("libres.dll")]
		public void EmbeddedResource (ModuleDefinition module)
		{
			var resource = module.Resources.Where (res => res.Name == "embedded1.txt").First () as EmbeddedResource;
			Assert.IsNotNull (resource);

			Assert.AreEqual ("embedded1.txt", resource.Name);
			Assert.AreEqual (ResourceType.Embedded, resource.ResourceType);
			Assert.IsTrue (resource.IsPublic);

			using (var reader = new StreamReader (resource.GetResourceStream ()))
				Assert.AreEqual ("Hello", reader.ReadToEnd ());

			resource = module.Resources.Where (res => res.Name == "embedded2.txt").First () as EmbeddedResource;
			Assert.IsNotNull (resource);

			Assert.AreEqual ("embedded2.txt", resource.Name);
			Assert.AreEqual (ResourceType.Embedded, resource.ResourceType);
			Assert.IsTrue (resource.IsPublic);

			using (var reader = new StreamReader (resource.GetResourceStream ()))
				Assert.AreEqual ("World", reader.ReadToEnd ());
		}

		[TestModule ("mma.exe")]
		public void ExportedTypeFromNetModule (ModuleDefinition module)
		{
			Assert.IsTrue (module.HasExportedTypes);
			Assert.AreEqual (2, module.ExportedTypes.Count);

			var exported_type = module.ExportedTypes [0];

			Assert.AreEqual ("Module.A.Foo", exported_type.FullName);
			Assert.AreEqual ("moda.netmodule", exported_type.Scope.Name);

			exported_type = module.ExportedTypes [1];

			Assert.AreEqual ("Module.B.Baz", exported_type.FullName);
			Assert.AreEqual ("modb.netmodule", exported_type.Scope.Name);
		}

		[TestCSharp ("CustomAttributes.cs")]
		public void NestedTypeForwarder (ModuleDefinition module)
		{
			Assert.IsTrue (module.HasExportedTypes);
			Assert.AreEqual (2, module.ExportedTypes.Count);

			var exported_type = module.ExportedTypes [0];

			Assert.AreEqual ("System.Diagnostics.DebuggableAttribute", exported_type.FullName);
			Assert.AreEqual ("mscorlib", exported_type.Scope.Name);
			Assert.IsTrue (exported_type.IsForwarder);

			var nested_exported_type = module.ExportedTypes [1];

			Assert.AreEqual ("System.Diagnostics.DebuggableAttribute/DebuggingModes", nested_exported_type.FullName);
			Assert.AreEqual (exported_type, nested_exported_type.DeclaringType);
			Assert.AreEqual ("mscorlib", nested_exported_type.Scope.Name);
		}

		[TestCSharp ("CustomAttributes.cs")]
		public void HasTypeReference (ModuleDefinition module)
		{
			Assert.IsTrue (module.HasTypeReference ("System.Attribute"));
			Assert.IsTrue (module.HasTypeReference ("mscorlib", "System.Attribute"));

			Assert.IsFalse (module.HasTypeReference ("System.Core", "System.Attribute"));
			Assert.IsFalse (module.HasTypeReference ("System.Linq.Enumerable"));
		}

		[TestModule ("libhello.dll")]
		public void Win32FileVersion (ModuleDefinition module)
		{
			var version = FileVersionInfo.GetVersionInfo (module.FullyQualifiedName);

			Assert.AreEqual ("0.0.0.0", version.FileVersion);
		}

		[TestModule ("noblob.dll")]
		public void ModuleWithoutBlob (ModuleDefinition module)
		{
			Assert.IsNull (module.Image.BlobHeap);
		}

		[Test]
		public void MixedModeModule ()
		{
			var module = GetResourceModule ("cppcli.dll");

			Assert.AreEqual (1, module.ModuleReferences.Count);
			Assert.AreEqual (string.Empty, module.ModuleReferences [0].Name);
		}

		[Test]
		[ExpectedException (typeof (BadImageFormatException))]
		public void OpenIrrelevantFile ()
		{
			GetResourceModule ("text_file.txt");
		}

		[Test]
		public void WriteModuleTwice ()
		{
			var module = GetResourceModule ("iterator.exe");

			var path = Path.Combine (Path.GetTempPath (), "cecil");
			var file = Path.Combine (path, "iteratorrt.exe");

			module.Write (file);
			module.Write (file);
		}

		[Test]
		public void GetTypeNamespacePlusName ()
		{
			var module = GetResourceModule ("moda.netmodule");

			var type = module.GetType ("Module.A", "Foo");
			Assert.IsNotNull (type);
		}

		[Test]
		public void OpenModuleImmediate ()
		{
			var module = GetResourceModule ("hello.exe", ReadingMode.Immediate);

			Assert.AreEqual (ReadingMode.Immediate, module.ReadingMode);
		}

		[Test]
		public void OpenModuleDeferred ()
		{
			var module = GetResourceModule ("hello.exe", ReadingMode.Deferred);

			Assert.AreEqual (ReadingMode.Deferred, module.ReadingMode);
		}
	}
}
