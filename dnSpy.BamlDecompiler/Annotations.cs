namespace dnSpy.BamlDecompiler {
	internal class BamlConnectionId {
		public uint Id { get; private set; }

		public BamlConnectionId(uint id) {
			Id = id;
		}
	}
}