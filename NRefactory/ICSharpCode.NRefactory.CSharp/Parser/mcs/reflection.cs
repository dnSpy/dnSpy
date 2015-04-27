//
// reflection.cs: System.Reflection and System.Reflection.Emit specific implementations
//
// Author: Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2009-2010 Novell, Inc. 
//
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using System.Security;

namespace Mono.CSharp
{
#if STATIC
	public class ReflectionImporter
	{
		public ReflectionImporter (ModuleContainer module, BuiltinTypes builtin)
		{
			throw new NotSupportedException ();
		}

		public void ImportAssembly (Assembly assembly, RootNamespace targetNamespace)
		{
			throw new NotSupportedException ();
		}

		public ImportedModuleDefinition ImportModule (Module module, RootNamespace targetNamespace)
		{
			throw new NotSupportedException ();
		}

		public TypeSpec ImportType (Type type)
		{
			throw new NotSupportedException ();
		}
	}
#else
	public sealed class ReflectionImporter : MetadataImporter
	{
		public ReflectionImporter (ModuleContainer module, BuiltinTypes builtin)
			: base (module)
		{
			Initialize (builtin);
		}

		public override void AddCompiledType (TypeBuilder builder, TypeSpec spec)
		{
		}

		protected override MemberKind DetermineKindFromBaseType (Type baseType)
		{
			if (baseType == typeof (ValueType))
				return MemberKind.Struct;

			if (baseType == typeof (System.Enum))
				return MemberKind.Enum;

			if (baseType == typeof (MulticastDelegate))
				return MemberKind.Delegate;

			return MemberKind.Class;
		}

		protected override bool HasVolatileModifier (Type[] modifiers)
		{
			foreach (var t in modifiers) {
				if (t == typeof (IsVolatile))
					return true;
			}

			return false;
		}

		public void ImportAssembly (Assembly assembly, RootNamespace targetNamespace)
		{
			// It can be used more than once when importing same assembly
			// into 2 or more global aliases
		GetAssemblyDefinition (assembly);

			//
			// This part tries to simulate loading of top-level
			// types only, any missing dependencies are ignores here.
			// Full error report is reported later when the type is
			// actually used
			//
			Type[] all_types;
			try {
				all_types = assembly.GetTypes ();
			} catch (ReflectionTypeLoadException e) {
				all_types = e.Types;
			}

			ImportTypes (all_types, targetNamespace, true);
		}

		public ImportedModuleDefinition ImportModule (Module module, RootNamespace targetNamespace)
		{
			var module_definition = new ImportedModuleDefinition (module);
			module_definition.ReadAttributes ();

			Type[] all_types;
			try {
				all_types = module.GetTypes ();
			} catch (ReflectionTypeLoadException e) {
				all_types = e.Types;
			}

			ImportTypes (all_types, targetNamespace, false);

			return module_definition;
		}

