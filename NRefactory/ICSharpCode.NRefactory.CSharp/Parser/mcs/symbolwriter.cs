//
// symbolwriter.cs: The debug symbol writer
//
// Authors: Martin Baulig (martin@ximian.com)
//          Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2010 Novell, Inc
//

using System;

#if STATIC
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif

using Mono.CompilerServices.SymbolWriter;

namespace Mono.CSharp
{
	static class SymbolWriter
	{
#if !NET_4_0 && !STATIC
		delegate int GetILOffsetFunc (ILGenerator ig);
		static GetILOffsetFunc get_il_offset_func;

		delegate Guid GetGuidFunc (ModuleBuilder mb);
		static GetGuidFunc get_guid_func;

		static void Initialize ()
		{
			var mi = typeof (ILGenerator).GetMethod (
				"Mono_GetCurrentOffset",
				BindingFlags.Static | BindingFlags.NonPublic);
			if (mi == null)
				throw new MissingMethodException ("Mono_GetCurrentOffset");

			get_il_offset_func = (GetILOffsetFunc) System.Delegate.CreateDelegate (
				typeof (GetILOffsetFunc), mi);

			mi = typeof (ModuleBuilder).GetMethod (
				"Mono_GetGuid",
				BindingFlags.Static | BindingFlags.NonPublic);
			if (mi == null)
				throw new MissingMethodException ("Mono_GetGuid");

			get_guid_func = (GetGuidFunc) System.Delegate.CreateDelegate (
				typeof (GetGuidFunc), mi);
		}
#endif

		static int GetILOffset (ILGenerator ig)
		{
#if NET_4_0 || STATIC
			return ig.ILOffset;
#else
			if (get_il_offset_func == null)
				Initialize ();

			return get_il_offset_func (ig);
#endif
		}

		public static Guid GetGuid (ModuleBuilder module)
		{
#if NET_4_0 || STATIC
			return module.ModuleVersionId;
#else
			if (get_guid_func == null)
				Initialize ();

			return get_guid_func (module);
#endif
		}

		public static bool HasSymbolWriter {
			get { return symwriter != null; }
		}

		public static MonoSymbolWriter symwriter;

		public static void DefineLocalVariable (string name, LocalBuilder builder)
		{
			if (symwriter != null) {
				symwriter.DefineLocalVariable (builder.LocalIndex, name);
			}
		}

		public static SourceMethodBuilder OpenMethod (ICompileUnit file, IMethodDef method)
		{
			if (symwriter != null)
				return symwriter.OpenMethod (file, -1 /* Not used */, method);
			else
				return null;
		}

		public static void CloseMethod ()
		{
			if (symwriter != null)
				symwriter.CloseMethod ();
		}

		public static int OpenScope (ILGenerator ig)
		{
			if (symwriter != null) {
				int offset = GetILOffset (ig);
				return symwriter.OpenScope (offset);
			} else {
				return -1;
			}
		}

		public static void CloseScope (ILGenerator ig)
		{
			if (symwriter != null) {
				int offset = GetILOffset (ig);
				symwriter.CloseScope (offset);
			}
		}

		public static void DefineAnonymousScope (int id)
		{
			if (symwriter != null)
				symwriter.DefineAnonymousScope (id);
		}

		public static void DefineScopeVariable (int scope, LocalBuilder builder)
		{
			if (symwriter != null) {
				symwriter.DefineScopeVariable (scope, builder.LocalIndex);
			}
		}

		public static void DefineScopeVariable (int scope)
		{
			if (symwriter != null)
				symwriter.DefineScopeVariable (scope, -1);
		}

		public static void DefineCapturedLocal (int scope_id, string name,
							string captured_name)
		{
			if (symwriter != null)
				symwriter.DefineCapturedLocal (scope_id, name, captured_name);
		}

		public static void DefineCapturedParameter (int scope_id, string name,
							    string captured_name)
		{
			if (symwriter != null)
				symwriter.DefineCapturedParameter (scope_id, name, captured_name);
		}

		public static void DefineCapturedThis (int scope_id, string captured_name)
		{
			if (symwriter != null)
				symwriter.DefineCapturedThis (scope_id, captured_name);
		}

		public static void DefineCapturedScope (int scope_id, int id, string captured_name)
		{
			if (symwriter != null)
				symwriter.DefineCapturedScope (scope_id, id, captured_name);
		}

		public static void OpenCompilerGeneratedBlock (EmitContext ec)
		{
			if (symwriter != null) {
				int offset = GetILOffset (ec.ig);
				symwriter.OpenCompilerGeneratedBlock (offset);
			}
		}

		public static void CloseCompilerGeneratedBlock (EmitContext ec)
		{
			if (symwriter != null) {
				int offset = GetILOffset (ec.ig);
				symwriter.CloseCompilerGeneratedBlock (offset);
			}
		}

		public static void StartIteratorBody (EmitContext ec)
		{
			if (symwriter != null) {
				int offset = GetILOffset (ec.ig);
				symwriter.StartIteratorBody (offset);
			}
		}

		public static void EndIteratorBody (EmitContext ec)
		{
			if (symwriter != null) {
				int offset = GetILOffset (ec.ig);
				symwriter.EndIteratorBody (offset);
			}
		}

		public static void StartIteratorDispatcher (EmitContext ec)
		{
			if (symwriter != null) {
				int offset = GetILOffset (ec.ig);
				symwriter.StartIteratorDispatcher (offset);
			}
		}

		public static void EndIteratorDispatcher (EmitContext ec)
		{
			if (symwriter != null) {
				int offset = GetILOffset (ec.ig);
				symwriter.EndIteratorDispatcher (offset);
			}
		}

		public static void MarkSequencePoint (ILGenerator ig, Location loc)
		{
			if (symwriter != null) {
				SourceFileEntry file = loc.SourceFile.SourceFileEntry;
				int offset = GetILOffset (ig);
				symwriter.MarkSequencePoint (offset, file, loc.Row, loc.Column, false);
			}
		}

		public static void Reset ()
		{
			symwriter = null;
		}
	}
}
