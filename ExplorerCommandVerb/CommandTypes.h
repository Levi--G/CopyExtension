#pragma once
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
	Custom1 = 9,
	Custom2 = 10,
	Custom3 = 11,
	Custom4 = 12,
	Custom5 = 13,
};

std::wstring CommandTypeToString(CommandType type, std::wstring target);