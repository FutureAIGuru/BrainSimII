using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using WebCamLib;

namespace Touchless.Vision.Camera
{
   public enum CameraProperty : int
   {
      Pan_degrees = WebCamLib.CameraProperty.Pan_degrees,
      Tilt_degrees = WebCamLib.CameraProperty.Tilt_degrees,
      Roll_degrees = WebCamLib.CameraProperty.Roll_degrees,
      Zoom_mm = WebCamLib.CameraProperty.Zoom_mm,
      Exposure_lgSec = WebCamLib.CameraProperty.Exposure_lgSec,
      Iris_10f = WebCamLib.CameraProperty.Iris_10f,
      FocalLength_mm = WebCamLib.CameraProperty.FocalLength_mm,
      Flash = WebCamLib.CameraProperty.Flash,
      Brightness = WebCamLib.CameraProperty.Brightness,
      Contrast = WebCamLib.CameraProperty.Brightness,
      Hue = WebCamLib.CameraProperty.Contrast,
      Saturation = WebCamLib.CameraProperty.Saturation,
      Sharpness = WebCamLib.CameraProperty.Sharpness,
      Gamma = WebCamLib.CameraProperty.Gamma,
      ColorEnable = WebCamLib.CameraProperty.ColorEnable,
      WhiteBalance = WebCamLib.CameraProperty.WhiteBalance,
      BacklightCompensation = WebCamLib.CameraProperty.BacklightCompensation,
      Gain = WebCamLib.CameraProperty.Gain,
   }

   public sealed class CameraPropertyValue : IComparable<CameraPropertyValue>, IEquatable<CameraPropertyValue>
   {
      public CameraPropertyValue( bool isPercentageValue, int value, bool isAuto )
      {
         IsPercentageValue = isPercentageValue;
         Value = value;
         IsAuto = isAuto;
      }

      public int Value
      {
         get;
         set;
      }

      private bool isAuto;

      public bool IsAuto
      {
         get
         {
            return isAuto;
         }

         set
         {
            isAuto = value;
         }
      }

      public bool IsManual
      {
         get
         {
            return !IsAuto;
         }

         set
         {
            IsAuto = !value;
         }
      }

      private bool isPercentageValue;

      public bool IsActualValue
      {
         get
         {
            return !IsPercentageValue;
         }

         set
         {
            IsPercentageValue = !value;
         }
      }

      public bool IsPercentageValue
      {
         get
         {
            return isPercentageValue;
         }

         set
         {
            isPercentageValue = value;
         }
      }

      #region ICompare<CameraPropertyValue> Members

      public int CompareTo( CameraPropertyValue other )
      {
         int result = 0;

         if( IsActualValue && other.IsPercentageValue )
            result = -1;
         else if( IsPercentageValue && other.IsActualValue )
            result = 1;
         else
         {
            if( Value < other.Value )
               result = -1;
            else if( Value > other.Value )
               result = 1;
            else
            {
               if( IsAuto && other.IsManual )
                  result = -1;
               else if( IsManual && other.IsAuto )
                  result = 1;
            }
         }

         return result;
      }

      #endregion

      #region IEquatable<CameraPropertyValue> Members

      public bool Equals( CameraPropertyValue other )
      {
         return Object.ReferenceEquals( this, other ) || CompareTo( other ) == 0;
      }

      #endregion

      public override bool Equals( Object obj )
      {
         bool result;

         if( !( result = Object.ReferenceEquals( this, obj ) ) )
         {
            CameraPropertyValue other = obj as CameraPropertyValue;

            if( result = other != null )
               result = Equals( other );
         }

         return result;
      }
   }

   public sealed class CameraPropertyRange : IComparable<CameraPropertyRange>, IEquatable<CameraPropertyRange>
   {
      public CameraPropertyRange( int minimum, int maximum, int step, int defaults, bool isAuto )
      {
         Minimum = minimum;
         Maximum = maximum;
         Step = step;
         Defaults = defaults;
         IsAuto = isAuto;
      }

      public int Minimum
      {
         get;
         private set;
      }

      public int Maximum
      {
         get;
         private set;
      }

      public int Range
      {
         get
         {
            return Maximum - Minimum;
         }
      }

