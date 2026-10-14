// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved

#pragma once
#define STRICT_TYPED_ITEMIDS
#define UNICODE 1
#include <windows.h>
#include "settings.h"
#include "CsApi.h"

void DllAddRef();
void DllRelease();

Settings& GetSettings();
CsApi& GetCsApi();

extern HINSTANCE g_hInst;