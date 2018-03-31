using System;
using DZ.MediaPlayer.Vlc.Tests;

namespace DZ.MediaPlayer.Vlc.Tests.Console {
	class MainClass {
		public static void Main (string[] args) {
            //Test test0 = new Test();
			//test0.TestCase();
            //
			/*
			VlcPlayerTestState testState = new VlcPlayerTestState();
			testState.SetUp();
			testState.TestPlayStateSinglePlayer();
			testState.TestPlayStateDoublePlayer();
			//return;
			//
			VlcMediaTest test = new VlcMediaTest();
			test.SetUp();
			test.TestPreparsedMediaCreate();
			*/
			//
			StreamingTest streamingTest = new StreamingTest();
			streamingTest.SetUp();
			streamingTest.TestStreaming();
		}
	}
}

