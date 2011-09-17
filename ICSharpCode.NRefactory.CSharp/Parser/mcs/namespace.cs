//
// namespace.cs: Tracks namespaces
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@seznam.cz)
//
// Copyright 2001 Ximian, Inc.
// Copyright 2003-2008 Novell, Inc.
//
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.CSharp {

	public class RootNamespace : Namespace {

		readonly string alias_name;
		readonly Dictionary<string, Namespace> all_namespaces;

		public RootNamespace (string alias_name)
			: base (null, String.Empty)
		{
			this.alias_name = alias_name;

			all_namespaces = new Dictionary<string, Namespace> ();
			all_namespaces.Add ("", this);
		}

		public string Alias {
			get {
				return alias_name;
			}
		}

		public void RegisterNamespace (Namespace child)
		{
			if (child != this)
				all_namespaces.Add (child.Name, child);
		}

		public bool IsNamespace (string name)
		{
			return all_namespaces.ContainsKey (name);
		}

		protected void RegisterNamespace (string dotted_name)
		{
			if (dotted_name != null && dotted_name.Length != 0 && ! IsNamespace (dotted_name))
				GetNamespace (dotted_name, true);
		}

		public override string GetSignatureForError ()
		{
			return alias_name + "::";
		}
	}

	public class GlobalRootNamespace : RootNamespace
	{
		public GlobalRootNamespace ()
			: base ("global")
		{
		}
	}

	//
	// Namespace cache for imported and compiled namespaces
	//
	// This is an Expression to allow it to be referenced in the
	// compiler parse/intermediate tree during name resolution.
	//
	public class Namespace : FullNamedExpression
	{
		Namespace parent;
		string fullname;
		protected Dictionary<string, Namespace> namespaces;
		protected Dictionary<string, IList<TypeSpec>> types;
		Dictionary<string, TypeExpr> cached_types;
		RootNamespace root;
		bool cls_checked;

		public readonly MemberName MemberName;

		/// <summary>
		///   Constructor Takes the current namespace and the
		///   name.  This is bootstrapped with parent == null
		///   and name = ""
		/// </summary>
		public Namespace (Namespace parent, string name)
		{
			// Expression members.
			this.eclass = ExprClass.Namespace;
			this.Type = InternalType.Namespace;
			this.loc = Location.Null;

			this.parent = parent;

			if (parent != null)
				this.root = parent.root;
			else
				this.root = this as RootNamespace;

			if (this.root == null)
				throw new InternalErrorException ("Root namespaces must be created using RootNamespace");
			
			string pname = parent != null ? parent.fullname : "";
				
			if (pname == "")
				fullname = name;
			else
				fullname = parent.fullname + "." + name;

			if (fullname == null)
				throw new InternalErrorException ("Namespace has a null fullname");

			if (parent != null && parent.MemberName != MemberName.Null)
				MemberName = new MemberName (parent.MemberName, name);
			else if (name.Length == 0)
				MemberName = MemberName.Null;
			else
				MemberName = new MemberName (name);

			namespaces = new Dictionary<string, Namespace> ();
			cached_types = new Dictionary<string, TypeExpr> ();

			root.RegisterNamespace (this);
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

		protected override Expression DoResolve (ResolveContext ec)
		{
			return this;
		}

		public void Error_NamespaceDoesNotExist (IMemberContext ctx, string name, int arity, Location loc)
		{
			var retval = LookupType (ctx, name, arity, LookupMode.IgnoreAccessibility, loc);
			if (retval != null) {
				ctx.Module.Compiler.Report.SymbolRelatedToPreviousError (retval.Type);
				ErrorIsInaccesible (ctx, retval.GetSignatureForError (), loc);
				return;
			}

			retval = LookupType (ctx, name, -System.Math.Max (1, arity), LookupMode.Probing, loc);
			if (retval != null) {
				Error_TypeArgumentsCannotBeUsed (ctx, retval.Type, arity, loc);
				return;
			}

			Namespace ns;
			if (arity > 0 && namespaces.TryGetValue (name, out ns)) {
				ns.Error_TypeArgumentsCannotBeUsed (ctx, null, arity, loc);
				return;
			}

			if (this is GlobalRootNamespace) {
				ctx.Module.Compiler.Report.Error (400, loc,
					"The type or namespace name `{0}' could not be found in the global namespace (are you missing an assembly reference?)",
					name);
			} else {
				ctx.Module.Compiler.Report.Error (234, loc,
					"The type or namespace name `{0}' does not exist in the namespace `{1}'. Are you missing an assembly reference?",
					name, GetSignatureForError ());
			}
		}

		public override string GetSignatureForError ()
		{
			return fullname;
		}
		
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

		public TypeExpr LookupType (IMemberContext ctx, string name, int arity, LookupMode mode, Location loc)
		{
			if (types == null)
				return null;

			TypeExpr te;
			if (arity == 0 && cached_types.TryGetValue (name, out te))
				return te;

			IList<TypeSpec> found;
			if (!types.TryGetValue (name, out found))
				return null;

			TypeSpec best = null;
			foreach (var ts in found) {
				if (ts.Arity == arity) {
					if (best == null) {
						if ((ts.Modifiers & Modifiers.INTERNAL) != 0 && !ts.MemberDefinition.IsInternalAsPublic (ctx.Module.DeclaringAssembly) && mode != LookupMode.IgnoreAccessibility)
							continue;

						best = ts;
						continue;
					}

					if (best.MemberDefinition.IsImported && ts.MemberDefinition.IsImported) {
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

					if (ts.MemberDefinition.IsImported)
						ctx.Module.Compiler.Report.SymbolRelatedToPreviousError (ts);

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

			if (best == null)
				return null;

			te = new TypeExpression (best, Location.Null);

			// TODO MemberCache: Cache more
			if (arity == 0 && mode == LookupMode.Normal)
				cached_types.Add (name, te);

			return te;
		}

		TypeSpec LookupType (string name, int arity)
		{
			if (types == null)
				return null;

			IList<TypeSpec> found;
			if (types.TryGetValue (name, out found)) {
				TypeSpec best = null;

				foreach (var ts in found) {
					if (ts.Arity == arity)
						return ts;

					//
					// Lookup for the best candidate with closest arity match
					//
					if (arity < 0) {
						if (best == null) {
							best = ts;
						} else if (System.Math.Abs (ts.Arity + arity) < System.Math.Abs (best.Arity + arity)) {
							best = ts;
						}
					}
				}
				
				return best;
			}

			return null;
		}

		public FullNamedExpression LookupTypeOrNamespace (IMemberContext ctx, string name, int arity, LookupMode mode, Location loc)
		{
			var texpr = LookupType (ctx, name, arity, mode, loc);

			Namespace ns;
			if (arity == 0 && namespaces.TryGetValue (name, out ns)) {
				if (texpr == null)
					return ns;

				if (mode != LookupMode.Probing) {
					ctx.Module.Compiler.Report.SymbolRelatedToPreviousError (texpr.Type);
					// ctx.Module.Compiler.Report.SymbolRelatedToPreviousError (ns.loc, "");
					ctx.Module.Compiler.Report.Warning (437, 2, loc,
						"The type `{0}' conflicts with the imported namespace `{1}'. Using the definition found in the source file",
						texpr.GetSignatureForError (), ns.GetSignatureForError ());
				}

				if (texpr.Type.MemberDefinition.IsImported)
					return ns;
			}

			return texpr;
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
		public List<MethodSpec> LookupExtensionMethod (IMemberContext invocationContext, TypeSpec extensionType, string name, int arity)
		{
			if (types == null)
				return null;

			List<MethodSpec> found = null;

			// TODO: Add per namespace flag when at least 1 type has extension

			foreach (var tgroup in types.Values) {
				foreach (var ts in tgroup) {
					if ((ts.Modifiers & Modifiers.METHOD_EXTENSION) == 0)
						continue;

					var res = ts.MemberCache.FindExtensionMethods (invocationContext, extensionType, name, arity);
					if (res == null)
						continue;

					if (found == null) {
						found = res;
					} else {
						found.AddRange (res);
					}
				}
			}

			return found;
		}

		//
		// Extension methods look up for dotted namespace names
		//
		public IList<MethodSpec> LookupExtensionMethod (IMemberContext invocationContext, TypeSpec extensionType, string name, int arity, out Namespace scope)
		{
			//
			// Inspect parent namespaces in namespace expression
			//
			scope = this;
			do {
				var candidates = scope.LookupExtensionMethod (invocationContext, extensionType, name, arity);
				if (candidates != null)
					return candidates;

				scope = scope.Parent;
			} while (scope != null);

			return null;
		}

		public void AddType (ModuleContainer module, TypeSpec ts)
		{
			if (types == null) {
				types = new Dictionary<string, IList<TypeSpec>> (64);
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

		public void RemoveDeclSpace (string name)
		{
			types.Remove (name);
			cached_types.Remove (name);
		}

		public override FullNamedExpression ResolveAsTypeOrNamespace (IMemberContext mc)
		{
			return this;
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

	//
	// Namespace block as created by the parser
	//
	public class NamespaceContainer : IMemberContext, ITypesContainer
	{
		Namespace ns;

		readonly ModuleContainer module;
		readonly NamespaceContainer parent;
		readonly CompilationSourceFile file;
		readonly Location loc;

		NamespaceContainer implicit_parent;
		int symfile_id;

		// Namespace using import block
		List<NamespaceUsingAlias> using_aliases;
		List<NamespaceUsing> using_clauses;
		public bool DeclarationFound;
		// End

		bool resolved;

		public readonly bool IsImplicit;
		public readonly TypeContainer SlaveDeclSpace;
		static readonly Namespace [] empty_namespaces = new Namespace [0];
		static readonly string[] empty_using_list = new string[0];

		Namespace [] namespace_using_table;

		public NamespaceContainer (MemberName name, ModuleContainer module, NamespaceContainer parent, CompilationSourceFile sourceFile)
		{
			this.module = module;
			this.parent = parent;
			this.file = sourceFile;
			this.loc = name == null ? Location.Null : name.Location;

			if (parent != null)
				ns = parent.NS.GetNamespace (name.GetName (), true);
			else if (name != null)
				ns = module.GlobalRootNamespace.GetNamespace (name.GetName (), true);
			else
				ns = module.GlobalRootNamespace;

			SlaveDeclSpace = new RootDeclSpace (module, this);
		}

		private NamespaceContainer (ModuleContainer module, NamespaceContainer parent, CompilationSourceFile file, Namespace ns, bool slave)
		{
			this.module = module;
			this.parent = parent;
			this.file = file;
			this.IsImplicit = true;
			this.ns = ns;
			this.SlaveDeclSpace = slave ? new RootDeclSpace (module, this) : null;
		}

		#region Properties

		public Location Location {
			get {
				return loc;
			}
		}

		public MemberName MemberName {
			get {
				return ns.MemberName;
			}
		}

		public CompilationSourceFile SourceFile {
			get {
				return file;
			}
		}

		public List<NamespaceUsing> Usings {
			get {
				return using_clauses;
			}
		}

		#endregion

		//
		// Extracts the using alises and using clauses into a couple of
		// arrays that might already have the same information;  Used by the
		// C# Eval mode.
		//
		public void Extract (List<NamespaceUsingAlias> out_using_aliases, List<NamespaceUsing> out_using_clauses)
		{
			if (using_aliases != null){
				foreach (NamespaceUsingAlias uae in using_aliases){
					bool replaced = false;
					
					for (int i = 0; i < out_using_aliases.Count; i++){
						NamespaceUsingAlias out_uea = (NamespaceUsingAlias) out_using_aliases [i];
						
						if (out_uea.Alias == uae.Alias){
							out_using_aliases [i] = uae;
							replaced = true;
							break;
						}
					}
					if (!replaced)
						out_using_aliases.Add (uae);
				}
			}

			if (using_clauses != null){
				foreach (NamespaceUsing ue in using_clauses){
					bool found = false;
					
					foreach (NamespaceUsing out_ue in out_using_clauses)
						if (out_ue.Name == ue.Name){
							found = true;
							break;
						}
					if (!found)
						out_using_clauses.Add (ue);
				}
			}
		}
		
		//
		// According to section 16.3.1 (using-alias-directive), the namespace-or-type-name is
		// resolved as if the immediately containing namespace body has no using-directives.
		//
		// Section 16.3.2 says that the same rule is applied when resolving the namespace-name
		// in the using-namespace-directive.
		//
		// To implement these rules, the expressions in the using directives are resolved using 
		// the "doppelganger" (ghostly bodiless duplicate).
		//
		NamespaceContainer doppelganger;
		NamespaceContainer Doppelganger {
			get {
				if (!IsImplicit && doppelganger == null) {
					doppelganger = new NamespaceContainer (module, ImplicitParent, file, ns, true);
					doppelganger.using_aliases = using_aliases;
				}
				return doppelganger;
			}
		}

		public Namespace NS {
			get { return ns; }
		}

		public NamespaceContainer Parent {
			get { return parent; }
		}

		public NamespaceContainer ImplicitParent {
			get {
				if (parent == null)
					return null;
				if (implicit_parent == null) {
					implicit_parent = (parent.NS == ns.Parent)
						? parent
						: new NamespaceContainer (module, parent, file, ns.Parent, false);
				}
				return implicit_parent;
			}
		}

		/// <summary>
		///   Records a new namespace for resolving name references
		/// </summary>
		public void AddUsing (MemberName name, Location loc)
		{
			if (DeclarationFound){
				Compiler.Report.Error (1529, loc, "A using clause must precede all other namespace elements except extern alias declarations");
			}

			if (using_clauses == null) {
				using_clauses = new List<NamespaceUsing> ();
			} else {
				foreach (NamespaceUsing old_entry in using_clauses) {
					if (name.Equals (old_entry.MemberName)) {
						Compiler.Report.SymbolRelatedToPreviousError (old_entry.Location, old_entry.GetSignatureForError ());
						Compiler.Report.Warning (105, 3, loc, "The using directive for `{0}' appeared previously in this namespace", name.GetSignatureForError ());
						return;
					}
				}
			}

			using_clauses.Add (new NamespaceUsing (name));
		}

		public void AddUsingAlias (string alias, MemberName name, Location loc)
		{
			if (DeclarationFound){
				Compiler.Report.Error (1529, loc, "A using clause must precede all other namespace elements except extern alias declarations");
			}

			AddUsingAlias (new LocalUsingAliasEntry (alias, name, loc));
		}

		public void AddUsingExternalAlias (string alias, Location loc, Report Report)
		{
			bool not_first = using_clauses != null || DeclarationFound;
			if (using_aliases != null && !not_first) {
				foreach (NamespaceUsingAlias uae in using_aliases) {
					if (uae is LocalUsingAliasEntry) {
						not_first = true;
						break;
					}
				}
			}

			if (not_first)
				Report.Error (439, loc, "An extern alias declaration must precede all other elements");

			if (alias == "global") {
				Error_GlobalNamespaceRedefined (loc, Report);
				return;
			}

			AddUsingAlias (new NamespaceUsingAlias (alias, loc));
		}

		void AddUsingAlias (NamespaceUsingAlias uae)
		{
			if (using_aliases == null) {
				using_aliases = new List<NamespaceUsingAlias> ();
			} else {
				foreach (NamespaceUsingAlias entry in using_aliases) {
					if (uae.Alias == entry.Alias) {
						Compiler.Report.SymbolRelatedToPreviousError (uae.Location, uae.Alias);
						Compiler.Report.Error (1537, entry.Location, "The using alias `{0}' appeared previously in this namespace",
							entry.Alias);
						return;
					}
				}
			}

			using_aliases.Add (uae);
		}

		//
		// Does extension methods look up to find a method which matches name and extensionType.
		// Search starts from this namespace and continues hierarchically up to top level.
		//
		public ExtensionMethodCandidates LookupExtensionMethod (TypeSpec extensionType, string name, int arity)
		{
			List<MethodSpec> candidates = null;
			foreach (Namespace n in GetUsingTable ()) {
				var a = n.LookupExtensionMethod (this, extensionType, name, arity);
				if (a == null)
					continue;

				if (candidates == null)
					candidates = a;
				else
					candidates.AddRange (a);
			}

			if (candidates != null)
				return new ExtensionMethodCandidates (candidates, this);

			if (parent == null)
				return null;

			Namespace ns_scope;
			var ns_candidates = ns.Parent.LookupExtensionMethod (this, extensionType, name, arity, out ns_scope);
			if (ns_candidates != null)
				return new ExtensionMethodCandidates (ns_candidates, this, ns_scope);

			//
			// Continue in parent container
			//
			return parent.LookupExtensionMethod (extensionType, name, arity);
		}

		public FullNamedExpression LookupNamespaceOrType (string name, int arity, LookupMode mode, Location loc)
		{
			// Precondition: Only simple names (no dots) will be looked up with this function.
			FullNamedExpression resolved = null;
			for (NamespaceContainer curr_ns = this; curr_ns != null; curr_ns = curr_ns.ImplicitParent) {
				if ((resolved = curr_ns.Lookup (name, arity, mode, loc)) != null)
					break;
			}

			return resolved;
		}

		public IList<string> CompletionGetTypesStartingWith (string prefix)
		{
			IEnumerable<string> all = Enumerable.Empty<string> ();
			
			for (NamespaceContainer curr_ns = this; curr_ns != null; curr_ns = curr_ns.ImplicitParent){
				foreach (Namespace using_ns in GetUsingTable ()){
					if (prefix.StartsWith (using_ns.Name)){
						int ld = prefix.LastIndexOf ('.');
						if (ld != -1){
							string rest = prefix.Substring (ld+1);

							all = all.Concat (using_ns.CompletionGetTypesStartingWith (rest));
						}
					}
					all = all.Concat (using_ns.CompletionGetTypesStartingWith (prefix));
				}
			}

			return all.Distinct ().ToList ();
		}
		
		// Looks-up a alias named @name in this and surrounding namespace declarations
		public FullNamedExpression LookupNamespaceAlias (string name)
		{
			for (NamespaceContainer n = this; n != null; n = n.ImplicitParent) {
				if (n.using_aliases == null)
					continue;

				foreach (NamespaceUsingAlias ue in n.using_aliases) {
					if (ue.Alias == name)
						return ue.Resolve (Doppelganger ?? this, Doppelganger == null);
				}
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
			if (using_aliases != null && arity == 0) {
				foreach (NamespaceUsingAlias ue in using_aliases) {
					if (ue.Alias == name) {
						if (fne != null) {
							if (Doppelganger != null) {
								if (mode == LookupMode.Normal) {
									// TODO: Namespace has broken location
									//Report.SymbolRelatedToPreviousError (fne.Location, null);
									Compiler.Report.SymbolRelatedToPreviousError (ue.Location, null);
									Compiler.Report.Error (576, loc,
										"Namespace `{0}' contains a definition with same name as alias `{1}'",
										GetSignatureForError (), name);
								}
							} else {
								return fne;
							}
						}

						return ue.Resolve (Doppelganger ?? this, Doppelganger == null);
					}
				}
			}

			if (fne != null)
				return fne;

			if (IsImplicit)
				return null;

			//
			// Check using entries.
			//
			FullNamedExpression match = null;
			foreach (Namespace using_ns in GetUsingTable ()) {
				// A using directive imports only types contained in the namespace, it
				// does not import any nested namespaces
				fne = using_ns.LookupType (this, name, arity, mode, loc);
				if (fne == null)
					continue;

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
				var better = Namespace.IsImportedTypeOverride (module, texpr_match.Type, texpr_fne.Type);
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

		Namespace [] GetUsingTable ()
		{
			if (namespace_using_table != null)
				return namespace_using_table;

			if (using_clauses == null) {
				namespace_using_table = empty_namespaces;
				return namespace_using_table;
			}

			var list = new List<Namespace> (using_clauses.Count);

			foreach (NamespaceUsing ue in using_clauses) {
				Namespace using_ns = ue.Resolve (Doppelganger);
				if (using_ns == null)
					continue;

				list.Add (using_ns);
			}

			namespace_using_table = list.ToArray ();
			return namespace_using_table;
		}

		public int SymbolFileID {
			get {
				if (symfile_id == 0 && file.SourceFileEntry != null) {
					int parent_id = parent == null ? 0 : parent.SymbolFileID;

					string [] using_list = empty_using_list;
					if (using_clauses != null) {
						using_list = new string [using_clauses.Count];
						for (int i = 0; i < using_clauses.Count; i++)
							using_list [i] = ((NamespaceUsing) using_clauses [i]).MemberName.GetName ();
					}

					symfile_id = SymbolWriter.DefineNamespace (ns.Name, file.CompileUnitEntry, using_list, parent_id);
				}
				return symfile_id;
			}
		}

		static void MsgtryRef (string s)
		{
			Console.WriteLine ("    Try using -r:" + s);
		}

		static void MsgtryPkg (string s)
		{
			Console.WriteLine ("    Try using -pkg:" + s);
		}

		public static void Error_GlobalNamespaceRedefined (Location loc, Report Report)
		{
			Report.Error (1681, loc, "The global extern alias cannot be redefined");
		}

		public static void Error_NamespaceNotFound (Location loc, string name, Report Report)
		{
			Report.Error (246, loc, "The type or namespace name `{0}' could not be found. Are you missing a using directive or an assembly reference?",
				name);

			switch (name) {
			case "Gtk": case "GtkSharp":
				MsgtryPkg ("gtk-sharp-2.0");
				break;

			case "Gdk": case "GdkSharp":
				MsgtryPkg ("gdk-sharp-2.0");
				break;

			case "Glade": case "GladeSharp":
				MsgtryPkg ("glade-sharp-2.0");
				break;

			case "System.Drawing":
			case "System.Web.Services":
			case "System.Web":
			case "System.Data":
			case "System.Windows.Forms":
				MsgtryRef (name);
				break;
			}
		}

		/// <summary>
		///   Used to validate that all the using clauses are correct
		///   after we are finished parsing all the files.  
		/// </summary>
		public void Resolve ()
		{
			if (resolved)
				return;

			resolved = true;

			if (using_aliases != null) {
				foreach (NamespaceUsingAlias ue in using_aliases)
					ue.Resolve (Doppelganger, Doppelganger == null);
			}

			if (using_clauses != null) {
				foreach (NamespaceUsing ue in using_clauses)
					ue.Resolve (Doppelganger);
			}

			if (parent != null)
				parent.Resolve ();
		}

		public string GetSignatureForError ()
		{
			return ns.GetSignatureForError ();
		}

		#region IMemberContext Members

		CompilerContext Compiler {
			get { return module.Compiler; }
		}

		public TypeSpec CurrentType {
			get { return SlaveDeclSpace.CurrentType; }
		}

		public MemberCore CurrentMemberDefinition {
			get { return SlaveDeclSpace.CurrentMemberDefinition; }
		}

		public TypeParameter[] CurrentTypeParameters {
			get { return SlaveDeclSpace.CurrentTypeParameters; }
		}

		public bool IsObsolete {
			get { return SlaveDeclSpace.IsObsolete; }
		}

		public bool IsUnsafe {
			get { return SlaveDeclSpace.IsUnsafe; }
		}

		public bool IsStatic {
			get { return SlaveDeclSpace.IsStatic; }
		}

		public ModuleContainer Module {
			get { return module; }
		}

		#endregion
	}

	public class NamespaceUsing
	{
		readonly MemberName name;
		Namespace resolved;

		public NamespaceUsing (MemberName name)
		{
			this.name = name;
		}

		public string GetSignatureForError ()
		{
			return name.GetSignatureForError ();
		}

		public Location Location
		{
			get { return name.Location; }
		}

		public MemberName MemberName
		{
			get { return name; }
		}

		public string Name
		{
			get { return GetSignatureForError (); }
		}

		public Namespace Resolve (IMemberContext rc)
		{
			if (resolved != null)
				return resolved;

			FullNamedExpression fne = name.GetTypeExpression ().ResolveAsTypeOrNamespace (rc);
			if (fne == null)
				return null;

			resolved = fne as Namespace;
			if (resolved == null) {
				rc.Module.Compiler.Report.SymbolRelatedToPreviousError (fne.Type);
				rc.Module.Compiler.Report.Error (138, Location,
					"`{0}' is a type not a namespace. A using namespace directive can only be applied to namespaces",
					GetSignatureForError ());
			}
			return resolved;
		}
	}

	public class NamespaceUsingAlias
	{
		public readonly string Alias;
		public Location Location;

		public NamespaceUsingAlias (string alias, Location loc)
		{
			this.Alias = alias;
			this.Location = loc;
		}

		public virtual FullNamedExpression Resolve (IMemberContext rc, bool local)
		{
			FullNamedExpression fne = rc.Module.GetRootNamespace (Alias);
			if (fne == null) {
				rc.Module.Compiler.Report.Error (430, Location,
					"The extern alias `{0}' was not specified in -reference option",
					Alias);
			}

			return fne;
		}
	}

	class LocalUsingAliasEntry : NamespaceUsingAlias
	{
		FullNamedExpression resolved;
		MemberName value;

		public LocalUsingAliasEntry (string alias, MemberName name, Location loc)
			: base (alias, loc)
		{
			this.value = name;
		}

		public override FullNamedExpression Resolve (IMemberContext rc, bool local)
		{
			if (resolved != null || value == null)
				return resolved;

			if (local)
				return null;

			resolved = value.GetTypeExpression ().ResolveAsTypeOrNamespace (rc);
			if (resolved == null) {
				value = null;
				return null;
			}

			return resolved;
		}
	}
}
