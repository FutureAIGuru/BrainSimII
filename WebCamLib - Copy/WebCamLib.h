//*****************************************************************************************
//  File:       WebCamLib.h
//  Project:    WebcamLib
//  Author(s):  John Conwell
//              Gary Caldwell
//
//  Declares the webcam DirectShow wrapper used by TouchlessLib
//*****************************************************************************************

#pragma once

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;

namespace WebCamLib
{
	/// <summary>
	/// Store webcam name, index
	/// </summary>
	public ref class CameraInfo
	{
	public:
		CameraInfo( int index, String^ name );

		property int Index
		{
			int get();
		private: void set( int value );
		}

		property String^ Name
		{
			String^ get();
		private: void set( String^ value );
		}

	private:
		int index;
		String^ name;
	};

	public enum class PropertyTypeMask : int
	{
		CameraControlPropertyMask = 0x01000,
		VideoProcAmpPropertyMask = 0x02000,
	};

	// Source: https://msdn.microsoft.com/en-us/library/aa925325.aspx
	public enum class CameraControlProperty : int
	{
		Pan_degrees,
		Tilt_degrees,
		Roll_degrees,
		Zoom_mm,
		Exposure_lgSec,
		Iris_10f,
		FocalLength_mm,
		Flash,
	};

	// Source: https://msdn.microsoft.com/en-us/library/aa920611.aspx
	public enum class VideoProcAmpProperty : int
	{
		Brightness,
		Contrast,
		Hue,
		Saturation,
		Sharpness,
		Gamma,
		ColorEnable,
		WhiteBalance,
		BacklightCompensation,
		Gain,
	};

	public enum class CameraProperty : int
	{
		Pan_degrees = WebCamLib::CameraControlProperty::Pan_degrees | PropertyTypeMask::CameraControlPropertyMask,
		Tilt_degrees = WebCamLib::CameraControlProperty::Tilt_degrees | PropertyTypeMask::CameraControlPropertyMask,
		Roll_degrees = WebCamLib::CameraControlProperty::Roll_degrees | PropertyTypeMask::CameraControlPropertyMask,
		Zoom_mm = WebCamLib::CameraControlProperty::Zoom_mm | PropertyTypeMask::CameraControlPropertyMask,
		Exposure_lgSec = WebCamLib::CameraControlProperty::Exposure_lgSec | PropertyTypeMask::CameraControlPropertyMask,
		Iris_10f = WebCamLib::CameraControlProperty::Iris_10f | PropertyTypeMask::CameraControlPropertyMask,
		FocalLength_mm = WebCamLib::CameraControlProperty::FocalLength_mm | PropertyTypeMask::CameraControlPropertyMask,
		Flash = WebCamLib::CameraControlProperty::Flash | PropertyTypeMask::CameraControlPropertyMask,
		Brightness = WebCamLib::VideoProcAmpProperty::Brightness | PropertyTypeMask::VideoProcAmpPropertyMask,
		Contrast = WebCamLib::VideoProcAmpProperty::Contrast | PropertyTypeMask::VideoProcAmpPropertyMask,
		Hue = WebCamLib::VideoProcAmpProperty::Hue | PropertyTypeMask::VideoProcAmpPropertyMask,
		Saturation = WebCamLib::VideoProcAmpProperty::Saturation | PropertyTypeMask::VideoProcAmpPropertyMask,
		Sharpness = WebCamLib::VideoProcAmpProperty::Sharpness | PropertyTypeMask::VideoProcAmpPropertyMask,
		Gamma = WebCamLib::VideoProcAmpProperty::Gamma | PropertyTypeMask::VideoProcAmpPropertyMask,
		ColorEnable = WebCamLib::VideoProcAmpProperty::ColorEnable | PropertyTypeMask::VideoProcAmpPropertyMask,
		WhiteBalance = WebCamLib::VideoProcAmpProperty::WhiteBalance | PropertyTypeMask::VideoProcAmpPropertyMask,
		BacklightCompensation = WebCamLib::VideoProcAmpProperty::BacklightCompensation | PropertyTypeMask::VideoProcAmpPropertyMask,
		Gain = WebCamLib::VideoProcAmpProperty::Gain | PropertyTypeMask::VideoProcAmpPropertyMask,
	};

	public ref class CameraPropertyCapabilities
	{
	public:
		CameraPropertyCapabilities( int cameraIndex, CameraProperty prop, bool isGetSupported, bool isSetSupported, bool isGetRangeSupported );

		property int CameraIndex
		{
			int get();
		private: void set( int cameraIndex );
		}

		property CameraProperty Property
		{
			CameraProperty get();
		private: void set( CameraProperty value );
		}

		property bool IsGetSupported
		{
			bool get();
		private: void set( bool value );
		}

		property bool IsSetSupported
		{
			bool get();
		private: void set( bool value );
		}

		property bool IsGetRangeSupported
		{
			bool get();
		private: void set( bool value );
		}

		property bool IsSupported
		{
			bool get();
		}

		property bool IsFullySupported
		{
			bool get();
		}

	private:
		int cameraIndex;
		CameraProperty prop;
		bool isGetSupported, isSetSupported, isGetRangeSupported;
	};