      public int DomainSize
      {
         get
         {
            return Range + 1;
         }
      }

      public int Step
      {
         get;
         private set;
      }

      public int Defaults
      {
         get;
         private set;
      }

      public bool IsAuto
      {
         get;
         private set;
      }

      public bool IsManual
      {
         get
         {
            return !IsAuto;
         }
      }

      #region ICompare<CameraPropertyRange> Members

      public int CompareTo( CameraPropertyRange other )
      {
         int result = 0;

         if( Minimum < other.Minimum )
            result = -1;
         else if( Minimum > other.Minimum )
            result = 1;
         else
         {
            if( Maximum < other.Maximum )
               result = -1;
            else if( Maximum > other.Maximum )
               result = 1;
            else
            {
               if( Step < other.Step )
                  result = -1;
               else if( Step > other.Step )
                  result = 1;
               else
               {
                  if( Defaults < other.Defaults )
                     result = -1;
                  else if( Defaults > other.Defaults )
                     result = 1;
                  else
                  {
                     if( IsAuto && other.IsManual )
                        result = -1;
                     else if( IsManual && other.IsAuto )
                        result = 1;
                  }
               }
            }
         }

         return result;
      }

      #endregion

      #region IEquatable<CameraPropertyRange> Members

      public bool Equals( CameraPropertyRange other )
      {
         return Object.ReferenceEquals( this, other ) || CompareTo( other ) == 0;
      }

      #endregion

      public override bool Equals( Object obj )
      {
         bool result;

         if( !( result = Object.ReferenceEquals( this, obj ) ) )
         {
            CameraPropertyRange other = obj as CameraPropertyRange;

            if( result = other != null )
               result = Equals( other );
         }

         return result;
      }
   }

   public sealed class CameraPropertyCapabilities : IComparable<CameraPropertyCapabilities>, IEquatable<CameraPropertyCapabilities>
   {
      internal CameraPropertyCapabilities( Camera camera, WebCamLib.CameraPropertyCapabilities capabilities )
      {
         Camera = camera;
         InternalCapabilities = capabilities;
      }

      public Camera Camera
      {
         get;
         private set;
      }

      internal WebCamLib.CameraPropertyCapabilities InternalCapabilities
      {
         get;
         private set;
      }

      public bool IsSupported
      {
         get
         {
            return InternalCapabilities.IsSupported;
         }
      }

      public bool IsFullySupported
      {
         get
         {
            return InternalCapabilities.IsFullySupported;
         }
      }

      public bool IsGetSupported
      {
         get
         {
            return InternalCapabilities.IsGetSupported;
         }
      }

      public bool IsSetSupported
      {
         get
         {
            return InternalCapabilities.IsSetSupported;
         }
      }

      public bool IsGetRangeSupported
      {
         get
         {
            return InternalCapabilities.IsGetRangeSupported;
         }
      }

      #region IComparable<CameraPropertyCapabilities> Members
      // sort order: IsGetSupported, IsSetSupported, IsGetRangeSupported; this exists and other doesn't first/less for all keys
      public int CompareTo( CameraPropertyCapabilities other )
      {
         int result = 0;

         if( IsGetSupported && !other.IsGetSupported )
            result = -1;
         else if( !IsGetSupported && other.IsGetSupported )
            result = 1;
         else
         {
            if( IsSetSupported && !other.IsSetSupported )
               result = -1;
            else if( !IsSetSupported && other.IsSetSupported )
               result = 1;
            else
            {
               if( IsGetRangeSupported && !other.IsGetRangeSupported )
                  result = -1;
               else if( !IsGetRangeSupported && other.IsGetRangeSupported )
                  result = 1;
            }
         }

         return result;
      }
      #endregion

      #region IEquatable<CameraPropertyCapabilities>
      public bool Equals( CameraPropertyCapabilities other )
      {
         return CompareTo( other ) == 0;
      }
      #endregion

      public override bool Equals( object obj )
      {
         bool result;

         CameraPropertyCapabilities capabilities = obj as CameraPropertyCapabilities;

         if( result = capabilities != null )
            result = Equals( capabilities );

         return result;
      }
   }

