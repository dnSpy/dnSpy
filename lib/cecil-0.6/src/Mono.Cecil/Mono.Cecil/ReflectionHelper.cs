//
// ReflectionHelper.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
// (C) 2006 Evaluant RC S.A.
// (C) 2007 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil {

	using System;
	using System.Collections;
	using SR = System.Reflection;
	using System.Text;

	internal sealed class ReflectionHelper {

		ModuleDefinition m_module;

		public ReflectionHelper (ModuleDefinition module)
		{
			m_module = module;
		}

		public AssemblyNameReference ImportAssembly (SR.Assembly asm)
		{
			AssemblyNameReference asmRef = GetAssemblyNameReference (asm.GetName ());
			if (asmRef != null)
				return asmRef;

			SR.AssemblyName asmName = asm.GetName ();
			asmRef = new AssemblyNameReference (
				asmName.Name, asmName.CultureInfo.Name, asmName.Version);
			asmRef.PublicKeyToken = asmName.GetPublicKeyToken ();
			asmRef.HashAlgorithm = (AssemblyHashAlgorithm) asmName.HashAlgorithm;
			asmRef.Culture = asmName.CultureInfo.ToString ();
			m_module.AssemblyReferences.Add (asmRef);
			return asmRef;
		}

		AssemblyNameReference GetAssemblyNameReference (SR.AssemblyName name)
		{
			foreach (AssemblyNameReference reference in m_module.AssemblyReferences)
				if (reference.FullName == name.FullName)
					return reference;

			return null;
		}

		public static string GetTypeSignature (Type t)
		{
			if (t.HasElementType) {
				if (t.IsPointer)
					return string.Concat (GetTypeSignature (t.GetElementType ()), "*");
				else if (t.IsArray) // deal with complex arrays
					return string.Concat (GetTypeSignature (t.GetElementType ()), "[]");
				else if (t.IsByRef)
					return string.Concat (GetTypeSignature (t.GetElementType ()), "&");
			}

			if (IsGenericTypeSpec (t)) {
				StringBuilder sb = new StringBuilder ();
				sb.Append (GetTypeSignature (GetGenericTypeDefinition (t)));
				sb.Append ("<");
				Type [] genArgs = GetGenericArguments (t);
				for (int i = 0; i < genArgs.Length; i++) {
					if (i > 0)
						sb.Append (",");
					sb.Append (GetTypeSignature (genArgs [i]));
				}
				sb.Append (">");
				return sb.ToString ();
			}

			if (IsGenericParameter (t))
				return t.Name;

			if (t.DeclaringType != null)
				return string.Concat (t.DeclaringType.FullName, "/", t.Name);

			if (t.Namespace == null || t.Namespace.Length == 0)
				return t.Name;

			return string.Concat (t.Namespace, ".", t.Name);
		}

		static bool GetProperty (object o, string prop)
		{
			SR.PropertyInfo pi = o.GetType ().GetProperty (prop);
			if (pi == null)
				return false;

			return (bool) pi.GetValue (o, null);
		}

		public static bool IsGenericType (Type t)
		{
			return GetProperty (t, "IsGenericType");
		}

		static bool IsGenericParameter (Type t)
		{
			return GetProperty (t, "IsGenericParameter");
		}

		static bool IsGenericTypeDefinition (Type t)
		{
			return GetProperty (t, "IsGenericTypeDefinition");
		}

		static bool IsGenericTypeSpec (Type t)
		{
			return IsGenericType (t) && !IsGenericTypeDefinition (t);
		}

		static Type GetGenericTypeDefinition (Type t)
		{
			return (Type) t.GetType ().GetMethod ("GetGenericTypeDefinition").Invoke (t, null);
		}

		static Type [] GetGenericArguments (Type t)
		{
			return (Type []) t.GetType ().GetMethod ("GetGenericArguments").Invoke (t, null);
		}

		GenericInstanceType GetGenericType (Type t, TypeReference element, ImportContext context)
		{
			GenericInstanceType git = new GenericInstanceType (element);
			foreach (Type genArg in GetGenericArguments (t))
				git.GenericArguments.Add (ImportSystemType (genArg, context));

			return git;
		}

		static bool GenericParameterOfMethod (Type t)
		{
			return t.GetType ().GetProperty ("DeclaringMethod").GetValue (t, null) != null;
		}

		static GenericParameter GetGenericParameter (Type t, ImportContext context)
		{
			int pos = (int) t.GetType ().GetProperty ("GenericParameterPosition").GetValue (t, null);
			if (GenericParameterOfMethod (t))
				return context.GenericContext.Method.GenericParameters [pos];
			else
				return context.GenericContext.Type.GenericParameters [pos];
		}

		TypeReference GetTypeSpec (Type t, ImportContext context)
		{
			Stack s = new Stack ();
			while (t.HasElementType || IsGenericTypeSpec (t)) {
				s.Push (t);
				if (t.HasElementType)
					t = t.GetElementType ();
				else if (IsGenericTypeSpec (t)) {
					t = (Type) t.GetType ().GetMethod ("GetGenericTypeDefinition").Invoke (t, null);
					break;
				}
			}

			TypeReference elementType = ImportSystemType (t, context);
			while (s.Count > 0) {
				t = (Type) s.Pop ();
				if (t.IsPointer)
					elementType = new PointerType (elementType);
				else if (t.IsArray) // deal with complex arrays
					elementType = new ArrayType (elementType);
				else if (t.IsByRef)
					elementType = new ReferenceType (elementType);
				else if (IsGenericTypeSpec (t))
					elementType = GetGenericType (t, elementType, context);
				else
					throw new ReflectionException ("Unknown element type");
			}

			return elementType;
		}

		public TypeReference ImportSystemType (Type t, ImportContext context)
		{
			if (t.HasElementType || IsGenericTypeSpec (t))
				return GetTypeSpec (t, context);

			if (IsGenericParameter (t))
				return GetGenericParameter (t, context);

			TypeReference type = m_module.TypeReferences [GetTypeSignature (t)];
			if (type != null) {
				if (t.IsValueType && !type.IsValueType)
					type.IsValueType = true;

				return type;
			}

			AssemblyNameReference asm = ImportAssembly (t.Assembly);
			type = new TypeReference (t.Name, t.Namespace, asm, t.IsValueType);

			if (IsGenericTypeDefinition (t))
				foreach (Type genParam in GetGenericArguments (t))
					type.GenericParameters.Add (new GenericParameter (genParam.Name, type));

			context.GenericContext.Type = type;

			m_module.TypeReferences.Add (type);
			return type;
		}

		static string GetMethodBaseSignature (SR.MethodBase meth, Type declaringType, Type retType)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (GetTypeSignature (retType));
			sb.Append (' ');
			sb.Append (GetTypeSignature (declaringType));
			sb.Append ("::");
			sb.Append (meth.Name);
			if (IsGenericMethodSpec (meth)) {
				sb.Append ("<");
				Type [] genArgs = GetGenericArguments (meth as SR.MethodInfo);
				for (int i = 0; i < genArgs.Length; i++) {
					if (i > 0)
						sb.Append (",");
					sb.Append (GetTypeSignature (genArgs [i]));
				}
				sb.Append (">");
			}
			sb.Append ("(");
			SR.ParameterInfo [] parameters = meth.GetParameters ();
			for (int i = 0; i < parameters.Length; i++) {
				if (i > 0)
					sb.Append (", ");
				sb.Append (GetTypeSignature (parameters [i].ParameterType));
			}
			sb.Append (")");
			return sb.ToString ();
		}

		static bool IsGenericMethod (SR.MethodBase mb)
		{
			return GetProperty (mb, "IsGenericMethod");
		}

		static bool IsGenericMethodDefinition (SR.MethodBase mb)
		{
			return GetProperty (mb, "IsGenericMethodDefinition");
		}

		static bool IsGenericMethodSpec (SR.MethodBase mb)
		{
			return IsGenericMethod (mb) && !IsGenericMethodDefinition (mb);
		}

		static Type [] GetGenericArguments (SR.MethodInfo mi)
		{
			return (Type []) mi.GetType ().GetMethod ("GetGenericArguments").Invoke (mi, null);
		}

		static int GetMetadataToken (SR.MethodInfo mi)
		{
			return (int) mi.GetType ().GetProperty ("MetadataToken").GetValue (mi, null);
		}

		MethodReference ImportGenericInstanceMethod (SR.MethodInfo mi, ImportContext context)
		{
			SR.MethodInfo gmd = (SR.MethodInfo) mi.GetType ().GetMethod ("GetGenericMethodDefinition").Invoke (mi, null);
			GenericInstanceMethod gim = new GenericInstanceMethod (
				ImportMethodBase (gmd, gmd.ReturnType, context));

			foreach (Type genArg in GetGenericArguments (mi))
				gim.GenericArguments.Add (ImportSystemType (genArg, context));

			return gim;
		}

		MethodReference ImportMethodBase (SR.MethodBase mb, Type retType, ImportContext context)
		{
			if (IsGenericMethod (mb) && !IsGenericMethodDefinition (mb))
				return ImportGenericInstanceMethod ((SR.MethodInfo) mb, context);

			Type originalDecType = mb.DeclaringType;
			Type declaringTypeDef = originalDecType;
			while (IsGenericTypeSpec (declaringTypeDef))
				declaringTypeDef = GetGenericTypeDefinition (declaringTypeDef);

			if (mb.DeclaringType != declaringTypeDef && mb is SR.MethodInfo) {
				int mt = GetMetadataToken (mb as SR.MethodInfo);
				// hack to get the generic method definition from the constructed method
				foreach (SR.MethodInfo mi in declaringTypeDef.GetMethods ()) {
					if (GetMetadataToken (mi) == mt) {
						mb = mi;
						retType = mi.ReturnType;
						break;
					}
				}
			}

			string sig = GetMethodBaseSignature (mb, originalDecType, retType);
			MethodReference meth = (MethodReference) GetMemberReference (sig);
			if (meth != null)
				return meth;

			meth = new MethodReference (
				mb.Name,
				(mb.CallingConvention & SR.CallingConventions.HasThis) > 0,
				(mb.CallingConvention & SR.CallingConventions.ExplicitThis) > 0,
				MethodCallingConvention.Default); // TODO: get the real callconv
			meth.DeclaringType = ImportSystemType (originalDecType, context);

			if (IsGenericMethod (mb))
				foreach (Type genParam in GetGenericArguments (mb as SR.MethodInfo))
					meth.GenericParameters.Add (new GenericParameter (genParam.Name, meth));

			context.GenericContext.Method = meth;
			context.GenericContext.Type = ImportSystemType (declaringTypeDef, context);

			meth.ReturnType.ReturnType = ImportSystemType (retType, context);

			SR.ParameterInfo [] parameters = mb.GetParameters ();
			for (int i = 0; i < parameters.Length; i++)
				meth.Parameters.Add (new ParameterDefinition (
					ImportSystemType (parameters [i].ParameterType, context)));

			m_module.MemberReferences.Add (meth);
			return meth;
		}

		public MethodReference ImportConstructorInfo (SR.ConstructorInfo ci, ImportContext context)
		{
			return ImportMethodBase (ci, typeof (void), context);
		}

		public MethodReference ImportMethodInfo (SR.MethodInfo mi, ImportContext context)
		{
			return ImportMethodBase (mi, mi.ReturnType, context);
		}

		static string GetFieldSignature (SR.FieldInfo field)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (GetTypeSignature (field.FieldType));
			sb.Append (' ');
			sb.Append (GetTypeSignature (field.DeclaringType));
			sb.Append ("::");
			sb.Append (field.Name);
			return sb.ToString ();
		}

		public FieldReference ImportFieldInfo (SR.FieldInfo fi, ImportContext context)
		{
			string sig = GetFieldSignature (fi);
			FieldReference f = (FieldReference) GetMemberReference (sig);
			if (f != null)
				return f;

			f = new FieldReference (
				fi.Name,
				ImportSystemType (fi.DeclaringType, context),
				ImportSystemType (fi.FieldType, context));

			m_module.MemberReferences.Add (f);
			return f;
		}

		MemberReference GetMemberReference (string signature)
		{
			foreach (MemberReference reference in m_module.MemberReferences)
				if (reference.ToString () == signature)
					return reference;

			return null;
		}
	}
}
