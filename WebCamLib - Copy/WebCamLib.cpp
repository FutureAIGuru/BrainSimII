//*****************************************************************************************
//  File:       WebCamLib.cpp
//  Project:    WebcamLib
//  Author(s):  John Conwell
//              Gary Caldwell
//
//  Defines the webcam DirectShow wrapper used by TouchlessLib
//*****************************************************************************************

#include <dshow.h>
#include <strsafe.h>
#define __IDxtCompositor_INTERFACE_DEFINED__
#define __IDxtAlphaSetter_INTERFACE_DEFINED__
#define __IDxtJpeg_INTERFACE_DEFINED__
#define __IDxtKey_INTERFACE_DEFINED__

#pragma include_alias( "dxtrans.h", "qedit.h" )

#include "qedit.h"
#include "WebCamLib.h"

using namespace System;
using namespace System::Reflection;
using namespace WebCamLib;


// Private variables
#define MAX_CAMERAS 10

#pragma region CameraInfo Items
CameraInfo::CameraInfo( int index, String^ name )
{
	Index = index;
	Name = name;
}

int CameraInfo::Index::get()
{
	return index;
}

void CameraInfo::Index::set( int value )
{
	index = value;
}

String^ CameraInfo::Name::get()
{
	return name;
}

void CameraInfo::Name::set( String^ value )
{
	if( value != nullptr )
		name = value;
	else
		throw gcnew ArgumentNullException( "Name cannot be null." );
}
#pragma endregion

#pragma region CameraPropertyCapabilities Items
CameraPropertyCapabilities::CameraPropertyCapabilities( int cameraIndex, CameraProperty prop, bool isGetSupported, bool isSetSupported, bool isGetRangeSupported )
{
	CameraIndex = cameraIndex;
	Property = prop;
	IsGetSupported = isGetSupported;
	IsSetSupported = isSetSupported;
	IsGetRangeSupported = isGetRangeSupported;
}

int CameraPropertyCapabilities::CameraIndex::get()
{
	return cameraIndex;
}

void CameraPropertyCapabilities::CameraIndex::set( int value )
{
	cameraIndex =value;
}

CameraProperty CameraPropertyCapabilities::Property::get()
{
	return prop;
}

void CameraPropertyCapabilities::Property::set( CameraProperty value )
{
	prop = value;
}

bool CameraPropertyCapabilities::IsGetSupported::get()
{
	return isGetSupported;
}

void CameraPropertyCapabilities::IsGetSupported::set( bool value )
{
	isGetSupported = value;
}

bool CameraPropertyCapabilities::IsSetSupported::get()
{
	return isSetSupported;
}

void CameraPropertyCapabilities::IsSetSupported::set( bool value )
{
	isSetSupported = value;
}

bool CameraPropertyCapabilities::IsGetRangeSupported::get()
{
	return isGetRangeSupported;
}

void CameraPropertyCapabilities::IsGetRangeSupported::set( bool value )
{
	isGetRangeSupported = value;
}

bool CameraPropertyCapabilities::IsSupported::get()
{
	return IsGetSupported || IsSetSupported || IsGetRangeSupported;
}

bool CameraPropertyCapabilities::IsFullySupported::get()
{
	return IsGetSupported && IsSetSupported && IsGetRangeSupported;
}
#pragma endregion

// Structure to hold camera information
struct CameraInfoStruct
{
	BSTR bstrName;
	IMoniker* pMoniker;
};


// Private global variables
IGraphBuilder* g_pGraphBuilder = NULL;
IMediaControl* g_pMediaControl = NULL;
ICaptureGraphBuilder2* g_pCaptureGraphBuilder = NULL;
IBaseFilter* g_pIBaseFilterCam = NULL;
IBaseFilter* g_pIBaseFilterSampleGrabber = NULL;
IBaseFilter* g_pIBaseFilterNullRenderer = NULL;
CameraInfoStruct g_aCameraInfo[MAX_CAMERAS] = {0};


// http://social.msdn.microsoft.com/Forums/sk/windowsdirectshowdevelopment/thread/052d6a15-f092-4913-b52d-d28f9a51e3b6
void MyFreeMediaType(AM_MEDIA_TYPE& mt) {
	if (mt.cbFormat != 0) {
		CoTaskMemFree((PVOID)mt.pbFormat);
		mt.cbFormat = 0;
		mt.pbFormat = NULL;
	}
	if (mt.pUnk != NULL) {
		// Unecessary because pUnk should not be used, but safest.
		mt.pUnk->Release();
		mt.pUnk = NULL;
	}
}
void MyDeleteMediaType(AM_MEDIA_TYPE *pmt) {
	if (pmt != NULL) {
		MyFreeMediaType(*pmt); // See FreeMediaType for the implementation.
		CoTaskMemFree(pmt);
	}
}


/// <summary>
/// Initializes information about all web cams connected to machine
/// </summary>
CameraMethods::CameraMethods()
{
	// Set to not disposed
	this->disposed = false;

	// Get and cache camera info
	RefreshCameraList();
}

/// <summary>
/// IDispose
/// </summary>
CameraMethods::~CameraMethods()
{
	Cleanup();
	disposed = true;
}

/// <summary>
/// Finalizer
/// </summary>
CameraMethods::!CameraMethods()
{
	if (!disposed)
	{
		Cleanup();
	}
}

