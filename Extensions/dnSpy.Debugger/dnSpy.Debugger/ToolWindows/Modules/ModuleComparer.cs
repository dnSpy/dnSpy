using System;

using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.ToolWindows.Modules {
	sealed class ModuleVMComparer : BaseVMComparer<ModuleVM> {
		public static readonly ModuleVMComparer Instance = new ModuleVMComparer();

		public override int Compare(ModuleVM x, ModuleVM y) {
			var result = base.Compare(x, y);
			return result != 0 ? result : x.Order - y.Order;
		}

		protected override int CompareByPropertyImpl(ModuleVM x, ModuleVM y, string propertyName) {
			switch (propertyName) {
				case nameof(ModuleVM.NameObject):
					return StringComparer.OrdinalIgnoreCase.Compare(x.Module.Name, y.Module.Name);
				case nameof(ModuleVM.PathObject):
					return StringComparer.OrdinalIgnoreCase.Compare(x.Module.Filename, y.Module.Filename);
				case nameof(ModuleVM.OptimizedObject):
					return CompareNullable(x.Module.IsOptimized, y.Module.IsOptimized);
				case nameof(ModuleVM.DynamicObject):
					return x.Module.IsDynamic.CompareTo(y.Module.IsDynamic);
				case nameof(ModuleVM.InMemoryObject):
					return x.Module.IsInMemory.CompareTo(y.Module.IsInMemory);
				case nameof(ModuleVM.OrderObject):
					return x.Module.Order.CompareTo(y.Module.Order);
				case nameof(ModuleVM.VersionObject):
					return x.Module.Version.CompareTo(y.Module.Version);
				case nameof(ModuleVM.TimestampObject):
					return CompareNullable(x.Module.Timestamp, y.Module.Timestamp);
				case nameof(ModuleVM.AddressObject):
					// compare by start address, then by module size
					var startAddrCompResult = x.Module.Address.CompareTo(y.Module.Address);
					return (startAddrCompResult != 0)
						? startAddrCompResult
						: x.Module.Size.CompareTo(y.Module.Size);
				case nameof(ModuleVM.ProcessObject):
					return x.Module.Process.Id.CompareTo(y.Module.Process.Id);
				case nameof(ModuleVM.AppDomainObject):
					return x.Module.AppDomain.Id.CompareTo(y.Module.AppDomain.Id);
			}
			return 0;
		}
	}
}
