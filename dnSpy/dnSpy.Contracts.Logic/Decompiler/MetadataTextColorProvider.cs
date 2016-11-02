/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using dnlib.DotNet;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Provides text colors
	/// </summary>
	public abstract class MetadataTextColorProvider {
		/// <summary>
		/// Gets a type color
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public virtual object GetColor(TypeDef type) {
			if (type == null)
				return BoxedTextColor.Text;

			if (type.IsInterface)
				return BoxedTextColor.Interface;
			if (type.IsEnum)
				return BoxedTextColor.Enum;
			if (type.IsValueType)
				return BoxedTextColor.ValueType;

			if (type.IsDelegate)
				return BoxedTextColor.Delegate;

			if (type.IsSealed && type.IsAbstract) {
				var bt = type.BaseType;
				if (bt != null && bt.DefinitionAssembly.IsCorLib()) {
					var baseTr = bt as TypeRef;
					if (baseTr != null) {
						if (baseTr.Namespace == systemString && baseTr.Name == objectString)
							return BoxedTextColor.StaticType;
					}
					else {
						var baseTd = bt as TypeDef;
						if (baseTd.Namespace == systemString && baseTd.Name == objectString)
							return BoxedTextColor.StaticType;
					}
				}
			}

			if (type.IsSealed)
				return BoxedTextColor.SealedType;
			return BoxedTextColor.Type;
		}
		static readonly UTF8String systemString = new UTF8String("System");
		static readonly UTF8String objectString = new UTF8String("Object");

		/// <summary>
		/// Gets a type color
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public virtual object GetColor(TypeRef type) {
			if (type == null)
				return BoxedTextColor.Text;

			var td = type.Resolve();
			if (td != null)
				return GetColor(td);

			return BoxedTextColor.Type;
		}

		static readonly UTF8String systemRuntimeCompilerServicesString = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String extensionAttributeString = new UTF8String("ExtensionAttribute");

		/// <summary>
		/// Gets a member color
		/// </summary>
		/// <param name="memberRef">Member</param>
		/// <returns></returns>
		public virtual object GetColor(IMemberRef memberRef) {
			if (memberRef == null)
				return BoxedTextColor.Text;

			if (memberRef.IsField) {
				var fd = ((IField)memberRef).ResolveFieldDef();
				if (fd == null)
					return BoxedTextColor.InstanceField;
				if (fd.DeclaringType.IsEnum)
					return BoxedTextColor.EnumField;
				if (fd.IsLiteral)
					return BoxedTextColor.LiteralField;
				if (fd.IsStatic)
					return BoxedTextColor.StaticField;
				return BoxedTextColor.InstanceField;
			}
			if (memberRef.IsMethod) {
				var mr = (IMethod)memberRef;
				if (mr.MethodSig == null)
					return BoxedTextColor.InstanceMethod;
				var md = mr.ResolveMethodDef();
				if (md != null && md.IsConstructor)
					return GetColor(md.DeclaringType);
				if (!mr.MethodSig.HasThis) {
					if (md != null && md.IsDefined(systemRuntimeCompilerServicesString, extensionAttributeString))
						return BoxedTextColor.ExtensionMethod;
					return BoxedTextColor.StaticMethod;
				}
				return BoxedTextColor.InstanceMethod;
			}
			if (memberRef.IsPropertyDef) {
				var p = (PropertyDef)memberRef;
				return GetColor(p.GetMethod ?? p.SetMethod, BoxedTextColor.StaticProperty, BoxedTextColor.InstanceProperty);
			}
			if (memberRef.IsEventDef) {
				var e = (EventDef)memberRef;
				return GetColor(e.AddMethod ?? e.RemoveMethod ?? e.InvokeMethod, BoxedTextColor.StaticEvent, BoxedTextColor.InstanceEvent);
			}

			var td = memberRef as TypeDef;
			if (td != null)
				return GetColor(td);

			var tr = memberRef as TypeRef;
			if (tr != null)
				return GetColor(tr);

			var ts = memberRef as TypeSpec;
			if (ts != null) {
				var gsig = ts.TypeSig as GenericSig;
				if (gsig != null)
					return GetColor(gsig);
				return BoxedTextColor.Type;
			}

			var gp = memberRef as GenericParam;
			if (gp != null)
				return GetColor(gp);

			// It can be a MemberRef if it doesn't have a field or method sig (invalid metadata)
			if (memberRef.IsMemberRef)
				return BoxedTextColor.Text;

			return BoxedTextColor.Text;
		}

		/// <summary>
		/// Gets a generic signature color
		/// </summary>
		/// <param name="genericSig">Generic signature</param>
		/// <returns></returns>
		public virtual object GetColor(GenericSig genericSig) {
			if (genericSig == null)
				return BoxedTextColor.Text;

			return genericSig.IsMethodVar ? BoxedTextColor.MethodGenericParameter : BoxedTextColor.TypeGenericParameter;
		}

		/// <summary>
		/// Gets a generic parameter color
		/// </summary>
		/// <param name="genericParam">Generic parameter</param>
		/// <returns></returns>
		public virtual object GetColor(GenericParam genericParam) {
			if (genericParam == null)
				return BoxedTextColor.Text;

			if (genericParam.DeclaringType != null)
				return BoxedTextColor.TypeGenericParameter;

			if (genericParam.DeclaringMethod != null)
				return BoxedTextColor.MethodGenericParameter;

			return BoxedTextColor.TypeGenericParameter;
		}

		static object GetColor(MethodDef method, object staticValue, object instanceValue) {
			if (method == null)
				return instanceValue;
			if (method.IsStatic)
				return staticValue;
			return instanceValue;
		}

		/// <summary>
		/// Gets an exported type color
		/// </summary>
		/// <param name="exportedType">Exported type</param>
		/// <returns></returns>
		public virtual object GetColor(ExportedType exportedType) {
			if (exportedType == null)
				return BoxedTextColor.Text;

			return GetColor(exportedType.ToTypeRef());
		}

		/// <summary>
		/// Gets a type signature color
		/// </summary>
		/// <param name="typeSig">Type signature</param>
		/// <returns></returns>
		public virtual object GetColor(TypeSig typeSig) {
			typeSig = typeSig.RemovePinnedAndModifiers();
			if (typeSig == null)
				return BoxedTextColor.Text;

			var tdr = typeSig as TypeDefOrRefSig;
			if (tdr != null)
				return GetColor(tdr.TypeDefOrRef);

			var gsig = typeSig as GenericSig;
			if (gsig != null)
				return GetColor(gsig);

			return BoxedTextColor.Text;
		}

		/// <summary>
		/// Gets a color
		/// </summary>
		/// <param name="obj">Object, eg. an instruction operand</param>
		/// <returns></returns>
		public virtual object GetColor(object obj) {
			if (obj == null)
				return BoxedTextColor.Text;

			if (obj is byte || obj is sbyte ||
				obj is ushort || obj is short ||
				obj is uint || obj is int ||
				obj is ulong || obj is long ||
				obj is UIntPtr || obj is IntPtr)
				return BoxedTextColor.Number;

			var r = obj as IMemberRef;
			if (r != null)
				return GetColor(r);

			var et = obj as ExportedType;
			if (et != null)
				return GetColor(et);

			var ts = obj as TypeSig;
			if (ts != null)
				return GetColor(ts);

			var gp = obj as GenericParam;
			if (gp != null)
				return GetColor(gp);

			if (obj is TextColor)
				return obj;

			if (obj is Parameter)
				return BoxedTextColor.Parameter;

			if (obj is dnlib.DotNet.Emit.Local)
				return BoxedTextColor.Local;

			if (obj is MethodSig)
				return BoxedTextColor.Text;//TODO:

			if (obj is string)
				return BoxedTextColor.String;

			return BoxedTextColor.Text;
		}
	}

	/// <summary>
	/// C# <see cref="TextColor"/> provider
	/// </summary>
	public sealed class CSharpMetadataTextColorProvider : MetadataTextColorProvider {
		/// <summary>
		/// Gets the instance
		/// </summary>
		public static readonly CSharpMetadataTextColorProvider Instance = new CSharpMetadataTextColorProvider();

		CSharpMetadataTextColorProvider() { }
	}

	/// <summary>
	/// Visual Basic <see cref="TextColor"/> provider
	/// </summary>
	public sealed class VisualBasicMetadataTextColorProvider : MetadataTextColorProvider {
		/// <summary>
		/// Gets the instance
		/// </summary>
		public static readonly VisualBasicMetadataTextColorProvider Instance = new VisualBasicMetadataTextColorProvider();

		VisualBasicMetadataTextColorProvider() { }

		/// <summary>
		/// Gets a type color
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public override object GetColor(TypeDef type) {
			if (type != null && type.DeclaringType == null && type.IsSealed && type.IsAbstract)
				return BoxedTextColor.Module;
			return base.GetColor(type);
		}
	}
}