/// <summary>
/// Initialize information about webcams installed on machine
/// </summary>
void CameraMethods::RefreshCameraList()
{
	IEnumMoniker* pclassEnum = NULL;
	ICreateDevEnum* pdevEnum = NULL;

	int count = 0;

	CleanupCameraInfo();

	HRESULT hr = CoCreateInstance(CLSID_SystemDeviceEnum,
		NULL,
		CLSCTX_INPROC,
		IID_ICreateDevEnum,
		(LPVOID*)&pdevEnum);

	if (SUCCEEDED(hr))
	{
		hr = pdevEnum->CreateClassEnumerator(CLSID_VideoInputDeviceCategory, &pclassEnum, 0);
	}

	if (pdevEnum != NULL)
	{
		pdevEnum->Release();
		pdevEnum = NULL;
	}

	if (pclassEnum != NULL)
	{ 
		IMoniker* apIMoniker[1];
		ULONG ulCount = 0;

		while (SUCCEEDED(hr) && (count) < MAX_CAMERAS && pclassEnum->Next(1, apIMoniker, &ulCount) == S_OK)
		{
			g_aCameraInfo[count].pMoniker = apIMoniker[0];
			g_aCameraInfo[count].pMoniker->AddRef();

			IPropertyBag *pPropBag;
			hr = apIMoniker[0]->BindToStorage(NULL, NULL, IID_IPropertyBag, (void **)&pPropBag);
			if (SUCCEEDED(hr))
			{
				// Retrieve the filter's friendly name
				VARIANT varName;
				VariantInit(&varName);
				hr = pPropBag->Read(L"FriendlyName", &varName, 0);
				if (SUCCEEDED(hr) && varName.vt == VT_BSTR)
				{
					g_aCameraInfo[count].bstrName = SysAllocString(varName.bstrVal);
				}
				VariantClear(&varName);

				pPropBag->Release();
			}

			count++;
		}

		pclassEnum->Release();
	}

	this->Count = count;

	if (!SUCCEEDED(hr))
		throw gcnew COMException("Error Refreshing Camera List", hr);
}

/// <summary>
/// Retrieve information about a specific camera
/// Use the count property to determine valid indicies to pass in
/// </summary>
CameraInfo^ CameraMethods::GetCameraInfo(int camIndex)
{
	if (camIndex >= Count)
		throw gcnew ArgumentOutOfRangeException("Camera index is out of bounds: " + Count.ToString());

	if (g_aCameraInfo[camIndex].pMoniker == NULL)
		throw gcnew ArgumentException("There is no camera at index: " + camIndex.ToString());

	CameraInfo^ camInfo = gcnew CameraInfo( camIndex, Marshal::PtrToStringBSTR((IntPtr)g_aCameraInfo[camIndex].bstrName) );

	return camInfo;
}

/// <summary>
/// Start the camera associated with the input handle
/// </summary>
void CameraMethods::StartCamera(int camIndex, interior_ptr<int> width, interior_ptr<int> height, interior_ptr<int> bpp, interior_ptr<bool> successful)
{
	*successful = StartCamera( camIndex, width, height, bpp );
}