   public sealed class CaptureSize : IComparable<CaptureSize>, IEquatable<CaptureSize>
   {
      public CaptureSize( int width, int height, int colorDepth )
      {
         Width = width;
         Height = height;
         ColorDepth = colorDepth;
      }

      public int Width
      {
         get;
         private set;
      }

      public int Height
      {
         get;
         private set;
      }

      public int ColorDepth
      {
         get;
         private set;
      }

      public override String ToString()
      {
         return String.Format( "{0} x {1} @ {2}", Width, Height, ColorDepth );
      }

      #region IComparable<CaptureSize> Members

      public int CompareTo( CaptureSize other )
      {
         int result;

         if( Width < other.Width )
            result = -1;
         else if( Width > other.Width )
            result = 1;
         else
         {
            if( Height < other.Height )
               result = -1;
            else if( Height > other.Height )
               result = 1;
            else
            {
               if( ColorDepth < other.ColorDepth )
                  result = -1;
               else if( ColorDepth > other.ColorDepth )
                  result = 1;
               else
                  result = 0;
            }
         }

         return result;
      }

      #endregion

      #region IEquatable<CaptureSize> Members

      public bool Equals( CaptureSize other )
      {
         return Object.ReferenceEquals( this, other ) || CompareTo( other ) == 0;
      }

      #endregion

      public override bool Equals( Object obj )
      {
         bool result;

         if( !( result = Object.ReferenceEquals( this, obj ) ) )
         {
            CaptureSize other = obj as CaptureSize;
            if( result = other != null )
               result = Equals( other );
         }

         return result;
      }

      public override int GetHashCode()
      {
         int result = 0;

         result ^= Width;
         result ^= Height << 11;
         result ^= ColorDepth << 23;

         return result;
      }
   }

   /// <summary>
   /// Represents a camera in use by the Touchless system
   /// </summary>
   public class Camera : IDisposable
   {
      public const int IgnoredBitsPerPixel = -1;

      private readonly Object CameraMethodsLock = new Object();

      private readonly CameraMethods _cameraMethods;
      private RotateFlipType _rotateFlip = RotateFlipType.RotateNoneFlipNone;

      public Camera( CameraMethods cameraMethods, string name, int index )
      {
         _name = name;
         _index = index;

         lock( CameraMethodsLock )
         {
            _cameraMethods = cameraMethods;
            _cameraMethods.OnImageCapture += CaptureCallbackProc;
         }
      }

      public string Name
      {
         get
         {
            return _name;
         }
      }

      /// <summary>
      /// Defines the frames per second limit that is in place, -1 means no limit
      /// </summary>
      public int Fps
      {
         get
         {
            return _fpslimit;
         }
         set
         {
            _fpslimit = value;
            _timeBetweenFrames = ( 1000.0 / _fpslimit );
         }
      }

      /// <summary>
      /// Determines the width of the image captured
      /// </summary>
      public int CaptureWidth
      {
         get
         {
            return _width;
         }
         set
         {
            _width = value;
         }
      }

      /// <summary>
      /// Defines the height of the image captured
      /// </summary>
      public int CaptureHeight
      {
         get
         {
            return _height;
         }
         set
         {
            _height = value;
         }
      }

      /// <summary>
      /// Defines the bits per pixel of image captured.
      /// </summary>
      public int CaptureBitsPerPixel
      {
         get
         {
            return _bpp;
         }

         set
         {
            _bpp = value;
         }
      }

      public bool HasFrameLimit
      {
         get
         {
            return _fpslimit != -1;
         }
      }

      public bool FlipHorizontal
      {
         get
         {
            return RotateFlip == RotateFlipType.RotateNoneFlipX || RotateFlip == RotateFlipType.Rotate180FlipNone;
         }

         set
         {
            if( value && FlipVertical )
            {
               RotateFlip = RotateFlipType.Rotate180FlipNone;
            }
            else if( value && !FlipVertical )
            {
               RotateFlip = RotateFlipType.RotateNoneFlipX;
            }
            else if( !value && FlipVertical )
            {
               RotateFlip = RotateFlipType.Rotate180FlipX;
            }
            else if( !value && !FlipVertical )
            {
               RotateFlip = RotateFlipType.RotateNoneFlipNone;
            }
         }
      }

