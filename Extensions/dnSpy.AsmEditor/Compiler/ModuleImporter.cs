/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;
using dnlib.Threading;
using dnSpy.AsmEditor.Event;
using dnSpy.AsmEditor.Field;
using dnSpy.AsmEditor.Method;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.Property;
using dnSpy.AsmEditor.Types;
using dnSpy.Contracts.AsmEditor.Compiler;

namespace dnSpy.AsmEditor.Compiler {
	[Serializable]
	sealed class ModuleImporterAbortedException : Exception {
	}

	[Flags]
	enum ModuleImporterOptions {
		None = 0,

		/// <summary>
		/// Module and assembly attributes replace target module and assembly attributes
		/// </summary>
		ReplaceModuleAssemblyAttributes			= 0x00000001,

		/// <summary>
		/// Assembly declared security attributes replace target assembly's declared security attributes
		/// </summary>
		ReplaceAssemblyDeclSecurities			= 0x00000002,
	}

	sealed partial class ModuleImporter {
		const string IM0001 = nameof(IM0001);
		const string IM0002 = nameof(IM0002);
		const string IM0003 = nameof(IM0003);
		const string IM0004 = nameof(IM0004);
		const string IM0005 = nameof(IM0005);
		const string IM0006 = nameof(IM0006);
		const string IM0007 = nameof(IM0007);
		const string IM0008 = nameof(IM0008);
		const string IM0009 = nameof(IM0009);
		const string IM0010 = nameof(IM0010);

		public CompilerDiagnostic[] Diagnostics => diagnostics.ToArray();
		public NewImportedType[] NewNonNestedTypes => newNonNestedImportedTypes.ToArray();
		public MergedImportedType[] MergedNonNestedTypes => nonNestedMergedImportedTypes.Where(a => !a.IsEmpty).ToArray();
		public CustomAttribute[] NewAssemblyCustomAttributes { get; private set; }
		public DeclSecurity[] NewAssemblyDeclSecurities { get; private set; }
		public CustomAttribute[] NewModuleCustomAttributes { get; private set; }
		public Resource[] NewResources { get; private set; }

		readonly ModuleDef targetModule;
		readonly bool makeEverythingPublic;
		readonly List<CompilerDiagnostic> diagnostics;
		readonly List<NewImportedType> newNonNestedImportedTypes;
		readonly List<MergedImportedType> nonNestedMergedImportedTypes;
		readonly HashSet<TypeDef> newStateMachineTypes;

		ModuleDef sourceModule;
		readonly Dictionary<TypeDef, ImportedType> oldTypeToNewType;
		readonly Dictionary<ITypeDefOrRef, ImportedType> oldTypeRefToNewType;
		readonly Dictionary<MethodDef, MemberInfo<MethodDef>> oldMethodToNewMethod;
		readonly Dictionary<FieldDef, MemberInfo<FieldDef>> oldFieldToNewField;
		readonly Dictionary<PropertyDef, MemberInfo<PropertyDef>> oldPropertyToNewProperty;
		readonly Dictionary<EventDef, MemberInfo<EventDef>> oldEventToNewEvent;
		readonly Dictionary<object, object> bodyDict;
		readonly Dictionary<ImportedType, ExtraImportedTypeData> toExtraData;
		readonly Dictionary<MethodDef, MethodDef> editedMethodsToFix;
		readonly Dictionary<FieldDef, FieldDef> editedFieldsToFix;
		readonly Dictionary<PropertyDef, PropertyDef> editedPropertiesToFix;
		readonly Dictionary<EventDef, EventDef> editedEventsToFix;
		readonly HashSet<MethodDef> usedMethods;
		readonly HashSet<object> isStub;
		ImportSigComparerOptions importSigComparerOptions;

		struct MemberInfo<T> where T : IMemberDef {
			public T TargetMember { get; }
			public T EditedMember { get; }
			public MemberInfo(T targetMember, T editedMember) {
				TargetMember = targetMember;
				EditedMember = editedMember;
			}
		}

		struct ExtraImportedTypeData {
			/// <summary>
			/// New type in temporary module created by the compiler
			/// </summary>
			public TypeDef CompiledType { get; }
			public ExtraImportedTypeData(TypeDef compiledType) => CompiledType = compiledType;
		}

		const SigComparerOptions SIG_COMPARER_OPTIONS = SigComparerOptions.TypeRefCanReferenceGlobalType | SigComparerOptions.PrivateScopeIsComparable;

		public ModuleImporter(ModuleDef targetModule, bool makeEverythingPublic) {
			this.targetModule = targetModule;
			this.makeEverythingPublic = makeEverythingPublic;
			diagnostics = new List<CompilerDiagnostic>();
			newNonNestedImportedTypes = new List<NewImportedType>();
			nonNestedMergedImportedTypes = new List<MergedImportedType>();
			newStateMachineTypes = new HashSet<TypeDef>();
			oldTypeToNewType = new Dictionary<TypeDef, ImportedType>();
			oldTypeRefToNewType = new Dictionary<ITypeDefOrRef, ImportedType>(TypeEqualityComparer.Instance);
			oldMethodToNewMethod = new Dictionary<MethodDef, MemberInfo<MethodDef>>();
			oldFieldToNewField = new Dictionary<FieldDef, MemberInfo<FieldDef>>();
			oldPropertyToNewProperty = new Dictionary<PropertyDef, MemberInfo<PropertyDef>>();
			oldEventToNewEvent = new Dictionary<EventDef, MemberInfo<EventDef>>();
			bodyDict = new Dictionary<object, object>();
			toExtraData = new Dictionary<ImportedType, ExtraImportedTypeData>();
			editedMethodsToFix = new Dictionary<MethodDef, MethodDef>();
			editedFieldsToFix = new Dictionary<FieldDef, FieldDef>();
			editedPropertiesToFix = new Dictionary<PropertyDef, PropertyDef>();
			editedEventsToFix = new Dictionary<EventDef, EventDef>();
			usedMethods = new HashSet<MethodDef>();
			isStub = new HashSet<object>();
		}

		void AddError(string id, string msg) => diagnostics.Add(new CompilerDiagnostic(CompilerDiagnosticSeverity.Error, msg, id, null, null));

		void AddErrorThrow(string id, string msg) {
			AddError(id, msg);
			throw new ModuleImporterAbortedException();
		}

		ModuleDefMD LoadModule(byte[] rawGeneratedModule, DebugFileResult debugFile) {
			var opts = new ModuleCreationOptions();

			switch (debugFile.Format) {
			case DebugFileFormat.None:
				break;

			case DebugFileFormat.Pdb:
				opts.PdbFileOrData = debugFile.RawFile;
				break;

			case DebugFileFormat.PortablePdb:
				Debug.Fail("Portable PDB isn't supported yet");//TODO:
				break;

			case DebugFileFormat.Embedded:
				Debug.Fail("Embedded Portable PDB isn't supported yet");//TODO:
				break;

			default:
				Debug.Fail($"Unknown debug file format: {debugFile.Format}");
				break;
			}

			return ModuleDefMD.Load(rawGeneratedModule, opts);
		}

		static void RemoveDuplicates(List<CustomAttribute> attributes, string fullName) {
			bool found = false;
			for (int i = 0; i < attributes.Count; i++) {
				var ca = attributes[i];
				if (ca.TypeFullName != fullName)
					continue;
				if (!found) {
					found = true;
					continue;
				}
				attributes.RemoveAt(i);
				i--;
			}
		}

		static void RemoveDuplicateSecurityPermissionAttributes(List<DeclSecurity> secAttrs) {
			foreach (var declSec in secAttrs) {
				if (declSec.Action != SecurityAction.RequestMinimum)
					continue;
				bool found = false;
				var list = declSec.SecurityAttributes;
				for (int i = 0; i < list.Count; i++) {
					var ca = list[i];
					if (ca.TypeFullName != "System.Security.Permissions.SecurityPermissionAttribute")
						continue;
					if (!found) {
						found = true;
						continue;
					}
					list.RemoveAt(i);
					i--;
				}
			}
		}

		void InitializeTypesAndMethods() {
			// Step 1: Initialize all definitions
			InitializeTypesStep1(oldTypeToNewType.Values.OfType<NewImportedType>());
			InitializeTypesStep1(oldTypeToNewType.Values.OfType<MergedImportedType>());

			// Step 2: import the rest, which depend on defs having been initialized,
			// eg. ca.Constructor could be a MethodDef
			InitializeTypesStep2(oldTypeToNewType.Values.OfType<NewImportedType>());
			InitializeTypesStep2(oldTypeToNewType.Values.OfType<MergedImportedType>());

			InitializeTypesMethods(oldTypeToNewType.Values.OfType<NewImportedType>());
			InitializeTypesMethods(oldTypeToNewType.Values.OfType<MergedImportedType>());

			UpdateEditedMethods();
			foreach (var kv in editedFieldsToFix) {
				var importedType = (MergedImportedType)oldTypeToNewType[kv.Key.DeclaringType];
				var info = oldFieldToNewField[kv.Key];
				Debug.Assert(info.TargetMember.Module == targetModule);
				importedType.EditedFields.Add(new EditedField(info.TargetMember, CreateFieldDefOptions(info.EditedMember, info.TargetMember)));
			}
			foreach (var kv in editedPropertiesToFix) {
				var importedType = (MergedImportedType)oldTypeToNewType[kv.Key.DeclaringType];
				var info = oldPropertyToNewProperty[kv.Key];
				Debug.Assert(info.TargetMember.Module == targetModule);
				importedType.EditedProperties.Add(new EditedProperty(info.TargetMember, CreatePropertyDefOptions(info.EditedMember)));
			}
			foreach (var kv in editedEventsToFix) {
				var importedType = (MergedImportedType)oldTypeToNewType[kv.Key.DeclaringType];
				var info = oldEventToNewEvent[kv.Key];
				Debug.Assert(info.TargetMember.Module == targetModule);
				importedType.EditedEvents.Add(new EditedEvent(info.TargetMember, CreateEventDefOptions(info.EditedMember)));
			}
		}

		void ImportResources() {
			var newResources = new List<Resource>(sourceModule.Resources.Count);
			foreach (var resource in sourceModule.Resources) {
				var newResource = Import(resource);
				if (newResource == null)
					continue;
				newResources.Add(newResource);
			}

			//TODO: Need to rename some resources if the owner type has been renamed, this also
			//		requires fixing strings in method bodies.

			NewResources = newResources.ToArray();
		}

		Resource Import(Resource resource) {
			if (resource is EmbeddedResource er)
				return Import(er);

			if (resource is AssemblyLinkedResource alr)
				return Import(alr);

			if (resource is LinkedResource lr)
				return Import(lr);

			Debug.Fail($"Unknown resource type: {resource?.GetType()}");
			return null;
		}

		EmbeddedResource Import(EmbeddedResource resource) =>
			new EmbeddedResource(resource.Name, resource.GetResourceData(), resource.Attributes);

		AssemblyLinkedResource Import(AssemblyLinkedResource resource) =>
			new AssemblyLinkedResource(resource.Name, resource.Assembly?.ToAssemblyRef(), resource.Attributes);

		LinkedResource Import(LinkedResource resource) =>
			new LinkedResource(resource.Name, Import(resource.File), resource.Attributes) { Hash = resource.Hash };

