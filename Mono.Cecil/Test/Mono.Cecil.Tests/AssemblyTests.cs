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
	}
}
