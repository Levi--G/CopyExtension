#pragma once

#include "Dll.h"
#include "InvokeCSCommandVerb.h"
#include "CommandTypes.h"

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
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BBB-C1A7-11F1-A431-FC9D053267A4"))
	MoveCommandVerb : public GenericInvokeCSCommandVerb<MoveCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Move; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BBC-C1A7-11F1-A431-FC9D053267A4"))
	CompareCommandVerb : public GenericInvokeCSCommandVerb<CompareCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Compare; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BBD-C1A7-11F1-A431-FC9D053267A4"))
	CompareNamesCommandVerb : public GenericInvokeCSCommandVerb<CompareNamesCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::CompareNames; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BBE-C1A7-11F1-A431-FC9D053267A4"))
	HardlinkCommandVerb : public GenericInvokeCSCommandVerb<HardlinkCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Hardlink; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BBF-C1A7-11F1-A431-FC9D053267A4"))
	NukeFilesCommandVerb : public GenericInvokeCSCommandVerb<NukeFilesCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::NukeFiles; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BC0-C1A7-11F1-A431-FC9D053267A4"))
	NukeFoldersCommandVerb : public GenericInvokeCSCommandVerb<NukeFoldersCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::NukeFolders; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BC1-C1A7-11F1-A431-FC9D053267A4"))
	CleanSourceCommandVerb : public GenericInvokeCSCommandVerb<CleanSourceCommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::CleanSource; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BCA-C1A7-11F1-A431-FC9D053267A4"))
	Custom1CommandVerb : public GenericInvokeCSCommandVerb<Custom1CommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Custom1; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BCB-C1A7-11F1-A431-FC9D053267A4"))
	Custom2CommandVerb : public GenericInvokeCSCommandVerb<Custom2CommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Custom2; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BCC-C1A7-11F1-A431-FC9D053267A4"))
	Custom3CommandVerb : public GenericInvokeCSCommandVerb<Custom3CommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Custom3; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BCD-C1A7-11F1-A431-FC9D053267A4"))
	Custom4CommandVerb : public GenericInvokeCSCommandVerb<Custom4CommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Custom4; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};

class __declspec(uuid("E4F42BCE-C1A7-11F1-A431-FC9D053267A4"))
	Custom5CommandVerb : public GenericInvokeCSCommandVerb<Custom5CommandVerb> {
public:
	IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override;
	CommandType GetVerbType() override { return CommandType::Custom5; }
	IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE*) override;
};