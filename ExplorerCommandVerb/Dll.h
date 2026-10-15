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
#include <string>
#include "settings.h"

void DllAddRef();
void DllRelease();

Settings& GetSettings();

extern HINSTANCE g_hInst;

std::wstring s2ws(const std::string& str);
std::string ws2s(const std::wstring& wstr);