/// <summary>
/// Start the camera associated with the input handle
/// </summary>
bool CameraMethods::StartCamera(int camIndex, interior_ptr<int> width, interior_ptr<int> height, interior_ptr<int> bpp)
{
	if (camIndex >= Count)
		throw gcnew ArgumentException("Camera index is out of bounds: " + Count.ToString());

	if (g_aCameraInfo[camIndex].pMoniker == NULL)
		throw gcnew ArgumentException("There is no camera at index: " + camIndex.ToString());

	if (g_pGraphBuilder != NULL)
		throw gcnew ArgumentException("Graph Builder was null");

	// Setup up function callback -- through evil reflection on private members
	Type^ baseType = this->GetType();
	FieldInfo^ field = baseType->GetField("<backing_store>OnImageCapture", BindingFlags::NonPublic | BindingFlags::Instance | BindingFlags::IgnoreCase);
	if (field != nullptr)
	{
		Object^ obj = field->GetValue(this);
		if (obj != nullptr)
		{
			CameraMethods::CaptureCallbackDelegate^ del = (CameraMethods::CaptureCallbackDelegate^)field->GetValue(this);
			if (del != nullptr)
			{
				ppCaptureCallback = GCHandle::Alloc(del);
				g_pfnCaptureCallback =
					static_cast<PFN_CaptureCallback>(Marshal::GetFunctionPointerForDelegate(del).ToPointer());
			}
		}
	}

	bool result = false;

	IMoniker *pMoniker = g_aCameraInfo[camIndex].pMoniker;
	pMoniker->AddRef();

	HRESULT hr = S_OK;

	// Build all the necessary interfaces to start the capture
	if (SUCCEEDED(hr))
	{
		hr = CoCreateInstance(CLSID_FilterGraph,
			NULL,
			CLSCTX_INPROC,
			IID_IGraphBuilder,
			(LPVOID*)&g_pGraphBuilder);
	}

	if (SUCCEEDED(hr))
	{
		hr = g_pGraphBuilder->QueryInterface(IID_IMediaControl, (LPVOID*)&g_pMediaControl);
	}

	if (SUCCEEDED(hr))
	{
		hr = CoCreateInstance(CLSID_CaptureGraphBuilder2,
			NULL,
			CLSCTX_INPROC,
			IID_ICaptureGraphBuilder2,
			(LPVOID*)&g_pCaptureGraphBuilder);
	}

	// Setup the filter graph
	if (SUCCEEDED(hr))
	{
		hr = g_pCaptureGraphBuilder->SetFiltergraph(g_pGraphBuilder);
	}

	// Build the camera from the moniker
	if (SUCCEEDED(hr))
	{
		hr = pMoniker->BindToObject(NULL, NULL, IID_IBaseFilter, (LPVOID*)&g_pIBaseFilterCam);
	}

	// Add the camera to the filter graph
	if (SUCCEEDED(hr))
	{
		hr = g_pGraphBuilder->AddFilter(g_pIBaseFilterCam, L"WebCam");
	}

	// Set the resolution
	if (SUCCEEDED(hr)) {
		hr = SetCaptureFormat(g_pIBaseFilterCam, *width, *height, *bpp);
	}

	// Create a SampleGrabber
	if (SUCCEEDED(hr))
	{
		hr = CoCreateInstance(CLSID_SampleGrabber, NULL, CLSCTX_INPROC_SERVER, IID_IBaseFilter, (void**)&g_pIBaseFilterSampleGrabber);
	}

	// Configure the Sample Grabber
	if (SUCCEEDED(hr))
	{
		hr = ConfigureSampleGrabber(g_pIBaseFilterSampleGrabber);
	}

	// Add Sample Grabber to the filter graph
	if (SUCCEEDED(hr))
	{
		hr = g_pGraphBuilder->AddFilter(g_pIBaseFilterSampleGrabber, L"SampleGrabber");
	}

	// Create the NullRender
	if (SUCCEEDED(hr))
	{
		hr = CoCreateInstance(CLSID_NullRenderer, NULL, CLSCTX_INPROC_SERVER, IID_IBaseFilter, (void**)&g_pIBaseFilterNullRenderer);
	}

	// Add the Null Render to the filter graph
	if (SUCCEEDED(hr))
	{
		hr = g_pGraphBuilder->AddFilter(g_pIBaseFilterNullRenderer, L"NullRenderer");
	}

	// Configure the render stream
	if (SUCCEEDED(hr))
	{
		hr = g_pCaptureGraphBuilder->RenderStream(&PIN_CATEGORY_CAPTURE, &MEDIATYPE_Video, g_pIBaseFilterCam, g_pIBaseFilterSampleGrabber, g_pIBaseFilterNullRenderer);
	}

	// Grab the capture width and height
	if (SUCCEEDED(hr))
	{
		ISampleGrabber* pGrabber = NULL;
		hr = g_pIBaseFilterSampleGrabber->QueryInterface(IID_ISampleGrabber, (LPVOID*)&pGrabber);
		if (SUCCEEDED(hr))
		{
			AM_MEDIA_TYPE mt;
			hr = pGrabber->GetConnectedMediaType(&mt);
			if (SUCCEEDED(hr))
			{
				VIDEOINFOHEADER *pVih;
				if ((mt.formattype == FORMAT_VideoInfo) &&
					(mt.cbFormat >= sizeof(VIDEOINFOHEADER)) &&
					(mt.pbFormat != NULL) )
				{
					pVih = (VIDEOINFOHEADER*)mt.pbFormat;
					*width = pVih->bmiHeader.biWidth;
					*height = pVih->bmiHeader.biHeight;
					*bpp = pVih->bmiHeader.biBitCount;
				}
				else
				{
					hr = E_FAIL;  // Wrong format
				}

				// FreeMediaType(mt); (from MSDN)
				if (mt.cbFormat != 0)
				{
					CoTaskMemFree((PVOID)mt.pbFormat);
					mt.cbFormat = 0;
					mt.pbFormat = NULL;
				}
				if (mt.pUnk != NULL)
				{
					// Unecessary because pUnk should not be used, but safest.
					mt.pUnk->Release();
					mt.pUnk = NULL;
				}
			}
		}

		if (pGrabber != NULL)
		{
			pGrabber->Release();
			pGrabber = NULL;
		}
	}

	// Start the capture
	if (SUCCEEDED(hr))
	{
		hr = g_pMediaControl->Run();
	}

	// If init fails then ensure that you cleanup
	if (FAILED(hr))
	{
		StopCamera();
	}
	else
	{
		hr = S_OK;  // Make sure we return S_OK for success
	}

	// Cleanup
	if (pMoniker != NULL)
	{
		pMoniker->Release();
		pMoniker = NULL;
	}

	if( result = SUCCEEDED( hr ) )
		this->activeCameraIndex = camIndex;

	return result;
}

#pragma region Camera Property Support
inline void CameraMethods::IsPropertySupported( CameraProperty prop, interior_ptr<bool> result )
{
	*result = IsPropertySupported( prop );
}

inline bool CameraMethods::IsPropertySupported( CameraProperty prop )
{
	bool result = false;

	if( IsCameraControlProperty( prop ) )
		result = IsPropertySupported( GetCameraControlProperty( prop ) );
	else if( IsVideoProcAmpProperty( prop ) )
		result = IsPropertySupported( GetVideoProcAmpProperty( prop ) );

	return result;
}

bool CameraMethods::IsPropertySupported( WebCamLib::CameraControlProperty prop )
{
	bool result = false;

	IAMCameraControl * cameraControl = NULL;
	HRESULT hr = g_pIBaseFilterCam->QueryInterface(IID_IAMCameraControl, (void**)&cameraControl);

	if(SUCCEEDED(hr))
	{
		long lProperty = static_cast< long >( prop );
		long value, captureFlags;
		hr = cameraControl->Get(lProperty, &value, &captureFlags);

		result = SUCCEEDED(hr);
	}
	else
		throw gcnew InvalidOperationException( "Unable to determine if the property is supported." );

	return result;
}