	/// <summary>
	/// DirectShow wrapper around a web cam, used for image capture
	/// </summary>
	public ref class CameraMethods
	{
	public:
		/// <summary>
		/// Initializes information about all web cams connected to machine
		/// </summary>
		CameraMethods();

		/// <summary>
		/// Delegate used by DirectShow to pass back captured images from webcam
		/// </summary>
		delegate void CaptureCallbackDelegate(
			int dwSize,
			[MarshalAsAttribute(UnmanagedType::LPArray, ArraySubType = UnmanagedType::I1, SizeParamIndex = 0)] array<System::Byte>^ abData);

		/// <summary>
		/// Event callback to capture images from webcam
		/// </summary>
		event CaptureCallbackDelegate^ OnImageCapture;

		/// <summary>
		/// Retrieve information about a specific camera
		/// Use the count property to determine valid indicies to pass in
		/// </summary>
		CameraInfo^ GetCameraInfo(int camIndex);

		/// <summary>
		/// Start the camera associated with the input handle
		/// </summary>
		bool StartCamera(int camIndex, interior_ptr<int> width, interior_ptr<int> height, interior_ptr<int> bpp);

		/// <summary>
		/// Start the camera associated with the input handle
		/// </summary>
		void StartCamera(int camIndex, interior_ptr<int> width, interior_ptr<int> height, interior_ptr<int> bpp, interior_ptr<bool> successful);

		#pragma region Camera Property Support
		void IsPropertySupported( CameraProperty prop, interior_ptr<bool> result );

		bool IsPropertySupported( CameraProperty prop );

		void GetProperty( CameraProperty prop, bool isValue, interior_ptr<long> value, interior_ptr<bool> bAuto, interior_ptr<bool> successful );

		bool GetProperty( CameraProperty prop, bool isValue, interior_ptr<long> value, interior_ptr<bool> bAuto );

		void GetProperty_value( CameraProperty prop, interior_ptr<long> value, interior_ptr<bool> bAuto, interior_ptr<bool> successful );

		bool GetProperty_value( CameraProperty prop, interior_ptr<long> value, interior_ptr<bool> bAuto );

		void GetProperty_percentage( CameraProperty prop, interior_ptr<long> percentage, interior_ptr<bool> bAuto, interior_ptr<bool> successful);

		bool GetProperty_percentage( CameraProperty prop, interior_ptr<long> percentage, interior_ptr<bool> bAuto );

		void SetProperty( CameraProperty prop, bool isValue, long value, bool bAuto, interior_ptr<bool> successful );

		bool SetProperty( CameraProperty prop, bool isValue, long value, bool bAuto );

		void SetProperty_value( CameraProperty prop, long value, bool bAuto, interior_ptr<bool> successful );

		bool SetProperty_value( CameraProperty prop, long value, bool bAuto );

		void SetProperty_value( CameraProperty prop, long value, bool bAuto, bool throwValidationError, interior_ptr<bool> successful );

		bool SetProperty_value( CameraProperty prop, long value, bool bAuto, bool throwValidationError );

		void SetProperty_percentage( CameraProperty prop, long percentage, bool bAuto, interior_ptr<bool> successful );

		bool SetProperty_percentage( CameraProperty prop, long percentage, bool bAuto );

		void PropertyHasRange( CameraProperty prop, interior_ptr<bool> successful );

		bool PropertyHasRange( CameraProperty prop );

		void GetPropertyRange( CameraProperty prop, interior_ptr<long> min, interior_ptr<long> max, interior_ptr<long> steppingDelta, interior_ptr<long> defaults, interior_ptr<bool> bAuto, interior_ptr<bool> successful );

		bool GetPropertyRange( CameraProperty prop, interior_ptr<long> min, interior_ptr<long> max, interior_ptr<long> steppingDelta, interior_ptr<long> defaults, interior_ptr<bool> bAuto );

		void ValidatePropertyValue( CameraProperty prop, long value, interior_ptr<bool> successful );

		bool ValidatePropertyValue( CameraProperty prop, long value );

		CameraPropertyCapabilities^ GetPropertyCapability( CameraProperty prop );

		property IDictionary<CameraProperty, CameraPropertyCapabilities^> ^ PropertyCapabilities
		{
			IDictionary<CameraProperty, CameraPropertyCapabilities^> ^ get();
		}
		#pragma endregion

		void GetCaptureSizes(int index, IList<Tuple<int,int,int>^> ^ sizes);

		property IList<Tuple<int,int,int>^> ^ CaptureSizes
		{
			IList<Tuple<int,int,int>^> ^ get();
		}

		/// <summary>
		/// Stops the currently running camera and cleans up any global resources
		/// </summary>
		void Cleanup();

		/// <summary>
		/// Stops the currently running camera
		/// </summary>
		void StopCamera();

		/// <summary>
		/// Show the properties dialog for the specified webcam
		/// </summary>
		void DisplayCameraPropertiesDialog(int camIndex);

		/// <summary>
		/// Count of the number of cameras installed
		/// </summary>
		property int Count;

		/// <summary>
		/// Queries which camera is currently running via StartCamera(), -1 for none
		/// </summary>
		property int ActiveCameraIndex
		{
			int get()
			{
				return activeCameraIndex;
			}
		}

		/// <summary>
		/// IDisposable
		/// </summary>
		~CameraMethods();

	protected:
		/// <summary>
		/// Finalizer
		/// </summary>
		!CameraMethods();

		#pragma region Camera Property Support
		bool IsCameraControlProperty( CameraProperty prop );

		bool IsVideoProcAmpProperty( CameraProperty prop );

		bool IsPropertyMaskEqual( CameraProperty prop, PropertyTypeMask mask );

		WebCamLib::CameraControlProperty GetCameraControlProperty( CameraProperty prop );

		WebCamLib::VideoProcAmpProperty GetVideoProcAmpProperty( CameraProperty prop );

		bool IsPropertySupported( WebCamLib::CameraControlProperty prop );

		bool IsPropertySupported( WebCamLib::VideoProcAmpProperty prop );

		bool GetPropertyRange( WebCamLib::CameraControlProperty prop, interior_ptr<long> min, interior_ptr<long> max, interior_ptr<long> steppingDelta, interior_ptr<long> defaults, interior_ptr<bool> bAuto );

		bool GetPropertyRange( WebCamLib::VideoProcAmpProperty prop, interior_ptr<long> min, interior_ptr<long> max, interior_ptr<long> steppingDelta, interior_ptr<long> defaults, interior_ptr<bool> bAuto );

		bool GetProperty_value( WebCamLib::CameraControlProperty prop, interior_ptr<long> value, interior_ptr<bool> bAuto );

		bool GetProperty_value( WebCamLib::VideoProcAmpProperty prop, interior_ptr<long> value, interior_ptr<bool> bAuto );

		bool SetProperty_value( WebCamLib::CameraControlProperty prop, long value, bool bAuto );

		bool SetProperty_value( WebCamLib::VideoProcAmpProperty prop, long value, bool bAuto );
		#pragma endregion

	private:
		/// <summary>
		/// Pinned pointer to delegate for CaptureCallbackDelegate
		/// Keeps the delegate instance in one spot
		/// </summary>
		GCHandle ppCaptureCallback;

		/// <summary>
		/// Initialize information about webcams installed on machine
		/// </summary>
		void RefreshCameraList();

		/// <summary>
		/// Has dispose already happened?
		/// </summary>
		bool disposed;

		/// <summary>
		/// Which camera is running? -1 for none
		/// </summary>
		int activeCameraIndex;

		/// <summary>
		/// Releases all unmanaged resources
		/// </summary>
		void CleanupCameraInfo();

		/// <summary>
		/// Setup the callback functionality for DirectShow
		/// </summary>
		HRESULT ConfigureSampleGrabber(IBaseFilter *pIBaseFilter);

		HRESULT SetCaptureFormat(IBaseFilter* pCap, int width, int height, int bpp );
	};

