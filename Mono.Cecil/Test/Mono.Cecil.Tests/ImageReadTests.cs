using System;
using System.IO;

using Mono.Cecil;
using Mono.Cecil.PE;
using Mono.Cecil.Metadata;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class ImageReadTests : BaseTestFixture {

		[Test]
		public void ImageSections ()
		{
			var image = GetResourceImage ("hello.exe");

			Assert.AreEqual (3, image.Sections.Length);
			Assert.AreEqual (".text", image.Sections [0].Name);
			Assert.AreEqual (".rsrc", image.Sections [1].Name);
			Assert.AreEqual (".reloc", image.Sections [2].Name);
		}

		[Test]
		public void ImageMetadataVersion ()
		{
			var image = GetResourceImage ("hello.exe");
			Assert.AreEqual (TargetRuntime.Net_2_0, image.Runtime);

			image = GetResourceImage ("hello1.exe");
			Assert.AreEqual (TargetRuntime.Net_1_1, image.Runtime);
		}

		[Test]
		public void ImageModuleKind ()
		{
			var image = GetResourceImage ("hello.exe");
			Assert.AreEqual (ModuleKind.Console, image.Kind);

			image = GetResourceImage ("libhello.dll");
			Assert.AreEqual (ModuleKind.Dll, image.Kind);

			image = GetResourceImage ("hellow.exe");
			Assert.AreEqual (ModuleKind.Windows, image.Kind);
		}

		[Test]
		public void MetadataHeaps ()
		{
			var image = GetResourceImage ("hello.exe");

			Assert.IsNotNull (image.TableHeap);

			Assert.IsNotNull (image.StringHeap);
			Assert.AreEqual (string.Empty, image.StringHeap.Read (0));
			Assert.AreEqual ("<Module>", image.StringHeap.Read (1));

			Assert.IsNotNull (image.UserStringHeap);
			Assert.AreEqual (string.Empty, image.UserStringHeap.Read (0));
			Assert.AreEqual ("Hello Cecil World !", image.UserStringHeap.Read (1));

			Assert.IsNotNull (image.GuidHeap);
			Assert.AreEqual (new Guid (), image.GuidHeap.Read (0));
			Assert.AreEqual (new Guid ("C3BC2BD3-2576-4D00-A80E-465B5632415F"), image.GuidHeap.Read (1));

			Assert.IsNotNull (image.BlobHeap);
			Assert.AreEqual (new byte [0], image.BlobHeap.Read (0));
		}

		[Test]
		public void TablesHeap ()
		{
			var image = GetResourceImage ("hello.exe");
			var heap = image.TableHeap;

			Assert.IsNotNull (heap);

			Assert.AreEqual (1, heap [Table.Module].Length);
			Assert.AreEqual (4, heap [Table.TypeRef].Length);
			Assert.AreEqual (2, heap [Table.TypeDef].Length);
			Assert.AreEqual (0, heap [Table.Field].Length);
			Assert.AreEqual (2, heap [Table.Method].Length);
			Assert.AreEqual (4, heap [Table.MemberRef].Length);
			Assert.AreEqual (2, heap [Table.CustomAttribute].Length);
			Assert.AreEqual (1, heap [Table.Assembly].Length);
			Assert.AreEqual (1, heap [Table.AssemblyRef].Length);
		}

		[TestModule ("hello.x64.exe")]
		public void X64Module (ModuleDefinition module)
		{
			Assert.AreEqual (TargetArchitecture.AMD64, module.Image.Architecture);
			Assert.AreEqual (ModuleAttributes.ILOnly, module.Image.Attributes);
		}

		[TestModule ("hello.ia64.exe")]
		public void IA64Module (ModuleDefinition module)
		{
			Assert.AreEqual (TargetArchitecture.IA64, module.Image.Architecture);
			Assert.AreEqual (ModuleAttributes.ILOnly, module.Image.Attributes);
		}

		[TestModule ("hello.x86.exe")]
		public void X86Module (ModuleDefinition module)
		{
			Assert.AreEqual (TargetArchitecture.I386, module.Image.Architecture);
			Assert.AreEqual (ModuleAttributes.ILOnly | ModuleAttributes.Required32Bit, module.Image.Attributes);
		}

		[TestModule ("hello.anycpu.exe")]
		public void AnyCpuModule (ModuleDefinition module)
		{
			Assert.AreEqual (TargetArchitecture.I386, module.Image.Architecture);
			Assert.AreEqual (ModuleAttributes.ILOnly, module.Image.Attributes);
		}

		[TestModule ("delay-signed.dll")]
		public void DelaySignedAssembly (ModuleDefinition module)
		{
			Assert.IsNotNull (module.Assembly.Name.PublicKey);
			Assert.AreNotEqual (0, module.Assembly.Name.PublicKey.Length);
			Assert.AreNotEqual (ModuleAttributes.StrongNameSigned, module.Attributes & ModuleAttributes.StrongNameSigned);
			Assert.AreNotEqual (0, module.Image.StrongName.VirtualAddress);
			Assert.AreNotEqual (0, module.Image.StrongName.Size);
		}

		[TestModule ("wp7.dll", Verify = false)]
		public void WindowsPhoneNonSignedAssembly (ModuleDefinition module)
		{
			Assert.AreEqual (0, module.Assembly.Name.PublicKey.Length);
			Assert.AreNotEqual (ModuleAttributes.StrongNameSigned, module.Attributes & ModuleAttributes.StrongNameSigned);
			Assert.AreEqual (0, module.Image.StrongName.VirtualAddress);
			Assert.AreEqual (0, module.Image.StrongName.Size);
		}

		[TestModule ("metro.exe", Verify = false)]
		public void MetroAssembly (ModuleDefinition module)
		{
			Assert.AreEqual (ModuleCharacteristics.AppContainer, module.Characteristics & ModuleCharacteristics.AppContainer);
		}
	}
}
