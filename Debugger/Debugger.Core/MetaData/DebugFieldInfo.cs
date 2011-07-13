// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Globalization;
using System.Reflection;

using Debugger.Interop.MetaData;
using Mono.Cecil.Signatures;

namespace Debugger.MetaData
{
	public class DebugFieldInfo : System.Reflection.FieldInfo, IDebugMemberInfo
	{
		DebugType declaringType;
		FieldProps fieldProps;
		
		internal DebugFieldInfo(DebugType declaringType, FieldProps fieldProps)
		{
			this.declaringType = declaringType;
			this.fieldProps = fieldProps;
		}
		
		/// <inheritdoc/>
		public override Type DeclaringType {
			get { return declaringType; }
		}
		
		internal FieldProps FieldProps {
			get { return fieldProps; }
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
			get { return (int)fieldProps.Token; }
		}
		
		/// <inheritdoc/>
		public override System.Reflection.Module Module {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override string Name {
			get { return fieldProps.Name; }
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
		public override FieldAttributes Attributes {
			get { return (FieldAttributes)fieldProps.Flags; }
		}
		
		/// <inheritdoc/>
		public override RuntimeFieldHandle FieldHandle {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override Type FieldType {
			get {
				SignatureReader sigReader = new SignatureReader(fieldProps.SigBlob.GetData());
				FieldSig fieldSig = sigReader.GetFieldSig(0);
				return DebugType.CreateFromSignature(this.DebugModule, fieldSig.Type, declaringType);
			}
		}
		
		//		public virtual Type[] GetOptionalCustomModifiers();
		//		public virtual object GetRawConstantValue();
		//		public virtual Type[] GetRequiredCustomModifiers();
		
		/// <inheritdoc/>
		public override object GetValue(object obj)
		{
			return Value.GetFieldValue((Value)obj, this);
		}
		
		/// <inheritdoc/>
		public override void SetValue(object obj, object value, System.Reflection.BindingFlags invokeAttr, Binder binder, CultureInfo culture)
		{
			Value.SetFieldValue((Value)obj, this, (Value)value);
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return this.FieldType + " " + this.Name;
		}
		
		DebugType IDebugMemberInfo.MemberType {
			get { return (DebugType)this.FieldType; }
		}
	}
}
