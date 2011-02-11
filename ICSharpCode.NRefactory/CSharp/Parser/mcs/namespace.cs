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

		public override void Error_NamespaceDoesNotExist (Location loc, string name, int arity, IMemberContext ctx)
		{
			ctx.Compiler.Report.Error (400, loc,
				"The type or namespace name `{0}' could not be found in the global namespace (are you missing an assembly reference?)",
				name);
		}
	}

	/// <summary>
	///   Keeps track of the namespaces defined in the C# code.
	///
	///   This is an Expression to allow it to be referenced in the
	///   compiler parse/intermediate tree during name resolution.
	/// </summary>
	public class Namespace : FullNamedExpression {
		
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
			this.Type = InternalType.FakeInternalType;
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

		public virtual void Error_NamespaceDoesNotExist (Location loc, string name, int arity, IMemberContext ctx)
		{
			FullNamedExpression retval = Lookup (ctx.Compiler, name, -System.Math.Max (1, arity), loc);
			if (retval != null) {
				Error_TypeArgumentsCannotBeUsed (ctx.Compiler.Report, loc, retval.Type, arity);
				return;
			}

			Namespace ns;
			if (arity > 0 && namespaces.TryGetValue (name, out ns)) {
				ns.Error_TypeArgumentsCannotBeUsed (ctx.Compiler.Report, loc, null, arity);
				return;
			}

			ctx.Compiler.Report.Error (234, loc,
				"The type or namespace name `{0}' does not exist in the namespace `{1}'. Are you missing an assembly reference?",
				name, GetSignatureForError ());
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

		public TypeExpr LookupType (CompilerContext ctx, string name, int arity, bool silent, Location loc)
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
						best = ts;
						continue;
					}

					var pts = best as BuildinTypeSpec;
					if (pts == null)
						pts = ts as BuildinTypeSpec;

					if (pts != null) {
						ctx.Report.SymbolRelatedToPreviousError (best);
						ctx.Report.SymbolRelatedToPreviousError (ts);

						// TODO: This should use different warning number but we want to be csc compatible
						ctx.Report.Warning (1685, 1, loc,
							"The predefined type `{0}.{1}' is redefined in the source code. Ignoring the local type definition",
							pts.Namespace, pts.Name);
						best = pts;
						continue;
					}

					if (best.MemberDefinition.IsImported && ts.MemberDefinition.IsImported) {
						ctx.Report.SymbolRelatedToPreviousError (best);
						ctx.Report.SymbolRelatedToPreviousError (ts);
						if (silent) {
							ctx.Report.Warning (1685, 1, loc,
								"The predefined type `{0}' is defined in multiple assemblies. Using definition from `{1}'",
								ts.GetSignatureForError (), best.MemberDefinition.DeclaringAssembly.Name);
						} else {
							ctx.Report.Error (433, loc, "The imported type `{0}' is defined multiple times", ts.GetSignatureForError ());
						}

						break;
					}

					if (best.MemberDefinition.IsImported)
						best = ts;

					if ((best.Modifiers & Modifiers.INTERNAL) != 0 && !best.MemberDefinition.IsInternalAsPublic (RootContext.ToplevelTypes.DeclaringAssembly))
						continue;

					if (silent)
						continue;

					if (ts.MemberDefinition.IsImported)
						ctx.Report.SymbolRelatedToPreviousError (ts);

					ctx.Report.Warning (436, 2, loc,
						"The type `{0}' conflicts with the imported type of same name'. Ignoring the imported type definition",
						best.GetSignatureForError ());
				}

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

			if (best == null)
				return null;

			if ((best.Modifiers & Modifiers.INTERNAL) != 0 && !best.MemberDefinition.IsInternalAsPublic (RootContext.ToplevelTypes.DeclaringAssembly))
				return null;

			te = new TypeExpression (best, Location.Null);

			// TODO MemberCache: Cache more
			if (arity == 0 && !silent)
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

		public FullNamedExpression Lookup (CompilerContext ctx, string name, int arity, Location loc)
		{
			if (arity == 0 && namespaces.ContainsKey (name))
				return namespaces [name];

			return LookupType (ctx, name, arity, false, loc);
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

		/// 
		/// Looks for extension method in this namespace
		/// 
		public List<MethodSpec> LookupExtensionMethod (TypeSpec extensionType, TypeContainer invocationContext, string name, int arity)
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

		public void AddType (TypeSpec ts)
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
						better_type = IsImportedTypeOverride (ts, found);
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

						better_type = IsImportedTypeOverride (ts, found);
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
		public static TypeSpec IsImportedTypeOverride (TypeSpec ts, TypeSpec found)
		{
			var ts_accessible = (ts.Modifiers & Modifiers.PUBLIC) != 0 || ts.MemberDefinition.IsInternalAsPublic (RootContext.ToplevelTypes.DeclaringAssembly);
			var found_accessible = (found.Modifiers & Modifiers.PUBLIC) != 0 || found.MemberDefinition.IsInternalAsPublic (RootContext.ToplevelTypes.DeclaringAssembly);

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
		}

		public void ReplaceTypeWithPredefined (TypeSpec ts, BuildinTypeSpec pts)
		{
			var found = types [ts.Name];
			cached_types.Remove (ts.Name);
			if (found.Count == 1) {
				types[ts.Name][0] = pts;
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
	// Namespace container as created by the parser
	//
	public class NamespaceEntry : IMemberContext {

		public class UsingEntry {
			readonly MemberName name;
			Namespace resolved;
			
			public UsingEntry (MemberName name)
			{
				this.name = name;
			}

			public string GetSignatureForError ()
			{
				return name.GetSignatureForError ();
			}

			public Location Location {
				get { return name.Location; }
			}

			public MemberName MemberName {
				get { return name; }
			}
			
			public string Name {
				get { return GetSignatureForError (); }
			}

			public Namespace Resolve (IMemberContext rc)
			{
				if (resolved != null)
					return resolved;

				FullNamedExpression fne = name.GetTypeExpression ().ResolveAsTypeStep (rc, false);
				if (fne == null)
					return null;

				resolved = fne as Namespace;
				if (resolved == null) {
					rc.Compiler.Report.SymbolRelatedToPreviousError (fne.Type);
					rc.Compiler.Report.Error (138, Location,
						"`{0}' is a type not a namespace. A using namespace directive can only be applied to namespaces",
						GetSignatureForError ());
				}
				return resolved;
			}

			public override string ToString ()
			{
				return Name;
			}
		}

		public class UsingAliasEntry {
			public readonly string Alias;
			public Location Location;

			public UsingAliasEntry (string alias, Location loc)
			{
				this.Alias = alias;
				this.Location = loc;
			}

			public virtual FullNamedExpression Resolve (IMemberContext rc, bool local)
			{
				FullNamedExpression fne = rc.Module.GetRootNamespace (Alias);
				if (fne == null) {
					rc.Compiler.Report.Error (430, Location,
						"The extern alias `{0}' was not specified in -reference option",
						Alias);
				}

				return fne;
			}

			public override string ToString ()
			{
				return Alias;
			}
			
		}

		class LocalUsingAliasEntry : UsingAliasEntry {
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

				resolved = value.GetTypeExpression ().ResolveAsTypeStep (rc, false);
				if (resolved == null) {
					value = null;
					return null;
				}

				if (resolved is TypeExpr)
					resolved = resolved.ResolveAsTypeTerminal (rc, false);

				return resolved;
			}

			public override string ToString ()
			{
				return String.Format ("{0} = {1}", Alias, value.GetSignatureForError ());
			}
		}

		Namespace ns;
		NamespaceEntry parent, implicit_parent;
		CompilationUnit file;
		int symfile_id;

		// Namespace using import block
		List<UsingAliasEntry> using_aliases;
		List<UsingEntry> using_clauses;
		public bool DeclarationFound;
		// End

		public readonly bool IsImplicit;
		public readonly DeclSpace SlaveDeclSpace;
		static readonly Namespace [] empty_namespaces = new Namespace [0];
		Namespace [] namespace_using_table;
		ModuleContainer ctx;

		static List<NamespaceEntry> entries = new List<NamespaceEntry> ();

		public static void Reset ()
		{
			entries = new List<NamespaceEntry> ();
		}

		public NamespaceEntry (ModuleContainer ctx, NamespaceEntry parent, CompilationUnit file, string name)
		{
			this.ctx = ctx;
			this.parent = parent;
			this.file = file;
			entries.Add (this);

			if (parent != null)
				ns = parent.NS.GetNamespace (name, true);
			else if (name != null)
				ns = ctx.GlobalRootNamespace.GetNamespace (name, true);
			else
				ns = ctx.GlobalRootNamespace;

			SlaveDeclSpace = new RootDeclSpace (this);
		}

		private NamespaceEntry (ModuleContainer ctx, NamespaceEntry parent, CompilationUnit file, Namespace ns, bool slave)
		{
			this.ctx = ctx;
			this.parent = parent;
			this.file = file;
			this.IsImplicit = true;
			this.ns = ns;
			this.SlaveDeclSpace = slave ? new RootDeclSpace (this) : null;
		}

		//
		// Populates the Namespace with some using declarations, used by the
		// eval mode. 
		//
		public void Populate (List<UsingAliasEntry> source_using_aliases, List<UsingEntry> source_using_clauses)
		{
			foreach (UsingAliasEntry uae in source_using_aliases){
				if (using_aliases == null)
					using_aliases = new List<UsingAliasEntry> ();
				
				using_aliases.Add (uae);
			}

			foreach (UsingEntry ue in source_using_clauses){
				if (using_clauses == null)
					using_clauses = new List<UsingEntry> ();
				
				using_clauses.Add (ue);
			}
		}

		//
		// Extracts the using alises and using clauses into a couple of
		// arrays that might already have the same information;  Used by the
		// C# Eval mode.
		//
		public void Extract (List<UsingAliasEntry> out_using_aliases, List<UsingEntry> out_using_clauses)
		{
			if (using_aliases != null){
				foreach (UsingAliasEntry uae in using_aliases){
					bool replaced = false;
					
					for (int i = 0; i < out_using_aliases.Count; i++){
						UsingAliasEntry out_uea = (UsingAliasEntry) out_using_aliases [i];
						
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
				foreach (UsingEntry ue in using_clauses){
					bool found = false;
					
					foreach (UsingEntry out_ue in out_using_clauses)
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
		NamespaceEntry doppelganger;
		NamespaceEntry Doppelganger {
			get {
				if (!IsImplicit && doppelganger == null) {
					doppelganger = new NamespaceEntry (ctx, ImplicitParent, file, ns, true);
					doppelganger.using_aliases = using_aliases;
				}
				return doppelganger;
			}
		}

		public Namespace NS {
			get { return ns; }
		}

		public NamespaceEntry Parent {
			get { return parent; }
		}

		public NamespaceEntry ImplicitParent {
			get {
				if (parent == null)
					return null;
				if (implicit_parent == null) {
					implicit_parent = (parent.NS == ns.Parent)
						? parent
						: new NamespaceEntry (ctx, parent, file, ns.Parent, false);
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
				using_clauses = new List<UsingEntry> ();
			} else {
				foreach (UsingEntry old_entry in using_clauses) {
					if (name.Equals (old_entry.MemberName)) {
						Compiler.Report.SymbolRelatedToPreviousError (old_entry.Location, old_entry.GetSignatureForError ());
						Compiler.Report.Warning (105, 3, loc, "The using directive for `{0}' appeared previously in this namespace", name.GetSignatureForError ());
						return;
					}
				}
			}

			using_clauses.Add (new UsingEntry (name));
		}

		public void AddUsingAlias (string alias, MemberName name, Location loc)
		{
			// TODO: This is parser bussines
			if (DeclarationFound){
				Compiler.Report.Error (1529, loc, "A using clause must precede all other namespace elements except extern alias declarations");
			}

			if (RootContext.Version != LanguageVersion.ISO_1 && alias == "global")
				Compiler.Report.Warning (440, 2, loc, "An alias named `global' will not be used when resolving 'global::';" +
					" the global namespace will be used instead");

			AddUsingAlias (new LocalUsingAliasEntry (alias, name, loc));
		}

		public void AddUsingExternalAlias (string alias, Location loc, Report Report)
		{
			// TODO: Do this in parser
			bool not_first = using_clauses != null || DeclarationFound;
			if (using_aliases != null && !not_first) {
				foreach (UsingAliasEntry uae in using_aliases) {
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

			AddUsingAlias (new UsingAliasEntry (alias, loc));
		}

		void AddUsingAlias (UsingAliasEntry uae)
		{
			if (using_aliases == null) {
				using_aliases = new List<UsingAliasEntry> ();
			} else {
				foreach (UsingAliasEntry entry in using_aliases) {
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

		///
		/// Does extension methods look up to find a method which matches name and extensionType.
		/// Search starts from this namespace and continues hierarchically up to top level.
		///
		public IList<MethodSpec> LookupExtensionMethod (TypeSpec extensionType, string name, int arity, ref NamespaceEntry scope)
		{
			List<MethodSpec> candidates = null;
			foreach (Namespace n in GetUsingTable ()) {
				var a = n.LookupExtensionMethod (extensionType, RootContext.ToplevelTypes, name, arity);
				if (a == null)
					continue;

				if (candidates == null)
					candidates = a;
				else
					candidates.AddRange (a);
			}

			scope = parent;
			if (candidates != null)
				return candidates;

			if (parent == null)
				return null;

			//
			// Inspect parent namespaces in namespace expression
			//
			Namespace parent_ns = ns.Parent;
			do {
				candidates = parent_ns.LookupExtensionMethod (extensionType, RootContext.ToplevelTypes, name, arity);
				if (candidates != null)
					return candidates;

				parent_ns = parent_ns.Parent;
			} while (parent_ns != null);

			//
			// Continue in parent scope
			//
			return parent.LookupExtensionMethod (extensionType, name, arity, ref scope);
		}

		public FullNamedExpression LookupNamespaceOrType (string name, int arity, Location loc, bool ignore_cs0104)
		{
			// Precondition: Only simple names (no dots) will be looked up with this function.
			FullNamedExpression resolved = null;
			for (NamespaceEntry curr_ns = this; curr_ns != null; curr_ns = curr_ns.ImplicitParent) {
				if ((resolved = curr_ns.Lookup (name, arity, loc, ignore_cs0104)) != null)
					break;
			}

			return resolved;
		}

		public IList<string> CompletionGetTypesStartingWith (string prefix)
		{
			IEnumerable<string> all = Enumerable.Empty<string> ();
			
			for (NamespaceEntry curr_ns = this; curr_ns != null; curr_ns = curr_ns.ImplicitParent){
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
			for (NamespaceEntry n = this; n != null; n = n.ImplicitParent) {
				if (n.using_aliases == null)
					continue;

				foreach (UsingAliasEntry ue in n.using_aliases) {
					if (ue.Alias == name)
						return ue.Resolve (Doppelganger ?? this, Doppelganger == null);
				}
			}

			return null;
		}

		private FullNamedExpression Lookup (string name, int arity, Location loc, bool ignore_cs0104)
		{
			//
			// Check whether it's in the namespace.
			//
			FullNamedExpression fne = ns.Lookup (Compiler, name, arity, loc);

			//
			// Check aliases. 
			//
			if (using_aliases != null && arity == 0) {
				foreach (UsingAliasEntry ue in using_aliases) {
					if (ue.Alias == name) {
						if (fne != null) {
							if (Doppelganger != null) {
								// TODO: Namespace has broken location
								//Report.SymbolRelatedToPreviousError (fne.Location, null);
								Compiler.Report.SymbolRelatedToPreviousError (ue.Location, null);
								Compiler.Report.Error (576, loc,
									"Namespace `{0}' contains a definition with same name as alias `{1}'",
									GetSignatureForError (), name);
							} else {
								return fne;
							}
						}

						return ue.Resolve (Doppelganger ?? this, Doppelganger == null);
					}
				}
			}

			if (fne != null) {
				if (!((fne.Type.Modifiers & Modifiers.INTERNAL) != 0 && !fne.Type.MemberDefinition.IsInternalAsPublic (RootContext.ToplevelTypes.DeclaringAssembly)))
					return fne;
			}

			if (IsImplicit)
				return null;

			//
			// Check using entries.
			//
			FullNamedExpression match = null;
			foreach (Namespace using_ns in GetUsingTable ()) {
				// A using directive imports only types contained in the namespace, it
				// does not import any nested namespaces
				fne = using_ns.LookupType (Compiler, name, arity, false, loc);
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

				if (ignore_cs0104)
					return match;

				// It can be top level accessibility only
				var better = Namespace.IsImportedTypeOverride (texpr_match.Type, texpr_fne.Type);
				if (better == null) {
					Compiler.Report.SymbolRelatedToPreviousError (texpr_match.Type);
					Compiler.Report.SymbolRelatedToPreviousError (texpr_fne.Type);
					Compiler.Report.Error (104, loc, "`{0}' is an ambiguous reference between `{1}' and `{2}'",
						name, texpr_match.GetSignatureForError (), texpr_fne.GetSignatureForError ());
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

			foreach (UsingEntry ue in using_clauses) {
				Namespace using_ns = ue.Resolve (Doppelganger);
				if (using_ns == null)
					continue;

				list.Add (using_ns);
			}

			namespace_using_table = list.ToArray ();
			return namespace_using_table;
		}

		static readonly string [] empty_using_list = new string [0];

		public int SymbolFileID {
			get {
				if (symfile_id == 0 && file.SourceFileEntry != null) {
					int parent_id = parent == null ? 0 : parent.SymbolFileID;

					string [] using_list = empty_using_list;
					if (using_clauses != null) {
						using_list = new string [using_clauses.Count];
						for (int i = 0; i < using_clauses.Count; i++)
							using_list [i] = ((UsingEntry) using_clauses [i]).MemberName.GetName ();
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
				MsgtryPkg ("gtk-sharp");
				break;

			case "Gdk": case "GdkSharp":
				MsgtryPkg ("gdk-sharp");
				break;

			case "Glade": case "GladeSharp":
				MsgtryPkg ("glade-sharp");
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
		void VerifyUsing ()
		{
			if (using_aliases != null) {
				foreach (UsingAliasEntry ue in using_aliases)
					ue.Resolve (Doppelganger, Doppelganger == null);
			}

			if (using_clauses != null) {
				foreach (UsingEntry ue in using_clauses)
					ue.Resolve (Doppelganger);
			}
		}

		/// <summary>
		///   Used to validate that all the using clauses are correct
		///   after we are finished parsing all the files.  
		/// </summary>
		static public void VerifyAllUsing ()
		{
			foreach (NamespaceEntry entry in entries)
				entry.VerifyUsing ();
		}

		public string GetSignatureForError ()
		{
			return ns.GetSignatureForError ();
		}

		public override string ToString ()
		{
			return ns.ToString ();
		}

		#region IMemberContext Members

		public CompilerContext Compiler {
			get { return ctx.Compiler; }
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

		// FIXME: It's false for expression types
		public bool HasUnresolvedConstraints {
			get { return true; }
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
			get { return SlaveDeclSpace.Module; }
		}

		#endregion
	}
}
