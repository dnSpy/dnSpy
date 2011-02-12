// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security.Policy;

namespace ILSpy.Debugger.Services.Debugger
{
	[Serializable]
	class RemotingConfigurationHelpper
	{
		public string path;

		public RemotingConfigurationHelpper(string path)
		{
			this.path = path;
		}

		public static string GetLoadedAssemblyPath(string assemblyName)
		{
			string path = null;
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				try {
					string fullFilename = assembly.Location;
					if (Path.GetFileName(fullFilename).Equals(assemblyName, StringComparison.OrdinalIgnoreCase)) {
						path = Path.GetDirectoryName(fullFilename);
						break;
					}
				} catch (NotSupportedException) {
					// assembly.Location throws NotSupportedException for assemblies emitted using
					// Reflection.Emit by custom controls used in the forms designer
				}
			}
			if (path == null) {
				throw new Exception("Assembly " + assemblyName + " is not loaded");
			}
			return path;
		}

		public void Configure()
		{
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
			
			RemotingConfiguration.Configure(Path.Combine(path, "Client.config"), false);

			string baseDir = Directory.GetDirectoryRoot(AppDomain.CurrentDomain.BaseDirectory);
			string relDirs = AppDomain.CurrentDomain.BaseDirectory + ";" + path;
			AppDomain serverAppDomain = AppDomain.CreateDomain("Debugging server",
			                                                   new Evidence(AppDomain.CurrentDomain.Evidence),
			                                                   baseDir,
			                                                   relDirs,
			                                                   AppDomain.CurrentDomain.ShadowCopyFiles);
			serverAppDomain.DoCallBack(new CrossAppDomainDelegate(ConfigureServer));
		}

		private void ConfigureServer()
		{
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
			RemotingConfiguration.Configure(Path.Combine(path, "Server.config"), false);
		}

		Assembly AssemblyResolve(object sender, ResolveEventArgs args)
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				try {
					string fullFilename = assembly.Location;
					if (Path.GetFileNameWithoutExtension(fullFilename).Equals(args.Name, StringComparison.OrdinalIgnoreCase) ||
					    assembly.FullName == args.Name) {
						return assembly;
					}
				} catch (NotSupportedException) {
					// assembly.Location throws NotSupportedException for assemblies emitted using
					// Reflection.Emit by custom controls used in the forms designer
				}
			}
			return null;
		}
	}
}
