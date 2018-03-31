using System;
using NUnit.Framework;
using DZ.MediaPlayer.Vlc.Io;
using System.Threading;
using System.IO;

namespace DZ.MediaPlayer.Vlc.Tests {
	
	[TestFixture]
	public class StreamingTest : BaseVlcPlayerTest {
		
		[Test]
		public void TestStreaming() {
			VlcMediaLibraryFactory factory = this.CreateNewFactory(new string[] {
            });
			VlcSinglePlayer playerStream = (VlcSinglePlayer)factory.CreatePlayer(new 
				PlayerOutput(":sout=#transcode{vcodec=mp4v,vb=1024,acodec=mp4a,ab=192}:standard{mux=ts,dst=127.0.0.1:8080,access=udp}"));
			
			//
			string filePath = GetTemporaryFilePath();
			PlayerOutput output = new PlayerOutput();
			output.Files.Add(new OutFile(filePath));
			//
			VlcSinglePlayer playerReceive = (VlcSinglePlayer)factory.CreatePlayer(output);
			
			try {
				playerReceive.SetMediaInput(new MediaInput(MediaInputType.UnparsedMrl, "udp://127.0.0.1:8080"));
				playerStream.SetMediaInput(new MediaInput(MediaInputType.UnparsedMrl, "file://" + GetSampleVideoPath()));
				//
				playerStream.Play();
				//
				Thread.Sleep(1000);
				//
				playerReceive.Play();
				//
				Thread.Sleep(5000);
				//
				Assert.IsTrue(File.Exists(filePath));
				FileInfo info = new FileInfo(filePath);
				Assert.Greater(info.Length, 0);
				//
				playerReceive.Stop();
				playerStream.Stop();
			} finally {
				playerStream.Dispose();
				playerReceive.Dispose();
				factory.Dispose();
			}
		}
		
	}
}

