//
// namespace.cs: Tracks namespaces
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@seznam.cz)
//
// Copyright 2001 Ximian, Inc.
// Copyright 2003-2008 Novell, Inc.
// Copyright 2011 Xamarin Inc
//
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.CompilerServices.SymbolWriter;

namespace Mono.CSharp {

	public class RootNamespace : Namespace {

		readonly string alias_name;
		readonly Dictionary<string, Namespace> all_namespaces;

		public RootNamespace (string alias_name)
			: base ()
		{
			this.alias_name = alias_name;
			RegisterNamespace (this);

			all_namespaces = new Dictionary<string, Namespace> ();
			all_namespaces.Add ("", this);
		}

		public string Alias {
			get {
				return alias_name;
			}
		}

		public static void Error_GlobalNamespaceRedefined (Report report, Location loc)
		{
			report.Error (1681, loc, "The global extern alias cannot be redefined");
		}

		//
		// For better error reporting where we try to guess missing using directive
		//
		public List<string> FindTypeNamespaces (IMemberContext ctx, string name, int arity)
		{
			List<string> res = null;

			foreach (var ns in all_namespaces) {
				var type = ns.Value.LookupType (ctx, name, arity, LookupMode.Normal, Location.Null);
				if (type != null) {
					if (res == null)
						res = new List<string> ();

					res.Add (ns.Key);
				}
			}

			return res;
		}

		//
		// For better error reporting where compiler tries to guess missing using directive
		//
		public List<string> FindExtensionMethodNamespaces (IMemberContext ctx, string name, int arity)
		{
			List<string> res = null;

			foreach (var ns in all_namespaces) {
				var methods = ns.Value.LookupExtensionMethod (ctx, name, arity);
				if (methods != null) {
					if (res == null)
						res = new List<string> ();

					res.Add (ns.Key);
				}
			}

			return res;
		}

		public void RegisterNamespace (Namespace child)
		{
			if (child != this)
				all_namespaces.Add (child.Name, child);
		}

		public override string GetSignatureForError ()
		{
			return alias_name + "::";
		}
	}

	public sealed class GlobalRootNamespace : RootNamespace
	{
		public GlobalRootNamespace ()
			: base ("global")
		{
		}
	}

	//
	// Namespace cache for imported and compiled namespaces
	//
	public class Namespace
	{
		readonly Namespace parent;
		string fullname;
		protected Dictionary<string, Namespace> namespaces;
		protected Dictionary<string, IList<TypeSpec>> types;
		List<TypeSpec> extension_method_types;
		Dictionary<string, TypeSpec> cached_types;
		bool cls_checked;

		/// <summary>
		///   Constructor Takes the current namespace and the
		///   name.  This is bootstrapped with parent == null
		///   and name = ""
		/// </summary>
		public Namespace (Namespace parent, string name)
			: this ()
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			this.parent = parent;

			string pname = parent != null ? parent.fullname : null;
				
			if (pname == null)
				fullname = name;
			else
				fullname = pname + "." + name;

			while (parent.parent != null)
				parent = parent.parent;

			var root = parent as RootNamespace;
			if (root == null)
				throw new InternalErrorException ("Root namespaces must be created using RootNamespace");

			root.RegisterNamespace (this);
		}

		protected Namespace ()
		{
			namespaces = new Dictionary<string, Namespace> ();
			cached_types = new Dictionary<string, TypeSpec> ();
		}

		#region Properties

		/// <summary>
		///   The qualified name of the current namespace
		/// </summary>
		public string Name {
			get { return fullname; }
		}

		/// <summary>
		///   The parent of this namespace, used by the parser to "Pop"
		///   the current namespace declaration
		/// </summary>
		public Namespace Parent {
			get { return parent; }
		}

		#endregion

		public Namespace AddNamespace (MemberName name)
		{
			var ns_parent = name.Left == null ? this : AddNamespace (name.Left);
			return ns_parent.TryAddNamespace (name.Basename);
		}

		Namespace TryAddNamespace (string name)
		{
			Namespace ns;

			if (!namespaces.TryGetValue (name, out ns)) {
				ns = new Namespace (this, name);
				namespaces.Add (name, ns);
			}

			return ns;
		}

		public bool TryGetNamespace (string name, out Namespace ns)
		{
			return namespaces.TryGetValue (name, out ns);
		}

		// TODO: Replace with CreateNamespace where MemberName is created for the method call
		public Namespace GetNamespace (string name, bool create)
		{
			int pos = name.IndexOf ('.');

			Namespace ns;
			string first;
			if (pos >= 0)
				first = name.Substring (0, pos);
			else
				first = name;

			if (!namespaces.TryGetValue (first, out ns)) {
				if (!create)
					return null;

				ns = new Namespace (this, first);
				namespaces.Add (first, ns);
			}

			if (pos >= 0)
				ns = ns.GetNamespace (name.Substring (pos + 1), create);

			return ns;
		}

		public IList<TypeSpec> GetAllTypes (string name)
		{
			IList<TypeSpec> found;
			if (types == null || !types.TryGetValue (name, out found))
				return null;

			return found;
		}