bool CameraMethods::IsPropertySupported( WebCamLib::VideoProcAmpProperty prop )
{
	bool result = false;

	IAMVideoProcAmp * pProcAmp = NULL;
	HRESULT hr = g_pIBaseFilterCam->QueryInterface(IID_IAMVideoProcAmp, (void**)&pProcAmp);

	if(SUCCEEDED(hr))
	{
		long lProperty = static_cast< long >( prop );
		long value, captureFlags;
		hr = pProcAmp->Get(lProperty, &value, &captureFlags);

		result = SUCCEEDED(hr);
	}
	else
		throw gcnew InvalidOperationException( "Unable to determine if the property is supported." );

	return result;
}

inline bool CameraMethods::IsCameraControlProperty( CameraProperty prop )
{
	return IsPropertyMaskEqual( prop, PropertyTypeMask::CameraControlPropertyMask );
}

inline bool CameraMethods::IsVideoProcAmpProperty( CameraProperty prop )
{
	return IsPropertyMaskEqual( prop, PropertyTypeMask::VideoProcAmpPropertyMask );
}

inline bool CameraMethods::IsPropertyMaskEqual( CameraProperty prop, PropertyTypeMask mask )
{
	return ( static_cast< int >( prop ) & static_cast< int >( mask ) ) != 0;
}

inline void CameraMethods::GetProperty( CameraProperty prop, bool isValue, interior_ptr<long> value, interior_ptr<bool> bAuto, interior_ptr<bool> successful )
{
	*successful = GetProperty( prop, isValue, value, bAuto );
}

inline bool CameraMethods::GetProperty( CameraProperty prop, bool isValue, interior_ptr<long> value, interior_ptr<bool> bAuto )
{
	bool result;

	if( isValue )
		result = GetProperty_value( prop, value, bAuto );
	else // is a percentage value
		result = GetProperty_percentage( prop, value, bAuto );

	return result;
}

inline WebCamLib::CameraControlProperty CameraMethods::GetCameraControlProperty( CameraProperty prop )
{
	if( IsCameraControlProperty( prop ) )
	{
		int value = static_cast< int >( prop );
		int mask = static_cast< int >( PropertyTypeMask::CameraControlPropertyMask );
		value &= ~mask;

		return static_cast< WebCamLib::CameraControlProperty >( value );
	}
	else
		throw gcnew OverflowException( "Property is not a camera property." );
}

inline WebCamLib::VideoProcAmpProperty CameraMethods::GetVideoProcAmpProperty( CameraProperty prop )
{
	if( IsVideoProcAmpProperty( prop ) )
	{
		int value = static_cast< int >( prop );
		int mask = static_cast< int >( PropertyTypeMask::VideoProcAmpPropertyMask );
		value &= ~mask;

		return static_cast< WebCamLib::VideoProcAmpProperty >( value );
	}
	else
		throw gcnew OverflowException( "Property is not a camera property." );
}

inline void CameraMethods::GetProperty_value( CameraProperty prop, interior_ptr<long> value, interior_ptr<bool> bAuto, interior_ptr<bool> successful)
{
	*successful = GetProperty_value( prop, value, bAuto );
}

inline bool CameraMethods::GetProperty_value( CameraProperty prop, interior_ptr<long> value, interior_ptr<bool> bAuto)
{
	bool result = false;

	if( IsCameraControlProperty( prop ) )
		result = GetProperty_value( GetCameraControlProperty( prop ), value, bAuto );
	else if( IsVideoProcAmpProperty( prop ) )
		result = GetProperty_value( GetVideoProcAmpProperty( prop ), value, bAuto );

	return result;
}

inline void CameraMethods::GetProperty_percentage( CameraProperty prop, interior_ptr<long> percentage, interior_ptr<bool> bAuto, interior_ptr<bool> successful)
{
	*successful = GetProperty_percentage( prop, percentage, bAuto );
}

bool CameraMethods::GetProperty_percentage( CameraProperty prop, interior_ptr<long> percentage, interior_ptr<bool> bAuto)
{
	bool result;

	long value;
	if( result = GetProperty_value( prop, &value, bAuto ) )
	{
		long min, max, steppingDelta, defaults;
		bool placeHolder;
		if( result = GetPropertyRange( prop, &min, &max, &steppingDelta, &defaults, &placeHolder ) )
			*percentage = ( ( value - min ) * 100 ) / ( max - min + 1 );
	}

	return result;
}

inline void CameraMethods::SetProperty( CameraProperty prop, bool isValue, long value, bool bAuto, interior_ptr<bool> successful )
{
	*successful = SetProperty( prop, isValue, value, bAuto );
}

inline bool CameraMethods::SetProperty( CameraProperty prop, bool isValue, long value, bool bAuto )
{
	bool result;

	if( isValue )
		result = SetProperty_value( prop, value, bAuto );
	else  // is a percentage value
		result = SetProperty_percentage( prop, value, bAuto );

	return result;
}

inline void CameraMethods::SetProperty_value(CameraProperty prop, long value, bool bAuto, interior_ptr<bool> successful)
{
	*successful = SetProperty_value( prop, value, bAuto );
}

inline bool CameraMethods::SetProperty_value(CameraProperty prop, long value, bool bAuto)
{
	return SetProperty_value( prop, value, bAuto, true );
}

inline void CameraMethods::SetProperty_value( CameraProperty prop, long value, bool bAuto, bool throwValidationError, interior_ptr<bool> successful )
{
	*successful = SetProperty_value( prop, value, bAuto, throwValidationError );
}

