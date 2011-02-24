// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;
using Microsoft.Win32;

namespace ICSharpCode.ILSpy.TreeNodes
{
	class ResourceEntryNode : ILSpyTreeNode
	{
		string key;
		Stream value;
		
		public override object Text {
			get { return key.ToString(); }
		}
		
		public override object Icon {
			get { return Images.Resource; }
		}
		
		public ResourceEntryNode(string key, Stream value)
		{
			this.key = key;
			this.value = value;
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, string.Format("{0} = {1}", key, value));
		}
		
		internal override bool View(DecompilerTextView textView)
		{
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			IHighlightingDefinition highlighting = null;
			
			if (LoadImage(output)) {
				textView.Show(output, highlighting);
			} else {
				textView.RunWithCancellation(
					token => Task.Factory.StartNew(
						() => {
							try {
								if (LoadBaml(output))
									highlighting = HighlightingManager.Instance.GetDefinitionByExtension(".xml");
							} catch (Exception ex) {
								output.Write(ex.ToString());
							}
							return output;
						}),
					t => textView.Show(t.Result, highlighting)
				);
			}
			return true;
		}
		
		bool LoadImage(AvalonEditTextOutput output)
		{
			try {
				value.Position = 0;
				BitmapImage image = new BitmapImage();
				image.BeginInit();
				image.StreamSource = value;
				image.EndInit();
				output.AddUIElement(() => new Image { Source = image });
				output.WriteLine();
				output.AddButton(Images.Save, "Save", delegate { Save(null); });
			} catch (Exception) {
				return false;
			}
			return true;
		}
		
		bool LoadBaml(AvalonEditTextOutput output)
		{
			var asm = this.Ancestors().OfType<AssemblyTreeNode>().FirstOrDefault().LoadedAssembly;
			
			AppDomain bamlDecompilerAppDomain = null;
			try {
				BamlDecompiler decompiler = CreateBamlDecompilerInAppDomain(ref bamlDecompilerAppDomain, asm.FileName);
				
				MemoryStream bamlStream = new MemoryStream();
				value.Position = 0;
				value.CopyTo(bamlStream);
				
				output.Write(decompiler.DecompileBaml(bamlStream, asm.FileName));
				return true;
			} finally {
				if (bamlDecompilerAppDomain != null)
					AppDomain.Unload(bamlDecompilerAppDomain);
			}
		}
		
		public static BamlDecompiler CreateBamlDecompilerInAppDomain(ref AppDomain appDomain, string assemblyFileName)
		{
			if (appDomain == null) {
				// Construct and initialize settings for a second AppDomain.
				AppDomainSetup bamlDecompilerAppDomainSetup = new AppDomainSetup();
				bamlDecompilerAppDomainSetup.ApplicationBase = "file:///" + Path.GetDirectoryName(assemblyFileName);
				bamlDecompilerAppDomainSetup.DisallowBindingRedirects = false;
				bamlDecompilerAppDomainSetup.DisallowCodeDownload = true;
				bamlDecompilerAppDomainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

				// Create the second AppDomain.
				appDomain = AppDomain.CreateDomain("BamlDecompiler AD", null, bamlDecompilerAppDomainSetup);
			}
			return (BamlDecompiler)appDomain.CreateInstanceFromAndUnwrap(typeof(BamlDecompiler).Assembly.Location, typeof(BamlDecompiler).FullName);
		}
		
		public override bool Save(DecompilerTextView textView)
		{
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.FileName = Path.GetFileName(DecompilerTextView.CleanUpName(key));
			if (dlg.ShowDialog() == true) {
				value.Position = 0;
				using (var fs = dlg.OpenFile()) {
					value.CopyTo(fs);
				}
			}
			return true;
		}
	}
}