		public virtual string GetSignatureForError ()
		{
			return fullname;
		}

		public TypeSpec LookupType (IMemberContext ctx, string name, int arity, LookupMode mode, Location loc)
		{
			if (types == null)
				return null;

			TypeSpec best = null;
			if (arity == 0 && cached_types.TryGetValue (name, out best)) {
				if (best != null || mode != LookupMode.IgnoreAccessibility)
					return best;
			}

			IList<TypeSpec> found;
			if (!types.TryGetValue (name, out found))
				return null;

			foreach (var ts in found) {
				if (ts.Arity == arity || mode == LookupMode.NameOf) {
					if (best == null) {
						if ((ts.Modifiers & Modifiers.INTERNAL) != 0 && !ts.MemberDefinition.IsInternalAsPublic (ctx.Module.DeclaringAssembly) && mode != LookupMode.IgnoreAccessibility)
							continue;

						best = ts;
						continue;
					}

					if (best.MemberDefinition.IsImported && ts.MemberDefinition.IsImported) {
						if (ts.Kind == MemberKind.MissingType)
							continue;

						if (best.Kind == MemberKind.MissingType) {
							best = ts;
							continue;
						}

						if (mode == LookupMode.Normal) {
							ctx.Module.Compiler.Report.SymbolRelatedToPreviousError (best);
							ctx.Module.Compiler.Report.SymbolRelatedToPreviousError (ts);
							ctx.Module.Compiler.Report.Error (433, loc, "The imported type `{0}' is defined multiple times", ts.GetSignatureForError ());
						}

						break;
					}

					if (best.MemberDefinition.IsImported)
						best = ts;

					if ((best.Modifiers & Modifiers.INTERNAL) != 0 && !best.MemberDefinition.IsInternalAsPublic (ctx.Module.DeclaringAssembly))
						continue;

					if (mode != LookupMode.Normal)
						continue;

					if (ts.MemberDefinition.IsImported) {
						ctx.Module.Compiler.Report.SymbolRelatedToPreviousError (best);
						ctx.Module.Compiler.Report.SymbolRelatedToPreviousError (ts);
					}

					ctx.Module.Compiler.Report.Warning (436, 2, loc,
						"The type `{0}' conflicts with the imported type of same name'. Ignoring the imported type definition",
						best.GetSignatureForError ());
				}

				//
				// Lookup for the best candidate with the closest arity match
				//
				if (arity < 0) {
					if (best == null) {
						best = ts;
					} else if (System.Math.Abs (ts.Arity + arity) < System.Math.Abs (best.Arity + arity)) {
						best = ts;
					}
				}
			}

			// TODO MemberCache: Cache more
			if (arity == 0 && mode == LookupMode.Normal)
				cached_types.Add (name, best);

			if (best != null) {
				var dep = best.GetMissingDependencies ();
				if (dep != null)
					ImportedTypeDefinition.Error_MissingDependency (ctx, dep, loc);
			}

			return best;
		}

		public FullNamedExpression LookupTypeOrNamespace (IMemberContext ctx, string name, int arity, LookupMode mode, Location loc)
		{
			var texpr = LookupType (ctx, name, arity, mode, loc);

			Namespace ns;
			if (arity == 0 && namespaces.TryGetValue (name, out ns)) {
				if (texpr == null)
					return new NamespaceExpression (ns, loc);

				if (mode != LookupMode.Probing) {
					//ctx.Module.Compiler.Report.SymbolRelatedToPreviousError (texpr.Type);
					// ctx.Module.Compiler.Report.SymbolRelatedToPreviousError (ns.loc, "");
					ctx.Module.Compiler.Report.Warning (437, 2, loc,
						"The type `{0}' conflicts with the imported namespace `{1}'. Using the definition found in the source file",
						texpr.GetSignatureForError (), ns.GetSignatureForError ());
				}

				if (texpr.MemberDefinition.IsImported)
					return new NamespaceExpression (ns, loc);
			}

			if (texpr == null)
				return null;

			return new TypeExpression (texpr, loc);
		}

		//
		// Completes types with the given `prefix'
		//
		public IEnumerable<string> CompletionGetTypesStartingWith (string prefix)
		{
			if (types == null)
				return Enumerable.Empty<string> ();

			var res = from item in types
					  where item.Key.StartsWith (prefix) && item.Value.Any (l => (l.Modifiers & Modifiers.PUBLIC) != 0)
					  select item.Key;

			if (namespaces != null)
				res = res.Concat (from item in namespaces where item.Key.StartsWith (prefix) select item.Key);

			return res;
		}

