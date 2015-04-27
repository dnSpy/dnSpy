//
// pending.cs: Pending method implementation
//
// Authors:
//   Miguel de Icaza (miguel@gnu.org)
//   Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002 Ximian, Inc (http://www.ximian.com)
// Copyright 2003-2008 Novell, Inc.
// Copyright 2011 Xamarin Inc
//

using System;
using System.Collections.Generic;
using System.Linq;

#if STATIC
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp {

	struct TypeAndMethods {
		public TypeSpec          type;
		public IList<MethodSpec> methods;

		// 
		// Whether it is optional, this is used to allow the explicit/implicit
		// implementation when a base class already implements an interface. 
		//
		// For example:
		//
		// class X : IA { }  class Y : X, IA { IA.Explicit (); }
		//
		public bool          optional;
				
		//
		// This flag on the method says `We found a match, but
		// because it was private, we could not use the match
		//
		public MethodData [] found;

		// If a method is defined here, then we always need to
		// create a proxy for it.  This is used when implementing
		// an interface's indexer with a different IndexerName.
		public MethodSpec [] need_proxy;
	}

	struct ProxyMethodContext : IMemberContext
	{
		readonly TypeContainer container;

		public ProxyMethodContext (TypeContainer container)
		{
			this.container = container;
		}

		public TypeSpec CurrentType {
			get {
				throw new NotImplementedException ();
			}
		}

		public TypeParameters CurrentTypeParameters {
			get {
				throw new NotImplementedException ();
			}
		}

		public MemberCore CurrentMemberDefinition {
			get {
				throw new NotImplementedException ();
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
				return false;
			}
		}

		public ModuleContainer Module {
			get {
				return container.Module;
			}
		}

		public string GetSignatureForError ()
		{
			throw new NotImplementedException ();
		}

		public ExtensionMethodCandidates LookupExtensionMethod (TypeSpec extensionType, string name, int arity)
		{
			throw new NotImplementedException ();
		}

		public FullNamedExpression LookupNamespaceOrType (string name, int arity, LookupMode mode, Location loc)
		{
			throw new NotImplementedException ();
		}

		public FullNamedExpression LookupNamespaceAlias (string name)
		{
			throw new NotImplementedException ();
		}
	}

	public class PendingImplementation
	{
		/// <summary>
		///   The container for this PendingImplementation
		/// </summary>
		readonly TypeDefinition container;
		
		/// <summary>
		///   This is the array of TypeAndMethods that describes the pending implementations
		///   (both interfaces and abstract methods in base class)
		/// </summary>
		TypeAndMethods [] pending_implementations;

		PendingImplementation (TypeDefinition container, MissingInterfacesInfo[] missing_ifaces, MethodSpec[] abstract_methods, int total)
		{
			var type_builder = container.Definition;
			
			this.container = container;
			pending_implementations = new TypeAndMethods [total];

			int i = 0;
			if (abstract_methods != null) {
				int count = abstract_methods.Length;
				pending_implementations [i].methods = new MethodSpec [count];
				pending_implementations [i].need_proxy = new MethodSpec [count];

				pending_implementations [i].methods = abstract_methods;
				pending_implementations [i].found = new MethodData [count];
				pending_implementations [i].type = type_builder;
				++i;
			}

			foreach (MissingInterfacesInfo missing in missing_ifaces) {
				var iface = missing.Type;
				var mi = MemberCache.GetInterfaceMethods (iface);

				int count = mi.Count;
				pending_implementations [i].type = iface;
				pending_implementations [i].optional = missing.Optional;
				pending_implementations [i].methods = mi;
				pending_implementations [i].found = new MethodData [count];
				pending_implementations [i].need_proxy = new MethodSpec [count];
				i++;
			}
		}

		Report Report {
			get {
				return container.Module.Compiler.Report;
			}
		}

		struct MissingInterfacesInfo {
			public TypeSpec Type;
			public bool Optional;

			public MissingInterfacesInfo (TypeSpec t)
			{
				Type = t;
				Optional = false;
			}
		}

		static readonly MissingInterfacesInfo [] EmptyMissingInterfacesInfo = new MissingInterfacesInfo [0];
		
		static MissingInterfacesInfo [] GetMissingInterfaces (TypeDefinition container)
		{
			//
			// Interfaces will return all interfaces that the container
			// implements including any inherited interfaces
			//
			var impl = container.Definition.Interfaces;

			if (impl == null || impl.Count == 0)
				return EmptyMissingInterfacesInfo;

			var ret = new MissingInterfacesInfo[impl.Count];

			for (int i = 0; i < ret.Length; i++)
				ret [i] = new MissingInterfacesInfo (impl [i]);

			// we really should not get here because Object doesnt implement any
			// interfaces. But it could implement something internal, so we have
			// to handle that case.
			if (container.BaseType == null)
				return ret;
			
			var base_impls = container.BaseType.Interfaces;
			if (base_impls != null) {
				foreach (TypeSpec t in base_impls) {
					for (int i = 0; i < ret.Length; i++) {
						if (t == ret[i].Type) {
							ret[i].Optional = true;
							break;
						}
					}
				}
			}

			return ret;
		}
		
		//
		// Factory method: if there are pending implementation methods, we return a PendingImplementation
		// object, otherwise we return null.
		//
		// Register method implementations are either abstract methods
		// flagged as such on the base class or interface methods
		//
		static public PendingImplementation GetPendingImplementations (TypeDefinition container)
		{
			TypeSpec b = container.BaseType;

			var missing_interfaces = GetMissingInterfaces (container);

			//
			// If we are implementing an abstract class, and we are not
			// ourselves abstract, and there are abstract methods (C# allows
			// abstract classes that have no abstract methods), then allocate
			// one slot.
			//
			// We also pre-compute the methods.
			//
			bool implementing_abstract = ((b != null) && b.IsAbstract && (container.ModFlags & Modifiers.ABSTRACT) == 0);
			MethodSpec[] abstract_methods = null;

			if (implementing_abstract){
				var am = MemberCache.GetNotImplementedAbstractMethods (b);

				if (am == null) {
					implementing_abstract = false;
				} else {
					abstract_methods = new MethodSpec[am.Count];
					am.CopyTo (abstract_methods, 0);
				}
			}
			
			int total = missing_interfaces.Length +  (implementing_abstract ? 1 : 0);
			if (total == 0)
				return null;

			var pending = new PendingImplementation (container, missing_interfaces, abstract_methods, total);

			//
			// check for inherited conflicting methods
			//
			foreach (var p in pending.pending_implementations) {
				//
				// It can happen for generic interfaces only
				//
				if (!p.type.IsGeneric)
					continue;

				//
				// CLR does not distinguishes between ref and out
				//
				for (int i = 0; i < p.methods.Count; ++i) {
					MethodSpec compared_method = p.methods[i];
					if (compared_method.Parameters.IsEmpty)
						continue;

					for (int ii = i + 1; ii < p.methods.Count; ++ii) {
						MethodSpec tested_method = p.methods[ii];
						if (compared_method.Name != tested_method.Name)
							continue;

						if (p.type != tested_method.DeclaringType)
							continue;

						if (!TypeSpecComparer.Override.IsSame (compared_method.Parameters.Types, tested_method.Parameters.Types))
							continue;

						bool exact_match = true;
						bool ref_only_difference = false;
						var cp = compared_method.Parameters.FixedParameters;
						var tp = tested_method.Parameters.FixedParameters;

						for (int pi = 0; pi < cp.Length; ++pi) {
							//
							// First check exact modifiers match
							//
							if ((cp[pi].ModFlags & Parameter.Modifier.RefOutMask) == (tp[pi].ModFlags & Parameter.Modifier.RefOutMask))
								continue;

							if (((cp[pi].ModFlags | tp[pi].ModFlags) & Parameter.Modifier.RefOutMask) == Parameter.Modifier.RefOutMask) {
								ref_only_difference = true;
								continue;
							}

							exact_match = false;
							break;
						}

						if (!exact_match || !ref_only_difference)
							continue;

						pending.Report.SymbolRelatedToPreviousError (compared_method);
						pending.Report.SymbolRelatedToPreviousError (tested_method);
						pending.Report.Error (767, container.Location,
							"Cannot implement interface `{0}' with the specified type parameters because it causes method `{1}' to differ on parameter modifiers only",
							p.type.GetDefinition().GetSignatureForError (), compared_method.GetSignatureForError ());

						break;
					}
				}
			}

			return pending;
		}

		public enum Operation {
			//
			// If you change this, review the whole InterfaceMethod routine as there
			// are a couple of assumptions on these three states
			//
			Lookup, ClearOne, ClearAll
		}

		/// <summary>
		///   Whether the specified method is an interface method implementation
		/// </summary>
		public MethodSpec IsInterfaceMethod (MemberName name, TypeSpec ifaceType, MethodData method, out MethodSpec ambiguousCandidate, ref bool optional)
		{
			return InterfaceMethod (name, ifaceType, method, Operation.Lookup, out ambiguousCandidate, ref optional);
		}

		public void ImplementMethod (MemberName name, TypeSpec ifaceType, MethodData method, bool clear_one, out MethodSpec ambiguousCandidate, ref bool optional)
		{
			InterfaceMethod (name, ifaceType, method, clear_one ? Operation.ClearOne : Operation.ClearAll, out ambiguousCandidate, ref optional);
		}

		/// <remarks>
		///   If a method in Type `t' (or null to look in all interfaces
		///   and the base abstract class) with name `Name', return type `ret_type' and
		///   arguments `args' implements an interface, this method will
		///   return the MethodInfo that this method implements.
		///
		///   If `name' is null, we operate solely on the method's signature.  This is for
		///   instance used when implementing indexers.
		///
		///   The `Operation op' controls whether to lookup, clear the pending bit, or clear
		///   all the methods with the given signature.
		///
		///   The `MethodInfo need_proxy' is used when we're implementing an interface's
		///   indexer in a class.  If the new indexer's IndexerName does not match the one
		///   that was used in the interface, then we always need to create a proxy for it.
		///
		/// </remarks>
		public MethodSpec InterfaceMethod (MemberName name, TypeSpec iType, MethodData method, Operation op, out MethodSpec ambiguousCandidate, ref bool optional)
		{
			ambiguousCandidate = null;

			if (pending_implementations == null)
				return null;

			TypeSpec ret_type = method.method.ReturnType;
			ParametersCompiled args = method.method.ParameterInfo;
			bool is_indexer = method.method is Indexer.SetIndexerMethod || method.method is Indexer.GetIndexerMethod;
			MethodSpec m;

			foreach (TypeAndMethods tm in pending_implementations){
				if (!(iType == null || tm.type == iType))
					continue;

				int method_count = tm.methods.Count;
				for (int i = 0; i < method_count; i++){
					m = tm.methods [i];

					if (m == null)
						continue;

					if (is_indexer) {
						if (!m.IsAccessor || m.Parameters.IsEmpty)
							continue;
					} else {
						if (name.Name != m.Name)
							continue;

						if (m.Arity != name.Arity)
							continue;
					}

					if (!TypeSpecComparer.Override.IsEqual (m.Parameters, args))
						continue;

					if (!TypeSpecComparer.Override.IsEqual (m.ReturnType, ret_type)) {
						tm.found[i] = method;
						continue;
					}

					//
					// `need_proxy' is not null when we're implementing an
					// interface indexer and this is Clear(One/All) operation.
					//
					// If `name' is null, then we do a match solely based on the
					// signature and not on the name (this is done in the Lookup
					// for an interface indexer).
					//
					if (op != Operation.Lookup) {
						if (m.IsAccessor != method.method.IsAccessor)
							continue;

						// If `t != null', then this is an explicitly interface
						// implementation and we can always clear the method.
						// `need_proxy' is not null if we're implementing an
						// interface indexer.  In this case, we need to create
						// a proxy if the implementation's IndexerName doesn't
						// match the IndexerName in the interface.
						if (m.DeclaringType.IsInterface && iType == null && name.Name != m.Name) {	// TODO: This is very expensive comparison
							tm.need_proxy[i] = method.method.Spec;
						} else {
							tm.methods[i] = null;
						}
					} else {
						tm.found [i] = method;
						optional = tm.optional;
					}

					if (op == Operation.Lookup && name.ExplicitInterface != null && ambiguousCandidate == null) {
						ambiguousCandidate = m;
						continue;
					}

					//
					// Lookups and ClearOne return
					//
					if (op != Operation.ClearAll)
						return m;
				}

				// If a specific type was requested, we can stop now.
				if (tm.type == iType)
					break;
			}

			m = ambiguousCandidate;
			ambiguousCandidate = null;
			return m;
		}

		/// <summary>
		///   C# allows this kind of scenarios:
		///   interface I { void M (); }
		///   class X { public void M (); }
		///   class Y : X, I { }
		///
		///   For that case, we create an explicit implementation function
		///   I.M in Y.
		/// </summary>
		void DefineProxy (TypeSpec iface, MethodSpec base_method, MethodSpec iface_method)
		{
			// TODO: Handle nested iface names
			string proxy_name;
			var ns = iface.MemberDefinition.Namespace;
			if (string.IsNullOrEmpty (ns))
				proxy_name = iface.MemberDefinition.Name + "." + iface_method.Name;
			else
				proxy_name = ns + "." + iface.MemberDefinition.Name + "." + iface_method.Name;

			var param = iface_method.Parameters;

			MethodBuilder proxy = container.TypeBuilder.DefineMethod (
				proxy_name,
				MethodAttributes.Private |
				MethodAttributes.HideBySig |
				MethodAttributes.NewSlot |
				MethodAttributes.CheckAccessOnOverride |
				MethodAttributes.Virtual | MethodAttributes.Final,
				CallingConventions.Standard | CallingConventions.HasThis,
				base_method.ReturnType.GetMetaInfo (), param.GetMetaInfo ());

			if (iface_method.IsGeneric) {
				var gnames = iface_method.GenericDefinition.TypeParameters.Select (l => l.Name).ToArray ();
				proxy.DefineGenericParameters (gnames);
			}

			for (int i = 0; i < param.Count; i++) {
				string name = param.FixedParameters [i].Name;
				ParameterAttributes attr = ParametersCompiled.GetParameterAttribute (param.FixedParameters [i].ModFlags);
				proxy.DefineParameter (i + 1, attr, name);
			}

			int top = param.Count;
			var ec = new EmitContext (new ProxyMethodContext (container), proxy.GetILGenerator (), null, null);
			ec.EmitThis ();
			// TODO: GetAllParametersArguments
			for (int i = 0; i < top; i++)
				ec.EmitArgumentLoad (i);

			ec.Emit (OpCodes.Call, base_method);
			ec.Emit (OpCodes.Ret);

			container.TypeBuilder.DefineMethodOverride (proxy, (MethodInfo) iface_method.GetMetaInfo ());
		}
		
		/// <summary>
		///   This function tells whether one of our base classes implements
		///   the given method (which turns out, it is valid to have an interface
		///   implementation in a base
		/// </summary>
		bool BaseImplements (TypeSpec iface_type, MethodSpec mi, out MethodSpec base_method)
		{
			base_method = null;
			var base_type = container.BaseType;

			//
			// Setup filter with no return type to give better error message
			// about mismatch at return type when the check bellow rejects them
			//
			var parameters = mi.Parameters;
			MethodSpec close_match = null;

			while (true) {
				var candidates = MemberCache.FindMembers (base_type, mi.Name, false);
				if (candidates == null) {
					base_method = close_match;
					return false;
				}

				MethodSpec similar_candidate = null;
				foreach (var candidate in candidates) {
					if (candidate.Kind != MemberKind.Method)
						continue;

					if (candidate.Arity != mi.Arity)
						continue;

					var candidate_param = ((MethodSpec) candidate).Parameters;
					if (!TypeSpecComparer.Override.IsEqual (parameters.Types, candidate_param.Types))
						continue;

					bool modifiers_match = true;
					for (int i = 0; i < parameters.Count; ++i) {
						//
						// First check exact ref/out match
						//
						if ((parameters.FixedParameters[i].ModFlags & Parameter.Modifier.RefOutMask) == (candidate_param.FixedParameters[i].ModFlags & Parameter.Modifier.RefOutMask))
							continue;

						modifiers_match = false;

						//
						// Different in ref/out only
						//
						if ((parameters.FixedParameters[i].ModFlags & Parameter.Modifier.RefOutMask) != (candidate_param.FixedParameters[i].ModFlags & Parameter.Modifier.RefOutMask)) {
							if (similar_candidate == null) {
								if (!candidate.IsPublic)
									break;

								if (!TypeSpecComparer.Override.IsEqual (mi.ReturnType, ((MethodSpec) candidate).ReturnType))
									break;

								// It's used for ref/out ambiguity overload check
								similar_candidate = (MethodSpec) candidate;
							}

							continue;
						}

						similar_candidate = null;
						break;
					}

					if (!modifiers_match)
						continue;

					//
					// From this point the candidate is used for detailed error reporting
					// because it's very close match to what we are looking for
					//
					var m = (MethodSpec) candidate;

					if (!m.IsPublic) {
						if (close_match == null)
							close_match = m;

						continue;
					}

					if (!TypeSpecComparer.Override.IsEqual (mi.ReturnType, m.ReturnType)) {
						if (close_match == null)
							close_match = m;

						continue;
					}
						
					base_method = m;

					if (mi.IsGeneric && !Method.CheckImplementingMethodConstraints (container, m, mi)) {
						return true;
					}
				}
				
				if (base_method != null) {
					if (similar_candidate != null) {
						Report.SymbolRelatedToPreviousError (similar_candidate);
						Report.SymbolRelatedToPreviousError (mi);
						Report.SymbolRelatedToPreviousError (container);
						Report.Warning (1956, 1, ((MemberCore) base_method.MemberDefinition).Location,
							"The interface method `{0}' implementation is ambiguous between following methods: `{1}' and `{2}' in type `{3}'",
							mi.GetSignatureForError (), base_method.GetSignatureForError (), similar_candidate.GetSignatureForError (), container.GetSignatureForError ());
					}

					break;
				}

				base_type = candidates[0].DeclaringType.BaseType;
				if (base_type == null) {
					base_method = close_match;
					return false;
				}
			}

			if (!base_method.IsVirtual) {
#if STATIC
				var base_builder = base_method.GetMetaInfo () as MethodBuilder;
				if (base_builder != null) {
					//
					// We can avoid creating a proxy if base_method can be marked 'final virtual'. This can
					// be done for all methods from compiled assembly
					//
					base_builder.__SetAttributes (base_builder.Attributes | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot);
					return true;
				}
#endif
				DefineProxy (iface_type, base_method, mi);
			}

			return true;
		}

		/// <summary>
		///   Verifies that any pending abstract methods or interface methods
		///   were implemented.
		/// </summary>
		public bool VerifyPendingMethods ()
		{
			int top = pending_implementations.Length;
			bool errors = false;
			int i;
			
			for (i = 0; i < top; i++){
				TypeSpec type = pending_implementations [i].type;

				bool base_implements_type = type.IsInterface &&
					container.BaseType != null &&
					container.BaseType.ImplementsInterface (type, false);

				for (int j = 0; j < pending_implementations [i].methods.Count; ++j) {
					var mi = pending_implementations[i].methods[j];
					if (mi == null)
						continue;

					if (type.IsInterface){
						var need_proxy =
							pending_implementations [i].need_proxy [j];

						if (need_proxy != null) {
							DefineProxy (type, need_proxy, mi);
							continue;
						}

						if (pending_implementations [i].optional)
							continue;

						MethodSpec candidate;
						if (base_implements_type || BaseImplements (type, mi, out candidate))
							continue;

						if (candidate == null) {
							MethodData md = pending_implementations [i].found [j];
							if (md != null)
								candidate = md.method.Spec;
						}

						Report.SymbolRelatedToPreviousError (mi);
						if (candidate != null) {
							Report.SymbolRelatedToPreviousError (candidate);
							if (candidate.IsStatic) {
								Report.Error (736, container.Location,
									"`{0}' does not implement interface member `{1}' and the best implementing candidate `{2}' is static",
									container.GetSignatureForError (), mi.GetSignatureForError (), candidate.GetSignatureForError ());
							} else if ((candidate.Modifiers & Modifiers.PUBLIC) == 0) {
								Report.Error (737, container.Location,
									"`{0}' does not implement interface member `{1}' and the best implementing candidate `{2}' is not public",
									container.GetSignatureForError (), mi.GetSignatureForError (), candidate.GetSignatureForError ());
							} else {
								Report.Error (738, container.Location,
									"`{0}' does not implement interface member `{1}' and the best implementing candidate `{2}' return type `{3}' does not match interface member return type `{4}'",
									container.GetSignatureForError (), mi.GetSignatureForError (), candidate.GetSignatureForError (),
									candidate.ReturnType.GetSignatureForError (), mi.ReturnType.GetSignatureForError ());
							}
						} else {
							Report.Error (535, container.Location, "`{0}' does not implement interface member `{1}'",
								container.GetSignatureForError (), mi.GetSignatureForError ());
						}
					} else {
						Report.SymbolRelatedToPreviousError (mi);
						Report.Error (534, container.Location, "`{0}' does not implement inherited abstract member `{1}'",
							container.GetSignatureForError (), mi.GetSignatureForError ());
					}
					errors = true;
				}
			}
			return errors;
		}
	}
}
