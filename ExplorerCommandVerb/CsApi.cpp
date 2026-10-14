#include "CsApi.h"
#include "myLog.h"

template <typename T>
T LoadSymbol(HMODULE module, const char* name)
{
	if (module == nullptr)
	{
		return nullptr;
	}
	FARPROC proc = ::GetProcAddress(module, name);
	if (proc == nullptr)
	{
		return nullptr;
	}
	return reinterpret_cast<T>(proc);
}

CsApi::CsApi()
{
	myLog("CsApi::CsApi() called");
	module = ::LoadLibraryW(L"CopyExtensionAOT.dll");
	executeCommand = LoadSymbol<ExecuteCommand_fn>(module, "ExecuteCommand");
}

//void CsApi::ExecuteCommand(CommandType command, LPCWSTR target, int32_t length)
//{
//	if (executeCommand == nullptr)
//	{
//		return;
//	}
//	executeCommand(command, target, length);
//}

void CsApi::ExecuteCommand(InvocationContext* context)
{
	if (executeCommand == nullptr)
	{
		return;
	}
	executeCommand(context->command, context->target.c_str(), context->length);
}

CsApi::~CsApi()
{
	myLog("CsApi::~CsApi() called");
	executeCommand = nullptr;
	FreeLibrary(module);
}