		// 
		// Looks for extension method in this namespace
		//
		public List<MethodSpec> LookupExtensionMethod (IMemberContext invocationContext, string name, int arity)
		{
			if (extension_method_types == null)
				return null;

			List<MethodSpec> found = null;
			for (int i = 0; i < extension_method_types.Count; ++i) {
				var ts = extension_method_types[i];

				//
				// When the list was built we didn't know what members the type
				// contains
				//
				if ((ts.Modifiers & Modifiers.METHOD_EXTENSION) == 0) {
					if (extension_method_types.Count == 1) {
						extension_method_types = null;
						return found;
					}

					extension_method_types.RemoveAt (i--);
					continue;
				}

				var res = ts.MemberCache.FindExtensionMethods (invocationContext, name, arity);
				if (res == null)
					continue;

				if (found == null) {
					found = res;
				} else {
					found.AddRange (res);
				}
			}

			return found;
		}

		public void AddType (ModuleContainer module, TypeSpec ts)
		{
			if (types == null) {
				types = new Dictionary<string, IList<TypeSpec>> (64);
			}

			if (ts.IsClass && ts.Arity == 0) {
				var extension_method_allowed = ts.MemberDefinition.IsImported ? (ts.Modifiers & Modifiers.METHOD_EXTENSION) != 0 : (ts.IsStatic || ts.MemberDefinition.IsPartial);
				if (extension_method_allowed) {
					if (extension_method_types == null)
						extension_method_types = new List<TypeSpec> ();

					extension_method_types.Add (ts);
				}
			}

			var name = ts.Name;
			IList<TypeSpec> existing;
			if (types.TryGetValue (name, out existing)) {
				TypeSpec better_type;
				TypeSpec found;
				if (existing.Count == 1) {
					found = existing[0];
					if (ts.Arity == found.Arity) {
						better_type = IsImportedTypeOverride (module, ts, found);
						if (better_type == found)
							return;

						if (better_type != null) {
							existing [0] = better_type;
							return;
						}
					}

					existing = new List<TypeSpec> ();
					existing.Add (found);
					types[name] = existing;
				} else {
					for (int i = 0; i < existing.Count; ++i) {
						found = existing[i];
						if (ts.Arity != found.Arity)
							continue;

						better_type = IsImportedTypeOverride (module, ts, found);
						if (better_type == found)
							return;

						if (better_type != null) {
							existing.RemoveAt (i);
							--i;
							continue;
						}
					}
				}

				existing.Add (ts);
			} else {
				types.Add (name, new TypeSpec[] { ts });
			}
		}

		//
		// We import any types but in the situation there are same types
		// but one has better visibility (either public or internal with friend)
		// the less visible type is removed from the namespace cache
		//
		public static TypeSpec IsImportedTypeOverride (ModuleContainer module, TypeSpec ts, TypeSpec found)
		{
			var ts_accessible = (ts.Modifiers & Modifiers.PUBLIC) != 0 || ts.MemberDefinition.IsInternalAsPublic (module.DeclaringAssembly);
			var found_accessible = (found.Modifiers & Modifiers.PUBLIC) != 0 || found.MemberDefinition.IsInternalAsPublic (module.DeclaringAssembly);

			if (ts_accessible && !found_accessible)
				return ts;

			// found is better always better for accessible or inaccessible ts
			if (!ts_accessible)
				return found;

			return null;
		}

		public void RemoveContainer (TypeContainer tc)
		{
			types.Remove (tc.Basename);
			cached_types.Remove (tc.Basename);
		}

		public void SetBuiltinType (BuiltinTypeSpec pts)
		{
			var found = types[pts.Name];
			cached_types.Remove (pts.Name);
			if (found.Count == 1) {
				types[pts.Name][0] = pts;
			} else {
				throw new NotImplementedException ();
			}
		}