      public bool FlipVertical
      {
         get
         {
            return RotateFlip == RotateFlipType.Rotate180FlipX || RotateFlip == RotateFlipType.Rotate180FlipNone;
         }

         set
         {
            if( value && FlipHorizontal )
            {
               RotateFlip = RotateFlipType.Rotate180FlipNone;
            }
            else if( value && !FlipHorizontal )
            {
               RotateFlip = RotateFlipType.Rotate180FlipX;
            }
            else if( !value && FlipHorizontal )
            {
               RotateFlip = RotateFlipType.RotateNoneFlipX;
            }
            else if( !value && !FlipHorizontal )
            {
               RotateFlip = RotateFlipType.RotateNoneFlipNone;
            }
         }
      }

      /// <summary>
      /// Command for rotating and flipping incoming images
      /// </summary>
      public RotateFlipType RotateFlip
      {
         get
         {
            return _rotateFlip;
         }
         set
         {
            // Swap height/width when rotating by 90 or 270
            if( ( int ) _rotateFlip % 2 != ( int ) value % 2 )
            {
               int temp = CaptureWidth;
               CaptureWidth = CaptureHeight;
               CaptureHeight = temp;
            }
            _rotateFlip = value;
         }
      }

      #region IDisposable Members

      /// <summary>
      /// Cleanup function for the camera
      /// </summary>
      public void Dispose()
      {
         StopCapture();
      }

      #endregion

      /// <summary>
      /// Returns the last image acquired from the camera
      /// </summary>
      /// <returns>A bitmap of the last image acquired from the camera</returns>
      public Bitmap GetCurrentImage()
      {
         Bitmap b = null;
         lock( _bitmapLock )
         {
            if( _bitmap == null )
            {
               return null;
            }

            b = new Bitmap( _bitmap );
         }

         return b;
      }

      public void ShowPropertiesDialog()
      {
         lock( CameraMethodsLock )
         {
            _cameraMethods.DisplayCameraPropertiesDialog( _index );
         }
      }

      public CameraInfo GetCameraInfo()
      {
         lock( CameraMethodsLock )
         {
            return _cameraMethods.GetCameraInfo( _index );
         }
      }

      #region Camera Properties
      public bool IsCameraPropertySupported( CameraProperty property )
      {
         bool result = false;

         lock( CameraMethodsLock )
         {
            _cameraMethods.IsPropertySupported( ( WebCamLib.CameraProperty ) property, ref result );
         }

         return result;
      }

      public bool SetCameraProperty( CameraProperty property, CameraPropertyValue value )
      {
         bool result;

         if( value.IsActualValue )
            result = SetCameraProperty_value( property, value.Value, value.IsAuto );
         else // if( value.IsPercentageValue )
            result = SetCameraProperty_percentage( property, value.Value, value.IsAuto );

         return result;
      }

      public bool SetCameraProperty( CameraProperty property, bool isActualValue, int value )
      {
         bool result = false;

         if( isActualValue )
            result = SetCameraProperty_value( property, value );
         else // is percentage value
            result = SetCameraProperty_percentage( property, value );

         return result;
      }

      public bool SetCameraProperty( CameraProperty property, bool isActualValue, int value, bool auto )
      {
         bool result = false;

         if( isActualValue )
            result = SetCameraProperty_value( property, value, auto );
         else // is percentage value
            result = SetCameraProperty_percentage( property, value, auto );

         return result;
      }

      public bool SetCameraProperty_value( CameraProperty property, bool auto )
      {
         return SetCameraProperty_value( property, 0, auto );
      }

      // Assume manual control
      public bool SetCameraProperty_value( CameraProperty property, int value )
      {
         return SetCameraProperty_value( property, value, false );
      }

      public bool SetCameraProperty_value( CameraProperty property, int value, bool auto )
      {
         bool result = false;

         lock( CameraMethodsLock )
         {
            _cameraMethods.SetProperty_value( ( WebCamLib.CameraProperty ) property, value, auto, ref result );
         }

         return result;
      }

