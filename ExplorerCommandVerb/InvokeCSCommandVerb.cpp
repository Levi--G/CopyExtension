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

// Trim from the end (in place)
inline void rtrim(std::wstring& s) {
	s.erase(std::find_if(s.rbegin(), s.rend(), [](wchar_t ch) {
		return ch != L'\\';
	}).base(), s.end());
}

void StartProcess(InvocationContext* pv) {
	std::wstring dllPath;
	GetDllPath(dllPath);
	std::wstring exe = L"\"" + dllPath + L"\\CopyExtensionAOT.exe\"";
	//myLog(L"Starting process: " + exe);
	auto target = pv->target;
	if (target.ends_with(L"\\")) {
		rtrim(target);
	}
	//myLog(target);
	std::wstring command = exe + L" " + CommandTypeToString(pv->command, target);
	//myLog(command);
	STARTUPINFO si;
	PROCESS_INFORMATION pi; // The function returns this
	ZeroMemory(&si, sizeof(si));
	si.cb = sizeof(si);
	ZeroMemory(&pi, sizeof(pi));

	if (CreateProcessW(NULL, command.data(), NULL, NULL, FALSE, NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW | DETACHED_PROCESS | CREATE_NEW_PROCESS_GROUP, NULL, NULL, &si, &pi))
	{
		//myLog("Process created successfully");
		CloseHandle(pi.hProcess);
		CloseHandle(pi.hThread);
	}
	//else
	//{
		//auto err = GetLastError();
		//myLog("Failed to create process. Error: " + std::to_string(err));
	//}
}

IFACEMETHODIMP InvokeCSCommandVerb::Invoke(IShellItemArray* psia, IBindCtx*)
{
	//myLog("InvokeCSCommandVerb::Invoke called");
	DWORD count = 0;
	psia->GetCount(&count);
	//myLog("InvokeCSCommandVerb::Invoke count: " + std::to_string(count));
	IShellItem2* psi;
	HRESULT hr = GetItemAt(psia, 0, IID_PPV_ARGS(&psi));
	if (SUCCEEDED(hr))
	{
		//myLog("InvokeCSCommandVerb::Invoke got item");
		PWSTR pszName;
		hr = psi->GetDisplayName(SIGDN_FILESYSPATH, &pszName);
		if (SUCCEEDED(hr))
		{
			//myLog("InvokeCSCommandVerb::Invoke got display name");
			InvocationContext* pContext = new InvocationContext();
			pContext->command = GetVerbType();
			pContext->target = std::wstring(pszName);
			pContext->length = (int32_t)wcslen(pszName);
			/*SHCreateThread(s_ThreadProc, pContext, CTF_COINIT_STA | CTF_PROCESS_REF, NULL);*/
			StartProcess(pContext);
			//myLog("InvokeCSCommandVerb::Invoke started process");
			delete pContext;
			CoTaskMemFree(pszName);
		}

		psi->Release();
	}
	//myLog("InvokeCSCommandVerb::Invoke finished");
	return S_OK;
}

DWORD __stdcall InvokeCSCommandVerb::s_ThreadProc(void* pv)
{
	//myLog("Thread proc started");
	InvocationContext* pContext = (InvocationContext*)pv;
	GetCsApi().ExecuteCommand(pContext);
	delete pContext;
	//myLog("Thread proc finished");
	return S_OK;
}