inline bool CameraMethods::SetProperty_value( CameraProperty prop, long value, bool bAuto, bool throwValidationError )
{
	bool result = false;

	if( ValidatePropertyValue( prop, value ) )
	{
		if( IsCameraControlProperty( prop ) )
			result = SetProperty_value( GetCameraControlProperty( prop ), value, bAuto );
		else if( IsVideoProcAmpProperty( prop ) )
			result = SetProperty_value( GetVideoProcAmpProperty( prop ), value, bAuto );
	}
	else if( throwValidationError )
		throw gcnew ArgumentOutOfRangeException( "Property value is outside of its defined range." );

	return result;
}

inline void CameraMethods::SetProperty_percentage(CameraProperty prop, long percentage, bool bAuto, interior_ptr<bool> successful)
{
	*successful = SetProperty_percentage( prop, percentage, bAuto );
}

bool CameraMethods::SetProperty_percentage(CameraProperty prop, long percentage, bool bAuto)
{
	if( !IsPropertySupported( prop ) )
		throw gcnew ArgumentException( "Property is not supported." );
	else if( percentage >= 0 && percentage <= 100 )
	{
		bool result;

		long min, max, steppingDelta, defaults;
		bool placeHolder;
		if( result = GetPropertyRange( prop, &min, &max, &steppingDelta, &defaults, &placeHolder ) )
		{
			long value = ( ( max - min ) * percentage ) / 100 + min;
			result = SetProperty_value( prop, value, bAuto );
		}

		return result;
	}
	else
		throw gcnew ArgumentOutOfRangeException( "Percentage is not valid." );
}

inline void CameraMethods::GetPropertyRange( CameraProperty prop, interior_ptr<long> min, interior_ptr<long> max, interior_ptr<long> steppingDelta, interior_ptr<long> defaults, interior_ptr<bool> bAuto, interior_ptr<bool> successful)
{
	*successful = GetPropertyRange( prop, min, max, steppingDelta, defaults, bAuto );
}

inline bool CameraMethods::GetPropertyRange( CameraProperty prop, interior_ptr<long> min, interior_ptr<long> max, interior_ptr<long> steppingDelta, interior_ptr<long> defaults, interior_ptr<bool> bAuto)
{
	bool result = false;

	if( IsCameraControlProperty( prop ) )
		result = GetPropertyRange( GetCameraControlProperty( prop ), min, max, steppingDelta, defaults, bAuto );
	else if( IsVideoProcAmpProperty( prop ) )
		result = GetPropertyRange( GetVideoProcAmpProperty( prop ), min, max, steppingDelta, defaults, bAuto );

	return result;
}

bool CameraMethods::GetPropertyRange( WebCamLib::CameraControlProperty prop, interior_ptr<long> min, interior_ptr<long> max, interior_ptr<long> steppingDelta, interior_ptr<long> defaults, interior_ptr<bool> bAuto )
{
	bool result = false;

	IAMCameraControl * cameraControl = NULL;
	HRESULT hr = g_pIBaseFilterCam->QueryInterface(IID_IAMCameraControl, (void**)&cameraControl);

	if( SUCCEEDED( hr ) )
	{
		long lProperty = static_cast< long >( prop );
		long minimum, maximum, step, default_value, flags;
		hr = cameraControl->GetRange( lProperty, &minimum, &maximum, &step, &default_value, &flags );

		if( SUCCEEDED( hr ) )
		{
			*min = minimum;
			*max = maximum;
			*steppingDelta = step;
			*defaults = default_value;
			*bAuto = flags == CameraControl_Flags_Auto;

			result = SUCCEEDED(hr);
		}
	}

	return result;
}

bool CameraMethods::GetPropertyRange( WebCamLib::VideoProcAmpProperty prop, interior_ptr<long> min, interior_ptr<long> max, interior_ptr<long> steppingDelta, interior_ptr<long> defaults, interior_ptr<bool> bAuto )
{
	bool result = false;

	IAMVideoProcAmp * pProcAmp = NULL;
	HRESULT hr = g_pIBaseFilterCam->QueryInterface(IID_IAMVideoProcAmp, (void**)&pProcAmp);

	if( SUCCEEDED( hr ) )
	{
		long lProperty = static_cast< long >( prop );
		long minimum, maximum, step, default_value, flags;
		hr = pProcAmp->GetRange( lProperty, &minimum, &maximum, &step, &default_value, &flags );

		if( SUCCEEDED( hr ) )
		{
			*min = minimum;
			*max = maximum;
			*steppingDelta = step;
			*defaults = default_value;
			*bAuto = flags == CameraControl_Flags_Auto;

			result = SUCCEEDED(hr);
		}
	}

	return result;
}

bool CameraMethods::GetProperty_value( WebCamLib::CameraControlProperty prop, interior_ptr<long> value, interior_ptr<bool> bAuto )
{
	if( g_pIBaseFilterCam == NULL )
		throw gcnew InvalidOperationException( "No camera started." );

	bool result = false;

	IAMCameraControl * cameraControl = NULL;
	HRESULT hr = g_pIBaseFilterCam->QueryInterface(IID_IAMCameraControl, (void**)&cameraControl);

	if( SUCCEEDED( hr ) )
	{
		long lProperty = static_cast< long >( prop );
		long lValue, captureFlags;
		hr = cameraControl->Get(lProperty, &lValue, &captureFlags);

		if( result = SUCCEEDED( hr ) )
		{
			*value = lValue;
			*bAuto = captureFlags == CameraControl_Flags_Auto;
		}
	}

	return result;
}