		FileDef Import(FileDef file) {
			var createdFile = targetModule.UpdateRowId(new FileDefUser(file.Name, file.Flags, file.HashValue));
			ImportCustomAttributes(createdFile, file);
			return createdFile;
		}

		/// <summary>
		/// Imports everything into the target module. All global members are merged and possibly renamed.
		/// All non-nested types are renamed if a type with the same name exists in the target module.
		/// </summary>
		/// <param name="rawGeneratedModule">Raw bytes of compiled assembly</param>
		/// <param name="debugFile">Debug file</param>
		/// <param name="options">Options</param>
		public void Import(byte[] rawGeneratedModule, DebugFileResult debugFile, ModuleImporterOptions options) {
			SetSourceModule(LoadModule(rawGeneratedModule, debugFile));

			AddGlobalTypeMembers(sourceModule.GlobalType);
			foreach (var type in sourceModule.Types) {
				if (type.IsGlobalModuleType)
					continue;
				newNonNestedImportedTypes.Add(CreateNewImportedType(type, targetModule.Types));
			}
			InitializeTypesAndMethods();

			if ((options & ModuleImporterOptions.ReplaceModuleAssemblyAttributes) != 0) {
				var attributes = new List<CustomAttribute>();
				ImportCustomAttributes(attributes, sourceModule);
				// The compiler adds [UnverifiableCode] causing a duplicate attribute
				RemoveDuplicates(attributes, "System.Security.UnverifiableCodeAttribute");
				NewModuleCustomAttributes = attributes.ToArray();
				var asm = sourceModule.Assembly;
				if (asm != null) {
					attributes.Clear();
					ImportCustomAttributes(attributes, asm);
					NewAssemblyCustomAttributes = attributes.ToArray();
				}
			}

			if ((options & ModuleImporterOptions.ReplaceAssemblyDeclSecurities) != 0) {
				var asm = sourceModule.Assembly;
				if (asm != null) {
					var declSecs = new List<DeclSecurity>();
					ImportDeclSecurities(declSecs, asm);
					// The C# compiler always adds this security attribute:
					//	SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)
					RemoveDuplicateSecurityPermissionAttributes(declSecs);
					NewAssemblyDeclSecurities = declSecs.ToArray();
				}
			}

			ImportResources();
			SetSourceModule(null);
		}

		/// <summary>
		/// Imports all new types and methods. Members that only exist in <paramref name="targetType"/>
		/// are considered deleted. Members that exist in both types are merged.
		/// </summary>
		/// <param name="rawGeneratedModule">Raw bytes of compiled assembly</param>
		/// <param name="debugFile">Debug file</param>
		/// <param name="targetType">Original type that was edited</param>
		public void Import(byte[] rawGeneratedModule, DebugFileResult debugFile, TypeDef targetType) {
			if (targetType.Module != targetModule)
				throw new InvalidOperationException();
			if (targetType.DeclaringType != null)
				throw new ArgumentException("Type must not be nested");
			SetSourceModule(LoadModule(rawGeneratedModule, debugFile));

			var newType = FindSourceType(targetType);
			if (newType.DeclaringType != null)
				throw new ArgumentException("Type must not be nested");

			if (!newType.IsGlobalModuleType)
				AddGlobalTypeMembers(sourceModule.GlobalType);
			foreach (var type in sourceModule.Types) {
				if (type.IsGlobalModuleType)
					continue;
				if (type == newType)
					continue;
				newNonNestedImportedTypes.Add(CreateNewImportedType(type, targetModule.Types));
			}
			nonNestedMergedImportedTypes.Add(MergeEditedTypes(newType, targetType));
			InitializeTypesAndMethods();

			ImportResources();
			SetSourceModule(null);
		}

		TypeDef FindSourceType(TypeDef targetType) {
			var newType = sourceModule.Find(targetType.Module.Import(targetType));
			if (newType != null)
				return newType;

			AddErrorThrow(IM0010, string.Format(dnSpy_AsmEditor_Resources.ERR_IM_CouldNotFindEditedType, targetType));
			throw new InvalidOperationException();
		}

		struct ExistingMember<T> where T : IMemberDef {
			/// <summary>Compiled member</summary>
			public T CompiledMember { get; }
			/// <summary>Original member that exists in the target module</summary>
			public T TargetMember { get; }
			public ExistingMember(T compiledMember, T targetMember) {
				CompiledMember = compiledMember;
				TargetMember = targetMember;
			}
		}

		abstract class MemberDiff<T> : IEqualityComparer<T> where T : IMemberDef {
			public List<T> Deleted { get; } = new List<T>();
			public List<T> New { get; } = new List<T>();
			public List<ExistingMember<T>> Existing { get; } = new List<ExistingMember<T>>();

			public void Initialize(TypeDef newType, TypeDef targetType, MergedImportedType mergedImportedType) {
				var newMembersDict = new Dictionary<T, T>(this);
				var targetMembersDict = new Dictionary<T, T>(this);
				foreach (var m in GetMembers(newType))
					newMembersDict[m] = m;
				foreach (var m in GetMembers(targetType))
					targetMembersDict[m] = m;
				if (newMembersDict.Count != GetMembers(newType).Count)
					throw new InvalidOperationException();
				if (targetMembersDict.Count != GetMembers(targetType).Count)
					throw new InvalidOperationException();

				foreach (var newMember in GetMembers(newType)) {
					if (targetMembersDict.TryGetValue(newMember, out var targetMember))
						Existing.Add(new ExistingMember<T>(newMember, targetMember));
					else
						New.Add(newMember);
				}

				Deleted.AddRange(targetMembersDict.Keys.Where(a => !newMembersDict.ContainsKey(a)));
			}
			protected abstract IList<T> GetMembers(TypeDef type);
			public abstract bool Equals(T x, T y);
			public abstract int GetHashCode(T obj);
		}

		sealed class FieldMemberDiff : MemberDiff<FieldDef> {
			readonly ImportFieldEqualityComparer comparer;
			public FieldMemberDiff(ImportSigComparerOptions importSigComparerOptions, ModuleDef targetModule) => comparer = new ImportFieldEqualityComparer(new ImportSigComparer(importSigComparerOptions, SIG_COMPARER_OPTIONS, targetModule));
			protected override IList<FieldDef> GetMembers(TypeDef type) => type.Fields;
			public override bool Equals(FieldDef x, FieldDef y) => comparer.Equals(x, y);
			public override int GetHashCode(FieldDef obj) => comparer.GetHashCode(obj);
		}

		sealed class MethodMemberDiff : MemberDiff<MethodDef> {
			readonly ImportMethodEqualityComparer comparer;
			public MethodMemberDiff(ImportSigComparerOptions importSigComparerOptions, ModuleDef targetModule) => comparer = new ImportMethodEqualityComparer(new ImportSigComparer(importSigComparerOptions, SIG_COMPARER_OPTIONS, targetModule));
			protected override IList<MethodDef> GetMembers(TypeDef type) => type.Methods;
			public override bool Equals(MethodDef x, MethodDef y) => comparer.Equals(x, y);
			public override int GetHashCode(MethodDef obj) => comparer.GetHashCode(obj);
		}

		sealed class PropertyMemberDiff : MemberDiff<PropertyDef> {
			readonly ImportPropertyEqualityComparer comparer;
			public PropertyMemberDiff(ImportSigComparerOptions importSigComparerOptions, ModuleDef targetModule) => comparer = new ImportPropertyEqualityComparer(new ImportSigComparer(importSigComparerOptions, SIG_COMPARER_OPTIONS, targetModule));
			protected override IList<PropertyDef> GetMembers(TypeDef type) => type.Properties;
			public override bool Equals(PropertyDef x, PropertyDef y) => comparer.Equals(x, y);
			public override int GetHashCode(PropertyDef obj) => comparer.GetHashCode(obj);
		}

		sealed class EventMemberDiff : MemberDiff<EventDef> {
			readonly ImportEventEqualityComparer comparer;
			public EventMemberDiff(ImportSigComparerOptions importSigComparerOptions, ModuleDef targetModule) => comparer = new ImportEventEqualityComparer(new ImportSigComparer(importSigComparerOptions, SIG_COMPARER_OPTIONS, targetModule));
			protected override IList<EventDef> GetMembers(TypeDef type) => type.Events;
			public override bool Equals(EventDef x, EventDef y) => comparer.Equals(x, y);
			public override int GetHashCode(EventDef obj) => comparer.GetHashCode(obj);
		}

		MergedImportedType MergeEditedTypes(TypeDef newType, TypeDef targetType) {
			var mergedImportedType = AddMergedImportedType(newType, targetType, MergeKind.Edit);

			InitializeNewStateMachineTypes(newType);

			var fieldDiff = new FieldMemberDiff(importSigComparerOptions, targetModule);
			fieldDiff.Initialize(newType, targetType, mergedImportedType);
			var methodDiff = new MethodMemberDiff(importSigComparerOptions, targetModule);
			methodDiff.Initialize(newType, targetType, mergedImportedType);
			var propertyDiff = new PropertyMemberDiff(importSigComparerOptions, targetModule);
			propertyDiff.Initialize(newType, targetType, mergedImportedType);
			var eventDiff = new EventMemberDiff(importSigComparerOptions, targetModule);
			eventDiff.Initialize(newType, targetType, mergedImportedType);

			mergedImportedType.DeletedFields.AddRange(fieldDiff.Deleted);
			mergedImportedType.DeletedMethods.AddRange(methodDiff.Deleted);
			mergedImportedType.DeletedProperties.AddRange(propertyDiff.Deleted);
			mergedImportedType.DeletedEvents.AddRange(eventDiff.Deleted);

			foreach (var info in fieldDiff.Existing)
				editedFieldsToFix.Add(info.CompiledMember, info.TargetMember);
			foreach (var info in methodDiff.Existing)
				editedMethodsToFix.Add(info.CompiledMember, info.TargetMember);
			foreach (var info in propertyDiff.Existing)
				editedPropertiesToFix.Add(info.CompiledMember, info.TargetMember);
			foreach (var info in eventDiff.Existing)
				editedEventsToFix.Add(info.CompiledMember, info.TargetMember);

			var typeComparer = new TypeEqualityComparer(SigComparerOptions.DontCompareTypeScope);
			var newNestedTypesDict = new Dictionary<TypeDef, TypeDef>(typeComparer);
			var targetTypesDict = new Dictionary<TypeDef, TypeDef>(typeComparer);
			foreach (var t in newType.NestedTypes)
				newNestedTypesDict[t] = t;
			foreach (var t in targetType.NestedTypes)
				targetTypesDict[t] = t;
			if (newNestedTypesDict.Count != newType.NestedTypes.Count)
				throw new InvalidOperationException();
			if (targetTypesDict.Count != targetType.NestedTypes.Count)
				throw new InvalidOperationException();

			var existingTypes = new List<ExistingMember<TypeDef>>();
			var newNestedTypes = new List<TypeDef>();
			var stateMachineTypes = new List<TypeDef>();

			foreach (var nestedType in newType.NestedTypes) {
				// If it's a state machine type, always create a new one.
				if (newStateMachineTypes.Contains(nestedType)) {
					stateMachineTypes.Add(nestedType);
					continue;
				}

				if (targetTypesDict.TryGetValue(nestedType, out var targetNestedType)) {
					existingTypes.Add(new ExistingMember<TypeDef>(nestedType, targetNestedType));
					continue;
				}

				newNestedTypes.Add(nestedType);
			}

			mergedImportedType.DeletedNestedTypes.AddRange(targetTypesDict.Keys.Where(a => !newNestedTypesDict.ContainsKey(a)));

			var usedTypeNames = new UsedTypeNames();
			foreach (var existing in existingTypes) {
				usedTypeNames.Add(existing.CompiledMember);
				mergedImportedType.NewOrExistingNestedTypes.Add(MergeEditedTypes(existing.CompiledMember, existing.TargetMember));
			}

			foreach (var newNestedType in newNestedTypes)
				mergedImportedType.NewOrExistingNestedTypes.Add(CreateNewImportedType(newNestedType, usedTypeNames));
			foreach (var smType in stateMachineTypes)
				mergedImportedType.NewOrExistingNestedTypes.Add(CreateNewImportedType(smType, usedTypeNames));

			return mergedImportedType;
		}

