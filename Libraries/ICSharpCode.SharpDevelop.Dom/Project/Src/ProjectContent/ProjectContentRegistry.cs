// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Contains project contents read from external assemblies.
	/// Caches loaded assemblies in memory and optionally also to disk.
	/// </summary>
	public class ProjectContentRegistry : IDisposable
	{
		internal DomPersistence persistence;
		Dictionary<string, IProjectContent> contents = new Dictionary<string, IProjectContent>(StringComparer.OrdinalIgnoreCase);
		
		/// <summary>
		/// Disposes all project contents stored in this registry.
		/// </summary>
		public virtual void Dispose()
		{
			List<IProjectContent> list;
			lock (contents) {
				list = new List<IProjectContent>(contents.Values);
				contents.Clear();
			}
			// dispose outside the lock
			foreach (IProjectContent pc in list) {
				pc.Dispose();
			}
		}
		
		/// <summary>
		/// Activate caching assemblies to disk.
		/// Cache files will be saved in the specified directory.
		/// </summary>
		public DomPersistence ActivatePersistence(string cacheDirectory)
		{
			if (cacheDirectory == null) {
				throw new ArgumentNullException("cacheDirectory");
			} else if (persistence != null && cacheDirectory == persistence.CacheDirectory) {
				return persistence;
			} else {
				persistence = new DomPersistence(cacheDirectory, this);
				return persistence;
			}
		}
		
		
		
		ReflectionProjectContent mscorlibContent;
		
		/// <summary>
		/// Runs the method inside the lock of the registry.
		/// Use this method if you want to call multiple methods on the ProjectContentRegistry and ensure
		/// that no other thread accesses the registry while your method runs.
		/// </summary>
		public void RunLocked(ThreadStart method)
		{
			lock (contents) {
				method();
			}
		}
		
		public virtual IProjectContent Mscorlib {
			get {
				if (mscorlibContent != null) return mscorlibContent;
				lock (contents) {
					if (contents.ContainsKey("mscorlib")) {
						mscorlibContent = (ReflectionProjectContent)contents["mscorlib"];
						return contents["mscorlib"];
					}
					int time = LoggingService.IsDebugEnabled ? Environment.TickCount : 0;
					LoggingService.Debug("Loading PC for mscorlib...");
					if (persistence != null) {
						mscorlibContent = persistence.LoadProjectContentByAssemblyName(MscorlibAssembly.FullName);
						if (mscorlibContent != null) {
							if (time != 0) {
								LoggingService.Debug("Loaded mscorlib from cache in " + (Environment.TickCount - time) + " ms");
							}
						}
					}
					if (mscorlibContent == null) {
						// We're using Cecil now for everything to find bugs in CecilReader faster
						//mscorlibContent = CecilReader.LoadAssembly(MscorlibAssembly.Location, this);
						
						// After SD 2.1 Beta 2, we're back to Reflection
						mscorlibContent = new ReflectionProjectContent(MscorlibAssembly, this);
						if (time != 0) {
							//LoggingService.Debug("Loaded mscorlib with Cecil in " + (Environment.TickCount - time) + " ms");
							LoggingService.Debug("Loaded mscorlib with Reflection in " + (Environment.TickCount - time) + " ms");
						}
						if (persistence != null) {
							persistence.SaveProjectContent(mscorlibContent);
							LoggingService.Debug("Saved mscorlib to cache");
						}
					}
					contents["mscorlib"] = mscorlibContent;
					contents[mscorlibContent.AssemblyFullName] = mscorlibContent;
					contents[mscorlibContent.AssemblyLocation] = mscorlibContent;
					return mscorlibContent;
				}
			}
		}
		
		public virtual ICollection<IProjectContent> GetLoadedProjectContents()
		{
			lock (contents) { // we need to return a copy because we have to lock
				return new List<IProjectContent>(contents.Values);
			}
		}
		
		/// <summary>
		/// Unloads the specified project content, causing it to be reloaded when
		/// GetProjectContentForReference is called the next time.
		/// Warning: do not unload project contents that are still in use! Doing so will result
		/// in an ObjectDisposedException when the unloaded project content is used the next time!
		/// </summary>
		public void UnloadProjectContent(IProjectContent pc)
		{
			if (pc == null)
				throw new ArgumentNullException("pc");
			LoggingService.Debug("ProjectContentRegistry.UnloadProjectContent: " + pc);
			lock (contents) {
				// find all keys used for the project content - might be the short name/full name/file name
				List<string> keys = new List<string>();
				foreach (KeyValuePair<string, IProjectContent> pair in contents) {
					if (pair.Value == pc) keys.Add(pair.Key);
				}
				foreach (string key in keys) {
					contents.Remove(key);
				}
			}
			pc.Dispose();
		}
		
		public IProjectContent GetExistingProjectContent(DomAssemblyName assembly)
		{
			return GetExistingProjectContent(assembly.FullName);
		}
		
		public virtual IProjectContent GetExistingProjectContent(string fileNameOrAssemblyName)
		{
			lock (contents) {
				if (contents.ContainsKey(fileNameOrAssemblyName)) {
					return contents[fileNameOrAssemblyName];
				}
			}
			
			// GetProjectContentForReference supports redirecting .NET base assemblies to the correct version,
			// so GetExistingProjectContent must support it, too (otherwise assembly interdependencies fail
			// to resolve correctly when a .NET 1.0 assembly is used in a .NET 2.0 project)
			int pos = fileNameOrAssemblyName.IndexOf(',');
			if (pos > 0) {
				string shortName = fileNameOrAssemblyName.Substring(0, pos);
				Assembly assembly = GetDefaultAssembly(shortName);
				if (assembly != null) {
					lock (contents) {
						if (contents.ContainsKey(assembly.FullName)) {
							return contents[assembly.FullName];
						}
					}
				}
			}
			
			return null;
		}
		
		public virtual IProjectContent GetProjectContentForReference(string itemInclude, string itemFileName)
		{
			lock (contents) {
				IProjectContent pc = GetExistingProjectContent(itemFileName);
				if (pc != null) {
					return pc;
				}
				
				LoggingService.Debug("Loading PC for " + itemInclude);
				
				string shortName = itemInclude;
				int pos = shortName.IndexOf(',');
				if (pos > 0)
					shortName = shortName.Substring(0, pos);
				
				#if DEBUG
				int time = Environment.TickCount;
				#endif
				
				try {
					pc = LoadProjectContent(itemInclude, itemFileName);
				} catch (BadImageFormatException ex) {
					HostCallback.ShowAssemblyLoadErrorInternal(itemFileName, itemInclude, ex.Message);
				} catch (Exception ex) {
					HostCallback.ShowError("Error loading assembly " + itemFileName, ex);
				} finally {
					#if DEBUG
					LoggingService.Debug(string.Format("Loaded {0} in {1}ms", itemInclude, Environment.TickCount - time));
					#endif
				}
				
				if (pc != null) {
					ReflectionProjectContent reflectionProjectContent = pc as ReflectionProjectContent;
					if (reflectionProjectContent != null) {
						reflectionProjectContent.InitializeReferences();
						if (reflectionProjectContent.AssemblyFullName != null) {
							contents[reflectionProjectContent.AssemblyFullName] = pc;
						}
					}
					contents[itemInclude] = pc;
					contents[itemFileName] = pc;
				}
				return pc;
			}
		}
		
		protected virtual IProjectContent LoadProjectContent(string itemInclude, string itemFileName)
		{
			string shortName = itemInclude;
			int pos = shortName.IndexOf(',');
			if (pos > 0)
				shortName = shortName.Substring(0, pos);
			
			Assembly assembly = GetDefaultAssembly(shortName);
			ReflectionProjectContent pc = null;
			if (assembly != null) {
				if (persistence != null) {
					pc = persistence.LoadProjectContentByAssemblyName(assembly.FullName);
				}
				if (pc == null) {
					pc = new ReflectionProjectContent(assembly, this);
					if (persistence != null) {
						persistence.SaveProjectContent(pc);
					}
				}
			} else {
				// find real file name for cecil:
				if (File.Exists(itemFileName)) {
					if (persistence != null) {
						pc = persistence.LoadProjectContentByAssemblyName(itemFileName);
					}
					if (pc == null) {
						pc = CecilReader.LoadAssembly(itemFileName, this);
						
						if (persistence != null) {
							persistence.SaveProjectContent(pc);
						}
					}
				} else {
					DomAssemblyName asmName = GacInterop.FindBestMatchingAssemblyName(itemInclude);
					if (persistence != null && asmName != null) {
						//LoggingService.Debug("Looking up in DOM cache: " + asmName.FullName);
						pc = persistence.LoadProjectContentByAssemblyName(asmName.FullName);
					}
					if (pc == null && asmName != null) {
						string subPath = Path.Combine(asmName.ShortName, GetVersion__Token(asmName));
						subPath = Path.Combine(subPath, asmName.ShortName + ".dll");
						foreach (string dir in Directory.GetDirectories(GacInterop.GacRootPathV4, "GAC*")) {
							itemFileName = Path.Combine(dir, subPath);
							if (File.Exists(itemFileName)) {
								pc = CecilReader.LoadAssembly(itemFileName, this);
								if (persistence != null) {
									persistence.SaveProjectContent(pc);
								}
								break;
							}
						}
					}
					if (pc == null) {
						HostCallback.ShowAssemblyLoadErrorInternal(itemFileName, itemInclude, "Could not find assembly file.");
					}
				}
			}
			return pc;
		}
		
		static string GetVersion__Token(DomAssemblyName asmName)
		{
			StringBuilder b = new StringBuilder(asmName.Version.ToString());
			b.Append("__");
			b.Append(asmName.PublicKeyToken);
			return b.ToString();
		}
		
		public static Assembly MscorlibAssembly {
			get {
				return typeof(object).Assembly;
			}
		}
		
		public static Assembly SystemAssembly {
			get {
				return typeof(Uri).Assembly;
			}
		}
		
		protected virtual Assembly GetDefaultAssembly(string shortName)
		{
			// These assemblies are already loaded by SharpDevelop, so we
			// don't need to load them in a separate AppDomain/with Cecil.
			switch (shortName) {
				case "mscorlib":
					return MscorlibAssembly;
				case "System": // System != mscorlib !!!
					return SystemAssembly;
				case "System.Core":
					return typeof(System.Linq.Enumerable).Assembly;
				case "System.Xml":
				case "System.XML":
					return typeof(XmlReader).Assembly;
				case "System.Data":
				case "System.Windows.Forms":
				case "System.Runtime.Remoting":
					return Assembly.Load(shortName + ", Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
				case "System.Configuration":
				case "System.Design":
				case "System.Deployment":
				case "System.Drawing":
				case "System.Drawing.Design":
				case "System.ServiceProcess":
				case "System.Security":
				case "System.Management":
				case "System.Messaging":
				case "System.Web":
				case "System.Web.Services":
					return Assembly.Load(shortName + ", Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
				case "Microsoft.VisualBasic":
					return Assembly.Load("Microsoft.VisualBasic, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
				default:
					return null;
			}
		}
	}
}
