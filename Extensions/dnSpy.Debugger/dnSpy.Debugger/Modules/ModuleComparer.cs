using System;

namespace dnSpy.Debugger.Modules {
	class ModuleComparer : BaseVMComparer<ModuleVM> {
		protected override int CompareByProperty(ModuleVM x, ModuleVM y, string propertyName) {
			switch (propertyName) {
				case nameof(ModuleVM.NameObject):
					return StringComparer.OrdinalIgnoreCase.Compare(
						DebugOutputUtils.GetFilename(x.Module.Name),
						DebugOutputUtils.GetFilename(y.Module.Name)
					);
				case nameof(ModuleVM.PathObject):
					return StringComparer.OrdinalIgnoreCase.Compare(
						x.Module.Name,
						y.Module.Name
					);
				case nameof(ModuleVM.IsOptimized):
					return x.IsOptimized.CompareTo(y.IsOptimized);
				case nameof(ModuleVM.DynamicObject):
					return x.Module.IsDynamic.CompareTo(y.Module.IsDynamic);
				case nameof(ModuleVM.InMemoryObject):
					return x.Module.IsInMemory.CompareTo(y.Module.IsInMemory);
				case nameof(ModuleVM.OrderObject):
					return x.Module.UniqueId.CompareTo(y.Module.UniqueId);
				case nameof(ModuleVM.VersionObject):
					return x.Version.CompareTo(y.Version);
				case nameof(ModuleVM.TimestampObject):
					return (int)((x.Timestamp ?? 0) - (y.Timestamp ?? 0));
				case nameof(ModuleVM.AddressObject):
					var startAddrCompResult = x.Module.Address.CompareTo(y.Module.Address);
					return (startAddrCompResult != 0)
						? startAddrCompResult
						: x.Module.Size.CompareTo(y.Module.Size);
				case nameof(ModuleVM.ProcessObject):
					return x.Module.Process.ProcessId.CompareTo(y.Module.Process.ProcessId);
				case nameof(ModuleVM.AppDomainObject):
					var domainIdCompResult = x.Module.AppDomain.Id.CompareTo(y.Module.AppDomain.Id);
					return (domainIdCompResult != 0)
						? domainIdCompResult
						: x.Module.AppDomain.Name.CompareTo(y.Module.AppDomain.Name);
			}
			return 0;
		}
	}
}