		void Initialize (BuiltinTypes builtin)
		{
			//
			// Setup mapping for build-in types to avoid duplication of their definition
			//
			compiled_types.Add (typeof (object), builtin.Object);
			compiled_types.Add (typeof (System.ValueType), builtin.ValueType);
			compiled_types.Add (typeof (System.Attribute), builtin.Attribute);

			compiled_types.Add (typeof (int), builtin.Int);
			compiled_types.Add (typeof (long), builtin.Long);
			compiled_types.Add (typeof (uint), builtin.UInt);
			compiled_types.Add (typeof (ulong), builtin.ULong);
			compiled_types.Add (typeof (byte), builtin.Byte);
			compiled_types.Add (typeof (sbyte), builtin.SByte);
			compiled_types.Add (typeof (short), builtin.Short);
			compiled_types.Add (typeof (ushort), builtin.UShort);

			compiled_types.Add (typeof (System.Collections.IEnumerator), builtin.IEnumerator);
			compiled_types.Add (typeof (System.Collections.IEnumerable), builtin.IEnumerable);
			compiled_types.Add (typeof (System.IDisposable), builtin.IDisposable);

			compiled_types.Add (typeof (char), builtin.Char);
			compiled_types.Add (typeof (string), builtin.String);
			compiled_types.Add (typeof (float), builtin.Float);
			compiled_types.Add (typeof (double), builtin.Double);
			compiled_types.Add (typeof (decimal), builtin.Decimal);
			compiled_types.Add (typeof (bool), builtin.Bool);
			compiled_types.Add (typeof (System.IntPtr), builtin.IntPtr);
			compiled_types.Add (typeof (System.UIntPtr), builtin.UIntPtr);

			compiled_types.Add (typeof (System.MulticastDelegate), builtin.MulticastDelegate);
			compiled_types.Add (typeof (System.Delegate), builtin.Delegate);
			compiled_types.Add (typeof (System.Enum), builtin.Enum);
			compiled_types.Add (typeof (System.Array), builtin.Array);
			compiled_types.Add (typeof (void), builtin.Void);
			compiled_types.Add (typeof (System.Type), builtin.Type);
			compiled_types.Add (typeof (System.Exception), builtin.Exception);
			compiled_types.Add (typeof (System.RuntimeFieldHandle), builtin.RuntimeFieldHandle);
			compiled_types.Add (typeof (System.RuntimeTypeHandle), builtin.RuntimeTypeHandle);
		}
	}

	[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Explicit)]
	struct SingleConverter
	{
		[System.Runtime.InteropServices.FieldOffset (0)]
		int i;

#pragma warning disable 414
		[System.Runtime.InteropServices.FieldOffset (0)]
		float f;
#pragma warning restore 414

		public static int SingleToInt32Bits (float v)
		{
			SingleConverter c = new SingleConverter ();
			c.f = v;
			return c.i;
		}
	}

