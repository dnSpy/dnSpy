using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using System.Runtime.InteropServices;
using dnlib.IO;

namespace ICSharpCode.NRefactory.TypeSystem
{
	class dnlibMarshalInfo
	{
		public UnmanagedType NativeType { get; set; }

		public static dnlibMarshalInfo Read(FieldMarshal marshal)
		{
			MemoryImageStream stream = new MemoryImageStream(0, marshal.NativeType, 0, marshal.NativeType.Length);
			UnmanagedType nativeType = (UnmanagedType)stream.ReadByte();
			dnlibMarshalInfo ret;
			switch (nativeType)
			{
				case UnmanagedType.LPArray:
					{
						dnlibArrayMarshalInfo arrayInfo =  new dnlibArrayMarshalInfo();
						ret = arrayInfo;

						arrayInfo.ArraySubType = (UnmanagedType)stream.ReadByte();
						if (stream.CanRead(1))
							arrayInfo.SizeParamIndex = stream.ReadCompressedUInt32();
						if (stream.CanRead(1))
							arrayInfo.SizeConst = stream.ReadCompressedUInt32();

					} break;
				case UnmanagedType.CustomMarshaler:
					{
						dnlibCustomMarshalInfo customInfo = new dnlibCustomMarshalInfo();
						ret = customInfo;

						customInfo.GUID = Guid.Parse(stream.ReadString());
						customInfo.UnmanagedType = stream.ReadString();
						customInfo.MarshalType = stream.ReadString();
						customInfo.MarshalCookie = stream.ReadString();
					} break;
				case UnmanagedType.ByValArray:
					{
						dnlibFixedArrayMarshalInfo fixedInfo = new dnlibFixedArrayMarshalInfo();
						ret = fixedInfo;

						fixedInfo.ArraySubType = (UnmanagedType)stream.ReadByte();
						fixedInfo.SizeConst = stream.ReadCompressedUInt32();
					} break;
				case UnmanagedType.SafeArray:
					{
						dnlibSafeArrayMarshalInfo safeInfo = new dnlibSafeArrayMarshalInfo();
						ret = safeInfo;

						safeInfo.SafeArraySubType = (VarEnum)stream.ReadByte();
					} break;
				default:
					{
						ret = new dnlibMarshalInfo();
					} break;
			}
			ret.NativeType = nativeType;
			return ret;
		}
	}

	class dnlibArrayMarshalInfo : dnlibMarshalInfo
	{
		public UnmanagedType ArraySubType { get; set; }
		public uint? SizeParamIndex { get; set; }
		public uint? SizeConst { get; set; }
	}

	class dnlibCustomMarshalInfo : dnlibMarshalInfo
	{
		public Guid GUID { get; set; }
		public string UnmanagedType { get; set; }
		public string MarshalType { get; set; }
		public string MarshalCookie { get; set; }
	}

	class dnlibFixedArrayMarshalInfo : dnlibMarshalInfo
	{
		public UnmanagedType ArraySubType { get; set; }
		public uint SizeConst { get; set; }
	}

	class dnlibSafeArrayMarshalInfo : dnlibMarshalInfo
	{
		public VarEnum SafeArraySubType { get; set; }
	}
}
