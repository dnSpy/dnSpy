using System;

using Mono.Cecil;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class AssemblyTests : BaseTestFixture {

		[TestModule ("hello.exe")]
		public void Name (ModuleDefinition module)
		{
			var name = module.Assembly.Name;

			Assert.IsNotNull (name);

			Assert.AreEqual ("hello", name.Name);
			Assert.AreEqual (string.Empty, name.Culture);
			Assert.AreEqual (new Version (0, 0, 0, 0), name.Version);
			Assert.AreEqual (AssemblyHashAlgorithm.SHA1, name.HashAlgorithm);
		}

		[Test]
		public void ParseLowerCaseNameParts()
		{
			var name = AssemblyNameReference.Parse ("Foo, version=2.0.0.0, culture=fr-FR");
			Assert.AreEqual ("Foo", name.Name);
			Assert.AreEqual (2, name.Version.Major);
			Assert.AreEqual (0, name.Version.Minor);
			Assert.AreEqual ("fr-FR", name.Culture);
		}
	}
}
