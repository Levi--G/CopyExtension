#include "CommandVerbs.h"

IFACEMETHODIMP CopyCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	/*const wchar_t* constMyString = L"Copy here";
	return SHStrDup(constMyString, ppszName);*/
	return SHStrDup(L"Copy here", ppszName);
}

IFACEMETHODIMP CopyCommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {

	bool hasData, isCopy, isMove;
	if (GetClipboardInfo(hasData, isCopy, isMove)) {
		if (hasData && isCopy) {
			*pCmdState = ECS_ENABLED;
			return S_OK;
		}
	}
	*pCmdState = ECS_HIDDEN;
	return S_OK;
}

IFACEMETHODIMP MoveCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	/*const wchar_t* constMyString = L"Move here";
	return SHStrDup(constMyString, ppszName);*/
	return SHStrDup(L"Move here", ppszName);
}

IFACEMETHODIMP MoveCommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {

	bool hasData, isCopy, isMove;
	if (GetClipboardInfo(hasData, isCopy, isMove)) {
		if (hasData && isMove) {
			*pCmdState = ECS_ENABLED;
			return S_OK;
		}
	}
	*pCmdState = ECS_HIDDEN;
	return S_OK;
}

IFACEMETHODIMP CompareCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	const wchar_t* constMyString = L"Compare here";
	return SHStrDup(constMyString, ppszName);
}

IFACEMETHODIMP CompareCommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {

	bool hasData, isCopy, isMove;
	if (GetClipboardInfo(hasData, isCopy, isMove)) {
		if (hasData && isCopy) {
			*pCmdState = ECS_ENABLED;
			return S_OK;
		}
	}
	*pCmdState = ECS_HIDDEN;
	return S_OK;
}

IFACEMETHODIMP CompareNamesCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	/*const wchar_t* constMyString = L"Move here";
	return SHStrDup(constMyString, ppszName);*/
	return SHStrDup(L"Compare names here", ppszName);
}

IFACEMETHODIMP CompareNamesCommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {

	bool hasData, isCopy, isMove;
	if (GetClipboardInfo(hasData, isCopy, isMove)) {
		if (hasData && isCopy) {
			*pCmdState = ECS_ENABLED;
			return S_OK;
		}
	}
	*pCmdState = ECS_HIDDEN;
	return S_OK;
}

IFACEMETHODIMP HardlinkCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	/*const wchar_t* constMyString = L"Move here";
	return SHStrDup(constMyString, ppszName);*/
	return SHStrDup(L"Hardlink here", ppszName);
}

IFACEMETHODIMP HardlinkCommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {

	bool hasData, isCopy, isMove;
	if (GetClipboardInfo(hasData, isCopy, isMove)) {
		if (hasData && isCopy) {
			*pCmdState = ECS_ENABLED;
			return S_OK;
		}
	}
	*pCmdState = ECS_HIDDEN;
	return S_OK;
}

IFACEMETHODIMP NukeFilesCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	/*const wchar_t* constMyString = L"Move here";
	return SHStrDup(constMyString, ppszName);*/
	return SHStrDup(L"Nuke files here", ppszName);
}

IFACEMETHODIMP NukeFoldersCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	/*const wchar_t* constMyString = L"Move here";
	return SHStrDup(constMyString, ppszName);*/
	return SHStrDup(L"Nuke folders here", ppszName);
}

IFACEMETHODIMP CleanSourceCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	/*const wchar_t* constMyString = L"Move here";
	return SHStrDup(constMyString, ppszName);*/
	return SHStrDup(L"Clean source here", ppszName);
}