		void InitializeNewStateMachineTypes(TypeDef compiledType) {
			foreach (var method in compiledType.Methods) {
				var smType = StateMachineHelpers.GetStateMachineType(method);
				if (smType != null) {
					Debug.Assert(!newStateMachineTypes.Contains(smType), "Two or more methods share the same state machine type");
					newStateMachineTypes.Add(smType);
				}
			}
		}

		/// <summary>
		/// Imports all new types and methods (compiler generated or created by the user). All new types and members
		/// in the global type are added to the target's global type. All duplicates are renamed.
		/// All removed classes and members in the edited method's type or it's declaring type etc are kept. All
		/// new types and members are added to the target module. Nothing needs to be renamed because a member that
		/// exists in both modules is assumed to be the original member stub.
		/// All the instructions in the edited method are imported, and its impl attributes. Nothing else is imported.
		/// </summary>
		/// <param name="rawGeneratedModule">Raw bytes of compiled assembly</param>
		/// <param name="debugFile">Debug file</param>
		/// <param name="targetMethod">Original method that was edited</param>
		public void Import(byte[] rawGeneratedModule, DebugFileResult debugFile, MethodDef targetMethod) {
			if (targetMethod.Module != targetModule)
				throw new InvalidOperationException();
			SetSourceModule(LoadModule(rawGeneratedModule, debugFile));

			var newMethod = FindSourceMethod(targetMethod);
			var newMethodNonNestedDeclType = newMethod.DeclaringType;
			while (newMethodNonNestedDeclType.DeclaringType != null)
				newMethodNonNestedDeclType = newMethodNonNestedDeclType.DeclaringType;

			AddEditedMethod(newMethod, targetMethod);
			if (!newMethodNonNestedDeclType.IsGlobalModuleType)
				AddGlobalTypeMembers(sourceModule.GlobalType);
			foreach (var type in sourceModule.Types) {
				if (type.IsGlobalModuleType)
					continue;
				if (type == newMethodNonNestedDeclType)
					continue;
				newNonNestedImportedTypes.Add(CreateNewImportedType(type, targetModule.Types));
			}
			InitializeTypesAndMethods();

			ImportResources();
			SetSourceModule(null);
		}

		MethodDef FindSourceMethod(MethodDef targetMethod) {
			var newType = sourceModule.Find(targetMethod.Module.Import(targetMethod.DeclaringType));
			if (newType == null)
				AddErrorThrow(IM0001, string.Format(dnSpy_AsmEditor_Resources.ERR_IM_CouldNotFindMethodType, targetMethod.DeclaringType));

			// Don't check type scopes or we won't be able to find methods with edited nested types.
			const SigComparerOptions comparerFlags = SIG_COMPARER_OPTIONS | SigComparerOptions.DontCompareTypeScope;

			var newMethod = newType.FindMethod(targetMethod.Name, targetMethod.MethodSig, comparerFlags, targetMethod.Module);
			if (newMethod != null)
				return newMethod;

			if (targetMethod.Overrides.Count != 0) {
				var targetOverriddenMethod = targetMethod.Overrides[0].MethodDeclaration;
				var comparer = new SigComparer(comparerFlags, targetModule);
				foreach (var method in newType.Methods) {
					foreach (var o in method.Overrides) {
						if (!comparer.Equals(o.MethodDeclaration, targetOverriddenMethod))
							continue;
						if (!comparer.Equals(o.MethodDeclaration.DeclaringType, targetOverriddenMethod.DeclaringType))
							continue;
						return method;
					}
				}
			}

			AddErrorThrow(IM0002, string.Format(dnSpy_AsmEditor_Resources.ERR_IM_CouldNotFindEditedMethod, targetMethod));
			throw new InvalidOperationException();
		}

		void SetSourceModule(ModuleDef newSourceModule) {
			sourceModule = newSourceModule;
			importSigComparerOptions = newSourceModule == null ? null : new ImportSigComparerOptions(newSourceModule, targetModule);
		}

