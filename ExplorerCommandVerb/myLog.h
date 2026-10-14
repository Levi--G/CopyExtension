#pragma once

#include <string>

std::wstring s2ws(const std::string& str);
std::string ws2s(const std::wstring& wstr);
void myLog(const std::string& text);
void myLog(const std::wstring& message);