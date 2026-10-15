#pragma once
#include <istream>

class Settings {
public:
	bool EnableCompareNames = false;
	bool EnableHardlink = false;
	bool EnableNukeFolder = false;
	bool EnableNukeFile = false;
	bool EnableCleanSource = false;
	std::wstring CustomName1;
	std::wstring CustomName2;
	std::wstring CustomName3;
	std::wstring CustomName4;
	std::wstring CustomName5;
	bool Custom1Enabled = false;
	bool Custom2Enabled = false;
	bool Custom3Enabled = false;
	bool Custom4Enabled = false;
	bool Custom5Enabled = false;
	bool Loaded = false;

	Settings();
private:
	void Load();
	void parse(std::istream& cfgfile);
};