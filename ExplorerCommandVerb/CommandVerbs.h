#pragma once

#include "Dll.h"
#include "InvokeCSCommandVerb.h"

// use UUDIGEN.EXE to generate unique CLSID values for your objects
/*

E4F42BBA-C1A7-11F1-A431-FC9D053267A4
E4F42BBB-C1A7-11F1-A431-FC9D053267A4
E4F42BBC-C1A7-11F1-A431-FC9D053267A4
E4F42BBD-C1A7-11F1-A431-FC9D053267A4
E4F42BBE-C1A7-11F1-A431-FC9D053267A4
E4F42BBF-C1A7-11F1-A431-FC9D053267A4
E4F42BC0-C1A7-11F1-A431-FC9D053267A4
E4F42BC1-C1A7-11F1-A431-FC9D053267A4
E4F42BC2-C1A7-11F1-A431-FC9D053267A4

*/

class __declspec(uuid("E4F42BBA-C1A7-11F1-A431-FC9D053267A4"))
	CopyCommandVerb : public GenericInvokeCSCommandVerb<CopyCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Copy; }
	IFACEMETHODIMP GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) override;
};

class __declspec(uuid("E4F42BBB-C1A7-11F1-A431-FC9D053267A4"))
	MoveCommandVerb : public GenericInvokeCSCommandVerb<MoveCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Move; }
	IFACEMETHODIMP GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) override;
};

class __declspec(uuid("E4F42BBC-C1A7-11F1-A431-FC9D053267A4"))
	CompareCommandVerb : public GenericInvokeCSCommandVerb<CompareCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Compare; }
	IFACEMETHODIMP GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) override;
};

class __declspec(uuid("E4F42BBD-C1A7-11F1-A431-FC9D053267A4"))
	CompareNamesCommandVerb : public GenericInvokeCSCommandVerb<CompareNamesCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::CompareNames; }
	IFACEMETHODIMP GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) override;
};

class __declspec(uuid("E4F42BBE-C1A7-11F1-A431-FC9D053267A4"))
	HardlinkCommandVerb : public GenericInvokeCSCommandVerb<HardlinkCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Hardlink; }
	IFACEMETHODIMP GetState(IShellItemArray* /*psia*/, BOOL /*fOkToBeSlow*/, EXPCMDSTATE* pCmdState) override;
};

class __declspec(uuid("E4F42BBF-C1A7-11F1-A431-FC9D053267A4"))
	NukeFilesCommandVerb : public GenericInvokeCSCommandVerb<NukeFilesCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::NukeFiles; }
};

class __declspec(uuid("E4F42BC0-C1A7-11F1-A431-FC9D053267A4"))
	NukeFoldersCommandVerb : public GenericInvokeCSCommandVerb<NukeFoldersCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::NukeFolders; }
};

class __declspec(uuid("E4F42BC1-C1A7-11F1-A431-FC9D053267A4"))
	CleanSourceCommandVerb : public GenericInvokeCSCommandVerb<CleanSourceCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::CleanSource; }
};