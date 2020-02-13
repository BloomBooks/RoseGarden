using System;
using System.Drawing;
using System.Drawing.Imaging;
using SIL.Windows.Forms.ClearShare;

namespace RoseGarden
{
	/// <summary>
	/// Image utility class.  This provides operations for deriving thumbnail images and for reading
	/// or writing the image metadata.
	/// </summary>
	public class ImageUtility : IDisposable
	{
		private Bitmap _originalImage;
		public ImageUtility(string path)
		{
			_originalImage = new Bitmap(path);
		}

		/// <summary>
		/// Create a thumbnail image with the given height.  Maintain the aspect ratio of the original picture.
		/// </summary>
		public void CreateThumbnail(int thumbHeight, string outputPath)
		{
			var thumbWidth = (int)Math.Round((double)_originalImage.Width * ((double)thumbHeight / (double)_originalImage.Height));
			var size = new Size(thumbWidth, thumbHeight);
			using (var image = new Bitmap(_originalImage, size))
			{
				if (outputPath.EndsWith(".jpg", StringComparison.InvariantCulture))
					image.Save(outputPath, ImageFormat.Jpeg);
				else
					image.Save(outputPath, ImageFormat.Png);
			}
		}

		#region IDisposable Support
		private bool isDisposed = false; // To detect redundant calls
		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
					_originalImage.Dispose();
				}
				isDisposed = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion

		/// <summary>
		/// Get the current metadata from an image file.
		/// </summary>
		public static Metadata GetImageMetadata(string path)
		{
			return Metadata.FromFile(path);
		}

		/// <summary>
		/// Write the interesting (intellectual property) metadata to an image file.
		/// </summary>
		public static void SetImageMetadata(string path, Metadata meta)
		{
			meta.WriteIntellectualPropertyOnly(path);
		}
	}
}
