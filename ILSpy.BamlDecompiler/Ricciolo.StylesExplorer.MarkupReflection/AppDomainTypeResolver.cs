// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Threading;
using System.Security.Permissions;
using System.Security;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	public delegate void AssemblyResolveEventHandler(object s, AssemblyResolveEventArgs e);

	public class AppDomainTypeResolver : MarshalByRefObject, ITypeResolver
	{
		private readonly AppDomain _domain;
		private string baseDir;

		public event AssemblyResolveEventHandler AssemblyResolve;

		public static AppDomainTypeResolver GetIntoNewAppDomain(string baseDir)
		{
			AppDomainSetup info = new AppDomainSetup();
			info.ApplicationBase = Environment.CurrentDirectory;
			AppDomain domain = AppDomain.CreateDomain("AppDomainTypeResolver", null, info, new PermissionSet(PermissionState.Unrestricted));
			AppDomainTypeResolver resolver = (AppDomainTypeResolver)domain.CreateInstanceAndUnwrap(typeof(AppDomainTypeResolver).Assembly.FullName,
			                                                                                       typeof(AppDomainTypeResolver).FullName, false, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, new object[] { domain, baseDir }, null, null, null);

			return resolver;
		}

		Assembly domain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			// Cerco di risolvere automaticamente
			AssemblyName name = new AssemblyName(args.Name);
			string fileName = Path.Combine(this.baseDir, name.Name + ".exe");
			if (!File.Exists(fileName))
				fileName = Path.Combine(this.baseDir, name.Name + ".dll");

			// Carico il percorso autocalcolato
			if (File.Exists(fileName))
				return Assembly.LoadFile(fileName);

			if (AssemblyResolve != null)
			{
				AssemblyResolveEventArgs e = new AssemblyResolveEventArgs(args.Name, this.baseDir);
				AssemblyResolve(this, e);
				if (!String.IsNullOrEmpty(e.Location) && File.Exists(e.Location))
					return Assembly.LoadFile(e.Location);
			}

			return null;
		}

		public static void DestroyResolver(AppDomainTypeResolver resolver)
		{
			if (resolver == null) throw new ArgumentNullException("resolver");

			ThreadPool.QueueUserWorkItem(delegate
			                             {
			                             	AppDomain.Unload(resolver.Domain);
			                             });
		}

		protected AppDomainTypeResolver(AppDomain domain, string baseDir)
		{
			_domain = domain;
			this.baseDir = baseDir;

			domain.AssemblyResolve += new ResolveEventHandler(domain_AssemblyResolve);
		}

		public BamlAssembly LoadAssembly(AssemblyName asm)
		{
			//return new BamlAssembly(Assembly.Load(asm));
			return new BamlAssembly(_domain.Load(asm));
		}

		public BamlAssembly LoadAssembly(string location)
		{
			Assembly asm = Assembly.LoadFile(location);
			return new BamlAssembly(asm);
			//return _domain.Load(System.IO.File.ReadAllBytes(location));
			//return Assembly.LoadFrom(location);
		}

		public BamlAssembly[] GetReferencedAssemblies(BamlAssembly asm)
		{
			AssemblyName[] list = asm.Assembly.GetReferencedAssemblies();

			return (from an in list
			        select this.LoadAssembly(an)).ToArray();
		}

		public AppDomain Domain
		{
			get { return _domain; }
		}

		#region ITypeResolver Members

		public IType GetTypeByAssemblyQualifiedName(string name)
		{
			return new DotNetType(name);
		}

		public IDependencyPropertyDescriptor GetDependencyPropertyDescriptor(string name, IType ownerType, IType targetType)
		{
			if (name == null) throw new ArgumentNullException("name");
			if (ownerType == null) throw new ArgumentNullException("ownerType");
			if (targetType == null) throw new ArgumentNullException("targetType");

			Type dOwnerType = ((DotNetType)ownerType).Type;
			Type dTargetType = ((DotNetType)targetType).Type;

			try
			{
				DependencyPropertyDescriptor propertyDescriptor = DependencyPropertyDescriptor.FromName(name, dOwnerType, dTargetType);
				if (propertyDescriptor != null)
					return new WpfDependencyPropertyDescriptor(propertyDescriptor);
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}
		
		public bool IsLocalAssembly(string name)
		{
			return false;
		}
		
		public string RuntimeVersion {
			get {
				throw new NotImplementedException();
			}
		}

		#endregion

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}

	public class AssemblyResolveEventArgs : MarshalByRefObject
	{

		private string _location;
		private string _name;
		private string _baseDir;

		public AssemblyResolveEventArgs(string name, string baseDir)
		{
			_name = name;
			_baseDir = baseDir;
		}

		public string Location
		{
			get { return _location; }
			set { _location = value; }
		}

		public string Name
		{
			get { return _name; }
		}

		public string BaseDir
		{
			get { return _baseDir; }
		}
	}
}
