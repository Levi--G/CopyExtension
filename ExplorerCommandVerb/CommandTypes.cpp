#include "CommandTypes.h"

std::wstring CommandTypeToString(CommandType type, std::wstring target)
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
	case CommandType::Custom1:
		return L"custom -i 1 \"" + target + L"\"";
	case CommandType::Custom2:
		return L"custom -i 2 \"" + target + L"\"";
	case CommandType::Custom3:
		return L"custom -i 3 \"" + target + L"\"";
	case CommandType::Custom4:
		return L"custom -i 4 \"" + target + L"\"";
	case CommandType::Custom5:
		return L"custom -i 5 \"" + target + L"\"";
	default:
		return L"unknown";
	}
}