using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using DZ.MediaPlayer.Vlc;
using DZ.MediaPlayer.Vlc.Deployment;
using Monobjc;
using Monobjc.Cocoa;

namespace MacPlayer {
	
	/// <summary>
	/// This class controls Slider behaviour.
	/// </summary>
	[ObjectiveCClass]
	public class Slider : NSSlider {
		
		private static readonly Common.Logging.ILog logger = 
			Common.Logging.LogManager.GetLogger(typeof(EntryPoint));
		
		/// <summary>
		/// Default contructor is obligatory for monobjc.
		/// </summary>
		public Slider() {
			logger.Info("Slider created");
			this.SendActionOn((int)(NSEventMask.NSLeftMouseUpMask | 
			                  NSEventMask.NSRightMouseUpMask | 
			                  NSEventMask.NSLeftMouseDownMask | 
			                  NSEventMask.NSRightMouseDownMask));
			this.ActionEvent += ActionEventHandler;
		}
			
		
		/// <summary>
		/// This constructor is obligatory too.
		/// </summary>
		/// <param name="ptr">
		/// A <see cref="IntPtr"/> - pointer to unmanaged NSController.
		/// </param>
		public Slider(IntPtr ptr) : base(ptr) {
			logger.Info("Slider created");
			this.SendActionOn((int)(NSEventMask.NSLeftMouseUpMask | 
			                  NSEventMask.NSRightMouseUpMask | 
			                  NSEventMask.NSLeftMouseDownMask | 
			                  NSEventMask.NSRightMouseDownMask));
			this.ActionEvent += ActionEventHandler;
		}
		
		private void ActionEventHandler(Id sender) {
			isMouseDown = !isMouseDown;
		}
		
		private bool isMouseDown;
		
		/// <summary>
		/// This property can tell us if mouse was pressed under control.
		/// </summary>
		public bool IsMouseDown {
			get {
				return (isMouseDown);
			}
		}
		
	}
}

