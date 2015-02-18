// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Debugger.MetaData
{
	public class DebugPropertyInfo : System.Reflection.PropertyInfo, IDebugMemberInfo, IOverloadable
	{
		DebugType declaringType;
		string name;
		MethodInfo getMethod;
		MethodInfo setMethod;
		
		internal DebugPropertyInfo(DebugType declaringType, string name, MethodInfo getMethod, MethodInfo setMethod)
		{
			if (getMethod == null && setMethod == null) throw new ArgumentNullException("Both getter and setter can not be null.");
			
			this.declaringType = declaringType;
			this.name      = name;
			this.getMethod = getMethod;
			this.setMethod = setMethod;
		}
		
		/// <inheritdoc/>
		public override Type DeclaringType {
			get { return declaringType; }
		}
		
		/// <summary> The AppDomain in which this member is declared </summary>
		public AppDomain AppDomain {
			get { return declaringType.AppDomain; }
		}
		
		/// <summary> The Process in which this member is declared </summary>
		public Process Process {
			get { return declaringType.Process; }
		}
		
		/// <summary> The Module in which this member is declared </summary>
		public Debugger.Module DebugModule {
			get { return declaringType.DebugModule; }
		}
		
		/// <inheritdoc/>
		public override int MetadataToken {
			get { return 0; }
		}
		
		/// <inheritdoc/>
		public override System.Reflection.Module Module {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override string Name {
			get { return name; }
		}
		
		/// <summary> Name including the declaring type, return type and parameters </summary>
		public string FullName {
			get {
				StringBuilder sb = new StringBuilder();
				
				if (this.IsStatic) {
					sb.Append("static ");
				}
				sb.Append(this.PropertyType.Name);
				sb.Append(" ");
				
				sb.Append(this.DeclaringType.FullName);
				sb.Append(".");
				sb.Append(this.Name);
				
				if (GetIndexParameters().Length > 0) {
					sb.Append("[");
					bool first = true;
					foreach(DebugParameterInfo p in GetIndexParameters()) {
						if (!first)
							sb.Append(", ");
						first = false;
						sb.Append(p.ParameterType.Name);
						sb.Append(" ");
						sb.Append(p.Name);
					}
					sb.Append("]");
				}
				return sb.ToString();
			}
		}
		
		/// <inheritdoc/>
		public override Type ReflectedType {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override object[] GetCustomAttributes(bool inherit)
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return DebugType.IsDefined(this, inherit, attributeType);
		}
		
		/// <inheritdoc/>
		public override PropertyAttributes Attributes {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override bool CanRead {
			get { return getMethod != null; }
		}
		
		/// <inheritdoc/>
		public override bool CanWrite {
			get { return setMethod != null; }
		}
		
		/// <inheritdoc/>
		public override Type PropertyType {
			get {
				if (getMethod != null) {
					return getMethod.ReturnType;
				} else {
					return setMethod.GetParameters()[setMethod.GetParameters().Length - 1].ParameterType;
				}
			}
		}
		
		/// <inheritdoc/>
		public override MethodInfo[] GetAccessors(bool nonPublic)
		{
			throw new NotSupportedException();
		}
		
		//		public virtual object GetConstantValue();
		
		/// <inheritdoc/>
		public override MethodInfo GetGetMethod(bool nonPublic)
		{
			return getMethod;
		}
		
		/// <inheritdoc/>
		public override ParameterInfo[] GetIndexParameters()
		{
			if (GetGetMethod() != null) {
				return GetGetMethod().GetParameters();
			}
			if (GetSetMethod() != null) {
				List<ParameterInfo> pars = new List<ParameterInfo>();
				pars.AddRange(GetSetMethod().GetParameters());
				pars.RemoveAt(pars.Count - 1);
				return pars.ToArray();
			}
			return null;
		}
		
		//		public virtual Type[] GetOptionalCustomModifiers();
		//		public virtual object GetRawConstantValue();
		//		public virtual Type[] GetRequiredCustomModifiers();
		
		/// <inheritdoc/>
		public override MethodInfo GetSetMethod(bool nonPublic)
		{
			return setMethod;
		}
		
		/// <inheritdoc/>
		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			List<Value> args = new List<Value>();
			foreach(object arg in index) {
				args.Add((Value)arg);
			}
			return Value.GetPropertyValue((Value)obj, this, args.ToArray());
		}
		
		/// <inheritdoc/>
		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
		{
			List<Value> args = new List<Value>();
			foreach(object arg in index) {
				args.Add((Value)arg);
			}
			Value.SetPropertyValue((Value)obj, this, args.ToArray(), (Value)value);
		}
		
		public bool IsPublic {
			get { return (getMethod ?? setMethod).IsPublic; }
		}
		
		public bool IsAssembly {
			get { return (getMethod ?? setMethod).IsAssembly; }
		}
		
		public bool IsFamily {
			get { return (getMethod ?? setMethod).IsFamily; }
		}
		
		public bool IsPrivate {
			get { return (getMethod ?? setMethod).IsPrivate; }
		}
		
		public bool IsStatic {
			get { return (getMethod ?? setMethod).IsStatic; }
		}
		
		DebugType IDebugMemberInfo.MemberType {
			get { return (DebugType)this.PropertyType; }
		}
		
		ParameterInfo[] IOverloadable.GetParameters()
		{
			return GetIndexParameters();
		}
		
		IntPtr IOverloadable.GetSignarture()
		{
			return ((IOverloadable)(getMethod ?? setMethod)).GetSignarture();
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return this.FullName;
		}
	}
}
