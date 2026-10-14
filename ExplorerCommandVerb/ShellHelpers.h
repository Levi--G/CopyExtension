//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//// PARTICULAR PURPOSE.
////
//// Copyright (c) Microsoft Corporation. All rights reserved
//
#pragma once

#define STRICT_TYPED_ITEMIDS    // in case you do IDList stuff you want this on for better type saftey
#define UNICODE 1

#include <windows.h>
#include <shlobj.h>             // shell stuff
#include <shlwapi.h>            // QISearch, easy way to implement QI
#include <strsafe.h>

#pragma comment(lib, "shlwapi.lib")     // link to this

__inline HRESULT GetItemAt(IShellItemArray *psia, DWORD i, REFIID riid, void **ppv)
{
    *ppv = NULL;
    IShellItem *psi = NULL;     // avoid error C4701
    HRESULT hr = psia ? psia->GetItemAt(i, &psi) : E_NOINTERFACE;
    if (SUCCEEDED(hr))
    {
        hr = psi->QueryInterface(riid, ppv);
        psi->Release();
    }
    return hr;
}