		FieldDefOptions CreateFieldDefOptions(FieldDef newField, FieldDef targetField) {
			var options = new FieldDefOptions(newField);
			// All fields are made public, so do not copy the access bits
			if (makeEverythingPublic && (options.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public)
				options.Attributes = (options.Attributes & ~FieldAttributes.FieldAccessMask) | (targetField.Attributes & FieldAttributes.FieldAccessMask);
			return options;
		}

		PropertyDefOptions CreatePropertyDefOptions(PropertyDef newProperty) =>
			new PropertyDefOptions(newProperty);

		EventDefOptions CreateEventDefOptions(EventDef newEvent) =>
			new EventDefOptions(newEvent);

		MethodDefOptions CreateMethodDefOptions(MethodDef newMethod, MethodDef targetMethod) {
			var options = new MethodDefOptions(newMethod);
			options.ParamDefs.Clear();
			options.ParamDefs.AddRange(newMethod.ParamDefs.Select(a => Clone(a)));
			options.GenericParameters.Clear();
			options.GenericParameters.AddRange(newMethod.GenericParameters.Select(a => Clone(a)));
			// All methods are made public, so do not copy the access bits
			if (makeEverythingPublic && (options.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public)
				options.Attributes = (options.Attributes & ~MethodAttributes.MemberAccessMask) | (targetMethod.Attributes & MethodAttributes.MemberAccessMask);
			return options;
		}

		ParamDef Clone(ParamDef paramDef) {
			if (paramDef == null)
				return null;
			var importedParamDef = new ParamDefUser(paramDef.Name, paramDef.Sequence, paramDef.Attributes);
			importedParamDef.Rid = paramDef.Rid;
			importedParamDef.CustomAttributes.AddRange(paramDef.CustomAttributes);
			importedParamDef.MarshalType = paramDef.MarshalType;
			importedParamDef.Constant = paramDef.Constant;
			return importedParamDef;
		}

		GenericParam Clone(GenericParam gp) {
			if (gp == null)
				return null;
			var importedGenericParam = new GenericParamUser(gp.Number, gp.Flags, gp.Name);
			importedGenericParam.Rid = gp.Rid;
			importedGenericParam.CustomAttributes.AddRange(gp.CustomAttributes);
			importedGenericParam.Kind = gp.Kind;
			foreach (var gpc in gp.GenericParamConstraints)
				importedGenericParam.GenericParamConstraints.Add(Clone(gpc));
			return importedGenericParam;
		}

		GenericParamConstraint Clone(GenericParamConstraint gpc) {
			if (gpc == null)
				return null;
			var importedGenericParamConstraint = new GenericParamConstraintUser(gpc.Constraint);
			importedGenericParamConstraint.Rid = gpc.Rid;
			importedGenericParamConstraint.CustomAttributes.AddRange(gpc.CustomAttributes);
			return importedGenericParamConstraint;
		}

		void UpdateEditedMethods() {
			foreach (var kv in editedMethodsToFix) {
				var newMethod = kv.Key;
				var info = oldMethodToNewMethod[newMethod];
				Debug.Assert(info.TargetMember.Module == targetModule);
				Debug.Assert(info.TargetMember == kv.Value);

				var importedType = (MergedImportedType)oldTypeToNewType[newMethod.DeclaringType];
				var methodDefOptions = CreateMethodDefOptions(info.EditedMember, info.TargetMember);
				importedType.EditedMethods.Add(new EditedMethod(info.TargetMember, info.EditedMember.Body, methodDefOptions));
			}
		}

		void AddEditedMethod(MethodDef newMethod, MethodDef targetMethod) {
			var newBaseType = newMethod.DeclaringType;
			var targetBaseType = targetMethod.DeclaringType;
			while (newBaseType.DeclaringType != null) {
				if (targetBaseType == null)
					throw new InvalidOperationException();
				newBaseType = newBaseType.DeclaringType;
				targetBaseType = targetBaseType.DeclaringType;
			}
			if (targetBaseType == null || targetBaseType.DeclaringType != null)
				throw new InvalidOperationException();

			var newStateMachineType = StateMachineHelpers.GetStateMachineType(newMethod);
			if (newStateMachineType != null)
				newStateMachineTypes.Add(newStateMachineType);
			nonNestedMergedImportedTypes.Add(AddMergedType(newBaseType, targetBaseType));
			editedMethodsToFix.Add(newMethod, targetMethod);
		}

		MergedImportedType AddMergedType(TypeDef newType, TypeDef targetType) {
			var importedType = AddMergedImportedType(newType, targetType, MergeKind.Merge);

			if (newType.NestedTypes.Count != 0 || targetType.NestedTypes.Count != 0) {
				var typeComparer = new TypeEqualityComparer(SigComparerOptions.DontCompareTypeScope);
				var newTypes = new Dictionary<TypeDef, TypeDef>(typeComparer);
				var targetTypes = new Dictionary<TypeDef, TypeDef>(typeComparer);
				foreach (var t in newType.NestedTypes)
					newTypes[t] = t;
				foreach (var t in targetType.NestedTypes)
					targetTypes[t] = t;
				if (newTypes.Count != newType.NestedTypes.Count)
					throw new InvalidOperationException();
				if (targetTypes.Count != targetType.NestedTypes.Count)
					throw new InvalidOperationException();

				foreach (var nestedTargetType in targetType.NestedTypes) {
					targetTypes.Remove(nestedTargetType);
					if (newTypes.TryGetValue(nestedTargetType, out var nestedNewType)) {
						// If it's a state machine type, it's a new type
						if (!newStateMachineTypes.Contains(nestedNewType)) {
							newTypes.Remove(nestedTargetType);
							var nestedImportedType = AddMergedType(nestedNewType, nestedTargetType);
							importedType.NewOrExistingNestedTypes.Add(nestedImportedType);
						}
					}
					else {
						// The user removed the type, or it was a compiler generated type that
						// was never shown in the decompiled code.
					}
				}
				// Whatever's left are types created by the user or the compiler
				var usedTypeNames = new UsedTypeNames(targetTypes.Keys);
				foreach (var newNestedType in newTypes.Values)
					importedType.NewOrExistingNestedTypes.Add(CreateNewImportedType(newNestedType, usedTypeNames));
			}

			return importedType;
		}

		void AddGlobalTypeMembers(TypeDef newGlobalType) =>
			nonNestedMergedImportedTypes.Add(MergeTypesRename(newGlobalType, targetModule.GlobalType));

		// Adds every member as a new member. If a member exists in the target type, it's renamed.
		// Nested types aren't merged with existing nested types, they're just renamed.
		MergedImportedType MergeTypesRename(TypeDef newType, TypeDef targetType) {
			var mergedImportedType = AddMergedImportedType(newType, targetType, MergeKind.Rename);
			foreach (var nestedType in newType.NestedTypes) {
				var nestedImportedType = CreateNewImportedType(nestedType, targetType.NestedTypes);
				mergedImportedType.NewOrExistingNestedTypes.Add(nestedImportedType);
			}
			return mergedImportedType;
		}

		static bool IsVirtual(PropertyDef property) => property.GetMethod?.IsVirtual == true || property.SetMethod?.IsVirtual == true;
		static bool IsVirtual(EventDef @event) => @event.AddMethod?.IsVirtual == true || @event.RemoveMethod?.IsVirtual == true || @event.InvokeMethod?.IsVirtual == true;

		void RenameMergedMembers(MergedImportedType mergedType) {
			if (mergedType.MergeKind != MergeKind.Rename)
				throw new InvalidOperationException();
			var existingProps = new HashSet<PropertyDef>(new ImportPropertyEqualityComparer(new ImportSigComparer(importSigComparerOptions, SIG_COMPARER_OPTIONS | SigComparerOptions.DontCompareReturnType, targetModule)));
			var existingMethods = new HashSet<MethodDef>(new ImportMethodEqualityComparer(new ImportSigComparer(importSigComparerOptions, SIG_COMPARER_OPTIONS | SigComparerOptions.DontCompareReturnType, targetModule)));
			var existingEventsFields = new HashSet<string>(StringComparer.Ordinal);
			var suggestedNames = new Dictionary<MethodDef, string>();

			foreach (var p in mergedType.TargetType.Properties)
				existingProps.Add(p);
			foreach (var e in mergedType.TargetType.Events)
				existingEventsFields.Add(e.Name);
			foreach (var m in mergedType.TargetType.Methods)
				existingMethods.Add(m);
			foreach (var f in mergedType.TargetType.Fields)
				existingEventsFields.Add(f.Name);

			var compiledType = toExtraData[mergedType].CompiledType;
			foreach (var compiledProp in compiledType.Properties) {
				var newProp = oldPropertyToNewProperty[compiledProp].EditedMember;
				if (!existingProps.Contains(newProp))
					continue;

				if (IsVirtual(compiledProp))
					AddError(IM0006, string.Format(dnSpy_AsmEditor_Resources.ERR_IM_RenamingVirtualPropsIsNotSupported, compiledProp));

				var origName = newProp.Name;
				int counter = 0;
				while (existingProps.Contains(newProp))
					newProp.Name = origName + "_" + (counter++).ToString();
				existingProps.Add(newProp);
				if (newProp.GetMethod != null)
					suggestedNames[newProp.GetMethod] = "get_" + newProp.Name;
				if (newProp.SetMethod != null)
					suggestedNames[newProp.SetMethod] = "set_" + newProp.Name;
			}

			foreach (var compiledEvent in compiledType.Events) {
				var newEvent = oldEventToNewEvent[compiledEvent].EditedMember;
				if (!existingEventsFields.Contains(newEvent.Name))
					continue;

				if (IsVirtual(compiledEvent))
					AddError(IM0007, string.Format(dnSpy_AsmEditor_Resources.ERR_IM_RenamingVirtualEventsIsNotSupported, compiledEvent));

				var origName = newEvent.Name;
				int counter = 0;
				while (existingEventsFields.Contains(newEvent.Name))
					newEvent.Name = origName + "_" + (counter++).ToString();
				existingEventsFields.Add(newEvent.Name);
				if (newEvent.AddMethod != null)
					suggestedNames[newEvent.AddMethod] = "add_" + newEvent.Name;
				if (newEvent.RemoveMethod != null)
					suggestedNames[newEvent.RemoveMethod] = "remove_" + newEvent.Name;
				if (newEvent.InvokeMethod != null)
					suggestedNames[newEvent.InvokeMethod] = "raise_" + newEvent.Name;
			}

			foreach (var compiledMethod in compiledType.Methods) {
				var newMethod = oldMethodToNewMethod[compiledMethod].EditedMember;

				suggestedNames.TryGetValue(newMethod, out string suggestedName);

				if (suggestedName == null && !existingMethods.Contains(newMethod))
					continue;

				if (compiledMethod.IsVirtual)
					AddError(IM0008, string.Format(dnSpy_AsmEditor_Resources.ERR_IM_RenamingVirtualMethodsIsNotSupported, compiledMethod));

				string baseName = suggestedName ?? newMethod.Name;
				int counter = 0;
				newMethod.Name = baseName;
				while (existingMethods.Contains(newMethod))
					newMethod.Name = baseName + "_" + (counter++).ToString();
				existingMethods.Add(newMethod);
			}

			foreach (var compiledField in compiledType.Fields) {
				var newField = oldFieldToNewField[compiledField].EditedMember;
				if (!existingEventsFields.Contains(newField.Name))
					continue;

				var origName = newField.Name;
				int counter = 0;
				while (existingEventsFields.Contains(newField.Name))
					newField.Name = origName + "_" + (counter++).ToString();
				existingEventsFields.Add(newField.Name);
			}
		}

		struct TypeName : IEquatable<TypeName> {
			readonly UTF8String ns;
			readonly UTF8String name;

			public TypeName(TypeDef type)
				: this(type.Namespace, type.Name) {
			}

			public TypeName(UTF8String ns, UTF8String name) {
				this.ns = ns ?? UTF8String.Empty;
				this.name = name ?? UTF8String.Empty;
			}

			public bool Equals(TypeName other) =>
				UTF8String.Equals(ns, other.ns) &&
				UTF8String.Equals(name, other.name);

			public override bool Equals(object obj) => obj is TypeName && Equals((TypeName)obj);

			public override int GetHashCode() =>
				UTF8String.GetHashCode(ns) ^
				UTF8String.GetHashCode(name);

			public override string ToString() {
				if (UTF8String.IsNullOrEmpty(ns))
					return name;
				return ns + "." + name;
			}
		}

		sealed class TypeNames {
			readonly HashSet<TypeName> names;

			public TypeNames() => names = new HashSet<TypeName>();

			public TypeNames(IEnumerable<TypeDef> existingTypes) {
				names = new HashSet<TypeName>();
				foreach (var t in existingTypes)
					names.Add(new TypeName(t.Namespace, t.Name));
			}

			public bool Contains(UTF8String ns, UTF8String name) =>
				names.Contains(new TypeName(ns, name));

			public bool Contains(TypeDef type) =>
				names.Contains(new TypeName(type.Namespace, type.Name));

			public void Add(UTF8String ns, UTF8String name) =>
				names.Add(new TypeName(ns, name));

			public void Add(TypeDef type) =>
				names.Add(new TypeName(type.Namespace, type.Name));
		}

		sealed class UsedTypeNames {
			readonly TypeNames typeNames;

			public UsedTypeNames() => typeNames = new TypeNames();

			public UsedTypeNames(IEnumerable<TypeDef> existingTypes) => typeNames = new TypeNames(existingTypes);

			public UTF8String GetNewName(TypeDef type) {
				var ns = type.Namespace;
				var name = type.Name;
				for (int counter = 0; ; counter++) {
					if (!typeNames.Contains(ns, name))
						break;
					// It's prepended because generic types have a `<number> appended to the name,
					// which they still should have after the rename.
					name = "__" + counter.ToString() + "__" + type.Name.String;
				}
				typeNames.Add(ns, name);
				return name;
			}

			public void Add(TypeDef type) =>
				typeNames.Add(type);
		}

		NewImportedType CreateNewImportedType(TypeDef newType, IList<TypeDef> existingTypes) =>
			CreateNewImportedType(newType, new UsedTypeNames(existingTypes));

		NewImportedType CreateNewImportedType(TypeDef newType, UsedTypeNames usedTypeNames) {
			var name = usedTypeNames.GetNewName(newType);
			var importedType = AddNewImportedType(newType, name);
			AddNewNestedTypes(newType);
			return importedType;
		}

		void AddNewNestedTypes(TypeDef newType) {
			if (newType.NestedTypes.Count == 0)
				return;
			var stack = new Stack<TypeDef>();
			foreach (var t in newType.NestedTypes)
				stack.Push(t);
			while (stack.Count > 0) {
				var newNestedType = stack.Pop();
				var importedNewNestedType = AddNewImportedType(newNestedType, newNestedType.Name);
				foreach (var t in newNestedType.NestedTypes)
					stack.Push(t);
			}
		}

		MergedImportedType AddMergedImportedType(TypeDef compiledType, TypeDef targetType, MergeKind mergeKind) {
			var importedType = new MergedImportedType(targetType, mergeKind);
			toExtraData.Add(importedType, new ExtraImportedTypeData(compiledType));
			oldTypeToNewType.Add(compiledType, importedType);
			if (!compiledType.IsGlobalModuleType)
				oldTypeRefToNewType.Add(compiledType, importedType);
			return importedType;
		}

		NewImportedType AddNewImportedType(TypeDef type, UTF8String name) {
			var createdType = targetModule.UpdateRowId(new TypeDefUser(type.Namespace, name) { Attributes = type.Attributes });
			var importedType = new NewImportedType(createdType);
			toExtraData.Add(importedType, new ExtraImportedTypeData(type));
			oldTypeToNewType.Add(type, importedType);
			oldTypeRefToNewType.Add(type, importedType);
			return importedType;
		}

		void InitializeTypesStep1(IEnumerable<NewImportedType> importedTypes) {
			foreach (var importedType in importedTypes) {
				var compiledType = toExtraData[importedType].CompiledType;
				foreach (var field in compiledType.Fields)
					Create(field);
				foreach (var method in compiledType.Methods)
					Create(method);
				foreach (var prop in compiledType.Properties)
					Create(prop);
				foreach (var evt in compiledType.Events)
					Create(evt);
			}
		}

		void InitializeTypesStep2(IEnumerable<NewImportedType> importedTypes) {
			foreach (var importedType in importedTypes) {
				var compiledType = toExtraData[importedType].CompiledType;
				importedType.TargetType.BaseType = Import(compiledType.BaseType);
				ImportCustomAttributes(importedType.TargetType, compiledType);
				ImportDeclSecurities(importedType.TargetType, compiledType);
				importedType.TargetType.ClassLayout = Import(compiledType.ClassLayout);
				foreach (var genericParam in compiledType.GenericParameters)
					importedType.TargetType.GenericParameters.Add(Import(genericParam));
				foreach (var iface in compiledType.Interfaces)
					importedType.TargetType.Interfaces.Add(Import(iface));
				foreach (var nestedType in compiledType.NestedTypes)
					importedType.TargetType.NestedTypes.Add(oldTypeToNewType[nestedType].TargetType);

				foreach (var field in compiledType.Fields)
					importedType.TargetType.Fields.Add(Initialize(field));
				foreach (var method in compiledType.Methods)
					importedType.TargetType.Methods.Add(Initialize(method));
				foreach (var prop in compiledType.Properties)
					importedType.TargetType.Properties.Add(Initialize(prop));
				foreach (var evt in compiledType.Events)
					importedType.TargetType.Events.Add(Initialize(evt));
			}
		}

		// Adds all methods of existing properties and events of a merged type to make sure
		// the methods aren't accidentally used twice. Could happen if the compiler (eg. mcs)
		// doesn't set the PropertyDef.Type's HasThis flag even if it's an instance property.
		// Roslyn will set this flag, so our comparison would fail to match the two (now different)
		// properties. This comparison has since been fixed.
		void AddUsedMethods(ImportedType importedType) {
			foreach (var p in importedType.TargetType.Properties) {
				foreach (var m in p.GetMethods)
					usedMethods.Add(m);
				foreach (var m in p.SetMethods)
					usedMethods.Add(m);
				foreach (var m in p.OtherMethods)
					usedMethods.Add(m);
			}
			foreach (var p in importedType.TargetType.Events) {
				if (p.AddMethod != null)
					usedMethods.Add(p.AddMethod);
				if (p.InvokeMethod != null)
					usedMethods.Add(p.InvokeMethod);
				if (p.RemoveMethod != null)
					usedMethods.Add(p.RemoveMethod);
				foreach (var m in p.OtherMethods)
					usedMethods.Add(m);
			}
		}

		void InitializeTypesStep1(IEnumerable<MergedImportedType> importedTypes) {
			var memberDict = new MemberLookup(new ImportSigComparer(importSigComparerOptions, 0, targetModule));
			foreach (var importedType in importedTypes) {
				var compiledType = toExtraData[importedType].CompiledType;

				if (importedType.MergeKind == MergeKind.Rename) {
					// All dupes are assumed to be new members and they're all renamed

					foreach (var field in compiledType.Fields)
						Create(field);
					foreach (var method in compiledType.Methods)
						Create(method);
					foreach (var prop in compiledType.Properties)
						Create(prop);
					foreach (var evt in compiledType.Events)
						Create(evt);
					AddUsedMethods(importedType);
				}
				else if (importedType.MergeKind == MergeKind.Merge) {
					// Duplicate members are assumed to be original members (stubs) and
					// we should just redirect refs to them to the original members.

					memberDict.Initialize(importedType.TargetType);

					foreach (var compiledField in compiledType.Fields) {
						FieldDef targetField;
						if ((targetField = memberDict.FindField(compiledField)) != null) {
							memberDict.Remove(targetField);
							isStub.Add(compiledField);
							isStub.Add(targetField);
							var editedField = targetModule.UpdateRowId(new FieldDefUser(targetField.Name));
							oldFieldToNewField.Add(compiledField, new MemberInfo<FieldDef>(targetField, editedField));
						}
						else
							Create(compiledField);
					}
					foreach (var compiledMethod in compiledType.Methods) {
						MethodDef targetMethod;
						if ((targetMethod = memberDict.FindMethod(compiledMethod)) != null || editedMethodsToFix.TryGetValue(compiledMethod, out targetMethod)) {
							memberDict.Remove(targetMethod);
							isStub.Add(compiledMethod);
							isStub.Add(targetMethod);
							var editedMethod = targetModule.UpdateRowId(new MethodDefUser(targetMethod.Name));
							oldMethodToNewMethod.Add(compiledMethod, new MemberInfo<MethodDef>(targetMethod, editedMethod));
						}
						else
							Create(compiledMethod);
					}
					foreach (var compiledProperty in compiledType.Properties) {
						PropertyDef targetProperty;
						if ((targetProperty = memberDict.FindProperty(compiledProperty)) != null) {
							memberDict.Remove(targetProperty);
							isStub.Add(compiledProperty);
							isStub.Add(targetProperty);
							var editedProperty = targetModule.UpdateRowId(new PropertyDefUser(targetProperty.Name));
							oldPropertyToNewProperty.Add(compiledProperty, new MemberInfo<PropertyDef>(targetProperty, editedProperty));
						}
						else
							Create(compiledProperty);
					}
					foreach (var compiledEvent in compiledType.Events) {
						EventDef targetEvent;
						if ((targetEvent = memberDict.FindEvent(compiledEvent)) != null) {
							memberDict.Remove(targetEvent);
							isStub.Add(compiledEvent);
							isStub.Add(targetEvent);
							var editedEvent = targetModule.UpdateRowId(new EventDefUser(targetEvent.Name));
							oldEventToNewEvent.Add(compiledEvent, new MemberInfo<EventDef>(targetEvent, editedEvent));
						}
						else
							Create(compiledEvent);
					}
				}
				else if (importedType.MergeKind == MergeKind.Edit) {
					foreach (var compiledField in compiledType.Fields) {
						if (editedFieldsToFix.TryGetValue(compiledField, out var targetField)) {
							var editedField = targetModule.UpdateRowId(new FieldDefUser(targetField.Name));
							oldFieldToNewField.Add(compiledField, new MemberInfo<FieldDef>(targetField, editedField));
						}
						else
							Create(compiledField);
					}
					foreach (var compiledMethod in compiledType.Methods) {
						if (editedMethodsToFix.TryGetValue(compiledMethod, out var targetMethod)) {
							var editedMethod = targetModule.UpdateRowId(new MethodDefUser(targetMethod.Name));
							oldMethodToNewMethod.Add(compiledMethod, new MemberInfo<MethodDef>(targetMethod, editedMethod));
						}
						else
							Create(compiledMethod);
					}
					foreach (var compiledProperty in compiledType.Properties) {
						if (editedPropertiesToFix.TryGetValue(compiledProperty, out var targetProperty)) {
							var editedProperty = targetModule.UpdateRowId(new PropertyDefUser(targetProperty.Name));
							oldPropertyToNewProperty.Add(compiledProperty, new MemberInfo<PropertyDef>(targetProperty, editedProperty));
						}
						else
							Create(compiledProperty);
					}
					foreach (var compiledEvent in compiledType.Events) {
						if (editedEventsToFix.TryGetValue(compiledEvent, out var targetEvent)) {
							var editedEvent = targetModule.UpdateRowId(new EventDefUser(targetEvent.Name));
							oldEventToNewEvent.Add(compiledEvent, new MemberInfo<EventDef>(targetEvent, editedEvent));
						}
						else
							Create(compiledEvent);
					}
				}
				else
					throw new InvalidOperationException();
			}
		}

		void Initialize(TypeDef compiledType, TypeDef targetType, TypeDefOptions options) {
			options.Attributes = compiledType.Attributes;

			// All types are made public, so do not copy the access bits
			var publicValue = targetType.DeclaringType == null ? TypeAttributes.Public : TypeAttributes.NestedPublic;
			if (makeEverythingPublic && (options.Attributes & TypeAttributes.VisibilityMask) == publicValue)
				options.Attributes = (options.Attributes & ~TypeAttributes.VisibilityMask) | (targetType.Attributes & TypeAttributes.VisibilityMask);

			options.Namespace = compiledType.Namespace;
			options.Name = compiledType.Name;
			options.PackingSize = compiledType.ClassLayout?.PackingSize;
			options.ClassSize = compiledType.ClassLayout?.ClassSize;
			options.BaseType = Import(compiledType.BaseType);
			options.CustomAttributes.Clear();
			ImportCustomAttributes(options.CustomAttributes, compiledType);
			options.DeclSecurities.Clear();
			ImportDeclSecurities(options.DeclSecurities, compiledType);
			options.GenericParameters.Clear();
			foreach (var genericParam in compiledType.GenericParameters)
				options.GenericParameters.Add(Import(genericParam));
			options.Interfaces.Clear();
			foreach (var ifaceImpl in compiledType.Interfaces)
				options.Interfaces.Add(Import(ifaceImpl));
		}

		void InitializeTypesStep2(IEnumerable<MergedImportedType> importedTypes) {
			var memberDict = new MemberLookup(new ImportSigComparer(importSigComparerOptions, 0, targetModule));
			foreach (var importedType in importedTypes) {
				var compiledType = toExtraData[importedType].CompiledType;

				Initialize(compiledType, importedType.TargetType, importedType.NewTypeDefOptions);

				if (importedType.MergeKind == MergeKind.Rename) {
					// All dupes are assumed to be new members and they're all renamed

					foreach (var field in compiledType.Fields)
						importedType.NewFields.Add(Initialize(field));
					foreach (var method in compiledType.Methods)
						importedType.NewMethods.Add(Initialize(method));
					foreach (var prop in compiledType.Properties)
						importedType.NewProperties.Add(Initialize(prop));
					foreach (var evt in compiledType.Events)
						importedType.NewEvents.Add(Initialize(evt));

					RenameMergedMembers(importedType);
				}
				else if (importedType.MergeKind == MergeKind.Merge) {
					// Duplicate members are assumed to be original members (stubs) and
					// we should just redirect refs to them to the original members.

					memberDict.Initialize(importedType.TargetType);

					foreach (var compiledField in compiledType.Fields) {
						FieldDef targetField;
						if ((targetField = memberDict.FindField(compiledField)) != null) {
							Initialize(compiledField);
							memberDict.Remove(targetField);
						}
						else
							importedType.NewFields.Add(Initialize(compiledField));
					}
					foreach (var compiledMethod in compiledType.Methods) {
						MethodDef targetMethod;
						if ((targetMethod = memberDict.FindMethod(compiledMethod)) != null || editedMethodsToFix.TryGetValue(compiledMethod, out targetMethod)) {
							Initialize(compiledMethod);
							memberDict.Remove(targetMethod);
						}
						else
							importedType.NewMethods.Add(Initialize(compiledMethod));
					}
					foreach (var compiledProperty in compiledType.Properties) {
						PropertyDef targetProperty;
						if ((targetProperty = memberDict.FindProperty(compiledProperty)) != null) {
							Initialize(compiledProperty);
							memberDict.Remove(targetProperty);
						}
						else
							importedType.NewProperties.Add(Initialize(compiledProperty));
					}
					foreach (var compiledEvent in compiledType.Events) {
						EventDef targetEvent;
						if ((targetEvent = memberDict.FindEvent(compiledEvent)) != null) {
							Initialize(compiledEvent);
							memberDict.Remove(targetEvent);
						}
						else
							importedType.NewEvents.Add(Initialize(compiledEvent));
					}
				}
				else if (importedType.MergeKind == MergeKind.Edit) {
					foreach (var compiledField in compiledType.Fields) {
						if (editedFieldsToFix.TryGetValue(compiledField, out var targetField))
							Initialize(compiledField);
						else
							importedType.NewFields.Add(Initialize(compiledField));
					}
					foreach (var compiledMethod in compiledType.Methods) {
						if (editedMethodsToFix.TryGetValue(compiledMethod, out var targetMethod))
							Initialize(compiledMethod);
						else
							importedType.NewMethods.Add(Initialize(compiledMethod));
					}
					foreach (var compiledProperty in compiledType.Properties) {
						if (editedPropertiesToFix.TryGetValue(compiledProperty, out var targetProperty))
							Initialize(compiledProperty);
						else
							importedType.NewProperties.Add(Initialize(compiledProperty));
					}
					foreach (var compiledEvent in compiledType.Events) {
						if (editedEventsToFix.TryGetValue(compiledEvent, out var targetEvent))
							Initialize(compiledEvent);
						else
							importedType.NewEvents.Add(Initialize(compiledEvent));
					}
				}
				else
					throw new InvalidOperationException();
			}
		}

		void InitializeTypesMethods(IEnumerable<NewImportedType> importedTypes) {
			foreach (var importedType in importedTypes) {
				foreach (var compiledMethod in toExtraData[importedType].CompiledType.Methods) {
					var targetMethod = oldMethodToNewMethod[compiledMethod].EditedMember;
					targetMethod.Body = CreateBody(targetMethod, compiledMethod);
				}
			}
		}

		void InitializeTypesMethods(IEnumerable<MergedImportedType> importedTypes) {
			foreach (var importedType in importedTypes) {
				foreach (var compiledMethod in toExtraData[importedType].CompiledType.Methods) {
					var targetInfo = oldMethodToNewMethod[compiledMethod];
					if (editedMethodsToFix.TryGetValue(compiledMethod, out var targetMethod2)) {
						if (targetInfo.TargetMember != targetMethod2)
							throw new InvalidOperationException();
						if (compiledMethod.IsStatic != targetInfo.TargetMember.IsStatic)
							AddError(IM0009, string.Format(dnSpy_AsmEditor_Resources.ERR_IM_AddingRemovingStaticFromEditedMethodNotSupported, targetInfo.TargetMember));
						targetInfo.EditedMember.Body = CreateBody(targetInfo.EditedMember, compiledMethod);
					}
					else if (!isStub.Contains(targetInfo.TargetMember))
						targetInfo.EditedMember.Body = CreateBody(targetInfo.EditedMember, compiledMethod);
				}
			}
		}

		MethodOverride Import(MethodOverride o) => new MethodOverride(Import(o.MethodBody), Import(o.MethodDeclaration));
		IMethodDefOrRef Import(IMethodDefOrRef method) => (IMethodDefOrRef)Import((IMethod)method);

		ITypeDefOrRef Import(ITypeDefOrRef type) {
			if (type == null)
				return null;

			var res = TryGetTypeInTargetModule(type, out var importedType);
			if (res != null)
				return res;

			if (type is TypeRef tr)
				return ImportTypeRefNoModuleChecks(tr, 0);

			if (type is TypeSpec ts)
				return ImportTypeSpec(ts);

			// TypeDefs are already handled elsewhere
			throw new InvalidOperationException();
		}

		TypeRef ImportTypeRefNoModuleChecks(TypeRef tr, int recurseCount) {
			const int MAX_RECURSE_COUNT = 500;
			if (recurseCount >= MAX_RECURSE_COUNT)
				return null;

			var scope = tr.ResolutionScope;
			IResolutionScope importedScope;

			if (scope is TypeRef scopeTypeRef)
				importedScope = ImportTypeRefNoModuleChecks(scopeTypeRef, recurseCount + 1);
			else if (scope is AssemblyRef)
				importedScope = Import((AssemblyRef)scope);
			else if (scope is ModuleRef)
				importedScope = Import((ModuleRef)scope, true);
			else if (scope is ModuleDef) {
				if (scope == targetModule || scope == sourceModule)
					importedScope = targetModule;
				else
					throw new InvalidOperationException();
			}
			else
				throw new InvalidOperationException();

			var importedTypeRef = targetModule.UpdateRowId(new TypeRefUser(targetModule, tr.Namespace, tr.Name, importedScope));
			ImportCustomAttributes(importedTypeRef, tr);
			return importedTypeRef;
		}

		AssemblyRef Import(AssemblyRef asmRef) {
			if (asmRef == null)
				return null;
			var importedAssemblyRef = targetModule.UpdateRowId(new AssemblyRefUser(asmRef.Name, asmRef.Version, asmRef.PublicKeyOrToken, asmRef.Culture));
			ImportCustomAttributes(importedAssemblyRef, asmRef);
			importedAssemblyRef.Attributes = asmRef.Attributes;
			importedAssemblyRef.Hash = asmRef.Hash;
			return importedAssemblyRef;
		}

		TypeSpec ImportTypeSpec(TypeSpec ts) {
			if (ts == null)
				return null;
			var importedTypeSpec = targetModule.UpdateRowId(new TypeSpecUser(Import(ts.TypeSig)));
			ImportCustomAttributes(importedTypeSpec, ts);
			importedTypeSpec.ExtraData = ts.ExtraData;
			return importedTypeSpec;
		}

		TypeDef TryGetTypeInTargetModule(ITypeDefOrRef tdr, out ImportedType importedType) {
			if (tdr == null) {
				importedType = null;
				return null;
			}

			if (tdr is TypeDef td)
				return (importedType = oldTypeToNewType[td]).TargetType;

			if (tdr is TypeRef tr) {
				if (oldTypeRefToNewType.TryGetValue(tr, out var importedTypeTmp))
					return (importedType = importedTypeTmp).TargetType;

				var tr2 = (TypeRef)tr.GetNonNestedTypeRefScope();
				if (IsTarget(tr2.ResolutionScope)) {
					td = targetModule.Find(tr);
					if (td != null) {
						importedType = null;
						return td;
					}

					AddError(IM0003, string.Format(dnSpy_AsmEditor_Resources.ERR_IM_CouldNotFindType, tr));
					importedType = null;
					return null;
				}
				if (IsSource(tr2.ResolutionScope))
					throw new InvalidOperationException();
			}

			importedType = null;
			return null;
		}

		bool IsSourceOrTarget(IResolutionScope scope) => IsSource(scope) || IsTarget(scope);

		bool IsSource(IResolutionScope scope) {
			if (scope is AssemblyRef asmRef)
				return IsSource(asmRef);

			if (scope is ModuleRef modRef)
				return IsSource(modRef);

			return scope == sourceModule;
		}

		bool IsTarget(IResolutionScope scope) {
			if (scope is AssemblyRef asmRef)
				return IsTarget(asmRef);

			if (scope is ModuleRef modRef)
				return IsTarget(modRef);

			return scope == targetModule;
		}

		bool IsSourceOrTarget(AssemblyRef asmRef) => IsSource(asmRef) || IsTarget(asmRef);
		bool IsSource(AssemblyRef asmRef) => AssemblyNameComparer.CompareAll.Equals(asmRef, sourceModule.Assembly);
		bool IsTarget(AssemblyRef asmRef) => AssemblyNameComparer.CompareAll.Equals(asmRef, targetModule.Assembly);

		bool IsSourceOrTarget(ModuleRef modRef) => IsSource(modRef) || IsTarget(modRef);
		bool IsSource(ModuleRef modRef) => StringComparer.OrdinalIgnoreCase.Equals(modRef?.Name, sourceModule.Name);
		bool IsTarget(ModuleRef modRef) => StringComparer.OrdinalIgnoreCase.Equals(modRef?.Name, targetModule.Name);

		TypeDef ImportTypeDef(TypeDef type) => type == null ? null : oldTypeToNewType[type].TargetType;
		MethodDef ImportMethodDef(MethodDef method) => method == null ? null : oldMethodToNewMethod[method].TargetMember;

		TypeSig Import(TypeSig type) {
			if (type == null)
				return null;

			TypeSig result;
			switch (type.ElementType) {
			case ElementType.Void:		result = targetModule.CorLibTypes.Void; break;
			case ElementType.Boolean:	result = targetModule.CorLibTypes.Boolean; break;
			case ElementType.Char:		result = targetModule.CorLibTypes.Char; break;
			case ElementType.I1:		result = targetModule.CorLibTypes.SByte; break;
			case ElementType.U1:		result = targetModule.CorLibTypes.Byte; break;
			case ElementType.I2:		result = targetModule.CorLibTypes.Int16; break;
			case ElementType.U2:		result = targetModule.CorLibTypes.UInt16; break;
			case ElementType.I4:		result = targetModule.CorLibTypes.Int32; break;
			case ElementType.U4:		result = targetModule.CorLibTypes.UInt32; break;
			case ElementType.I8:		result = targetModule.CorLibTypes.Int64; break;
			case ElementType.U8:		result = targetModule.CorLibTypes.UInt64; break;
			case ElementType.R4:		result = targetModule.CorLibTypes.Single; break;
			case ElementType.R8:		result = targetModule.CorLibTypes.Double; break;
			case ElementType.String:	result = targetModule.CorLibTypes.String; break;
			case ElementType.TypedByRef:result = targetModule.CorLibTypes.TypedReference; break;
			case ElementType.I:			result = targetModule.CorLibTypes.IntPtr; break;
			case ElementType.U:			result = targetModule.CorLibTypes.UIntPtr; break;
			case ElementType.Object:	result = targetModule.CorLibTypes.Object; break;
			case ElementType.Ptr:		result = new PtrSig(Import(type.Next)); break;
			case ElementType.ByRef:		result = new ByRefSig(Import(type.Next)); break;
			case ElementType.ValueType: result = CreateClassOrValueType((type as ClassOrValueTypeSig).TypeDefOrRef, true); break;
			case ElementType.Class:		result = CreateClassOrValueType((type as ClassOrValueTypeSig).TypeDefOrRef, false); break;
			case ElementType.Var:		result = new GenericVar((type as GenericVar).Number, ImportTypeDef((type as GenericVar).OwnerType)); break;
			case ElementType.ValueArray:result = new ValueArraySig(Import(type.Next), (type as ValueArraySig).Size); break;
			case ElementType.FnPtr:		result = new FnPtrSig(Import((type as FnPtrSig).Signature)); break;
			case ElementType.SZArray:	result = new SZArraySig(Import(type.Next)); break;
			case ElementType.MVar:		result = new GenericMVar((type as GenericMVar).Number, ImportMethodDef((type as GenericMVar).OwnerMethod)); break;
			case ElementType.CModReqd:	result = new CModReqdSig(Import((type as ModifierSig).Modifier), Import(type.Next)); break;
			case ElementType.CModOpt:	result = new CModOptSig(Import((type as ModifierSig).Modifier), Import(type.Next)); break;
			case ElementType.Module:	result = new ModuleSig((type as ModuleSig).Index, Import(type.Next)); break;
			case ElementType.Sentinel:	result = new SentinelSig(); break;
			case ElementType.Pinned:	result = new PinnedSig(Import(type.Next)); break;

			case ElementType.Array:
				var arraySig = (ArraySig)type;
				var sizes = new List<uint>(arraySig.Sizes);
				var lbounds = new List<int>(arraySig.LowerBounds);
				result = new ArraySig(Import(type.Next), arraySig.Rank, sizes, lbounds);
				break;

			case ElementType.GenericInst:
				var gis = (GenericInstSig)type;
				var genArgs = new List<TypeSig>(gis.GenericArguments.Count);
				foreach (var ga in gis.GenericArguments)
					genArgs.Add(Import(ga));
				result = new GenericInstSig(Import(gis.GenericType) as ClassOrValueTypeSig, genArgs);
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				result = null;
				break;
			}

			return result;
		}

		TypeSig CreateClassOrValueType(ITypeDefOrRef type, bool isValueType) {
			var corLibType = targetModule.CorLibTypes.GetCorLibTypeSig(type);
			if (corLibType != null)
				return corLibType;

			if (isValueType)
				return new ValueTypeSig(Import(type));
			return new ClassSig(Import(type));
		}

		void ImportCustomAttributes(IHasCustomAttribute target, IHasCustomAttribute source) =>
			ImportCustomAttributes(target.CustomAttributes, source);

		void ImportCustomAttributes(IList<CustomAttribute> targetList, IHasCustomAttribute source) {
			foreach (var ca in source.CustomAttributes)
				targetList.Add(Import(ca));
		}

		ICustomAttributeType Import(ICustomAttributeType caType) {
			if (caType is MemberRef mr) {
				Debug.Assert(mr.IsMethodRef);
				if (mr.IsMethodRef)
					return (ICustomAttributeType)Import((IMethod)mr);
				return (ICustomAttributeType)Import((IField)mr);
			}
			if (caType is MethodDef md)
				return oldMethodToNewMethod[md].TargetMember;
			return null;
		}

		CustomAttribute Import(CustomAttribute ca) {
			if (ca == null)
				return null;
			if (ca.IsRawBlob)
				return new CustomAttribute(Import(ca.Constructor), ca.RawData);

			var importedCustomAttribute = new CustomAttribute(Import(ca.Constructor));
			foreach (var arg in ca.ConstructorArguments)
				importedCustomAttribute.ConstructorArguments.Add(Import(arg));
			foreach (var namedArg in ca.NamedArguments)
				importedCustomAttribute.NamedArguments.Add(Import(namedArg));

			return importedCustomAttribute;
		}

		CAArgument Import(CAArgument arg) => new CAArgument(Import(arg.Type), ImportCAValue(arg.Value));

		object ImportCAValue(object value) {
			if (value is CAArgument)
				return Import((CAArgument)value);
			if (value is IList<CAArgument> args) {
				var newArgs = ThreadSafeListCreator.Create<CAArgument>(args.Count);
				foreach (var arg in args)
					newArgs.Add(Import(arg));
				return newArgs;
			}
			if (value is TypeSig)
				return Import((TypeSig)value);
			return value;
		}

		CANamedArgument Import(CANamedArgument namedArg) =>
			new CANamedArgument(namedArg.IsField, Import(namedArg.Type), namedArg.Name, Import(namedArg.Argument));

		void ImportDeclSecurities(IHasDeclSecurity target, IHasDeclSecurity source) =>
			ImportDeclSecurities(target.DeclSecurities, source);

		void ImportDeclSecurities(IList<DeclSecurity> targetList, IHasDeclSecurity source) {
			foreach (var ds in source.DeclSecurities)
				targetList.Add(Import(ds));
		}

		DeclSecurity Import(DeclSecurity ds) {
			if (ds == null)
				return null;

			var importedDeclSecurity = targetModule.UpdateRowId(new DeclSecurityUser());
			ImportCustomAttributes(importedDeclSecurity, ds);
			importedDeclSecurity.Action = ds.Action;
			foreach (var sa in ds.SecurityAttributes)
				importedDeclSecurity.SecurityAttributes.Add(Import(sa));

			return importedDeclSecurity;
		}

		SecurityAttribute Import(SecurityAttribute sa) {
			if (sa == null)
				return null;

			var importedSecurityAttribute = new SecurityAttribute(Import(sa.AttributeType));
			foreach (var namedArg in sa.NamedArguments)
				importedSecurityAttribute.NamedArguments.Add(Import(namedArg));

			return importedSecurityAttribute;
		}

		Constant Import(Constant constant) {
			if (constant == null)
				return null;
			return targetModule.UpdateRowId(new ConstantUser(constant.Value, constant.Type));
		}

		MarshalType Import(MarshalType marshalType) {
			if (marshalType == null)
				return null;

			if (marshalType is RawMarshalType) {
				var mt = (RawMarshalType)marshalType;
				return new RawMarshalType(mt.Data);
			}

			if (marshalType is FixedSysStringMarshalType) {
				var mt = (FixedSysStringMarshalType)marshalType;
				return new FixedSysStringMarshalType(mt.Size);
			}

			if (marshalType is SafeArrayMarshalType) {
				var mt = (SafeArrayMarshalType)marshalType;
				return new SafeArrayMarshalType(mt.VariantType, Import(mt.UserDefinedSubType));
			}

			if (marshalType is FixedArrayMarshalType) {
				var mt = (FixedArrayMarshalType)marshalType;
				return new FixedArrayMarshalType(mt.Size, mt.ElementType);
			}

			if (marshalType is ArrayMarshalType) {
				var mt = (ArrayMarshalType)marshalType;
				return new ArrayMarshalType(mt.ElementType, mt.ParamNumber, mt.Size, mt.Flags);
			}

			if (marshalType is CustomMarshalType) {
				var mt = (CustomMarshalType)marshalType;
				return new CustomMarshalType(mt.Guid, mt.NativeTypeName, Import(mt.CustomMarshaler), mt.Cookie);
			}

			if (marshalType is InterfaceMarshalType) {
				var mt = (InterfaceMarshalType)marshalType;
				return new InterfaceMarshalType(mt.NativeType, mt.IidParamIndex);
			}

			Debug.Assert(marshalType.GetType() == typeof(MarshalType));
			return new MarshalType(marshalType.NativeType);
		}

		ImplMap Import(ImplMap implMap) {
			if (implMap == null)
				return null;
			return targetModule.UpdateRowId(new ImplMapUser(Import(implMap.Module, false), implMap.Name, implMap.Attributes));
		}

		ModuleRef Import(ModuleRef module, bool canConvertToTargetModule) {
			var name = canConvertToTargetModule && IsSourceOrTarget(module) ? targetModule.Name : module.Name;
			var importedModuleRef = targetModule.UpdateRowId(new ModuleRefUser(targetModule, name));
			ImportCustomAttributes(importedModuleRef, module);
			return importedModuleRef;
		}

		ClassLayout Import(ClassLayout classLayout) {
			if (classLayout == null)
				return null;
			return targetModule.UpdateRowId(new ClassLayoutUser(classLayout.PackingSize, classLayout.ClassSize));
		}

		CallingConventionSig Import(CallingConventionSig signature) {
			if (signature == null)
				return null;

			if (signature is MethodSig)
				return Import((MethodSig)signature);
			if (signature is FieldSig)
				return Import((FieldSig)signature);
			if (signature is GenericInstMethodSig)
				return Import((GenericInstMethodSig)signature);
			if (signature is PropertySig)
				return Import((PropertySig)signature);
			if (signature is LocalSig)
				return Import((LocalSig)signature);
			return null;
		}

		MethodSig Import(MethodSig sig) {
			if (sig == null)
				return null;
			return Import(new MethodSig(sig.GetCallingConvention()), sig);
		}

		PropertySig Import(PropertySig sig) {
			if (sig == null)
				return null;
			return Import(new PropertySig(sig.HasThis), sig);
		}

		T Import<T>(T sig, T old) where T : MethodBaseSig {
			sig.RetType = Import(old.RetType);
			foreach (var p in old.Params)
				sig.Params.Add(Import(p));
			sig.GenParamCount = old.GenParamCount;
			var paramsAfterSentinel = sig.ParamsAfterSentinel;
			if (paramsAfterSentinel != null) {
				foreach (var p in old.ParamsAfterSentinel)
					paramsAfterSentinel.Add(Import(p));
			}
			return sig;
		}

		FieldSig Import(FieldSig sig) {
			if (sig == null)
				return null;
			return new FieldSig(Import(sig.Type));
		}

		GenericInstMethodSig Import(GenericInstMethodSig sig) {
			if (sig == null)
				return null;

			var result = new GenericInstMethodSig();
			foreach (var l in sig.GenericArguments)
				result.GenericArguments.Add(Import(l));

			return result;
		}

		LocalSig Import(LocalSig sig) {
			if (sig == null)
				return null;

			var result = new LocalSig();
			foreach (var l in sig.Locals)
				result.Locals.Add(Import(l));

			return result;
		}

		static readonly bool keepImportedRva = false;
		RVA GetRVA(RVA rva) => keepImportedRva ? rva : 0;

		void Create(FieldDef field) {
			var importedField = targetModule.UpdateRowId(new FieldDefUser(field.Name));
			oldFieldToNewField.Add(field, new MemberInfo<FieldDef>(importedField, importedField));
		}

		void Create(MethodDef method) {
			var importedMethodDef = targetModule.UpdateRowId(new MethodDefUser(method.Name));
			oldMethodToNewMethod.Add(method, new MemberInfo<MethodDef>(importedMethodDef, importedMethodDef));
		}

		void Create(PropertyDef propDef) {
			var importedPropertyDef = targetModule.UpdateRowId(new PropertyDefUser(propDef.Name));
			oldPropertyToNewProperty.Add(propDef, new MemberInfo<PropertyDef>(importedPropertyDef, importedPropertyDef));
		}

		void Create(EventDef eventDef) {
			var importedEventDef = targetModule.UpdateRowId(new EventDefUser(eventDef.Name));
			oldEventToNewEvent.Add(eventDef, new MemberInfo<EventDef>(importedEventDef, importedEventDef));
		}

		FieldDef Initialize(FieldDef field) {
			if (field == null)
				return null;
			var importedField = oldFieldToNewField[field].EditedMember;
			ImportCustomAttributes(importedField, field);
			importedField.Signature = Import(field.Signature);
			importedField.Attributes = field.Attributes;
			importedField.RVA = GetRVA(field.RVA);
			importedField.InitialValue = field.InitialValue;
			importedField.Constant = Import(field.Constant);
			importedField.FieldOffset = field.FieldOffset;
			importedField.MarshalType = Import(field.MarshalType);
			importedField.ImplMap = Import(field.ImplMap);
			return importedField;
		}

		MethodDef Initialize(MethodDef method) {
			if (method == null)
				return null;
			var importedMethodDef = oldMethodToNewMethod[method].EditedMember;
			ImportCustomAttributes(importedMethodDef, method);
			ImportDeclSecurities(importedMethodDef, method);
			importedMethodDef.RVA = GetRVA(method.RVA);
			importedMethodDef.ImplAttributes = method.ImplAttributes;
			importedMethodDef.Attributes = method.Attributes;
			importedMethodDef.Signature = Import(method.Signature);
			importedMethodDef.SemanticsAttributes = method.SemanticsAttributes;
			importedMethodDef.ImplMap = Import(method.ImplMap);
			foreach (var paramDef in method.ParamDefs)
				importedMethodDef.ParamDefs.Add(Import(paramDef));
			foreach (var genericParam in method.GenericParameters)
				importedMethodDef.GenericParameters.Add(Import(genericParam));
			foreach (var ovr in method.Overrides)
				importedMethodDef.Overrides.Add(new MethodOverride(Import(ovr.MethodBody), Import(ovr.MethodDeclaration)));
			importedMethodDef.Parameters.UpdateParameterTypes();
			return importedMethodDef;
		}

		GenericParam Import(GenericParam gp) {
			if (gp == null)
				return null;
			var importedGenericParam = targetModule.UpdateRowId(new GenericParamUser(gp.Number, gp.Flags, gp.Name));
			ImportCustomAttributes(importedGenericParam, gp);
			importedGenericParam.Kind = Import(gp.Kind);
			foreach (var gpc in gp.GenericParamConstraints)
				importedGenericParam.GenericParamConstraints.Add(Import(gpc));
			return importedGenericParam;
		}

		GenericParamConstraint Import(GenericParamConstraint gpc) {
			if (gpc == null)
				return null;
			var importedGenericParamConstraint = targetModule.UpdateRowId(new GenericParamConstraintUser(Import(gpc.Constraint)));
			ImportCustomAttributes(importedGenericParamConstraint, gpc);
			return importedGenericParamConstraint;
		}

		InterfaceImpl Import(InterfaceImpl ifaceImpl) {
			if (ifaceImpl == null)
				return null;
			var importedInterfaceImpl = targetModule.UpdateRowId(new InterfaceImplUser(Import(ifaceImpl.Interface)));
			ImportCustomAttributes(importedInterfaceImpl, ifaceImpl);
			return importedInterfaceImpl;
		}

		ParamDef Import(ParamDef paramDef) {
			if (paramDef == null)
				return null;
			var importedParamDef = targetModule.UpdateRowId(new ParamDefUser(paramDef.Name, paramDef.Sequence, paramDef.Attributes));
			ImportCustomAttributes(importedParamDef, paramDef);
			importedParamDef.MarshalType = Import(paramDef.MarshalType);
			importedParamDef.Constant = Import(paramDef.Constant);
			return importedParamDef;
		}

		PropertyDef Initialize(PropertyDef propDef) {
			if (propDef == null)
				return null;
			var importedPropertyDef = oldPropertyToNewProperty[propDef].EditedMember;
			ImportCustomAttributes(importedPropertyDef, propDef);
			importedPropertyDef.Attributes = propDef.Attributes;
			importedPropertyDef.Type = Import(propDef.Type);
			importedPropertyDef.Constant = Import(propDef.Constant);
			foreach (var m in propDef.GetMethods) {
				var newMethod = TryGetMethod(m);
				if (newMethod != null)
					importedPropertyDef.GetMethods.Add(newMethod);
			}
			foreach (var m in propDef.SetMethods) {
				var newMethod = TryGetMethod(m);
				if (newMethod != null)
					importedPropertyDef.SetMethods.Add(newMethod);
			}
			foreach (var m in propDef.OtherMethods) {
				var newMethod = TryGetMethod(m);
				if (newMethod != null)
					importedPropertyDef.OtherMethods.Add(newMethod);
			}
			return importedPropertyDef;
		}

		MethodDef TryGetMethod(MethodDef method) {
			if (method == null)
				return null;
			var m = oldMethodToNewMethod[method].TargetMember;
			if (usedMethods.Contains(m))
				return null;
			usedMethods.Add(m);
			return m;
		}

		EventDef Initialize(EventDef eventDef) {
			if (eventDef == null)
				return null;
			var importedEventDef = oldEventToNewEvent[eventDef].EditedMember;
			importedEventDef.EventType = Import(eventDef.EventType);
			importedEventDef.Attributes = eventDef.Attributes;
			ImportCustomAttributes(importedEventDef, eventDef);
			if (eventDef.AddMethod != null) {
				var newMethod = TryGetMethod(eventDef.AddMethod);
				if (newMethod != null)
					importedEventDef.AddMethod = newMethod;
			}
			if (eventDef.InvokeMethod != null) {
				var newMethod = TryGetMethod(eventDef.InvokeMethod);
				if (newMethod != null)
					importedEventDef.InvokeMethod = newMethod;
			}
			if (eventDef.RemoveMethod != null) {
				var newMethod = TryGetMethod(eventDef.RemoveMethod);
				if (newMethod != null)
					importedEventDef.RemoveMethod = newMethod;
			}
			foreach (var m in eventDef.OtherMethods) {
				var newMethod = TryGetMethod(m);
				if (newMethod != null)
					importedEventDef.OtherMethods.Add(newMethod);
			}
			return importedEventDef;
		}

		CilBody CreateBody(MethodDef paramsSourceMethod, MethodDef sourceMethod) {
			// NOTE: Both methods can be identical: targetMethod == sourceMethod

			var sourceBody = sourceMethod.Body;
			if (sourceBody == null)
				return null;

			var targetBody = new CilBody();
			targetBody.KeepOldMaxStack = sourceBody.KeepOldMaxStack;
			targetBody.InitLocals = sourceBody.InitLocals;
			targetBody.HeaderSize = sourceBody.HeaderSize;
			targetBody.MaxStack = sourceBody.MaxStack;
			targetBody.LocalVarSigTok = sourceBody.LocalVarSigTok;

			bodyDict.Clear();
			foreach (var local in sourceBody.Variables) {
				var newLocal = new Local(Import(local.Type), local.Name);
				bodyDict[local] = newLocal;
				newLocal.PdbAttributes = local.PdbAttributes;
				targetBody.Variables.Add(newLocal);
			}

			int si = sourceMethod.IsStatic ? 0 : 1;
			int ti = paramsSourceMethod.IsStatic ? 0 : 1;
			if (sourceMethod.Parameters.Count - si != paramsSourceMethod.Parameters.Count - ti)
				throw new InvalidOperationException();
			for (; si < sourceMethod.Parameters.Count && ti < paramsSourceMethod.Parameters.Count; si++, ti++)
				bodyDict[sourceMethod.Parameters[si]] = paramsSourceMethod.Parameters[ti];

			foreach (var instr in sourceBody.Instructions) {
				var newInstr = new Instruction(instr.OpCode, instr.Operand);
				newInstr.Offset = instr.Offset;
				newInstr.SequencePoint = instr.SequencePoint?.Clone();
				bodyDict[instr] = newInstr;
				targetBody.Instructions.Add(newInstr);
			}

			foreach (var eh in sourceBody.ExceptionHandlers) {
				var newEh = new ExceptionHandler(eh.HandlerType);
				newEh.TryStart = GetInstruction(bodyDict, eh.TryStart);
				newEh.TryEnd = GetInstruction(bodyDict, eh.TryEnd);
				newEh.FilterStart = GetInstruction(bodyDict, eh.FilterStart);
				newEh.HandlerStart = GetInstruction(bodyDict, eh.HandlerStart);
				newEh.HandlerEnd = GetInstruction(bodyDict, eh.HandlerEnd);
				newEh.CatchType = Import(eh.CatchType);
				targetBody.ExceptionHandlers.Add(newEh);
			}

			foreach (var newInstr in targetBody.Instructions) {
				var op = newInstr.Operand;
				if (op == null)
					continue;

				if (bodyDict.TryGetValue(op, out object obj)) {
					newInstr.Operand = obj;
					continue;
				}

				if (op is IList<Instruction> oldList) {
					var targets = new Instruction[oldList.Count];
					for (int i = 0; i < oldList.Count; i++)
						targets[i] = GetInstruction(bodyDict, oldList[i]);
					newInstr.Operand = targets;
					continue;
				}

				if (op is ITypeDefOrRef tdr) {
					newInstr.Operand = Import(tdr);
					continue;
				}

				if (op is IMethod method && method.IsMethod) {
					newInstr.Operand = Import(method);
					continue;
				}

				if (op is IField field) {
					newInstr.Operand = Import(field);
					continue;
				}

				if (op is MethodSig msig) {
					newInstr.Operand = Import(msig);
					continue;
				}

				Debug.Assert(op is sbyte || op is byte || op is int || op is long || op is float || op is double || op is string);
			}

			return targetBody;
		}

		static Instruction GetInstruction(Dictionary<object, object> dict, Instruction instr) {
			if (instr == null || !dict.TryGetValue(instr, out object obj))
				return null;
			return (Instruction)obj;
		}

		IMethod Import(IMethod method) {
			if (method == null)
				return null;

			if (method is MethodDef md)
				return oldMethodToNewMethod[md].TargetMember;

			if (method is MethodSpec ms) {
				var importedMethodSpec = new MethodSpecUser(Import(ms.Method), Import(ms.GenericInstMethodSig));
				ImportCustomAttributes(importedMethodSpec, ms);
				return importedMethodSpec;
			}

			var mr = (MemberRef)method;
			var td = TryGetTypeInTargetModule(mr.Class as ITypeDefOrRef, out var importedType);
			if (td != null) {
				var targetMethod = FindMethod(td, mr);
				if (targetMethod != null)
					return targetMethod;
				if (importedType != null) {
					var compiledMethod = FindMethod(toExtraData[importedType].CompiledType, mr);
					if (compiledMethod != null)
						return oldMethodToNewMethod[compiledMethod].TargetMember;
				}

				AddError(IM0004, string.Format(dnSpy_AsmEditor_Resources.ERR_IM_CouldNotFindMethod, mr));
				return null;
			}

			return ImportNoCheckForDefs(mr);
		}

		MethodDef FindMethod(TypeDef targetType, MemberRef mr) {
			var comparer = new ImportSigComparer(importSigComparerOptions, SIG_COMPARER_OPTIONS, targetModule);
			foreach (var method in targetType.Methods) {
				if (!UTF8String.Equals(method.Name, mr.Name))
					continue;
				if (comparer.Equals(method.MethodSig, mr.MethodSig))
					return method;
			}
			return null;
		}

		IField Import(IField field) {
			if (field == null)
				return null;

			if (field is FieldDef fd)
				return oldFieldToNewField[fd].TargetMember;

			var mr = (MemberRef)field;
			var td = TryGetTypeInTargetModule(mr.Class as ITypeDefOrRef, out var importedType);
			if (td != null) {
				var targetField = FindField(td, mr);
				if (targetField != null)
					return targetField;
				if (importedType != null) {
					var compiledField = FindField(toExtraData[importedType].CompiledType, mr);
					if (compiledField != null)
						return oldFieldToNewField[compiledField].TargetMember;
				}

				AddError(IM0005, string.Format(dnSpy_AsmEditor_Resources.ERR_IM_CouldNotFindField, mr));
				return null;
			}

			return ImportNoCheckForDefs(mr);
		}

		FieldDef FindField(TypeDef targetType, MemberRef mr) {
			var comparer = new ImportSigComparer(importSigComparerOptions, SIG_COMPARER_OPTIONS, targetModule);
			foreach (var field in targetType.Fields) {
				if (!UTF8String.Equals(field.Name, mr.Name))
					continue;
				if (comparer.Equals(field.FieldSig, mr.FieldSig))
					return field;
			}
			return null;
		}

		MemberRef ImportNoCheckForDefs(MemberRef mr) {
			var importedMemberRef = targetModule.UpdateRowId(new MemberRefUser(targetModule, mr.Name));
			ImportCustomAttributes(importedMemberRef, mr);
			importedMemberRef.Signature = Import(mr.Signature);
			importedMemberRef.Class = Import(mr.Class);
			return importedMemberRef;
		}

		IMemberRefParent Import(IMemberRefParent cls) {
			if (cls == null)
				return null;

			if (cls is ITypeDefOrRef tdr)
				return Import(tdr);

			if (cls is MethodDef md)
				return oldMethodToNewMethod[md].TargetMember;

			if (cls is ModuleRef modRef)
				return Import(modRef, true);

			throw new InvalidOperationException();
		}
	}
}
