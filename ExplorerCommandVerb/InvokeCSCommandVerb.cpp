#include "InvokeCSCommandVerb.h"
#include "myLog.h"
#include <string>
#include <vector>
#include <tchar.h>
#include <filesystem>

void GetDllPath(std::wstring& dllPath)
{
	wchar_t buffer[MAX_PATH] = { 0 };
	GetModuleFileNameW(g_hInst, buffer, MAX_PATH);
	dllPath = std::filesystem::path(buffer).parent_path().wstring();
}

inline void ltrim(std::wstring& s) {
	s.erase(s.begin(), std::find_if(s.begin(), s.end(), [](wchar_t ch) {
		return ch != L'\\';
	}));
}

inline void rtrim(std::wstring& s) {
	s.erase(std::find_if(s.rbegin(), s.rend(), [](wchar_t ch) {
		return ch != L'\\';
	}).base(), s.end());
}

void InvokeCSCommandVerb::StartProcess(std::wstring& target) {
	std::wstring dllPath;
	GetDllPath(dllPath);
	std::wstring exe = L"\"" + dllPath + L"\\CopyExtension.exe\"";
	if (target.ends_with(L"\\")) {
		rtrim(target);
	}
	std::wstring command = exe + L" " + CommandTypeToString(GetVerbType(), target);
	STARTUPINFO si;
	PROCESS_INFORMATION pi;
	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);
	ZeroMemory(&pi, sizeof(pi));

	if (CreateProcessW(NULL, command.data(), NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW | DETACHED_PROCESS | CREATE_NEW_PROCESS_GROUP, NULL, NULL, &si, &pi))
	{
		CloseHandle(pi.hProcess);
		CloseHandle(pi.hThread);
	}
}

IFACEMETHODIMP InvokeCSCommandVerb::Invoke(IShellItemArray* psia, IBindCtx*)
{
	DWORD count = 0;
	psia->GetCount(&count);
	IShellItem2* psi;
	HRESULT hr = GetItemAt(psia, 0, IID_PPV_ARGS(&psi));
	if (SUCCEEDED(hr))
	{
		PWSTR pszName;
		hr = psi->GetDisplayName(SIGDN_FILESYSPATH, &pszName);
		if (SUCCEEDED(hr))
		{
			auto target = std::wstring(pszName);
			StartProcess(target);
			CoTaskMemFree(pszName);
		}

		psi->Release();
	}
	return S_OK;
}
