using System;
using System.Diagnostics;
using NUnit.Framework;
using DZ.MediaPlayer.Vlc.Deployment;
using System.Collections.Generic;
using System.IO;

namespace DZ.MediaPlayer.Vlc.Tests {
	/// <summary>
	/// These tests are used to generate some portion of code.
	/// </summary>
	[TestFixture()]
	public class Test {
		/// <summary>
		/// TestCase
		/// </summary>
		[Test]
		public void TestCase ()
		{
		    Debug.WriteLine("");
			//
            //string path = @"C:\all\work\libvlc.net-git\libvlcnet\3rd-party\temporary";
			string path = @"/Users/rz/Projects/libvlc.net/libvlcnet-git/3rd-party/temporary";
			//
			Dictionary<string, string> hashes = VlcDeployment.GetDirectoryStructureHashes(path, VlcDeployment.GetDefaultHashAlgorithm());
			string code = VlcDeployment.GetCSharpHashDictionaryConstructor("dictionary", hashes);
            Debug.WriteLine(code);
			//
			//string pathZip = Path.Combine(path, "../libvlc-1.1.9-win32.zip");
			string pathZip = Path.Combine(path, "../libvlc-1.1.9-macosx.zip");
			VlcDeployment.CreateDeploymentPackage(path, pathZip);
			//
			string hash = VlcDeployment.GetFileHash(pathZip, VlcDeployment.GetDefaultHashAlgorithm());
            Debug.WriteLine(hash);
		}
		
		/// <summary>
		/// TestCase2
		/// </summary>
        [Test, Ignore]
		public void TestCase2 () {
			//
			//string path = @"";
			string path = @"/Users/rz/Projects/libvlc.net-git/libvlcnet/3rd-party/libvlc/libvlc-1.1.7-macosx.zip";
			//
			string hash = VlcDeployment.GetFileHash(path, VlcDeployment.GetDefaultHashAlgorithm());
			Console.WriteLine(hash);
		}
	}
}

