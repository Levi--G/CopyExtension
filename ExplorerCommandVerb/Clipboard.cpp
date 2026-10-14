#include "Dll.h"
#include "Clipboard.h"
#include <sstream>
#include <filesystem>
#include "myLog.h"
#include <shlobj.h>

bool DoesClipboardHaveData()
{
	bool data = false;
	if (OpenClipboard(NULL))
	{
		data = IsClipboardFormatAvailable(CF_HDROP) || IsClipboardFormatAvailable(CF_TEXT);
		CloseClipboard();
	}SendMessage
	return data;
}

/// <summary>
/// malloc allocated string or null, free with free!
/// </summary>
/// <returns></returns>
LPTSTR GetClipboardText()
{
	myLog("GetClipboardText called");
	HGLOBAL handle = NULL;
	LPTSTR lptstr = NULL;
	LPTSTR dup = NULL;
	if (OpenClipboard(NULL)) {
		myLog("Clipboard opened successfully");
		if (IsClipboardFormatAvailable(CF_UNICODETEXT)) {
			myLog("Clipboard has CF_TEXT format available");
			handle = GetClipboardData(CF_UNICODETEXT);
			if (handle != NULL) {
				myLog("Clipboard data handle obtained");
				lptstr = (LPTSTR)GlobalLock(handle);
				if (lptstr != NULL) {
					myLog("Clipboard data locked successfully");
					dup = _wcsdup(lptstr);
					myLog("Clipboard data duplicated successfully");
					GlobalUnlock(handle);
				}
			}
		}
		CloseClipboard();
	}
	myLog(L"GetClipboardText returning: " + std::wstring(dup ? dup : L"(null)"));
	return dup;
}

bool DoesClipboardHaveValidPaths()
{
	myLog("DoesClipboardHaveValidPaths called");
	bool ok = false;
	auto data = GetClipboardText();
	if (data)
	{
		myLog(L"Clipboard data: " + std::wstring(data));
		ok = true;
		std::wistringstream stream;
		stream.str(std::wstring(data));
		std::wstring line;
		while (std::getline(stream, line)) {
			myLog(L"Checking path: " + line);
			if (!std::filesystem::exists(line)) {
				myLog(L"Path does not exist: " + line);
				ok = false;
			}
		}
		free(data);
	}
	myLog("DoesClipboardHaveValidPaths returning: " + std::to_string(ok));
	return ok;
}
bool GetClipboardInfo(bool& hasData, bool& isCopy, bool& isMove) {
	hasData = false;
	isCopy = false;
	isMove = false;
	myLog("GetClipboardInfo called");
	if (OpenClipboard(NULL)) {
		myLog("Clipboard opened successfully");
		if (IsClipboardFormatAvailable(CF_HDROP)) {
			myLog("Clipboard has CF_HDROP format available");
			HDROP hDrop = (HDROP)GetClipboardData(CF_HDROP);
			if (hDrop) {
				hasData = true;
				//UINT fileCount = DragQueryFileW(hDrop, 0xFFFFFFFF, nullptr, 0);
				UINT uFormatPreferredDropEffect = RegisterClipboardFormatW(CFSTR_PREFERREDDROPEFFECT);
				if (IsClipboardFormatAvailable(uFormatPreferredDropEffect)) {
					myLog("Clipboard has CFSTR_PREFERREDDROPEFFECT format available");
					HGLOBAL hGlobal = GetClipboardData(uFormatPreferredDropEffect);
					if (hGlobal) {
						DWORD* pDropEffect = (DWORD*)GlobalLock(hGlobal);
						if (pDropEffect) {
							// Inspect the flag
							if (*pDropEffect & DROPEFFECT_MOVE) {
								isMove = true;
							}
							else if (*pDropEffect & DROPEFFECT_COPY) {
								isCopy = true;
							}
							GlobalUnlock(hGlobal);
						}
					}
				}
				myLog("After CFSTR_PREFERREDDROPEFFECT: hasData=" + std::to_string(hasData) + ", isCopy=" + std::to_string(isCopy) + ", isMove=" + std::to_string(isMove));
				if (!isMove && !isCopy) {
					isCopy = true;
					isMove = true;
				}
			}
		}
		else if (IsClipboardFormatAvailable(CF_UNICODETEXT)) {
			hasData = true;
			isCopy = true;
			isMove = true;
		}
		CloseClipboard();
		myLog("After CloseClipboard: hasData=" + std::to_string(hasData) + ", isCopy=" + std::to_string(isCopy) + ", isMove=" + std::to_string(isMove));
		return true;
	}
	return false;
}