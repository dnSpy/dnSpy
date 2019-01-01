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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Images;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	abstract class DbgValueNodeImageReferenceService {
		public abstract ImageReference GetImageReference(string imageName);
	}

	[Export(typeof(DbgValueNodeImageReferenceService))]
	sealed class DbgValueNodeImageReferenceServiceImpl : DbgValueNodeImageReferenceService {
		readonly Dictionary<string, ImageReference> toImageReference;

		[ImportingConstructor]
		DbgValueNodeImageReferenceServiceImpl() {
			const int TOTAL_COUNT = 110;
			toImageReference = new Dictionary<string, ImageReference>(TOTAL_COUNT, StringComparer.Ordinal) {
				{ PredefinedDbgValueNodeImageNames.Edit, DsImages.Edit },
				{ PredefinedDbgValueNodeImageNames.Information, DsImages.StatusInformation },
				{ PredefinedDbgValueNodeImageNames.Warning, DsImages.StatusWarning },
				{ PredefinedDbgValueNodeImageNames.Error, DsImages.StatusError },
				{ PredefinedDbgValueNodeImageNames.ReturnValue, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.GenericTypeParameter, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.GenericMethodParameter, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.Data, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.Local, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.Parameter, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.Array, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.ArrayElement, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.Exception, DsImages.ExceptionPublic },
				{ PredefinedDbgValueNodeImageNames.This, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.TypeVariables, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.StowedException, DsImages.ExceptionPublic },
				{ PredefinedDbgValueNodeImageNames.ObjectId, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.ObjectAddress, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.EEVariable, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.Pointer, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.DereferencedPointer, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.InstanceMembers, DsImages.ClassPublic },
				{ PredefinedDbgValueNodeImageNames.StaticMembers, DsImages.ClassPublic },
				{ PredefinedDbgValueNodeImageNames.RawView, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.ResultsView, DsImages.MethodPublic },
				{ PredefinedDbgValueNodeImageNames.DynamicView, DsImages.MethodPublic },
				{ PredefinedDbgValueNodeImageNames.DynamicViewElement, DsImages.Property },
				{ PredefinedDbgValueNodeImageNames.ExceptionInternal, DsImages.ExceptionInternal },
				{ PredefinedDbgValueNodeImageNames.ExceptionPrivate, DsImages.ExceptionPrivate },
				{ PredefinedDbgValueNodeImageNames.ExceptionProtected, DsImages.ExceptionProtected },
				{ PredefinedDbgValueNodeImageNames.ExceptionPublic, DsImages.ExceptionPublic },
				{ PredefinedDbgValueNodeImageNames.Class, DsImages.ClassPublic },
				{ PredefinedDbgValueNodeImageNames.ClassInternal, DsImages.ClassInternal },
				{ PredefinedDbgValueNodeImageNames.ClassPrivate, DsImages.ClassPrivate },
				{ PredefinedDbgValueNodeImageNames.ClassProtected, DsImages.ClassProtected },
				{ PredefinedDbgValueNodeImageNames.ClassPublic, DsImages.ClassPublic },
				{ PredefinedDbgValueNodeImageNames.Structure, DsImages.StructurePublic },
				{ PredefinedDbgValueNodeImageNames.StructureInternal, DsImages.StructureInternal },
				{ PredefinedDbgValueNodeImageNames.StructurePrivate, DsImages.StructurePrivate },
				{ PredefinedDbgValueNodeImageNames.StructureProtected, DsImages.StructureProtected },
				{ PredefinedDbgValueNodeImageNames.StructurePublic, DsImages.StructurePublic },
				{ PredefinedDbgValueNodeImageNames.Interface, DsImages.InterfacePublic },
				{ PredefinedDbgValueNodeImageNames.InterfaceInternal, DsImages.InterfaceInternal },
				{ PredefinedDbgValueNodeImageNames.InterfacePrivate, DsImages.InterfacePrivate },
				{ PredefinedDbgValueNodeImageNames.InterfaceProtected, DsImages.InterfaceProtected },
				{ PredefinedDbgValueNodeImageNames.InterfacePublic, DsImages.InterfacePublic },
				{ PredefinedDbgValueNodeImageNames.Enumeration, DsImages.EnumerationPublic },
				{ PredefinedDbgValueNodeImageNames.EnumerationInternal, DsImages.EnumerationInternal },
				{ PredefinedDbgValueNodeImageNames.EnumerationPrivate, DsImages.EnumerationPrivate },
				{ PredefinedDbgValueNodeImageNames.EnumerationProtected, DsImages.EnumerationProtected },
				{ PredefinedDbgValueNodeImageNames.EnumerationPublic, DsImages.EnumerationPublic },
				{ PredefinedDbgValueNodeImageNames.EnumerationItem, DsImages.EnumerationItemPublic },
				{ PredefinedDbgValueNodeImageNames.EnumerationItemPrivate, DsImages.EnumerationItemPrivate },
				{ PredefinedDbgValueNodeImageNames.EnumerationItemPublic, DsImages.EnumerationItemPublic },
				{ PredefinedDbgValueNodeImageNames.EnumerationItemFamily, DsImages.EnumerationItemProtected },
				{ PredefinedDbgValueNodeImageNames.EnumerationItemAssembly, DsImages.EnumerationItemInternal },
				{ PredefinedDbgValueNodeImageNames.EnumerationItemFamilyAndAssembly, DsImages.EnumerationItemInternal },
				{ PredefinedDbgValueNodeImageNames.EnumerationItemFamilyOrAssembly, DsImages.EnumerationItemShortcut },
				{ PredefinedDbgValueNodeImageNames.EnumerationItemCompilerControlled, DsImages.EnumerationItemSealed },
				{ PredefinedDbgValueNodeImageNames.Module, DsImages.ModulePublic },
				{ PredefinedDbgValueNodeImageNames.ModuleInternal, DsImages.ModuleInternal },
				{ PredefinedDbgValueNodeImageNames.ModulePrivate, DsImages.ModulePrivate },
				{ PredefinedDbgValueNodeImageNames.ModuleProtected, DsImages.ModuleProtected },
				{ PredefinedDbgValueNodeImageNames.ModulePublic, DsImages.ModulePublic },
				{ PredefinedDbgValueNodeImageNames.Delegate, DsImages.DelegatePublic },
				{ PredefinedDbgValueNodeImageNames.DelegateInternal, DsImages.DelegateInternal },
				{ PredefinedDbgValueNodeImageNames.DelegatePrivate, DsImages.DelegatePrivate },
				{ PredefinedDbgValueNodeImageNames.DelegateProtected, DsImages.DelegateProtected },
				{ PredefinedDbgValueNodeImageNames.DelegatePublic, DsImages.DelegatePublic },
				{ PredefinedDbgValueNodeImageNames.Constant, DsImages.ConstantPublic },
				{ PredefinedDbgValueNodeImageNames.ConstantPrivate, DsImages.ConstantPrivate },
				{ PredefinedDbgValueNodeImageNames.ConstantPublic, DsImages.ConstantPublic },
				{ PredefinedDbgValueNodeImageNames.ConstantFamily, DsImages.ConstantProtected },
				{ PredefinedDbgValueNodeImageNames.ConstantAssembly, DsImages.ConstantInternal },
				{ PredefinedDbgValueNodeImageNames.ConstantFamilyAndAssembly, DsImages.ConstantInternal },
				{ PredefinedDbgValueNodeImageNames.ConstantFamilyOrAssembly, DsImages.ConstantShortcut },
				{ PredefinedDbgValueNodeImageNames.ConstantCompilerControlled, DsImages.ConstantSealed },
				{ PredefinedDbgValueNodeImageNames.Field, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.FieldPrivate, DsImages.FieldPrivate },
				{ PredefinedDbgValueNodeImageNames.FieldPublic, DsImages.FieldPublic },
				{ PredefinedDbgValueNodeImageNames.FieldFamily, DsImages.FieldProtected },
				{ PredefinedDbgValueNodeImageNames.FieldAssembly, DsImages.FieldInternal },
				{ PredefinedDbgValueNodeImageNames.FieldFamilyAndAssembly, DsImages.FieldInternal },
				{ PredefinedDbgValueNodeImageNames.FieldFamilyOrAssembly, DsImages.FieldShortcut },
				{ PredefinedDbgValueNodeImageNames.FieldCompilerControlled, DsImages.FieldSealed },
				{ PredefinedDbgValueNodeImageNames.ExtensionMethod, DsImages.ExtensionMethod },
				{ PredefinedDbgValueNodeImageNames.Method, DsImages.MethodPublic },
				{ PredefinedDbgValueNodeImageNames.MethodPrivate, DsImages.MethodPrivate },
				{ PredefinedDbgValueNodeImageNames.MethodPublic, DsImages.MethodPublic },
				{ PredefinedDbgValueNodeImageNames.MethodFamily, DsImages.MethodProtected },
				{ PredefinedDbgValueNodeImageNames.MethodAssembly, DsImages.MethodInternal },
				{ PredefinedDbgValueNodeImageNames.MethodFamilyAndAssembly, DsImages.MethodInternal },
				{ PredefinedDbgValueNodeImageNames.MethodFamilyOrAssembly, DsImages.MethodShortcut },
				{ PredefinedDbgValueNodeImageNames.MethodCompilerControlled, DsImages.MethodSealed },
				{ PredefinedDbgValueNodeImageNames.Property, DsImages.Property },
				{ PredefinedDbgValueNodeImageNames.PropertyPrivate, DsImages.PropertyPrivate },
				{ PredefinedDbgValueNodeImageNames.PropertyPublic, DsImages.Property },
				{ PredefinedDbgValueNodeImageNames.PropertyFamily, DsImages.PropertyProtected },
				{ PredefinedDbgValueNodeImageNames.PropertyAssembly, DsImages.PropertyInternal },
				{ PredefinedDbgValueNodeImageNames.PropertyFamilyAndAssembly, DsImages.PropertyInternal },
				{ PredefinedDbgValueNodeImageNames.PropertyFamilyOrAssembly, DsImages.PropertyShortcut },
				{ PredefinedDbgValueNodeImageNames.PropertyCompilerControlled, DsImages.PropertySealed },
				{ PredefinedDbgValueNodeImageNames.Event, DsImages.EventPublic },
				{ PredefinedDbgValueNodeImageNames.EventPrivate, DsImages.EventPrivate },
				{ PredefinedDbgValueNodeImageNames.EventPublic, DsImages.EventPublic },
				{ PredefinedDbgValueNodeImageNames.EventFamily, DsImages.EventProtected },
				{ PredefinedDbgValueNodeImageNames.EventAssembly, DsImages.EventInternal },
				{ PredefinedDbgValueNodeImageNames.EventFamilyAndAssembly, DsImages.EventInternal },
				{ PredefinedDbgValueNodeImageNames.EventFamilyOrAssembly, DsImages.EventShortcut },
				{ PredefinedDbgValueNodeImageNames.EventCompilerControlled, DsImages.EventSealed }
			};
			Debug.Assert(toImageReference.Count == TOTAL_COUNT);
		}

		public override ImageReference GetImageReference(string imageName) {
			if (imageName == null)
				return ImageReference.None;
			if (toImageReference.TryGetValue(imageName, out var imgRef))
				return imgRef;
			Debug.Fail($"Unknown image name: {imageName}");
			return DsImages.FieldPublic;
		}
	}
}
