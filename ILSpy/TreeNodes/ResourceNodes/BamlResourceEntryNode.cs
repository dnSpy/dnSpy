// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.Baml
{
	[Export(typeof(IResourceNodeFactory))]
	sealed class BamlResourceNodeFactory : IResourceNodeFactory
	{
		public ILSpyTreeNode CreateNode(Mono.Cecil.Resource resource)
		{
			return null;
		}

		public ILSpyTreeNode CreateNode(string key, Stream data)
		{
			if (key.EndsWith(".baml", StringComparison.OrdinalIgnoreCase))
				return new BamlResourceEntryNode(key, data);
			else
				return null;
		}
	}

	sealed class BamlResourceEntryNode : ResourceEntryNode
	{
		public BamlResourceEntryNode(string key, Stream data)
			: base(key, data)
		{
		}

		internal override bool View(DecompilerTextView textView)
		{
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			IHighlightingDefinition highlighting = null;
			textView.RunWithCancellation(token => Task.Factory.StartNew(() => {
				try {
					if (LoadBaml(output))
						highlighting = HighlightingManager.Instance.GetDefinitionByExtension(".xml");
				}
				catch (Exception ex) {
					output.Write(ex.ToString());
				}
				return output;
			}), t => textView.ShowNode(t.Result, this, highlighting));
			return true;
		}

		bool LoadBaml(AvalonEditTextOutput output)
		{
			var asm = this.Ancestors().OfType<AssemblyTreeNode>().FirstOrDefault().LoadedAssembly;
			AppDomain bamlDecompilerAppDomain = null;
			try {
				BamlDecompiler decompiler = CreateBamlDecompilerInAppDomain(ref bamlDecompilerAppDomain, asm.FileName);
				MemoryStream bamlStream = new MemoryStream();
				Data.Position = 0;
				Data.CopyTo(bamlStream);
				output.Write(decompiler.DecompileBaml(bamlStream, asm.FileName, new ConnectMethodDecompiler(asm), new AssemblyResolver(asm)));
				return true;
			}
			finally {
				if (bamlDecompilerAppDomain != null)
					AppDomain.Unload(bamlDecompilerAppDomain);
			}
		}

		public static BamlDecompiler CreateBamlDecompilerInAppDomain(ref AppDomain appDomain, string assemblyFileName)
		{
			if (appDomain == null) {
				// Construct and initialize settings for a second AppDomain.
				AppDomainSetup bamlDecompilerAppDomainSetup = new AppDomainSetup();
				//				bamlDecompilerAppDomainSetup.ApplicationBase = "file:///" + Path.GetDirectoryName(assemblyFileName);
				bamlDecompilerAppDomainSetup.DisallowBindingRedirects = false;
				bamlDecompilerAppDomainSetup.DisallowCodeDownload = true;
				bamlDecompilerAppDomainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
				// Create the second AppDomain.
				appDomain = AppDomain.CreateDomain("BamlDecompiler AD", null, bamlDecompilerAppDomainSetup);
			}
			return (BamlDecompiler)appDomain.CreateInstanceAndUnwrap(typeof(BamlDecompiler).Assembly.FullName, typeof(BamlDecompiler).FullName);
		}
	}
}
