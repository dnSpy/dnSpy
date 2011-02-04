using System;
using System.IO;
using System.Reflection;

using NUnit.Framework;

using Mono.Cecil.PE;

namespace Mono.Cecil.Tests {

	public abstract class BaseTestFixture {

		public static string GetResourcePath (string name, Assembly assembly)
		{
			return Path.Combine (FindResourcesDirectory (assembly), name);
		}

		public static string GetAssemblyResourcePath (string name, Assembly assembly)
		{
			return GetResourcePath (Path.Combine ("assemblies", name), assembly);
		}

		public static string GetCSharpResourcePath (string name, Assembly assembly)
		{
			return GetResourcePath (Path.Combine ("cs", name), assembly);
		}

		public static string GetILResourcePath (string name, Assembly assembly)
		{
			return GetResourcePath (Path.Combine ("il", name), assembly);
		}

		public static ModuleDefinition GetResourceModule (string name)
		{
			return ModuleDefinition.ReadModule (GetAssemblyResourcePath (name, typeof (BaseTestFixture).Assembly));
		}

		public static ModuleDefinition GetResourceModule (string name, ReaderParameters parameters)
		{
			return ModuleDefinition.ReadModule (GetAssemblyResourcePath (name, typeof (BaseTestFixture).Assembly), parameters);
		}

		public static ModuleDefinition GetResourceModule (string name, ReadingMode mode)
		{
			return ModuleDefinition.ReadModule (GetAssemblyResourcePath (name, typeof (BaseTestFixture).Assembly), new ReaderParameters (mode));
		}

		internal static Image GetResourceImage (string name)
		{
			using (var fs = new FileStream (GetAssemblyResourcePath (name, typeof (BaseTestFixture).Assembly), FileMode.Open, FileAccess.Read))
				return ImageReader.ReadImageFrom (fs);
		}

		public static ModuleDefinition GetCurrentModule ()
		{
			return ModuleDefinition.ReadModule (typeof (BaseTestFixture).Module.FullyQualifiedName);
		}

		public static ModuleDefinition GetCurrentModule (ReaderParameters parameters)
		{
			return ModuleDefinition.ReadModule (typeof (BaseTestFixture).Module.FullyQualifiedName, parameters);
		}

		public static string FindResourcesDirectory (Assembly assembly)
		{
			var path = Path.GetDirectoryName (new Uri (assembly.CodeBase).LocalPath);
			while (!Directory.Exists (Path.Combine (path, "Resources"))) {
				var old = path;
				path = Path.GetDirectoryName (path);
				Assert.AreNotEqual (old, path);
			}

			return Path.Combine (path, "Resources");
		}
	}
}
