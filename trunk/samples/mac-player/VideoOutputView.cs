using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using Monobjc;
using Monobjc.Cocoa;

namespace MacPlayer {
	
	/// <summary>
	/// This protocol should be implemented in order to inject video view properly.
	/// </summary>
	[ObjectiveCProtocol]
	public interface IVLCOpenGLVideoViewEmbedding {
		/// <summary>
		/// This method will be called from VLC side. Passed view should be placed somewhere.
		/// </summary>
		/// <param name="view">View where rendering occurs.</param>
		[ObjectiveCMessage("addVoutSubview:")]
		void AddVoutSubview(NSView view);
		
		/// <summary>
		/// This method will be called from VLC side. Passed view should be removed from parent.
		/// This method called during uninitialization.
		/// </summary>
		/// <param name="view">View where rendering occurs. Same as passed to <see cref="AddVoutSubview"/></param>
		[ObjectiveCMessage("removeVoutSubview:")]
		void RemoveVoutSubview(NSView view);
	}
	
	/// <summary>
	/// This class controls Cocoa window.
	/// </summary>
	[ObjectiveCClass]
	public class VideoOutputView : NSView, IVLCOpenGLVideoViewEmbedding {
		
		private static readonly Common.Logging.ILog logger = 
			Common.Logging.LogManager.GetLogger(typeof(EntryPoint));
		
		private NSView subView;
		
		/// <summary>
		/// Default contructor is obligatory for monobjc.
		/// </summary>
		public VideoOutputView() {
		}
			
		
		/// <summary>
		/// This constructor is obligatory too.
		/// </summary>
		/// <param name="ptr">
		/// A <see cref="IntPtr"/> - pointer to unmanaged NSController.
		/// </param>
		public VideoOutputView(IntPtr ptr) : base(ptr) {
		}
		
		// update sub view size
		private void updateSize() {
			if ( subView != null ) {
				subView.SetFrameSize(this.Frame.size);
				subView.SetFrameOrigin(new NSPoint(0, 0));
			}
		}
		
		/// <summary>
		/// This method is called by libvlc itself to make a place for view where rendering occurs.
		/// </summary>
		/// <param name="view">
		/// A <see cref="NSView"/>
		/// </param>
		[ObjectiveCMessage("addVoutSubview:")]
		public void AddVoutSubview(NSView view) {
			logger.Info("addVoutSubview:");
			//
			subView = view;
			//
			updateSize();
			subView.AutoresizingMask = NSResizingFlags.NSViewHeightSizable | NSResizingFlags.NSViewWidthSizable;
			this.AddSubview(subView);
		}
		
		/// <summary>
		/// This method is called when NSView where rendering occurs needs to be removed.
		/// </summary>
		/// <param name="view">
		/// A <see cref="NSView"/>
		/// </param>
		[ObjectiveCMessage("removeVoutSubview:")]
		public void RemoveVoutSubview(NSView view) {
			logger.Info("removeVoutSubview:");
			//
			view.RemoveFromSuperview();
			subView = null;
		}
		
		/// <summary>
		/// TODO: why we should implement this?
		/// This method should return something.
		/// </summary>
		[ObjectiveCMessage("stretchesVideo")]
		public void StretchesVideo() {
			logger.Info("stretchesVideo:");
		}
		
	}
}

