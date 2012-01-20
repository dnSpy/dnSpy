
using System.IO;
using System.Linq;

using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class PdbTests : BaseTestFixture {

		[TestModule ("test.exe", SymbolReaderProvider = typeof (PdbReaderProvider), SymbolWriterProvider = typeof (PdbWriterProvider))]
		public void Main (ModuleDefinition module)
		{
			var type = module.GetType ("Program");
			var main = type.GetMethod ("Main");

			AssertCode (@"
	.locals init (System.Int32 i, System.Int32 CS$1$0000, System.Boolean CS$4$0001)
	.line 6,6:2,3 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0000: nop
	.line 7,7:8,18 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0001: ldc.i4.0
	IL_0002: stloc.0
	.line 16707566,16707566:0,0 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0003: br.s IL_0012
	.line 8,8:4,21 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0005: ldarg.0
	IL_0006: ldloc.0
	IL_0007: ldelem.ref
	IL_0008: call System.Void Program::Print(System.String)
	IL_000d: nop
	.line 7,7:36,39 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_000e: ldloc.0
	IL_000f: ldc.i4.1
	IL_0010: add
	IL_0011: stloc.0
	.line 7,7:19,34 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0012: ldloc.0
	IL_0013: ldarg.0
	IL_0014: ldlen
	IL_0015: conv.i4
	IL_0016: clt
	IL_0018: stloc.2
	.line 16707566,16707566:0,0 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0019: ldloc.2
	IL_001a: brtrue.s IL_0005
	.line 10,10:3,12 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_001c: ldc.i4.0
	IL_001d: stloc.1
	IL_001e: br.s IL_0020
	.line 11,11:2,3 'c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs'
	IL_0020: ldloc.1
	IL_0021: ret
", main);
		}

		[TestModule ("test.exe", SymbolReaderProvider = typeof (PdbReaderProvider), SymbolWriterProvider = typeof (PdbWriterProvider))]
		public void Document (ModuleDefinition module)
		{
			var type = module.GetType ("Program");
			var method = type.GetMethod ("Main");

			var sequence_point = method.Body.Instructions.Where (i => i.SequencePoint != null).First ().SequencePoint;
			var document = sequence_point.Document;

			Assert.IsNotNull (document);

			Assert.AreEqual (@"c:\sources\cecil\symbols\Mono.Cecil.Pdb\Test\Resources\assemblies\test.cs", document.Url);
			Assert.AreEqual (DocumentType.Text, document.Type);
			Assert.AreEqual (DocumentHashAlgorithm.None, document.HashAlgorithm);
			Assert.AreEqual (DocumentLanguage.CSharp, document.Language);
			Assert.AreEqual (DocumentLanguageVendor.Microsoft, document.LanguageVendor);
		}

		[TestModule ("VBConsApp.exe", SymbolReaderProvider = typeof (PdbReaderProvider), SymbolWriterProvider = typeof (PdbWriterProvider))]
		public void BasicDocument (ModuleDefinition module)
		{
			var type = module.GetType ("VBConsApp.Program");
			var method = type.GetMethod ("Main");

			var sequence_point = method.Body.Instructions.Where (i => i.SequencePoint != null).First ().SequencePoint;
			var document = sequence_point.Document;

			Assert.IsNotNull (document);

			Assert.AreEqual (@"c:\tmp\VBConsApp\Program.vb", document.Url);
			Assert.AreEqual (DocumentType.Text, document.Type);
			Assert.AreEqual (DocumentHashAlgorithm.None, document.HashAlgorithm);
			Assert.AreEqual (DocumentLanguage.Basic, document.Language);
			Assert.AreEqual (DocumentLanguageVendor.Microsoft, document.LanguageVendor);
		}

		[TestModule ("fsapp.exe", SymbolReaderProvider = typeof (PdbReaderProvider), SymbolWriterProvider = typeof (PdbWriterProvider))]
		public void FSharpDocument (ModuleDefinition module)
		{
			var type = module.GetType ("Program");
			var method = type.GetMethod ("fact");

			var sequence_point = method.Body.Instructions.Where (i => i.SequencePoint != null).First ().SequencePoint;
			var document = sequence_point.Document;

			Assert.IsNotNull (document);

			Assert.AreEqual (@"c:\tmp\fsapp\Program.fs", document.Url);
			Assert.AreEqual (DocumentType.Text, document.Type);
			Assert.AreEqual (DocumentHashAlgorithm.None, document.HashAlgorithm);
			Assert.AreEqual (DocumentLanguage.FSharp, document.Language);
			Assert.AreEqual (DocumentLanguageVendor.Microsoft, document.LanguageVendor);
		}

		[Test]
		public void CreateMethodFromScratch ()
		{
			var module = ModuleDefinition.CreateModule ("Pan", ModuleKind.Dll);
			var type = new TypeDefinition ("Pin", "Pon", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.Import (typeof (object)));
			module.Types.Add (type);

			var method = new MethodDefinition ("Pang", MethodAttributes.Public | MethodAttributes.Static, module.Import (typeof (string)));
			type.Methods.Add (method);

			var body = method.Body;

			body.InitLocals = true;

			var il = body.GetILProcessor ();
			var temp = new VariableDefinition ("temp", module.Import (typeof (string)));
			body.Variables.Add (temp);

			il.Emit (OpCodes.Nop);
			il.Emit (OpCodes.Ldstr, "hello");
			il.Emit (OpCodes.Stloc, temp);
			il.Emit (OpCodes.Ldloc, temp);
			il.Emit (OpCodes.Ret);

			body.Instructions [0].SequencePoint = new SequencePoint (new Document (@"C:\test.cs")) {
				StartLine = 0,
				StartColumn = 0,
				EndLine = 0,
				EndColumn = 4,
			};

			var file = Path.Combine (Path.GetTempPath (), "Pan.dll");
			module.Write (file, new WriterParameters {
				SymbolWriterProvider = new PdbWriterProvider (),
			});

			module = ModuleDefinition.ReadModule (file, new ReaderParameters {
				SymbolReaderProvider = new PdbReaderProvider (),
			});

			method = module.GetType ("Pin.Pon").GetMethod ("Pang");

			Assert.AreEqual ("temp", method.Body.Variables [0].Name);
		}

		static void AssertCode (string expected, MethodDefinition method)
		{
			Assert.IsTrue (method.HasBody);
			Assert.IsNotNull (method.Body);

			System.Console.WriteLine (Formatter.FormatMethodBody (method));

			Assert.AreEqual (Normalize (expected), Normalize (Formatter.FormatMethodBody (method)));
		}

		static string Normalize (string str)
		{
			return str.Trim ().Replace ("\r\n", "\n");
		}
	}
}