bool CameraMethods::GetProperty_value( WebCamLib::VideoProcAmpProperty prop, interior_ptr<long> value, interior_ptr<bool> bAuto )
{
	if( g_pIBaseFilterCam == NULL )
		throw gcnew InvalidOperationException( "No camera started." );

	bool result = false;

	IAMVideoProcAmp * pProcAmp = NULL;
	HRESULT hr = g_pIBaseFilterCam->QueryInterface(IID_IAMVideoProcAmp, (void**)&pProcAmp);

	if( SUCCEEDED( hr ) )
	{
		long lProperty = static_cast< long >( prop );
		long lValue, captureFlags;
		hr = pProcAmp->Get(lProperty, &lValue, &captureFlags);

		if( result = SUCCEEDED( hr ) )
		{
			*value = lValue;
			*bAuto = captureFlags == VideoProcAmp_Flags_Auto;
		}
	}

	return result;
}

bool CameraMethods::SetProperty_value( WebCamLib::CameraControlProperty prop, long value, bool bAuto )
{
	if( g_pIBaseFilterCam == NULL )
		throw gcnew InvalidOperationException( "No camera started." );

	bool result = false;

	// Query the capture filter for the IAMCameraControl interface.
	IAMCameraControl * cameraControl = NULL;
	HRESULT hr = g_pIBaseFilterCam->QueryInterface(IID_IAMCameraControl, (void**)&cameraControl);

	if( SUCCEEDED( hr ) )
	{
		long lProperty = static_cast< long >( prop );
		hr = cameraControl->Set(lProperty, value, bAuto ? CameraControl_Flags_Auto : CameraControl_Flags_Manual);

		result = SUCCEEDED( hr );
	}

	return result;
}

bool CameraMethods::SetProperty_value( WebCamLib::VideoProcAmpProperty prop, long value, bool bAuto )
{
	if( g_pIBaseFilterCam == NULL )
		throw gcnew InvalidOperationException( "No camera started." );

	bool result = false;

	// Query the capture filter for the IAMVideoProcAmp interface.
	IAMVideoProcAmp * pProcAmp = NULL;
	HRESULT hr = g_pIBaseFilterCam->QueryInterface(IID_IAMVideoProcAmp, (void**)&pProcAmp);

	if( SUCCEEDED( hr ) )
	{
		long lProperty = static_cast< long >( prop );
		hr = pProcAmp->Set(lProperty, value, bAuto ? VideoProcAmp_Flags_Auto : VideoProcAmp_Flags_Manual);

		result = SUCCEEDED( hr );
	}

	return result;
}

void CameraMethods::PropertyHasRange( CameraProperty prop, interior_ptr<bool> successful )
{
	*successful = PropertyHasRange( prop );
}

bool CameraMethods::PropertyHasRange( CameraProperty prop )
{
	bool result = prop != CameraProperty::WhiteBalance && prop != CameraProperty::Gain;

	return result;
}

inline void CameraMethods::ValidatePropertyValue( CameraProperty prop, long value, interior_ptr<bool> successful )
{
	*successful = ValidatePropertyValue( prop, value );
}

bool CameraMethods::ValidatePropertyValue( CameraProperty prop, long value )
{
	bool result = true;

	if( PropertyHasRange( prop ) )
	{
		long min, max, steppingDelta, defaults;
		bool bAuto;
		GetPropertyRange( prop, &min, &max, &steppingDelta, &defaults, &bAuto );

		result = value >= min && value <= max;
	}

	return result;
}

CameraPropertyCapabilities^ CameraMethods::GetPropertyCapability( CameraProperty prop )
{
	long value;
	bool isAuto;

	bool propertyHasRange = PropertyHasRange( prop );
	bool isSetSupported;
	bool isGetSupported = GetProperty_value( prop, &value, &isAuto );
	if( isGetSupported )
		isSetSupported = SetProperty_value( prop, value, isAuto );
	else
		isSetSupported = false;

	CameraPropertyCapabilities^ result = gcnew CameraPropertyCapabilities( ActiveCameraIndex, prop, isGetSupported, isSetSupported, propertyHasRange );

	return result;
}

IDictionary<CameraProperty, CameraPropertyCapabilities^> ^ CameraMethods::PropertyCapabilities::get()
{
	Array^ propertyValues = Enum::GetValues( CameraProperty::typeid );

	IDictionary<CameraProperty, CameraPropertyCapabilities^> ^ result = gcnew Dictionary<CameraProperty, CameraPropertyCapabilities^>( propertyValues->Length );

	for( Int32 i = 0; i < propertyValues->Length; ++i )
	{
		CameraProperty prop = static_cast<CameraProperty>( propertyValues->GetValue( i ) );
		result->Add( prop, GetPropertyCapability( prop ) );
	}

	return result;
}
#pragma endregion

/// <summary>
/// Closes any open webcam and releases all unmanaged resources
/// </summary>
void CameraMethods::Cleanup()
{
	StopCamera();
	CleanupCameraInfo();

	// Clean up pinned pointer to callback delegate
	if (ppCaptureCallback.IsAllocated)
	{
		ppCaptureCallback.Free();
	}
}

/// <summary>
/// Stops the current open webcam
/// </summary>
void CameraMethods::StopCamera()
{
	if (g_pMediaControl != NULL)
	{
		g_pMediaControl->Stop();
		g_pMediaControl->Release();
		g_pMediaControl = NULL;
	}

	g_pfnCaptureCallback = NULL;

	if (g_pIBaseFilterNullRenderer != NULL)
	{
		g_pIBaseFilterNullRenderer->Release();
		g_pIBaseFilterNullRenderer = NULL;
	}

	if (g_pIBaseFilterSampleGrabber != NULL)
	{
		g_pIBaseFilterSampleGrabber->Release();
		g_pIBaseFilterSampleGrabber = NULL;
	}

	if (g_pIBaseFilterCam != NULL)
	{
		g_pIBaseFilterCam->Release();
		g_pIBaseFilterCam = NULL;
	}

	if (g_pGraphBuilder != NULL)
	{
		g_pGraphBuilder->Release();
		g_pGraphBuilder = NULL;
	}

	if (g_pCaptureGraphBuilder != NULL)
	{
		g_pCaptureGraphBuilder->Release();
		g_pCaptureGraphBuilder = NULL;
	}

	this->activeCameraIndex = -1;
}

