//
// AssemblyFactory.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil {

	using System;
	using System.IO;
	using SR = System.Reflection;

	using Mono.Cecil.Binary;

	public sealed class AssemblyFactory {

		AssemblyFactory ()
		{
		}

		static AssemblyDefinition GetAssembly (ImageReader irv, bool manifestOnly)
		{
			StructureReader srv = new StructureReader (irv, manifestOnly);
			AssemblyDefinition asm = new AssemblyDefinition (
				new AssemblyNameDefinition (), srv);

			asm.Accept (srv);
			return asm;
		}

		static AssemblyDefinition GetAssembly (ImageReader reader)
		{
			return GetAssembly (reader, false);
		}

		static AssemblyDefinition GetAssemblyManifest (ImageReader reader)
		{
			return GetAssembly (reader, true);
		}

		public static AssemblyDefinition GetAssembly (string file)
		{
			return GetAssembly (ImageReader.Read (file));
		}

		public static AssemblyDefinition GetAssembly (byte [] assembly)
		{
			return GetAssembly (ImageReader.Read (assembly));
		}

		public static AssemblyDefinition GetAssembly (Stream stream)
		{
			return GetAssembly (ImageReader.Read (stream));
		}

		public static AssemblyDefinition GetAssemblyManifest (string file)
		{
			return GetAssemblyManifest (ImageReader.Read (file));
		}

		public static AssemblyDefinition GetAssemblyManifest (byte [] assembly)
		{
			return GetAssemblyManifest (ImageReader.Read (assembly));
		}

		public static AssemblyDefinition GetAssemblyManifest (Stream stream)
		{
			return GetAssemblyManifest (ImageReader.Read (stream));
		}

		static TargetRuntime CurrentRuntime ()
		{
			Version corlib = typeof (object).Assembly.GetName ().Version;
			if (corlib.Major == 1)
				return corlib.Minor == 0 ? TargetRuntime.NET_1_0 : TargetRuntime.NET_1_1;
			else // if (corlib.Major == 2)
				return TargetRuntime.NET_2_0;
		}

		public static AssemblyDefinition DefineAssembly (string name, AssemblyKind kind)
		{
			return DefineAssembly (name, name, CurrentRuntime (), kind);
		}

		public static AssemblyDefinition DefineAssembly (string name, TargetRuntime rt, AssemblyKind kind)
		{
			return DefineAssembly (name, name, rt, kind);
		}

		public static AssemblyDefinition DefineAssembly (string assemblyName, string moduleName, TargetRuntime rt, AssemblyKind kind)
		{
			AssemblyNameDefinition asmName = new AssemblyNameDefinition ();
			asmName.Name = assemblyName;
			AssemblyDefinition asm = new AssemblyDefinition (asmName);
			asm.Runtime = rt;
			asm.Kind = kind;
			ModuleDefinition main = new ModuleDefinition (moduleName, asm, true);
			asm.Modules.Add (main);
			return asm;
		}

		static void WriteAssembly (AssemblyDefinition asm, BinaryWriter bw)
		{
			asm.Accept (new StructureWriter (asm, bw));
		}

		public static void SaveAssembly (AssemblyDefinition asm, string file)
		{
			using (FileStream fs = new FileStream (
				file, FileMode.Create, FileAccess.Write, FileShare.None)) {

				SaveAssembly (asm, fs);
				asm.MainModule.Image.SetFileInfo (new FileInfo (file));
			}
		}

		public static void SaveAssembly (AssemblyDefinition asm, out byte [] assembly)
		{
			MemoryBinaryWriter bw = new MemoryBinaryWriter ();
			SaveAssembly (asm, bw.BaseStream);
			assembly = bw.ToArray ();
		}

		public static void SaveAssembly (AssemblyDefinition asm, Stream stream)
		{
			BinaryWriter bw = new BinaryWriter (stream);
			try {
				WriteAssembly (asm, bw);
			} finally {
				bw.Close ();
			}

			foreach (ModuleDefinition module in asm.Modules)
				if (module.Controller.Writer.SaveSymbols)
					module.Controller.Writer.WriteSymbols (module);
		}

#if !CF_1_0 && !CF_2_0
		public static SR.Assembly CreateReflectionAssembly (AssemblyDefinition asm, AppDomain domain)
		{
			using (MemoryBinaryWriter writer = new MemoryBinaryWriter ()) {

				WriteAssembly (asm, writer);
				return domain.Load (writer.ToArray ());
			}
		}

		public static SR.Assembly CreateReflectionAssembly (AssemblyDefinition asm)
		{
			return CreateReflectionAssembly (asm, AppDomain.CurrentDomain);
		}
#endif
	}
}
