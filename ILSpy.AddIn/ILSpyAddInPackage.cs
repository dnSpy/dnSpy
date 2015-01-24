using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Reflection;
using System.IO;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.AddIn
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	///
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the 
	/// IVsPackage interface and uses the registration attributes defined in the framework to 
	/// register itself and its components with the shell.
	/// </summary>
	// This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
	// a package.
	[PackageRegistration(UseManagedResourcesOnly = true)]
	// This attribute is used to register the information needed to show this package
	// in the Help/About dialog of Visual Studio.
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	// This attribute is needed to let the shell know that this package exposes some menus.
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(GuidList.guidILSpyAddInPkgString)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string)]
	public sealed class ILSpyAddInPackage : Package
	{
		/// <summary>
		/// Default constructor of the package.
		/// Inside this method you can place any initialization code that does not require 
		/// any Visual Studio service because at this point the package object is created but 
		/// not sited yet inside Visual Studio environment. The place to do all the other 
		/// initialization is the Initialize method.
		/// </summary>
		public ILSpyAddInPackage()
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
		}



		/////////////////////////////////////////////////////////////////////////////
		// Overridden Package Implementation
		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override void Initialize()
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
			base.Initialize();

			// Add our command handlers for menu (commands must exist in the .vsct file)
			OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (null != mcs) {
				// Create the command for the menu item.
				CommandID menuCommandID = new CommandID(GuidList.guidILSpyAddInCmdSet, (int)PkgCmdIDList.cmdidOpenInILSpy);
				MenuCommand menuItem = new MenuCommand(OpenInILSpyCallback, menuCommandID);
				mcs.AddCommand(menuItem);

				// Create the command for the menu item.
				CommandID menuCommandID2 = new CommandID(GuidList.guidILSpyAddInCmdSet, (int)PkgCmdIDList.cmdidOpenILSpy);
				MenuCommand menuItem2 = new MenuCommand(OpenILSpyCallback, menuCommandID2);
				mcs.AddCommand(menuItem2);
			}
		}
		#endregion

		/// <summary>
		/// This function is the callback used to execute a command when the a menu item is clicked.
		/// See the Initialize method to see how the menu item is associated to this function using
		/// the OleMenuCommandService service and the MenuCommand class.
		/// </summary>
		private void OpenInILSpyCallback(object sender, EventArgs e)
		{
			var explorer = ((EnvDTE80.DTE2)GetGlobalService(typeof(EnvDTE.DTE))).ToolWindows.SolutionExplorer;
			var items =(object[]) explorer.SelectedItems;

			foreach (EnvDTE.UIHierarchyItem item in items) {
				dynamic reference = item.Object;
				string path = null;
				if (reference.PublicKeyToken != "") {
					var token = Utils.HexStringToBytes(reference.PublicKeyToken);
					path = GacInterop.FindAssemblyInNetGac(new AssemblyNameReference(reference.Identity, new Version(reference.Version)) { PublicKeyToken = token });
				}
				if (path == null)
					path = reference.Path;
				OpenAssemblyInILSpy(path);
			}
		}

		private void OpenILSpyCallback(object sender, EventArgs e)
		{
			Process.Start(GetILSpyPath());
		}

		private string GetILSpyPath()
		{
			var basePath = Path.GetDirectoryName(typeof(ILSpyAddInPackage).Assembly.Location);
			return Path.Combine(basePath, "ILSpy.exe");
		}

		private void OpenAssemblyInILSpy(string assemblyFileName)
		{
			if (!File.Exists(assemblyFileName)) {
				ShowMessage("Could not find assembly '{0}', please ensure the project and all references were built correctly!", assemblyFileName);
				return;
			}
			Process.Start(GetILSpyPath(), Utils.ArgumentArrayToCommandLine(assemblyFileName));
		}

		private void ShowMessage(string format, params object[] items)
		{
			IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
			Guid clsid = Guid.Empty;
			int result;
			Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
				uiShell.ShowMessageBox(
					0,
					ref clsid,
					"ILSpy.AddIn",
					string.Format(CultureInfo.CurrentCulture, format, items),
					string.Empty,
					0,
					OLEMSGBUTTON.OLEMSGBUTTON_OK,
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
					OLEMSGICON.OLEMSGICON_INFO,
					0,        // false
					out result
				)
			);
		}
	}
}