using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using UnityEngine;
public class ReprocessBinary : MonoBehaviour
{
	private BinaryReader dumpReader;
	private BinaryWriter dumpWriter;
	public string session="Body";

	// Use this for initialization
	void Start ()
	{
		DeadReconningPacket p=new DeadReconningPacket();

		string indumpFile = Config.DumpPath(session, "DeadReconning");
		string outdumpFile = Config.DumpPath(session, "DeadReconningNew");

		dumpReader = new BinaryReader(File.Open(indumpFile, FileMode.Open));
		dumpWriter = new BinaryWriter(File.Open(outdumpFile, FileMode.Create));

		try
		{

		while (true)
		{
				p.FromBinary(dumpReader);
				p.ToBinaryNew(dumpWriter);
			
		}
		}
		catch(Exception e)
		{
			print (e.ToString ());
		}
		dumpReader.Close ();
		dumpWriter.Close ();
	}
	
}