	// Forward declarations of callbacks
	typedef void (__stdcall *PFN_CaptureCallback)(DWORD dwSize, BYTE* pbData);
	PFN_CaptureCallback g_pfnCaptureCallback = NULL;

	/// <summary>
	/// Lightweight SampleGrabber callback interface
	/// </summary>
	class SampleGrabberCB : public ISampleGrabberCB
	{
	public:
		SampleGrabberCB()
		{
			m_nRefCount = 0;
		}

		virtual HRESULT STDMETHODCALLTYPE SampleCB(double SampleTime, IMediaSample *pSample)
		{
			return E_FAIL;
		}

		virtual HRESULT STDMETHODCALLTYPE BufferCB(double SampleTime, BYTE *pBuffer, long BufferLen)
		{
			if (g_pfnCaptureCallback != NULL)
			{
				g_pfnCaptureCallback(BufferLen, pBuffer);
			}

			return S_OK;
		}

		virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject)
		{
			return E_FAIL;  // Not a very accurate implementation
		}

		virtual ULONG STDMETHODCALLTYPE AddRef()
		{
			return ++m_nRefCount;
		}

		virtual ULONG STDMETHODCALLTYPE Release()
		{
			int n = --m_nRefCount;
			if (n <= 0)
			{
				delete this;
			}
			return n;
		}

	private:
		int m_nRefCount;
	};
}
