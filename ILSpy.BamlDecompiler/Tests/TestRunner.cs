// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Xml.Linq;
using ICSharpCode.Decompiler.Tests.Helpers;
using ICSharpCode.ILSpy;
using Mono.Cecil;
using NUnit.Framework;
using Ricciolo.StylesExplorer.MarkupReflection;

namespace ILSpy.BamlDecompiler.Tests
{
	[TestFixture]
	public class TestRunner
	{
		[Test]
		public void Simple()
		{
			RunTest("cases/simple");
		}
		
		[Test]
		public void SimpleDictionary()
		{
			RunTest("cases/simpledictionary");
		}
		
		void RunTest(string name)
		{
			string asmPath = typeof(TestRunner).Assembly.Location;
			var assembly = AssemblyDefinition.ReadAssembly(asmPath);
			Resource res = assembly.MainModule.Resources.First();
			Stream bamlStream = LoadBaml(res, name + ".baml");
			Assert.IsNotNull(bamlStream);
			XDocument document = BamlResourceEntryNode.LoadIntoDocument(new DefaultAssemblyResolver(), assembly, bamlStream);
			string path = Path.Combine("..\\..\\Tests", name + ".xaml");
			
			CodeAssert.AreEqual(document.ToString(), File.ReadAllText(path));
		}
		
		Stream LoadBaml(Resource res, string name)
		{
			EmbeddedResource er = res as EmbeddedResource;
			if (er != null) {
				Stream s = er.GetResourceStream();
				s.Position = 0;
				ResourceReader reader;
				try {
					reader = new ResourceReader(s);
				}
				catch (ArgumentException) {
					return null;
				}
				foreach (DictionaryEntry entry in reader.Cast<DictionaryEntry>().OrderBy(e => e.Key.ToString())) {
					if (entry.Key.ToString() == name) {
						if (entry.Value is Stream)
							return (Stream)entry.Value;
						if (entry.Value is byte[])
							return new MemoryStream((byte[])entry.Value);
					}
				}
			}
			
			return null;
		}
	}
}
