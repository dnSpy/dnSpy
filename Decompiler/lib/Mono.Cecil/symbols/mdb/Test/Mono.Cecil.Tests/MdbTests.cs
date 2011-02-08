
using Mono.Cecil.Mdb;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class MdbTests : BaseTestFixture {

		[TestModule ("hello.exe", SymbolReaderProvider = typeof (MdbReaderProvider), SymbolWriterProvider = typeof (MdbWriterProvider))]
		public void Main (ModuleDefinition module)
		{
			var type = module.GetType ("Program");
			var main = type.GetMethod ("Main");

			AssertCode (@"
	.locals init (System.Int32 i)
	.line 7,7:0,0 'C:\sources\cecil\symbols\Mono.Cecil.Mdb\Test\Resources\assemblies\hello.cs'
	IL_0000: ldc.i4.0
	IL_0001: stloc.0
	.line 7,7:0,0 'C:\sources\cecil\symbols\Mono.Cecil.Mdb\Test\Resources\assemblies\hello.cs'
	IL_0002: br IL_0013
	.line 8,8:0,0 'C:\sources\cecil\symbols\Mono.Cecil.Mdb\Test\Resources\assemblies\hello.cs'
	IL_0007: ldarg.0
	IL_0008: ldloc.0
	IL_0009: ldelem.ref
	IL_000a: call System.Void Program::Print(System.String)
	.line 7,7:0,0 'C:\sources\cecil\symbols\Mono.Cecil.Mdb\Test\Resources\assemblies\hello.cs'
	IL_000f: ldloc.0
	IL_0010: ldc.i4.1
	IL_0011: add
	IL_0012: stloc.0
	IL_0013: ldloc.0
	IL_0014: ldarg.0
	IL_0015: ldlen
	IL_0016: conv.i4
	IL_0017: blt IL_0007
	.line 10,10:0,0 'C:\sources\cecil\symbols\Mono.Cecil.Mdb\Test\Resources\assemblies\hello.cs'
	IL_001c: ldc.i4.0
	IL_001d: ret
", main);
		}

		static void AssertCode (string expected, MethodDefinition method)
		{
			Assert.IsTrue (method.HasBody);
			Assert.IsNotNull (method.Body);

			Assert.AreEqual (Normalize (expected), Normalize (Formatter.FormatMethodBody (method)));
		}

		static string Normalize (string str)
		{
			return str.Trim ().Replace ("\r\n", "\n");
		}
	}
}
