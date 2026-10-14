
#include "myLog.h"
#include <fstream>
#include <codecvt>
#include <locale>
#include <Windows.h>

std::wstring s2ws(const std::string& str)
{
	return std::wstring_convert<std::codecvt_utf8<wchar_t>>().from_bytes(str);
}

std::string ws2s(const std::wstring& wstr)
{
	return std::wstring_convert<std::codecvt_utf8<wchar_t>>().to_bytes(wstr);
}

void myLog(const std::string& text)
{
	std::ofstream logFile("D:\\log.txt", std::ios::app);
	if (logFile)
	{
		logFile << text << '\n';
	}
}

void myLog(const std::wstring& message)
{
	if (message.empty())
	{
		myLog("");
		return;
	}
	myLog(ws2s(message));
}