      public bool SetCameraProperty_percentage( CameraProperty property, bool auto )
      {
         return SetCameraProperty_percentage( property, 0, auto );
      }

      // Assume manual control
      public bool SetCameraProperty_percentage( CameraProperty property, int percentage )
      {
         return SetCameraProperty_percentage( property, percentage, false );
      }

      public bool SetCameraProperty_percentage( CameraProperty property, int percentage, bool auto )
      {
         bool result = false;

         lock( CameraMethodsLock )
         {
            _cameraMethods.SetProperty_percentage( ( WebCamLib.CameraProperty ) property, percentage, auto, ref result );
         }

         return result;
      }

      public CameraPropertyValue GetCameraProperty( CameraProperty property, bool isActualValue )
      {
         CameraPropertyValue result;

         if( isActualValue )
            result = GetCameraProperty_value( property );
         else // is percentage value
            result = GetCameraProperty_percentage( property );

         return result;
      }

      public CameraPropertyValue GetCameraProperty_value( CameraProperty property )
      {
         CameraPropertyValue result;

         bool successful = false;

         int value = -1;
         bool isAuto = false;

         lock( CameraMethodsLock )
         {
            _cameraMethods.GetProperty_value( ( WebCamLib.CameraProperty ) property, ref value, ref isAuto, ref successful );
         }

         if( successful )
            result = new CameraPropertyValue( false, value, isAuto );
         else
            result = null;

         return result;
      }

      public CameraPropertyValue GetCameraProperty_percentage( CameraProperty property )
      {
         CameraPropertyValue result;

         bool successful = false;

         int value = -1;
         bool isAuto = false;

         lock( CameraMethodsLock )
         {
            _cameraMethods.GetProperty_percentage( ( WebCamLib.CameraProperty ) property, ref value, ref isAuto, ref successful );
         }

         if( successful )
            result = new CameraPropertyValue( true, value, isAuto );
         else
            result = null;

         return result;
      }

      public CameraPropertyRange GetCameraPropertyRange( CameraProperty property )
      {
         CameraPropertyRange result;

         bool successful = false;

         int minimum, maximum, step, defaults;
         bool isAuto;

         minimum = maximum = step = defaults = -1;
         isAuto = false;

         lock( CameraMethodsLock )
         {
            _cameraMethods.GetPropertyRange( ( WebCamLib.CameraProperty ) property, ref minimum, ref maximum, ref step, ref defaults, ref isAuto, ref successful );
         }

         if( successful )
            result = new CameraPropertyRange( minimum, maximum, step, defaults, isAuto );
         else
            result = null;

         return result;
      }

      public bool CameraPropertyHasRange( CameraProperty property )
      {
         bool result = false;

         _cameraMethods.PropertyHasRange( ( WebCamLib.CameraProperty ) property, ref result );

         return result;
      }

      public bool ValidateCameraProperty( CameraProperty property, int value )
      {
         bool result = false;

         _cameraMethods.ValidatePropertyValue( ( WebCamLib.CameraProperty ) property, value, ref result );

         return result;
      }

      public IDictionary<CameraProperty, CameraPropertyCapabilities> CameraPropertyCapabilities
      {
         get
         {
            IDictionary<CameraProperty, CameraPropertyCapabilities> result = new Dictionary<CameraProperty, CameraPropertyCapabilities>( _cameraMethods.PropertyCapabilities.Count );

            foreach( WebCamLib.CameraProperty property in _cameraMethods.PropertyCapabilities.Keys )
            {
               CameraProperty prop = ( CameraProperty ) property;
               CameraPropertyCapabilities capabilities = new CameraPropertyCapabilities( this, _cameraMethods.PropertyCapabilities[ property ] );

               result.Add( prop, capabilities );
            }

            return result;
         }
      }
      #endregion

      public IList<CaptureSize> CaptureSizes
      {
         get
         {
            IList<Tuple<int, int, int>> rawSizes = new List<Tuple<int, int, int>>();

            lock( CameraMethodsLock )
            {
               _cameraMethods.GetCaptureSizes( _index, rawSizes );
            }

            IList<CaptureSize> result = new List<CaptureSize>( rawSizes.Count );
            foreach( Tuple<int, int, int> size in rawSizes )
            {
               CaptureSize newSize = new CaptureSize( size.Item1, size.Item2, size.Item3 );
               result.Add( newSize );
            }

            return result;
         }
      }