#endif

	public class AssemblyDefinitionDynamic : AssemblyDefinition
	{
		//
		// In-memory only assembly container
		//
		public AssemblyDefinitionDynamic (ModuleContainer module, string name)
			: base (module, name)
		{
		}

		//
		// Assembly container with file output
		//
		public AssemblyDefinitionDynamic (ModuleContainer module, string name, string fileName)
			: base (module, name, fileName)
		{
		}

		public Module IncludeModule (string moduleFile)
		{
			return builder_extra.AddModule (moduleFile);
		}

#if !STATIC
		public override ModuleBuilder CreateModuleBuilder ()
		{
			if (file_name == null)
				return Builder.DefineDynamicModule (Name, false);

			return base.CreateModuleBuilder ();
		}
#endif
		//
		// Initializes the code generator
		//
		public bool Create (AppDomain domain, AssemblyBuilderAccess access)
		{
#if STATIC || FULL_AOT_RUNTIME
			throw new NotSupportedException ();
#else
			ResolveAssemblySecurityAttributes ();
			var an = CreateAssemblyName ();

			Builder = file_name == null ?
				domain.DefineDynamicAssembly (an, access) :
				domain.DefineDynamicAssembly (an, access, Dirname (file_name));

			module.Create (this, CreateModuleBuilder ());
			builder_extra = new AssemblyBuilderMonoSpecific (Builder, Compiler);
			return true;
#endif
		}

		static string Dirname (string name)
		{
			int pos = name.LastIndexOf ('/');

			if (pos != -1)
				return name.Substring (0, pos);

			pos = name.LastIndexOf ('\\');
			if (pos != -1)
				return name.Substring (0, pos);

			return ".";
		}

#if !STATIC
		protected override void SaveModule (PortableExecutableKinds pekind, ImageFileMachine machine)
		{
			try {
				var module_only = typeof (AssemblyBuilder).GetProperty ("IsModuleOnly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				var set_module_only = module_only.GetSetMethod (true);

				set_module_only.Invoke (Builder, new object[] { true });
			} catch {
				base.SaveModule (pekind, machine);
			}

			Builder.Save (file_name, pekind, machine);
		}
#endif
	}

	//
	// Extension to System.Reflection.Emit.AssemblyBuilder to have fully compatible
	// compiler
	//
	class AssemblyBuilderMonoSpecific : AssemblyBuilderExtension
	{
		static MethodInfo adder_method;
		static MethodInfo add_permission;
		static MethodInfo add_type_forwarder;
		static MethodInfo win32_icon_define;
		static FieldInfo assembly_version;
		static FieldInfo assembly_algorithm;
		static FieldInfo assembly_culture;
		static FieldInfo assembly_flags;

		AssemblyBuilder builder;

		public AssemblyBuilderMonoSpecific (AssemblyBuilder ab, CompilerContext ctx)
			: base (ctx)
		{
			this.builder = ab;
		}

		public override Module AddModule (string module)
		{
			try {
				if (adder_method == null)
					adder_method = typeof (AssemblyBuilder).GetMethod ("AddModule", BindingFlags.Instance | BindingFlags.NonPublic);

				return (Module) adder_method.Invoke (builder, new object[] { module });
			} catch {
				return base.AddModule (module);
			}
		}

		public override void AddPermissionRequests (PermissionSet[] permissions)
		{
			try {
				if (add_permission == null)
					add_permission = typeof (AssemblyBuilder).GetMethod ("AddPermissionRequests", BindingFlags.Instance | BindingFlags.NonPublic);

				add_permission.Invoke (builder, permissions);
			} catch {
				base.AddPermissionRequests (permissions);
			}
		}

		public override void AddTypeForwarder (TypeSpec type, Location loc)
		{
			try {
				if (add_type_forwarder == null) {
					add_type_forwarder = typeof (AssemblyBuilder).GetMethod ("AddTypeForwarder", BindingFlags.NonPublic | BindingFlags.Instance);
				}

				add_type_forwarder.Invoke (builder, new object[] { type.GetMetaInfo () });
			} catch {
				base.AddTypeForwarder (type, loc);
			}
		}

		public override void DefineWin32IconResource (string fileName)
		{
			try {
				if (win32_icon_define == null)
					win32_icon_define = typeof (AssemblyBuilder).GetMethod ("DefineIconResource", BindingFlags.Instance | BindingFlags.NonPublic);

				win32_icon_define.Invoke (builder, new object[] { fileName });
			} catch {
				base.DefineWin32IconResource (fileName);
			}
		}

		public override void SetAlgorithmId (uint value, Location loc)
		{
			try {
				if (assembly_algorithm == null)
					assembly_algorithm = typeof (AssemblyBuilder).GetField ("algid", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField);

				assembly_algorithm.SetValue (builder, value);
			} catch {
				base.SetAlgorithmId (value, loc);
			}
		}

		public override void SetCulture (string culture, Location loc)
		{
			try {
				if (assembly_culture == null)
					assembly_culture = typeof (AssemblyBuilder).GetField ("culture", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField);

				assembly_culture.SetValue (builder, culture);
			} catch {
				base.SetCulture (culture, loc);
			}
		}

		public override void SetFlags (uint flags, Location loc)
		{
			try {
				if (assembly_flags == null)
					assembly_flags = typeof (AssemblyBuilder).GetField ("flags", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField);

				assembly_flags.SetValue (builder, flags);
			} catch {
				base.SetFlags (flags, loc);
			}
		}

		public override void SetVersion (Version version, Location loc)
		{
			try {
				if (assembly_version == null)
					assembly_version = typeof (AssemblyBuilder).GetField ("version", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField);

				assembly_version.SetValue (builder, version.ToString (4));
			} catch {
				base.SetVersion (version, loc);
			}
		}
	}

	//
	// Reflection based references loader
	//
	class DynamicLoader : AssemblyReferencesLoader<Assembly>
	{
		readonly ReflectionImporter importer;

		public DynamicLoader (ReflectionImporter importer, CompilerContext compiler)
			: base (compiler)
		{
			paths.Add (GetSystemDir ());

			this.importer = importer;
		}

		public ReflectionImporter Importer {
			get {
				return importer;
			}
		}

		protected override string[] GetDefaultReferences ()
		{
			//
			// For now the "default config" is harcoded into the compiler
			// we can move this outside later
			//
			var default_references = new List<string> (8);

			default_references.Add ("System");
			default_references.Add ("System.Xml");
#if NET_2_1
			default_references.Add ("System.Net");
			default_references.Add ("System.Windows");
			default_references.Add ("System.Windows.Browser");
#endif

			if (compiler.Settings.Version > LanguageVersion.ISO_2)
				default_references.Add ("System.Core");
			if (compiler.Settings.Version > LanguageVersion.V_3)
				default_references.Add ("Microsoft.CSharp");

			return default_references.ToArray ();
		}

		//
		// Returns the directory where the system assemblies are installed
		//
		static string GetSystemDir ()
		{
			return Path.GetDirectoryName (typeof (object).Assembly.Location);
		}

		public override bool HasObjectType (Assembly assembly)
		{
			return assembly.GetType (compiler.BuiltinTypes.Object.FullName) != null;
		}

		public override Assembly LoadAssemblyFile (string assembly, bool isImplicitReference)
		{
			Assembly a = null;

			try {
				try {
					char[] path_chars = { '/', '\\' };

					if (assembly.IndexOfAny (path_chars) != -1) {
						a = Assembly.LoadFrom (assembly);
					} else {
						string ass = assembly;
						if (ass.EndsWith (".dll") || ass.EndsWith (".exe"))
							ass = assembly.Substring (0, assembly.Length - 4);
						a = Assembly.Load (ass);
					}
				} catch (FileNotFoundException) {
					bool err = !isImplicitReference;
					foreach (string dir in paths) {
						string full_path = Path.Combine (dir, assembly);
						if (!assembly.EndsWith (".dll") && !assembly.EndsWith (".exe"))
							full_path += ".dll";

						try {
							a = Assembly.LoadFrom (full_path);
							err = false;
							break;
						} catch (FileNotFoundException) {
						}
					}

					if (err) {
						Error_FileNotFound (assembly);
						return a;
					}
				}
			} catch (BadImageFormatException) {
				Error_FileCorrupted (assembly);
			}

			return a;
		}

		Module LoadModuleFile (AssemblyDefinitionDynamic assembly, string module)
		{
			string total_log = "";

			try {
				try {
					return assembly.IncludeModule (module);
				} catch (FileNotFoundException) {
					bool err = true;
					foreach (string dir in paths) {
						string full_path = Path.Combine (dir, module);
						if (!module.EndsWith (".netmodule"))
							full_path += ".netmodule";

						try {
							return assembly.IncludeModule (full_path);
						} catch (FileNotFoundException ff) {
							total_log += ff.FusionLog;
						}
					}
					if (err) {
						Error_FileNotFound (module);
						return null;
					}
				}
			} catch (BadImageFormatException) {
				Error_FileCorrupted (module);
			}

			return null;
		}

		public void LoadModules (AssemblyDefinitionDynamic assembly, RootNamespace targetNamespace)
		{
			foreach (var moduleName in compiler.Settings.Modules) {
				var m = LoadModuleFile (assembly, moduleName);
				if (m == null)
					continue;

				var md = importer.ImportModule (m, targetNamespace);
				assembly.AddModule (md);
			}
		}

		public override void LoadReferences (ModuleContainer module)
		{
			Assembly corlib;
			List<Tuple<RootNamespace, Assembly>> loaded;
			base.LoadReferencesCore (module, out corlib, out loaded);

			if (corlib == null)
				return;

			importer.ImportAssembly (corlib, module.GlobalRootNamespace);
			foreach (var entry in loaded) {
				importer.ImportAssembly (entry.Item2, entry.Item1);
			}
		}
	}
}
