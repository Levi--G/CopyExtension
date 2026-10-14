#pragma once
#include <istream>

class Settings {
	bool EnableCompareNames = false;
	bool EnableHardlink = false;
	bool EnableNukeFolder = false;
	bool EnableNukeFile = false;
	bool EnableCleanSource = false;
	bool Loaded = false;

	void Load();
private:
	void parse(std::istream& cfgfile);
};