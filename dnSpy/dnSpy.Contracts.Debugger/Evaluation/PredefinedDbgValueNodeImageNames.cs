/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Image names returned by <see cref="DbgValueNode"/>
	/// </summary>
	public static class PredefinedDbgValueNodeImageNames {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public const string Edit = nameof(Edit);
		public const string Information = nameof(Information);
		public const string Warning = nameof(Warning);
		public const string Error = nameof(Error);
		public const string ReturnValue = nameof(ReturnValue);
		public const string GenericTypeParameter = nameof(GenericTypeParameter);
		public const string GenericMethodParameter = nameof(GenericMethodParameter);
		public const string Data = nameof(Data);
		public const string Local = nameof(Local);
		public const string Parameter = nameof(Parameter);
		public const string Array = nameof(Array);
		public const string ArrayElement = nameof(ArrayElement);
		public const string Exception = nameof(Exception);
		public const string This = nameof(This);
		public const string TypeVariables = nameof(TypeVariables);
		public const string StowedException = nameof(StowedException);
		public const string ObjectId = nameof(ObjectId);
		public const string ObjectAddress = nameof(ObjectAddress);
		public const string EEVariable = nameof(EEVariable);
		public const string Pointer = nameof(Pointer);
		public const string DereferencedPointer = nameof(DereferencedPointer);
		public const string InstanceMembers = nameof(InstanceMembers);
		public const string StaticMembers = nameof(StaticMembers);
		public const string RawView = nameof(RawView);
		public const string ResultsView = nameof(ResultsView);
		public const string DynamicView = nameof(DynamicView);
		public const string DynamicViewElement = nameof(DynamicViewElement);
		public const string ExceptionInternal = nameof(ExceptionInternal);
		public const string ExceptionPrivate = nameof(ExceptionPrivate);
		public const string ExceptionProtected = nameof(ExceptionProtected);
		public const string ExceptionPublic = nameof(ExceptionPublic);
		public const string Class = nameof(Class);
		public const string ClassInternal = nameof(ClassInternal);
		public const string ClassPrivate = nameof(ClassPrivate);
		public const string ClassProtected = nameof(ClassProtected);
		public const string ClassPublic = nameof(ClassPublic);
		public const string Structure = nameof(Structure);
		public const string StructureInternal = nameof(StructureInternal);
		public const string StructurePrivate = nameof(StructurePrivate);
		public const string StructureProtected = nameof(StructureProtected);
		public const string StructurePublic = nameof(StructurePublic);
		public const string Interface = nameof(Interface);
		public const string InterfaceInternal = nameof(InterfaceInternal);
		public const string InterfacePrivate = nameof(InterfacePrivate);
		public const string InterfaceProtected = nameof(InterfaceProtected);
		public const string InterfacePublic = nameof(InterfacePublic);
		public const string Enumeration = nameof(Enumeration);
		public const string EnumerationInternal = nameof(EnumerationInternal);
		public const string EnumerationPrivate = nameof(EnumerationPrivate);
		public const string EnumerationProtected = nameof(EnumerationProtected);
		public const string EnumerationPublic = nameof(EnumerationPublic);
		public const string EnumerationItem = nameof(EnumerationItem);
		public const string EnumerationItemPrivate = nameof(EnumerationItemPrivate);
		public const string EnumerationItemPublic = nameof(EnumerationItemPublic);
		public const string EnumerationItemFamily = nameof(EnumerationItemFamily);
		public const string EnumerationItemAssembly = nameof(EnumerationItemAssembly);
		public const string EnumerationItemFamilyAndAssembly = nameof(EnumerationItemFamilyAndAssembly);
		public const string EnumerationItemFamilyOrAssembly = nameof(EnumerationItemFamilyOrAssembly);
		public const string EnumerationItemCompilerControlled = nameof(EnumerationItemCompilerControlled);
		public const string Module = nameof(Module);
		public const string ModuleInternal = nameof(ModuleInternal);
		public const string ModulePrivate = nameof(ModulePrivate);
		public const string ModuleProtected = nameof(ModuleProtected);
		public const string ModulePublic = nameof(ModulePublic);
		public const string Delegate = nameof(Delegate);
		public const string DelegateInternal = nameof(DelegateInternal);
		public const string DelegatePrivate = nameof(DelegatePrivate);
		public const string DelegateProtected = nameof(DelegateProtected);
		public const string DelegatePublic = nameof(DelegatePublic);
		public const string Constant = nameof(Constant);
		public const string ConstantPrivate = nameof(ConstantPrivate);
		public const string ConstantPublic = nameof(ConstantPublic);
		public const string ConstantFamily = nameof(ConstantFamily);
		public const string ConstantAssembly = nameof(ConstantAssembly);
		public const string ConstantFamilyAndAssembly = nameof(ConstantFamilyAndAssembly);
		public const string ConstantFamilyOrAssembly = nameof(ConstantFamilyOrAssembly);
		public const string ConstantCompilerControlled = nameof(ConstantCompilerControlled);
		public const string Field = nameof(Field);
		public const string FieldPrivate = nameof(FieldPrivate);
		public const string FieldPublic = nameof(FieldPublic);
		public const string FieldFamily = nameof(FieldFamily);
		public const string FieldAssembly = nameof(FieldAssembly);
		public const string FieldFamilyAndAssembly = nameof(FieldFamilyAndAssembly);
		public const string FieldFamilyOrAssembly = nameof(FieldFamilyOrAssembly);
		public const string FieldCompilerControlled = nameof(FieldCompilerControlled);
		public const string ExtensionMethod = nameof(ExtensionMethod);
		public const string Method = nameof(Method);
		public const string MethodPrivate = nameof(MethodPrivate);
		public const string MethodPublic = nameof(MethodPublic);
		public const string MethodFamily = nameof(MethodFamily);
		public const string MethodAssembly = nameof(MethodAssembly);
		public const string MethodFamilyAndAssembly = nameof(MethodFamilyAndAssembly);
		public const string MethodFamilyOrAssembly = nameof(MethodFamilyOrAssembly);
		public const string MethodCompilerControlled = nameof(MethodCompilerControlled);
		public const string Property = nameof(Property);
		public const string PropertyPrivate = nameof(PropertyPrivate);
		public const string PropertyPublic = nameof(PropertyPublic);
		public const string PropertyFamily = nameof(PropertyFamily);
		public const string PropertyAssembly = nameof(PropertyAssembly);
		public const string PropertyFamilyAndAssembly = nameof(PropertyFamilyAndAssembly);
		public const string PropertyFamilyOrAssembly = nameof(PropertyFamilyOrAssembly);
		public const string PropertyCompilerControlled = nameof(PropertyCompilerControlled);
		public const string Event = nameof(Event);
		public const string EventPrivate = nameof(EventPrivate);
		public const string EventPublic = nameof(EventPublic);
		public const string EventFamily = nameof(EventFamily);
		public const string EventAssembly = nameof(EventAssembly);
		public const string EventFamilyAndAssembly = nameof(EventFamilyAndAssembly);
		public const string EventFamilyOrAssembly = nameof(EventFamilyOrAssembly);
		public const string EventCompilerControlled = nameof(EventCompilerControlled);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
