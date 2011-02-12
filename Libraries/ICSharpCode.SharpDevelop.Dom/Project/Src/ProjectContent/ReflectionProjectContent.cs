// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using ICSharpCode.SharpDevelop.Dom.ReflectionLayer;

namespace ICSharpCode.SharpDevelop.Dom
{
	public class ReflectionProjectContent : DefaultProjectContent
	{
		string assemblyFullName;
		string assemblyName;
		DomAssemblyName[] referencedAssemblyNames;
		ICompilationUnit assemblyCompilationUnit;
		string assemblyLocation;
		ProjectContentRegistry registry;
		
		public string AssemblyLocation {
			get {
				return assemblyLocation;
			}
		}
		
		public string AssemblyFullName {
			get {
				return assemblyFullName;
			}
		}
		
		public override string AssemblyName {
			get { return assemblyName; }
		}
		
		/// <summary>
		/// Gets the list of assembly names referenced by this project content.
		/// </summary>
		public IList<DomAssemblyName> ReferencedAssemblyNames {
			get { return Array.AsReadOnly(referencedAssemblyNames); }
		}
		
		public ICompilationUnit AssemblyCompilationUnit {
			get { return assemblyCompilationUnit; }
		}
		
		public override IList<IAttribute> GetAssemblyAttributes()
		{
			return assemblyCompilationUnit.Attributes;
		}
		
		DateTime assemblyFileLastWriteTime;
		
		/// <summary>
		/// Gets if the project content is representing the current version of the assembly.
		/// This property always returns true for ParseProjectContents but might return false
		/// for ReflectionProjectContent/CecilProjectContent if the file was changed.
		/// </summary>
		public override bool IsUpToDate {
			get {
				DateTime newWriteTime;
				try {
					newWriteTime = File.GetLastWriteTimeUtc(assemblyLocation);
				} catch (Exception ex) {
					LoggingService.Warn(ex);
					return true;
				}
				return assemblyFileLastWriteTime == newWriteTime;
			}
		}
		
		public ReflectionProjectContent(Assembly assembly, ProjectContentRegistry registry)
			: this(assembly, assembly.Location, registry)
		{
		}
		
		public ReflectionProjectContent(Assembly assembly, string assemblyLocation, ProjectContentRegistry registry)
			: this(assembly.FullName, assemblyLocation, DomAssemblyName.Convert(assembly.GetReferencedAssemblies()), registry)
		{
			foreach (Type type in assembly.GetExportedTypes()) {
				string name = type.FullName;
				if (name.IndexOf('+') < 0) { // type.IsNested
					AddClassToNamespaceListInternal(new ReflectionClass(assemblyCompilationUnit, type, name, null));
				}
			}
			InitializeSpecialClasses();
			AddAssemblyAttributes(assembly);
			assemblyCompilationUnit.Freeze();
		}
		
		/// <summary>
		/// Adds assembly attributes from the specified assembly.
		/// 
		/// The constructor already does this, this method is meant for unit tests only!
		/// </summary>
		public void AddAssemblyAttributes(Assembly assembly)
		{
			ReflectionClass.AddAttributes(this, assemblyCompilationUnit.Attributes, CustomAttributeData.GetCustomAttributes(assembly));
		}
		
		public ReflectionProjectContent(string assemblyFullName, string assemblyLocation, DomAssemblyName[] referencedAssemblies, ProjectContentRegistry registry)
		{
			if (assemblyFullName == null)
				throw new ArgumentNullException("assemblyFullName");
			if (assemblyLocation == null)
				throw new ArgumentNullException("assemblyLocation");
			if (registry == null)
				throw new ArgumentNullException("registry");
			
			this.registry = registry;
			this.assemblyFullName = assemblyFullName;
			this.assemblyName = (assemblyFullName.IndexOf(',') > -1) ? assemblyFullName.Substring(0, assemblyFullName.IndexOf(',')) : assemblyFullName;
			this.referencedAssemblyNames = referencedAssemblies;
			this.assemblyLocation = assemblyLocation;
			this.assemblyCompilationUnit = new DefaultCompilationUnit(this);
			
			try {
				assemblyFileLastWriteTime = File.GetLastWriteTimeUtc(assemblyLocation);
			} catch (Exception ex) {
				LoggingService.Warn(ex);
			}
			
			string fileName = XmlDoc.LookupLocalizedXmlDoc(assemblyLocation);
			if (fileName == null) {
				// Not found -> look in other directories:
				foreach (string testDirectory in XmlDoc.XmlDocLookupDirectories) {
					fileName = XmlDoc.LookupLocalizedXmlDoc(Path.Combine(testDirectory, Path.GetFileName(assemblyLocation)));
					if (fileName != null)
						break;
				}
			}
			
			if (fileName != null) {
				if (registry.persistence != null) {
					this.XmlDoc = XmlDoc.Load(fileName, Path.Combine(registry.persistence.CacheDirectory, "XmlDoc"));
				} else {
					this.XmlDoc = XmlDoc.Load(fileName, null);
				}
			}
		}
		
		public void InitializeSpecialClasses()
		{
			// Replace the class representing System.Void with VoidClass.Instance
			IClass voidClass = GetClassInternal(VoidClass.VoidName, 0, Language);
			if (voidClass != null) {
				RemoveClass(voidClass);
				AddClassToNamespaceList(new VoidClass(this));
			}
		}
		
		bool initialized = false;
		List<DomAssemblyName> missingNames;
		
		public void InitializeReferences()
		{
			bool changed = false;
			if (initialized) {
				if (missingNames != null) {
					for (int i = 0; i < missingNames.Count; i++) {
						IProjectContent content = registry.GetExistingProjectContent(missingNames[i]);
						if (content != null) {
							changed = true;
							lock (ReferencedContents) {
								ReferencedContents.Add(content);
							}
							missingNames.RemoveAt(i--);
						}
					}
					if (missingNames.Count == 0) {
						missingNames = null;
					}
				}
			} else {
				initialized = true;
				foreach (DomAssemblyName name in referencedAssemblyNames) {
					IProjectContent content = registry.GetExistingProjectContent(name);
					if (content != null) {
						changed = true;
						lock (ReferencedContents) {
							ReferencedContents.Add(content);
						}
					} else {
						if (missingNames == null)
							missingNames = new List<DomAssemblyName>();
						missingNames.Add(name);
					}
				}
			}
			if (changed)
				OnReferencedContentsChanged(EventArgs.Empty);
		}
		
		public override string ToString()
		{
			return string.Format("[{0}: {1}]", GetType().Name, assemblyFullName);
		}
	}
}
