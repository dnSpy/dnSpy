ILSpy uses MEF (Managed Extensibility Framework) for plugins.
Plugins must be placed in the same directory as ILSpy.exe, and must be called "*.Plugin.dll".

To write a plugin, you need to add a reference to ILSpy.exe and to System.ComponentModel.Composition.
Depending on what your plugin is doing, you might also need references to the other libraries shipping with ILSpy.

Plugins work by exporting types for certain extension points.
Here is a list of extension points:

Adding another language:

	[Export(typeof(Language))]
	public class CustomLanguage : Language
	
	This adds an additional language to the combobox in the toolbar.
	The language has to implement its own decompiler (all the way from IL), but it can also re-use
	the ICSharpCode.Decompiler library to decompile to C#, and then translate the C# code to the target language.

---

Adding an entry to the main menu:

	[ExportMainMenuCommand(Menu = "_File", Header = "_Clear List", MenuCategory = "Open", MenuOrder = 1.5)]
	public class UnloadAllAssembliesCommand : SimpleCommand