/// <summary>
/// Show the properties dialog for the specified webcam
/// </summary>
void CameraMethods::DisplayCameraPropertiesDialog(int camIndex)
{
	if (camIndex >= Count)
		throw gcnew ArgumentException("Camera index is out of bounds: " + Count.ToString());

	if (g_aCameraInfo[camIndex].pMoniker == NULL)
		throw gcnew ArgumentException("There is no camera at index: " + camIndex.ToString());

	HRESULT hr = S_OK;
	IBaseFilter *pFilter = NULL;
	ISpecifyPropertyPages *pProp = NULL;
	IMoniker *pMoniker = g_aCameraInfo[camIndex].pMoniker;
	pMoniker->AddRef();

	// Create a filter graph for the moniker
	if (SUCCEEDED(hr))
	{
		hr = pMoniker->BindToObject(NULL, NULL, IID_IBaseFilter, (LPVOID*)&pFilter);
	}

	// See if it implements a property page
	if (SUCCEEDED(hr))
	{
		hr = pFilter->QueryInterface(IID_ISpecifyPropertyPages, (LPVOID*)&pProp);
	}

	// Show the property page
	if (SUCCEEDED(hr))
	{
		FILTER_INFO filterinfo;
		hr = pFilter->QueryFilterInfo(&filterinfo);

		IUnknown *pFilterUnk = NULL;
		if (SUCCEEDED(hr))
		{
			hr = pFilter->QueryInterface(IID_IUnknown, (LPVOID*)&pFilterUnk);
		}

		if (SUCCEEDED(hr))
		{
			CAUUID caGUID;
			pProp->GetPages(&caGUID);

			OleCreatePropertyFrame(
				NULL,                   // Parent window
				0, 0,                   // Reserved
				filterinfo.achName,     // Caption for the dialog box
				1,                      // Number of objects (just the filter)
				&pFilterUnk,            // Array of object pointers. 
				caGUID.cElems,          // Number of property pages
				caGUID.pElems,          // Array of property page CLSIDs
				0,                      // Locale identifier
				0, NULL                 // Reserved
				);
		}

		if (pFilterUnk != NULL)
		{
			pFilterUnk->Release();
			pFilterUnk = NULL;
		}
	}

	if (pProp != NULL)
	{
		pProp->Release();
		pProp = NULL;
	}

	if (pMoniker != NULL)
	{
		pMoniker->Release();
		pMoniker = NULL;
	}

	if (pFilter != NULL)
	{
		pFilter->Release();
		pFilter = NULL;
	}

	if (!SUCCEEDED(hr))
		throw gcnew COMException("Error displaying camera properties dialog", hr);
}

/// <summary>
/// Releases all unmanaged resources
/// </summary>
void CameraMethods::CleanupCameraInfo()
{
	for (int n = 0; n < MAX_CAMERAS; n++)
	{
		SysFreeString(g_aCameraInfo[n].bstrName);
		g_aCameraInfo[n].bstrName = NULL;
		if (g_aCameraInfo[n].pMoniker != NULL)
		{
			g_aCameraInfo[n].pMoniker->Release();
			g_aCameraInfo[n].pMoniker = NULL;
		}
	}
}


/// <summary>
/// Setup the callback functionality for DirectShow
/// </summary>
HRESULT CameraMethods::ConfigureSampleGrabber(IBaseFilter *pIBaseFilter)
{
	HRESULT hr = S_OK;

	ISampleGrabber *pGrabber = NULL;

	hr = pIBaseFilter->QueryInterface(IID_ISampleGrabber, (void**)&pGrabber);
	if (SUCCEEDED(hr))
	{
		AM_MEDIA_TYPE mt;
		ZeroMemory(&mt, sizeof(AM_MEDIA_TYPE));
		mt.majortype = MEDIATYPE_Video;
		mt.subtype = MEDIASUBTYPE_RGB24;
		mt.formattype = FORMAT_VideoInfo;
		hr = pGrabber->SetMediaType(&mt);
	}

	if (SUCCEEDED(hr))
	{
		hr = pGrabber->SetCallback(new SampleGrabberCB(), 1);
	}

	if (pGrabber != NULL)
	{
		pGrabber->Release();
		pGrabber = NULL;
	}

	return hr;
}

