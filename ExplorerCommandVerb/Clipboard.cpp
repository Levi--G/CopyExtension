#include "Dll.h"
#include "Clipboard.h"
#include <sstream>
#include <filesystem>
#include "myLog.h"
#include <shlobj.h>

//bool DoesClipboardHaveData()
//{
//	bool data = false;
//	if (OpenClipboard(NULL))
//	{
//		data = IsClipboardFormatAvailable(CF_HDROP) || IsClipboardFormatAvailable(CF_TEXT);
//		CloseClipboard();
//	}
//	return data;
//}
//
///// <summary>
///// malloc allocated string or null, free with free!
///// </summary>
///// <returns></returns>
//LPTSTR GetClipboardText()
//{
//	HGLOBAL handle = NULL;
//	LPTSTR lptstr = NULL;
//	LPTSTR dup = NULL;
//	if (OpenClipboard(NULL)) {
//		if (IsClipboardFormatAvailable(CF_UNICODETEXT)) {
//			handle = GetClipboardData(CF_UNICODETEXT);
//			if (handle != NULL) {
//				lptstr = (LPTSTR)GlobalLock(handle);
//				if (lptstr != NULL) {
//					dup = _wcsdup(lptstr);
//					GlobalUnlock(handle);
//				}
//			}
//		}
//		CloseClipboard();
//	}
//	return dup;
//}
//
//bool DoesClipboardHaveValidPaths()
//{
//	bool ok = false;
//	auto data = GetClipboardText();
//	if (data)
//	{
//		ok = true;
//		std::wistringstream stream;
//		stream.str(std::wstring(data));
//		std::wstring line;
//		while (std::getline(stream, line)) {
//			if (!std::filesystem::exists(line)) {
//				ok = false;
//			}
//		}
//		free(data);
//	}
//	return ok;
//}

bool GetClipboardInfo(bool& hasData, bool& isCopy, bool& isMove) {
	hasData = false;
	isCopy = false;
	isMove = false;
	if (OpenClipboard(NULL)) {
		if (IsClipboardFormatAvailable(CF_HDROP)) {
			HDROP hDrop = (HDROP)GetClipboardData(CF_HDROP);
			if (hDrop) {
				hasData = true;
				//UINT fileCount = DragQueryFileW(hDrop, 0xFFFFFFFF, nullptr, 0);
				UINT uFormatPreferredDropEffect = RegisterClipboardFormatW(CFSTR_PREFERREDDROPEFFECT);
				if (IsClipboardFormatAvailable(uFormatPreferredDropEffect)) {
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
		return true;
	}
	return false;
}