      /// <summary>
      /// Event fired when an image from the camera is captured
      /// </summary>
      public event EventHandler<CameraEventArgs> OnImageCaptured;

      /// <summary>
      /// Returns the camera name as the ToString implementation
      /// </summary>
      /// <returns>The name of the camera</returns>
      public override string ToString()
      {
         return _name;
      }

      #region Internal Implementation

      private readonly object _bitmapLock = new object();
      private readonly int _index;
      private readonly string _name;
      private Bitmap _bitmap;
      private DateTime _dtLastCap = DateTime.MinValue;
      private int _fpslimit = -1;
      private int _height = 240;
      private double _timeBehind;
      private double _timeBetweenFrames;
      private int _width = 320;
      private int _bpp = 24;

      internal bool StartCapture()
      {
         bool result = false;

         lock( CameraMethodsLock )
         {
            _cameraMethods.StartCamera( _index, ref _width, ref _height, ref _bpp, ref result );
         }

         return result;
      }

      internal void StopCapture()
      {
         lock( CameraMethodsLock )
         {
            _cameraMethods.StopCamera();
         }
      }

      /// <summary>
      /// Here is where the images come in as they are collected, as fast as they can and on a background thread
      /// </summary>
      private void CaptureCallbackProc( int dataSize, byte[] data )
      {
         // Do the magic to create a bitmap
         int stride = _width * 3;
         GCHandle handle = GCHandle.Alloc( data, GCHandleType.Pinned );
         var scan0 = handle.AddrOfPinnedObject();
         scan0 += ( _height - 1 ) * stride;
         var b = new Bitmap( _width, _height, -stride, PixelFormat.Format24bppRgb, scan0 );
         b.RotateFlip( _rotateFlip );
         // NOTE: It seems that bntr has made that resolution property work properly
         var copyBitmap = ( Bitmap ) b.Clone();
         // Copy the image using the Thumbnail function to also resize if needed
         //var copyBitmap = (Bitmap)b.GetThumbnailImage(_width, _height, null, IntPtr.Zero);

         // Now you can free the handle
         handle.Free();

         ImageCaptured( copyBitmap );
      }

      private void ImageCaptured( Bitmap bitmap )
      {
         DateTime dtCap = DateTime.Now;

         // Always save the bitmap
         lock( _bitmapLock )
         {
            _bitmap = bitmap;
         }

         // FPS affects the callbacks only
         if( _fpslimit != -1 )
         {
            if( _dtLastCap != DateTime.MinValue )
            {
               double milliseconds = ( ( dtCap.Ticks - _dtLastCap.Ticks ) / TimeSpan.TicksPerMillisecond ) * 1.15;
               if( milliseconds + _timeBehind >= _timeBetweenFrames )
               {
                  _timeBehind = ( milliseconds - _timeBetweenFrames );
                  if( _timeBehind < 0.0 )
                  {
                     _timeBehind = 0.0;
                  }
               }
               else
               {
                  _timeBehind = 0.0;
                  return; // ignore the frame
               }
            }
         }

         if( OnImageCaptured != null )
         {
            var fps = ( int ) ( 1 / dtCap.Subtract( _dtLastCap ).TotalSeconds );
            OnImageCaptured.Invoke( this, new CameraEventArgs( bitmap, fps ) );
         }

         _dtLastCap = dtCap;
      }

      #endregion
   }

   /// <summary>
   /// Camera specific EventArgs that provides the Image being captured
   /// </summary>
   public class CameraEventArgs : EventArgs
   {
      /// <summary>
      /// Current Camera Image
      /// </summary>
      public Bitmap Image
      {
         get
         {
            return _image;
         }
      }

      public int CameraFps
      {
         get
         {
            return _cameraFps;
         }
      }

      #region Internal Implementation

      private readonly int _cameraFps;
      private readonly Bitmap _image;

      internal CameraEventArgs( Bitmap i, int fps )
      {
         _image = i;
         _cameraFps = fps;
      }

      #endregion
   }
}