void CameraMethods::GetCaptureSizes(int index, IList<Tuple<int,int,int>^> ^ sizes)
{
	sizes->Clear();

	HRESULT hr = S_OK;

	if (index >= Count)
		throw gcnew ArgumentException("Camera index is out of bounds: " + Count.ToString());

	if (g_aCameraInfo[index].pMoniker == NULL)
		throw gcnew ArgumentException("There is no camera at index: " + index.ToString());

	if (g_pGraphBuilder != NULL)
		throw gcnew ArgumentException("Graph Builder was null");

	IMoniker *pMoniker = g_aCameraInfo[index].pMoniker;
	pMoniker->AddRef();

	IBaseFilter* pCap = NULL;
	// Build the camera from the moniker
	if (SUCCEEDED(hr))
		hr = pMoniker->BindToObject(NULL, NULL, IID_IBaseFilter, (LPVOID*)&pCap);

	ICaptureGraphBuilder2* captureGraphBuilder = NULL;
	if (SUCCEEDED(hr))
	{
		hr = CoCreateInstance(CLSID_CaptureGraphBuilder2,
			NULL,
			CLSCTX_INPROC,
			IID_ICaptureGraphBuilder2,
			(LPVOID*)&captureGraphBuilder);
	}

	IAMStreamConfig *pConfig = NULL;
	if(SUCCEEDED(hr))
		hr = captureGraphBuilder->FindInterface(
		&PIN_CATEGORY_CAPTURE,
		&MEDIATYPE_Video, 
		pCap, // Pointer to the capture filter.
		IID_IAMStreamConfig, (void**)&pConfig);

	int iCount = 0, iSize = 0;
	if(SUCCEEDED(hr))
		hr = pConfig->GetNumberOfCapabilities(&iCount, &iSize);

	// Check the size to make sure we pass in the correct structure.
	if (SUCCEEDED(hr) && iSize == sizeof(VIDEO_STREAM_CONFIG_CAPS))
	{
		// Use the video capabilities structure.
		for (int iFormat = 0; iFormat < iCount; iFormat++)
		{
			VIDEO_STREAM_CONFIG_CAPS scc;
			AM_MEDIA_TYPE *pmt;
			/* Note:  Use of the VIDEO_STREAM_CONFIG_CAPS structure to configure a video device is 
			deprecated. Although the caller must allocate the buffer, it should ignore the 
			contents after the method returns. The capture device will return its supported 
			formats through the pmt parameter. */
			hr = pConfig->GetStreamCaps(iFormat, &pmt, (BYTE*)&scc);
			if (SUCCEEDED(hr))
			{
				/* Examine the format, and possibly use it. */
				if (pmt->formattype == FORMAT_VideoInfo) {
					// Check the buffer size.
					if (pmt->cbFormat >= sizeof(VIDEOINFOHEADER))
					{
						VIDEOINFOHEADER *pVih =  reinterpret_cast<VIDEOINFOHEADER*>(pmt->pbFormat);
						BITMAPINFOHEADER *bmiHeader = &pVih->bmiHeader;

						int width = bmiHeader->biWidth;
						int height = bmiHeader->biHeight;
						int bitCount = bmiHeader->biBitCount;

						sizes->Add( gcnew Tuple<int,int,int>( width, height, bitCount ) );
					}
				}

				// Delete the media type when you are done.
				MyDeleteMediaType(pmt);
			}
		}
	}

	// Cleanup
	if (pMoniker != NULL)
	{
		pMoniker->Release();
		pMoniker = NULL;
	}
}

IList<Tuple<int,int,int>^> ^ CameraMethods::CaptureSizes::get()
{
	IList<Tuple<int,int,int>^> ^ result = gcnew List<Tuple<int,int,int>^>();

	GetCaptureSizes( ActiveCameraIndex, result );

	return result;
}

// If bpp is -1, the first format matching the width and height is selected.
// based on http://stackoverflow.com/questions/7383372/cant-make-iamstreamconfig-setformat-to-work-with-lifecam-studio
HRESULT CameraMethods::SetCaptureFormat(IBaseFilter* pCap, int width, int height, int bpp)
{
	HRESULT hr = S_OK;

	IAMStreamConfig *pConfig = NULL;
	hr = g_pCaptureGraphBuilder->FindInterface(
		&PIN_CATEGORY_CAPTURE,
		&MEDIATYPE_Video, 
		pCap, // Pointer to the capture filter.
		IID_IAMStreamConfig, (void**)&pConfig);
	if (!SUCCEEDED(hr)) return hr;

	int iCount = 0, iSize = 0;
	hr = pConfig->GetNumberOfCapabilities(&iCount, &iSize);
	if (!SUCCEEDED(hr)) return hr;

	// Check the size to make sure we pass in the correct structure.
	if (iSize == sizeof(VIDEO_STREAM_CONFIG_CAPS))
	{
		// Use the video capabilities structure.
		for (int iFormat = 0; iFormat < iCount; iFormat++)
		{
				VIDEO_STREAM_CONFIG_CAPS scc;
				AM_MEDIA_TYPE *pmt;
				/* Note:  Use of the VIDEO_STREAM_CONFIG_CAPS structure to configure a video device is 
				deprecated. Although the caller must allocate the buffer, it should ignore the 
				contents after the method returns. The capture device will return its supported 
				formats through the pmt parameter. */
				hr = pConfig->GetStreamCaps(iFormat, &pmt, (BYTE*)&scc);
				if (SUCCEEDED(hr))
				{
					/* Examine the format, and possibly use it. */
					if (pmt->formattype == FORMAT_VideoInfo) {
						// Check the buffer size.
						if (pmt->cbFormat >= sizeof(VIDEOINFOHEADER))
						{
								VIDEOINFOHEADER *pVih =  reinterpret_cast<VIDEOINFOHEADER*>(pmt->pbFormat);
								BITMAPINFOHEADER *bmiHeader = &pVih->bmiHeader;

								/* Access VIDEOINFOHEADER members through pVih. */
								if( bmiHeader->biWidth == width && bmiHeader->biHeight == height && ( bmiHeader->biBitCount == -1 || bmiHeader->biBitCount == bpp) )
								{
									hr = pConfig->SetFormat(pmt);

									break;
								}
						}
					}

					// Delete the media type when you are done.
					MyDeleteMediaType(pmt);
				}
		}
	}

	return hr;
}
