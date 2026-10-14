#pragma once
#include <windows.h>
#include <stdint.h>
#include <string>

enum class CommandType : int32_t
{
	Unknown = 0,
	Copy = 1,
	Move = 2,
	Compare = 3,
	CompareNames = 4,
	Hardlink = 5,
	NukeFiles = 6,
	NukeFolders = 7,
	CleanSource = 8,
};

inline std::wstring CommandTypeToString(CommandType type, std::wstring target)
{
	switch (type)
	{
	case CommandType::Copy:
		return L"copy -c \"" + target + L"\"";
	case CommandType::Move:
		return L"move -c \"" + target + L"\"";
	case CommandType::Compare:
		return L"compare -c \"" + target + L"\"";
	case CommandType::CompareNames:
		return L"compare --names -c \"" + target + L"\"";
	case CommandType::Hardlink:
		return L"hardlink -c \"" + target + L"\"";
	case CommandType::NukeFiles:
		return L"nuke -f -s \"" + target + L"\"";
	case CommandType::NukeFolders:
		return L"nuke -d -s \"" + target + L"\"";
	case CommandType::CleanSource:
		return L"nuke -v -s \"" + target + L"\"";
	default:
		return L"unknown";
	}
}

struct InvocationContext
{
	CommandType command = CommandType::Unknown;
	int32_t length = 0;
	std::wstring target = std::wstring();
};

typedef void(__cdecl* ExecuteCommand_fn)(CommandType command, LPCWSTR target, int32_t length);

class CsApi
{
public:
	CsApi();
	~CsApi();
	//void ExecuteCommand(CommandType command, LPCWSTR target, int32_t length);
	void ExecuteCommand(InvocationContext* context);
private:
	ExecuteCommand_fn executeCommand;
	HMODULE module;
};