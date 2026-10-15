// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved

#include "dll.h"
#include "CommandVerbs.h"
#include <new>
#include <codecvt>
#include <locale>

typedef HRESULT(*PFNCREATEINSTANCE)(REFIID riid, void** ppvObject);
struct CLASS_OBJECT_INIT
{
	const CLSID* pClsid;
	PFNCREATEINSTANCE pfnCreate;
};

template <typename T>
HRESULT Verb_CreateInstance(REFIID riid, void** ppv)
{
	*ppv = NULL;
	T* pVerb = new (std::nothrow) T();
	HRESULT hr = pVerb ? S_OK : E_OUTOFMEMORY;
	if (SUCCEEDED(hr))
	{
		pVerb->QueryInterface(riid, ppv);
		pVerb->Release();
	}
	return hr;
}

// add classes supported by this module here
const CLASS_OBJECT_INIT c_rgClassObjectInit[] =
{
	{ &__uuidof(CopyCommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<CopyCommandVerb>) },
	{ &__uuidof(MoveCommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<MoveCommandVerb>) },
	{ &__uuidof(CompareCommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<CompareCommandVerb>) },
	{ &__uuidof(CompareNamesCommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<CompareNamesCommandVerb>) },
	{ &__uuidof(HardlinkCommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<HardlinkCommandVerb>) },
	{ &__uuidof(NukeFilesCommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<NukeFilesCommandVerb>) },
	{ &__uuidof(NukeFoldersCommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<NukeFoldersCommandVerb>) },
	{ &__uuidof(CleanSourceCommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<CleanSourceCommandVerb>) },
	{ &__uuidof(Custom1CommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<Custom1CommandVerb>) },
	{ &__uuidof(Custom2CommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<Custom2CommandVerb>) },
	{ &__uuidof(Custom3CommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<Custom3CommandVerb>) },
	{ &__uuidof(Custom4CommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<Custom4CommandVerb>) },
	{ &__uuidof(Custom5CommandVerb),	(PFNCREATEINSTANCE)(Verb_CreateInstance<Custom5CommandVerb>) },
};


long g_cRefModule = 0;

// Handle the the DLL's module
HINSTANCE g_hInst = NULL;

// Standard DLL functions
STDAPI_(BOOL) DllMain(HINSTANCE hInstance, DWORD dwReason, void*)
{
	if (dwReason == DLL_PROCESS_ATTACH)
	{
		g_hInst = hInstance;
		DisableThreadLibraryCalls(hInstance);
	}
	return TRUE;
}

void DllAddRef()
{
	InterlockedIncrement(&g_cRefModule);
}

void DllRelease()
{
	InterlockedDecrement(&g_cRefModule);
}

class CClassFactory : public IClassFactory
{
public:
	static HRESULT CreateInstance(REFCLSID clsid, const CLASS_OBJECT_INIT* pClassObjectInits, size_t cClassObjectInits, REFIID riid, void** ppv)
	{
		*ppv = NULL;
		HRESULT hr = CLASS_E_CLASSNOTAVAILABLE;
		for (size_t i = 0; i < cClassObjectInits; i++)
		{
			if (clsid == *pClassObjectInits[i].pClsid)
			{
				IClassFactory* pClassFactory = new (std::nothrow) CClassFactory(pClassObjectInits[i].pfnCreate);
				hr = pClassFactory ? S_OK : E_OUTOFMEMORY;
				if (SUCCEEDED(hr))
				{
					hr = pClassFactory->QueryInterface(riid, ppv);
					pClassFactory->Release();
				}
				break; // match found
			}
		}
		return hr;
	}

	CClassFactory(PFNCREATEINSTANCE pfnCreate) : _cRef(1), _pfnCreate(pfnCreate)
	{
		DllAddRef();
	}

	// IUnknown
	IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv)
	{
		static const QITAB qit[] =
		{
			QITABENT(CClassFactory, IClassFactory),
			{ 0 }
		};
		return QISearch(this, qit, riid, ppv);
	}

	IFACEMETHODIMP_(ULONG) AddRef()
	{
		return InterlockedIncrement(&_cRef);
	}

	IFACEMETHODIMP_(ULONG) Release()
	{
		long cRef = InterlockedDecrement(&_cRef);
		if (cRef == 0)
		{
			delete this;
		}
		return cRef;
	}

	// IClassFactory
	IFACEMETHODIMP CreateInstance(IUnknown* punkOuter, REFIID riid, void** ppv)
	{
		return punkOuter ? CLASS_E_NOAGGREGATION : _pfnCreate(riid, ppv);
	}

	IFACEMETHODIMP LockServer(BOOL fLock)
	{
		if (fLock)
		{
			DllAddRef();
		}
		else
		{
			DllRelease();
		}
		return S_OK;
	}

private:
	~CClassFactory()
	{
		DllRelease();
	}

	long _cRef;
	PFNCREATEINSTANCE _pfnCreate;
};

//entry

STDAPI DllGetClassObject(REFCLSID clsid, REFIID riid, void** ppv)
{
	return CClassFactory::CreateInstance(clsid, c_rgClassObjectInit, ARRAYSIZE(c_rgClassObjectInit), riid, ppv);
}

STDAPI DllCanUnloadNow()
{
	// Only allow the DLL to be unloaded after all outstanding references have been released
	return (g_cRefModule == 0) ? S_OK : S_FALSE;
}

Settings& GetSettings() {
	static Settings settings;
	return settings;
}

std::wstring s2ws(const std::string& str)
{
	return std::wstring_convert<std::codecvt_utf8<wchar_t>>().from_bytes(str);
}

std::string ws2s(const std::wstring& wstr)
{
	return std::wstring_convert<std::codecvt_utf8<wchar_t>>().to_bytes(wstr);
}