#include "CommandVerbs.h"

IFACEMETHODIMP CopyCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
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
	return SHStrDup(L"Compare names here", ppszName);
}

IFACEMETHODIMP CompareNamesCommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {
	if (GetSettings().EnableCompareNames) {
		bool hasData, isCopy, isMove;
		if (GetClipboardInfo(hasData, isCopy, isMove)) {
			if (hasData && isCopy) {
				*pCmdState = ECS_ENABLED;
				return S_OK;
			}
		}
	}
	*pCmdState = ECS_HIDDEN;
	return S_OK;
}

IFACEMETHODIMP HardlinkCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	return SHStrDup(L"Hardlink here", ppszName);
}

IFACEMETHODIMP HardlinkCommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {
	if (GetSettings().EnableHardlink) {
		bool hasData, isCopy, isMove;
		if (GetClipboardInfo(hasData, isCopy, isMove)) {
			if (hasData && isCopy) {
				*pCmdState = ECS_ENABLED;
				return S_OK;
			}
		}
	}
	*pCmdState = ECS_HIDDEN;
	return S_OK;
}

IFACEMETHODIMP NukeFilesCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	return SHStrDup(L"Nuke files here", ppszName);
}

IFACEMETHODIMP NukeFilesCommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {
	if (GetSettings().EnableNukeFile) {
		*pCmdState = ECS_ENABLED;
	}
	else {
		*pCmdState = ECS_HIDDEN;
	}
	return S_OK;
}

IFACEMETHODIMP NukeFoldersCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	return SHStrDup(L"Nuke folders here", ppszName);
}

IFACEMETHODIMP NukeFoldersCommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {
	if (GetSettings().EnableNukeFolder) {
		*pCmdState = ECS_ENABLED;
	}
	else {
		*pCmdState = ECS_HIDDEN;
	}
	return S_OK;
}

IFACEMETHODIMP CleanSourceCommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	return SHStrDup(L"Clean source here", ppszName);
}

IFACEMETHODIMP CleanSourceCommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {
	if (GetSettings().EnableCleanSource) {
		*pCmdState = ECS_ENABLED;
	}
	else {
		*pCmdState = ECS_HIDDEN;
	}
	return S_OK;
}

IFACEMETHODIMP Custom1CommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	return SHStrDup(GetSettings().CustomName1.c_str(), ppszName);
}

IFACEMETHODIMP Custom2CommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	return SHStrDup(GetSettings().CustomName2.c_str(), ppszName);
}

IFACEMETHODIMP Custom3CommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	return SHStrDup(GetSettings().CustomName3.c_str(), ppszName);
}

IFACEMETHODIMP Custom4CommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	return SHStrDup(GetSettings().CustomName4.c_str(), ppszName);
}

IFACEMETHODIMP Custom5CommandVerb::GetTitle(IShellItemArray*, LPWSTR* ppszName)
{
	return SHStrDup(GetSettings().CustomName5.c_str(), ppszName);
}

IFACEMETHODIMP Custom1CommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {
	if (GetSettings().Custom1Enabled) {
		*pCmdState = ECS_ENABLED;
	}
	else {
		*pCmdState = ECS_HIDDEN;
	}
	return S_OK;
}

IFACEMETHODIMP Custom2CommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {
	if (GetSettings().Custom2Enabled) {
		*pCmdState = ECS_ENABLED;
	}
	else {
		*pCmdState = ECS_HIDDEN;
	}
	return S_OK;
}

IFACEMETHODIMP Custom3CommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {
	if (GetSettings().Custom3Enabled) {
		*pCmdState = ECS_ENABLED;
	}
	else {
		*pCmdState = ECS_HIDDEN;
	}
	return S_OK;
}

IFACEMETHODIMP Custom4CommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {
	if (GetSettings().Custom4Enabled) {
		*pCmdState = ECS_ENABLED;
	}
	else {
		*pCmdState = ECS_HIDDEN;
	}
	return S_OK;
}

IFACEMETHODIMP Custom5CommandVerb::GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) {
	if (GetSettings().Custom5Enabled) {
		*pCmdState = ECS_ENABLED;
	}
	else {
		*pCmdState = ECS_HIDDEN;
	}
	return S_OK;
}