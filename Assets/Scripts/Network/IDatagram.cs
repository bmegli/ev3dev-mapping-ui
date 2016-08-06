public interface IDatagram
{
	void FromBinary(System.IO.BinaryReader reader);
	void ToBinary(System.IO.BinaryWriter writer);
	int BinarySize();
	ulong GetTimestampUs();
}