		public void VerifyClsCompliance ()
		{
			if (types == null || cls_checked)
				return;

			cls_checked = true;

			// TODO: This is quite ugly way to check for CLS compliance at namespace level

			var locase_types = new Dictionary<string, List<TypeSpec>> (StringComparer.OrdinalIgnoreCase);
			foreach (var tgroup in types.Values) {
				foreach (var tm in tgroup) {
					if ((tm.Modifiers & Modifiers.PUBLIC) == 0 || !tm.IsCLSCompliant ())
						continue;

					List<TypeSpec> found;
					if (!locase_types.TryGetValue (tm.Name, out found)) {
						found = new List<TypeSpec> ();
						locase_types.Add (tm.Name, found);
					}

					found.Add (tm);
				}
			}

			foreach (var locase in locase_types.Values) {
				if (locase.Count < 2)
					continue;

				bool all_same = true;
				foreach (var notcompliant in locase) {
					all_same = notcompliant.Name == locase[0].Name;
					if (!all_same)
						break;
				}

				if (all_same)
					continue;

				TypeContainer compiled = null;
				foreach (var notcompliant in locase) {
					if (!notcompliant.MemberDefinition.IsImported) {
						if (compiled != null)
							compiled.Compiler.Report.SymbolRelatedToPreviousError (compiled);

						compiled = notcompliant.MemberDefinition as TypeContainer;
					} else {
						compiled.Compiler.Report.SymbolRelatedToPreviousError (notcompliant);
					}
				}

				compiled.Compiler.Report.Warning (3005, 1, compiled.Location,
					"Identifier `{0}' differing only in case is not CLS-compliant", compiled.GetSignatureForError ());
			}
		}
	}

	public class CompilationSourceFile : NamespaceContainer
	{
		readonly SourceFile file;
		CompileUnitEntry comp_unit;
		Dictionary<string, SourceFile> include_files;
		Dictionary<string, bool> conditionals;

		public CompilationSourceFile (ModuleContainer parent, SourceFile sourceFile)
			: this (parent)
		{
			this.file = sourceFile;
		}

		public CompilationSourceFile (ModuleContainer parent)
			: base (parent)
		{
		}

		public CompileUnitEntry SymbolUnitEntry {
			get {
				return comp_unit;
			}
		}

		public IDictionary<string, bool> Conditionals {
			get {
				return conditionals ?? new Dictionary<string, bool> ();
			}
		}

		public string FileName {
			get {
				return file.Name;
			}
		}

		public SourceFile SourceFile {
			get {
				return file;
			}
		}

		public void AddIncludeFile (SourceFile file)
		{
			if (file == this.file)
				return;

			if (include_files == null)
				include_files = new Dictionary<string, SourceFile> ();

			if (!include_files.ContainsKey (file.FullPathName))
				include_files.Add (file.FullPathName, file);
		}

		public void AddDefine (string value)
		{
			if (conditionals == null)
				conditionals = new Dictionary<string, bool> (2);

			conditionals[value] = true;
		}

		public void AddUndefine (string value)
		{
			if (conditionals == null)
				conditionals = new Dictionary<string, bool> (2);

			conditionals[value] = false;
		}

		public override void PrepareEmit ()
		{
			var sw = Module.DeclaringAssembly.SymbolWriter;
			if (sw != null) {
				CreateUnitSymbolInfo (sw);
			}

			base.PrepareEmit ();
		}

		//
		// Creates symbol file index in debug symbol file
		//
		void CreateUnitSymbolInfo (MonoSymbolFile symwriter)
		{
			var si = file.CreateSymbolInfo (symwriter);
			comp_unit = new CompileUnitEntry (symwriter, si);

			if (include_files != null) {
				foreach (SourceFile include in include_files.Values) {
					si = include.CreateSymbolInfo (symwriter);
					comp_unit.AddFile (si);
				}
			}
		}

		public bool IsConditionalDefined (string value)
		{
			if (conditionals != null) {
				bool res;
				if (conditionals.TryGetValue (value, out res))
					return res;

				// When conditional was undefined
				if (conditionals.ContainsKey (value))
					return false;
			}

			return Compiler.Settings.IsConditionalSymbolDefined (value);
		}

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}
	}


	//
	// Namespace block as created by the parser
	//
	public class NamespaceContainer : TypeContainer, IMemberContext
	{
		static readonly Namespace[] empty_namespaces = new Namespace[0];

		readonly Namespace ns;

		public new readonly NamespaceContainer Parent;

		List<UsingNamespace> clauses;

		// Used by parsed to check for parser errors
		public bool DeclarationFound;

		Namespace[] namespace_using_table;
		TypeSpec[] types_using_table;
		Dictionary<string, UsingAliasNamespace> aliases;
		public readonly MemberName RealMemberName;

		public NamespaceContainer (MemberName name, NamespaceContainer parent)
			: base (parent, name, null, MemberKind.Namespace)
		{
			this.RealMemberName = name;
			this.Parent = parent;
			this.ns = parent.NS.AddNamespace (name);

			containers = new List<TypeContainer> ();
		}

		protected NamespaceContainer (ModuleContainer parent)
			: base (parent, null, null, MemberKind.Namespace)
		{
			ns = parent.GlobalRootNamespace;
			containers = new List<TypeContainer> (2);
		}

		#region Properties

		public override AttributeTargets AttributeTargets {
			get {
				throw new NotSupportedException ();
			}
		}

		public override string DocCommentHeader {
			get {
				throw new NotSupportedException ();
			}
		}

		public Namespace NS {
			get {
				return ns;
			}
		}

		public List<UsingNamespace> Usings {
			get {
				return clauses;
			}
		}

		public override string[] ValidAttributeTargets {
			get {
				throw new NotSupportedException ();
			}
		}

		#endregion

		public void AddUsing (UsingNamespace un)
		{
			if (DeclarationFound){
				Compiler.Report.Error (1529, un.Location, "A using clause must precede all other namespace elements except extern alias declarations");
			}

			if (clauses == null)
				clauses = new List<UsingNamespace> ();

			clauses.Add (un);
		}

		public void AddUsing (UsingAliasNamespace un)
		{
			if (DeclarationFound){
				Compiler.Report.Error (1529, un.Location, "A using clause must precede all other namespace elements except extern alias declarations");
			}

			AddAlias (un);
		}

		void AddAlias (UsingAliasNamespace un)
		{
			if (clauses == null) {
				clauses = new List<UsingNamespace> ();
			} else {
				foreach (var entry in clauses) {
					var a = entry as UsingAliasNamespace;
					if (a != null && a.Alias.Value == un.Alias.Value) {
						Compiler.Report.SymbolRelatedToPreviousError (a.Location, "");
						Compiler.Report.Error (1537, un.Location,
							"The using alias `{0}' appeared previously in this namespace", un.Alias.Value);
					}
				}
			}

			clauses.Add (un);
		}

		public override void AddPartial (TypeDefinition next_part)
		{
			var existing = ns.LookupType (this, next_part.MemberName.Name, next_part.MemberName.Arity, LookupMode.Probing, Location.Null);
			var td = existing != null ? existing.MemberDefinition as TypeDefinition : null;
			AddPartial (next_part, td);
		}

		public override void AddTypeContainer (TypeContainer tc)
		{
			string name = tc.Basename;

			var mn = tc.MemberName;
			while (mn.Left != null) {
				mn = mn.Left;
				name = mn.Name;
			}

			var names_container = Parent == null ? Module : (TypeContainer) this;

			MemberCore mc;
			if (names_container.DefinedNames.TryGetValue (name, out mc)) {
				if (tc is NamespaceContainer && mc is NamespaceContainer) {
					AddTypeContainerMember (tc);
					return;
				}

				Report.SymbolRelatedToPreviousError (mc);
				if ((mc.ModFlags & Modifiers.PARTIAL) != 0 && (tc is ClassOrStruct || tc is Interface)) {
					Error_MissingPartialModifier (tc);
				} else {
					Report.Error (101, tc.Location, "The namespace `{0}' already contains a definition for `{1}'",
						GetSignatureForError (), mn.GetSignatureForError ());
				}
			} else {
				names_container.DefinedNames.Add (name, tc);

				var tdef = tc.PartialContainer;
				if (tdef != null) {
					//
					// Same name conflict in different namespace containers
					//
					var conflict = ns.GetAllTypes (name);
					if (conflict != null) {
						foreach (var e in conflict) {
							if (e.Arity == mn.Arity) {
								mc = (MemberCore) e.MemberDefinition;
								break;
							}
						}
					}

					if (mc != null) {
						Report.SymbolRelatedToPreviousError (mc);
						Report.Error (101, tc.Location, "The namespace `{0}' already contains a definition for `{1}'",
							GetSignatureForError (), mn.GetSignatureForError ());
					} else {
						ns.AddType (Module, tdef.Definition);
					}
				}
			}

			base.AddTypeContainer (tc);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			throw new NotSupportedException ();
		}

		public override void EmitContainer ()
		{
			VerifyClsCompliance ();

			base.EmitContainer ();
		}

		public ExtensionMethodCandidates LookupExtensionMethod (IMemberContext invocationContext, TypeSpec extensionType, string name, int arity, int position)
		{
			//
			// Here we try to resume the search for extension method at the point
			// where the last bunch of candidates was found. It's more tricky than
			// it seems as we have to check both namespace containers and namespace
			// in correct order.
			//
			// Consider:
			// 
			// namespace A {
			//	using N1;
			//  namespace B.C.D {
			//		<our first search found candidates in A.B.C.D
			//  }
			// }
			//
			// In the example above namespace A.B.C.D, A.B.C and A.B have to be
			// checked before we hit A.N1 using
			//
			ExtensionMethodCandidates candidates;
			var container = this;
			do {
				candidates = container.LookupExtensionMethodCandidates (invocationContext, name, arity, ref position);
				if (candidates != null || container.MemberName == null)
					return candidates;

				var container_ns = container.ns.Parent;
				var mn = container.MemberName.Left;
				int already_checked = position - 2;
				while (already_checked-- > 0) {
					mn = mn.Left;
					container_ns = container_ns.Parent;
				}

				while (mn != null) {
					++position;

					var methods = container_ns.LookupExtensionMethod (invocationContext, name, arity);
					if (methods != null) {
						return new ExtensionMethodCandidates (invocationContext, methods, container, position);
					}

					mn = mn.Left;
					container_ns = container_ns.Parent;
				}

				position = 0;
				container = container.Parent;
			} while (container != null);

			return null;
		}

		ExtensionMethodCandidates LookupExtensionMethodCandidates (IMemberContext invocationContext, string name, int arity, ref int position)
		{
			List<MethodSpec> candidates = null;

			if (position == 0) {
				++position;

				candidates = ns.LookupExtensionMethod (invocationContext, name, arity);
				if (candidates != null) {
					return new ExtensionMethodCandidates (invocationContext, candidates, this, position);
				}
			}

			if (position == 1) {
				++position;

				foreach (Namespace n in namespace_using_table) {
					var a = n.LookupExtensionMethod (invocationContext, name, arity);
					if (a == null)
						continue;

					if (candidates == null)
						candidates = a;
					else
						candidates.AddRange (a);
				}

				if (candidates != null)
					return new ExtensionMethodCandidates (invocationContext, candidates, this, position);
			}

			// LAMESPEC: TODO no spec about priority over normal extension methods yet
			if (types_using_table != null) {
				foreach (var t in types_using_table) {

					var res = t.MemberCache.FindExtensionMethods (invocationContext, name, arity);
					if (res == null)
						continue;

					if (candidates == null)
						candidates = res;
					else
						candidates.AddRange (res);
				}

				if (candidates != null)
					return new ExtensionMethodCandidates (invocationContext, candidates, this, position);
			}

			return null;
		}

		public override FullNamedExpression LookupNamespaceOrType (string name, int arity, LookupMode mode, Location loc)
		{
			//
			// Only simple names (no dots) will be looked up with this function
			//
			FullNamedExpression resolved;
			for (NamespaceContainer container = this; container != null; container = container.Parent) {
				resolved = container.Lookup (name, arity, mode, loc);
				if (resolved != null || container.MemberName == null)
					return resolved;

				var container_ns = container.ns.Parent;
				var mn = container.MemberName.Left;
				while (mn != null) {
					resolved = container_ns.LookupTypeOrNamespace (this, name, arity, mode, loc);
					if (resolved != null)
						return resolved;

					mn = mn.Left;
					container_ns = container_ns.Parent;
				}
			}

			return null;
		}

		public override void GetCompletionStartingWith (string prefix, List<string> results)
		{
			if (Usings == null)
				return;

			foreach (var un in Usings) {
				if (un.Alias != null)
					continue;

				var name = un.NamespaceExpression.Name;
				if (name.StartsWith (prefix))
					results.Add (name);
			}


			IEnumerable<string> all = Enumerable.Empty<string> ();

			foreach (Namespace using_ns in namespace_using_table) {
				if (prefix.StartsWith (using_ns.Name)) {
					int ld = prefix.LastIndexOf ('.');
					if (ld != -1) {
						string rest = prefix.Substring (ld + 1);

						all = all.Concat (using_ns.CompletionGetTypesStartingWith (rest));
					}
				}
				all = all.Concat (using_ns.CompletionGetTypesStartingWith (prefix));
			}

			results.AddRange (all);

			base.GetCompletionStartingWith (prefix, results);
		}

		
		//
		// Looks-up a alias named @name in this and surrounding namespace declarations
		//
		public FullNamedExpression LookupExternAlias (string name)
		{
			if (aliases == null)
				return null;

			UsingAliasNamespace uan;
			if (aliases.TryGetValue (name, out uan) && uan is UsingExternAlias)
				return uan.ResolvedExpression;

			return null;
		}
		
		//
		// Looks-up a alias named @name in this and surrounding namespace declarations
		//
		public override FullNamedExpression LookupNamespaceAlias (string name)
		{
			for (NamespaceContainer n = this; n != null; n = n.Parent) {
				if (n.aliases == null)
					continue;

				UsingAliasNamespace uan;
				if (n.aliases.TryGetValue (name, out uan))
					return uan.ResolvedExpression;
			}

			return null;
		}

		FullNamedExpression Lookup (string name, int arity, LookupMode mode, Location loc)
		{
			//
			// Check whether it's in the namespace.
			//
			FullNamedExpression fne = ns.LookupTypeOrNamespace (this, name, arity, mode, loc);

			//
			// Check aliases. 
			//
			if (aliases != null && arity == 0) {
				UsingAliasNamespace uan;
				if (aliases.TryGetValue (name, out uan)) {
					if (fne != null && mode != LookupMode.Probing) {
						// TODO: Namespace has broken location
						//Report.SymbolRelatedToPreviousError (fne.Location, null);
						Compiler.Report.SymbolRelatedToPreviousError (uan.Location, null);
						Compiler.Report.Error (576, loc,
							"Namespace `{0}' contains a definition with same name as alias `{1}'",
							GetSignatureForError (), name);
					}

					return uan.ResolvedExpression;
				}
			}

			if (fne != null)
				return fne;

			//
			// Lookup can be called before the namespace is defined from different namespace using alias clause
			//
			if (namespace_using_table == null) {
				DoDefineNamespace ();
			}

			//
			// Check using entries.
			//
			FullNamedExpression match = null;
			foreach (Namespace using_ns in namespace_using_table) {
				//
				// A using directive imports only types contained in the namespace, it
				// does not import any nested namespaces
				//
				var t = using_ns.LookupType (this, name, arity, mode, loc);
				if (t == null)
					continue;

				fne = new TypeExpression (t, loc);
				if (match == null) {
					match = fne;
					continue;
				}

				// Prefer types over namespaces
				var texpr_fne = fne as TypeExpr;
				var texpr_match = match as TypeExpr;
				if (texpr_fne != null && texpr_match == null) {
					match = fne;
					continue;
				} else if (texpr_fne == null) {
					continue;
				}

				// It can be top level accessibility only
				var better = Namespace.IsImportedTypeOverride (Module, texpr_match.Type, texpr_fne.Type);
				if (better == null) {
					if (mode == LookupMode.Normal) {
						Compiler.Report.SymbolRelatedToPreviousError (texpr_match.Type);
						Compiler.Report.SymbolRelatedToPreviousError (texpr_fne.Type);
						Compiler.Report.Error (104, loc, "`{0}' is an ambiguous reference between `{1}' and `{2}'",
							name, texpr_match.GetSignatureForError (), texpr_fne.GetSignatureForError ());
					}

					return match;
				}

				if (better == texpr_fne.Type)
					match = texpr_fne;
			}

			return match;
		}

		public static MethodGroupExpr LookupStaticUsings (IMemberContext mc, string name, int arity, Location loc)
		{
			for (var m = mc.CurrentMemberDefinition; m != null; m = m.Parent) {

				var nc = m as NamespaceContainer;
				if (nc == null)
					continue;

				List<MemberSpec> candidates = null;
				if (nc.types_using_table != null) {
					foreach (var using_type in nc.types_using_table) {
						var members = MemberCache.FindMembers (using_type, name, true);
						if (members != null) {
							foreach (var member in members) {
								if ((member.Modifiers & Modifiers.METHOD_EXTENSION) != 0)
									continue;

								if (arity > 0 && member.Arity != arity)
									continue;

								if (candidates == null)
									candidates = new List<MemberSpec> ();

								candidates.Add (member);
							}
						}
					}
				}

				if (candidates != null)
					return new MethodGroupExpr (candidates, null, loc);
			}

			return null;
		}

		protected override void DefineNamespace ()
		{
			if (namespace_using_table == null)
				DoDefineNamespace ();

			base.DefineNamespace ();
		}

		void DoDefineNamespace ()
		{
			namespace_using_table = empty_namespaces;

			if (clauses != null) {
				List<Namespace> namespaces = null;
				List<TypeSpec> types = null;

				bool post_process_using_aliases = false;

				for (int i = 0; i < clauses.Count; ++i) {
					var entry = clauses[i];

					if (entry.Alias != null) {
						if (aliases == null)
							aliases = new Dictionary<string, UsingAliasNamespace> ();

						//
						// Aliases are not available when resolving using section
						// except extern aliases
						//
						if (entry is UsingExternAlias) {
							entry.Define (this);
							if (entry.ResolvedExpression != null)
								aliases.Add (entry.Alias.Value, (UsingExternAlias) entry);

							clauses.RemoveAt (i--);
						} else {
							post_process_using_aliases = true;
						}

						continue;
					}

					entry.Define (this);

					//
					// It's needed for repl only, when using clause cannot be resolved don't hold it in
					// global list which is resolved for each evaluation
					//
					if (entry.ResolvedExpression == null) {
						clauses.RemoveAt (i--);
						continue;
					}

					var using_ns = entry.ResolvedExpression as NamespaceExpression;
					if (using_ns == null) {

						var type = ((TypeExpr)entry.ResolvedExpression).Type;

						if (types == null)
							types = new List<TypeSpec> ();

						if (types.Contains (type)) {
							Warning_DuplicateEntry (entry);
						} else {
							types.Add (type);
						}
					} else {
						if (namespaces == null)
							namespaces = new List<Namespace> ();

						if (namespaces.Contains (using_ns.Namespace)) {
							// Ensure we don't report the warning multiple times in repl
							clauses.RemoveAt (i--);

							Warning_DuplicateEntry (entry);
						} else {
							namespaces.Add (using_ns.Namespace);
						}
					}
				}

				namespace_using_table = namespaces == null ? new Namespace [0] : namespaces.ToArray ();
				if (types != null)
					types_using_table = types.ToArray ();

				if (post_process_using_aliases) {
					for (int i = 0; i < clauses.Count; ++i) {
						var entry = clauses[i];
						if (entry.Alias != null) {
							entry.Define (this);
							if (entry.ResolvedExpression != null) {
								aliases.Add (entry.Alias.Value, (UsingAliasNamespace) entry);
							}

							clauses.RemoveAt (i--);
						}
					}
				}
			}
		}

		public void EnableRedefinition ()
		{
			is_defined = false;
			namespace_using_table = null;
		}

		internal override void GenerateDocComment (DocumentationBuilder builder)
		{
			if (containers != null) {
				foreach (var tc in containers)
					tc.GenerateDocComment (builder);
			}
		}

		public override string GetSignatureForError ()
		{
			return MemberName == null ? "global::" : base.GetSignatureForError ();
		}

		public override void RemoveContainer (TypeContainer cont)
		{
			base.RemoveContainer (cont);
			NS.RemoveContainer (cont);
		}

		protected override bool VerifyClsCompliance ()
		{
			if (Module.IsClsComplianceRequired ()) {
				if (MemberName != null && MemberName.Name[0] == '_') {
					Warning_IdentifierNotCompliant ();
				}

				ns.VerifyClsCompliance ();
				return true;
			}

			return false;
		}

		void Warning_DuplicateEntry (UsingNamespace entry)
		{
			Compiler.Report.Warning (105, 3, entry.Location,
				"The using directive for `{0}' appeared previously in this namespace",
				entry.ResolvedExpression.GetSignatureForError ());
		}

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}
	}

	public class UsingNamespace
	{
		readonly ATypeNameExpression expr;
		readonly Location loc;
		protected FullNamedExpression resolved;

		public UsingNamespace (ATypeNameExpression expr, Location loc)
		{
			this.expr = expr;
			this.loc = loc;
		}

		#region Properties

		public virtual SimpleMemberName Alias {
			get {
				return null;
			}
		}

		public Location Location {
			get {
				return loc;
			}
		}

		public ATypeNameExpression NamespaceExpression	{
			get {
				return expr;
			}
		}

		public FullNamedExpression ResolvedExpression {
			get {
				return resolved;
			}
		}

		#endregion

		public string GetSignatureForError ()
		{
			return expr.GetSignatureForError ();
		}

		public virtual void Define (NamespaceContainer ctx)
		{
			resolved = expr.ResolveAsTypeOrNamespace (ctx, false);
			var ns = resolved as NamespaceExpression;
			if (ns != null)
				return;

			if (resolved != null) {
				var compiler = ctx.Module.Compiler;
				var type = resolved.Type;
				if (compiler.Settings.Version >= LanguageVersion.V_6) {
					if (!type.IsClass || !type.IsStatic) {
						compiler.Report.SymbolRelatedToPreviousError (type);
						compiler.Report.Error (7007, Location,
							"`{0}' is not a static class. A using namespace directive can only be applied to static classes or namespace",
							GetSignatureForError ());
					}

					return;
				}

				compiler.Report.SymbolRelatedToPreviousError (type);
				compiler.Report.Error (138, Location,
					"`{0}' is a type not a namespace. A using namespace directive can only be applied to namespaces",
					GetSignatureForError ());
			}
		}
		
		public virtual void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override string ToString()
		{
			return resolved.ToString();
		}
	}

	public class UsingExternAlias : UsingAliasNamespace
	{
		public UsingExternAlias (SimpleMemberName alias, Location loc)
			: base (alias, null, loc)
		{
		}

		public override void Define (NamespaceContainer ctx)
		{
			var ns = ctx.Module.GetRootNamespace (Alias.Value);
			if (ns == null) {
				ctx.Module.Compiler.Report.Error (430, Location,
					"The extern alias `{0}' was not specified in -reference option",
					Alias.Value);
				return;
			}

			resolved = new NamespaceExpression (ns, Location);
		}
		
		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}
	}

	public class UsingAliasNamespace : UsingNamespace
	{
		readonly SimpleMemberName alias;

		public struct AliasContext : IMemberContext
		{
			readonly NamespaceContainer ns;

			public AliasContext (NamespaceContainer ns)
			{
				this.ns = ns;
			}

			public TypeSpec CurrentType {
				get {
					return null;
				}
			}

			public TypeParameters CurrentTypeParameters {
				get {
					return null;
				}
			}

			public MemberCore CurrentMemberDefinition {
				get {
					return null;
				}
			}

			public bool IsObsolete {
				get {
					return false;
				}
			}

			public bool IsUnsafe {
				get {
					throw new NotImplementedException ();
				}
			}

			public bool IsStatic {
				get {
					throw new NotImplementedException ();
				}
			}

			public ModuleContainer Module {
				get {
					return ns.Module;
				}
			}

			public string GetSignatureForError ()
			{
				throw new NotImplementedException ();
			}

			public ExtensionMethodCandidates LookupExtensionMethod (TypeSpec extensionType, string name, int arity)
			{
				return null;
			}

			public FullNamedExpression LookupNamespaceOrType (string name, int arity, LookupMode mode, Location loc)
			{
				var fne = ns.NS.LookupTypeOrNamespace (ns, name, arity, mode, loc);
				if (fne != null)
					return fne;

				//
				// Only extern aliases are allowed in this context
				//
				fne = ns.LookupExternAlias (name);
				if (fne != null || ns.MemberName == null)
					return fne;

				var container_ns = ns.NS.Parent;
				var mn = ns.MemberName.Left;
				while (mn != null) {
					fne = container_ns.LookupTypeOrNamespace (this, name, arity, mode, loc);
					if (fne != null)
						return fne;

					mn = mn.Left;
					container_ns = container_ns.Parent;
				}

				if (ns.Parent != null)
					return ns.Parent.LookupNamespaceOrType (name, arity, mode, loc);

				return null;
			}

			public FullNamedExpression LookupNamespaceAlias (string name)
			{
				return ns.LookupNamespaceAlias (name);
			}
		}

		public UsingAliasNamespace (SimpleMemberName alias, ATypeNameExpression expr, Location loc)
			: base (expr, loc)
		{
			this.alias = alias;
		}

		public override SimpleMemberName Alias {
			get {
				return alias;
			}
		}

		public override void Define (NamespaceContainer ctx)
		{
			//
			// The namespace-or-type-name of a using-alias-directive is resolved as if
			// the immediately containing compilation unit or namespace body had no
			// using-directives. A using-alias-directive may however be affected
			// by extern-alias-directives in the immediately containing compilation
			// unit or namespace body
			//
			// We achieve that by introducing alias-context which redirect any local
			// namespace or type resolve calls to parent namespace
			//
			resolved = NamespaceExpression.ResolveAsTypeOrNamespace (new AliasContext (ctx), false);
		}
		
		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}
	}
}
