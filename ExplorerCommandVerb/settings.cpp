#include "settings.h"
#include <fstream>
#include <ShlObj.h>
#include <string>

inline void Settings::Load() {
	if (Loaded) {
		return;
	}
	PWSTR path = NULL;
	if (SUCCEEDED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, NULL, &path))) {
		// Use the localAppDataPath
		std::wstring filepath = path;

		if (!filepath.empty() && filepath.back() != L'\\' && filepath.back() != L'/')
		{
			filepath += L'\\';
		}
		filepath += L"CopyExtension\\settings.txt";

		std::ifstream cfgfile(filepath);
		if (cfgfile)
		{
			parse(cfgfile);
		}
		else {
			std::ofstream cfgfilew(filepath);
			if (cfgfilew) {
				cfgfilew << "# Settings for CopyExtension\n";
				cfgfilew << "EnableCompareNames=false\n";
				cfgfilew << "EnableHardlink=false\n";
				cfgfilew << "EnableNukeFolder=false\n";
				cfgfilew << "EnableNukeFile=false\n";
				cfgfilew << "EnableCleanSource=false\n";
			}
			cfgfilew.close();
		}
		cfgfile.close();
	}
	CoTaskMemFree(path);
}

inline void Settings::parse(std::istream& cfgfile)
{
	//std::map<std::string, std::string> options;
	for (std::string line; std::getline(cfgfile, line); )
	{
		if (line[0] == '#')
		{
			continue;
		}
		size_t eq = line.find('=');
		if (eq == -1 || eq == 0 || eq == line.length() - 1) {
			continue;
		}

		std::string id = line.substr(0, eq);
		std::string val = line.substr(eq + 1);

		if (val == "true") {
			if (id == "EnableCompareNames") {
				EnableCompareNames = true;
			}
			else if (id == "EnableHardlink") {
				EnableHardlink = true;
			}
			else if (id == "EnableNukeFolder") {
				EnableNukeFolder = true;
			}
			else if (id == "EnableNukeFile") {
				EnableNukeFile = true;
			}
			else if (id == "EnableCleanSource") {
				EnableCleanSource = true;
			}
		}

		//options[id] = val;
	}
	Loaded = true;
	//options["EnableCompareNames"] == "true" ? EnableCompareNames = true : EnableCompareNames = false;
	//options["EnableHardlink"] == "true" ? EnableHardlink = true : EnableHardlink = false;
	//options["EnableNukeFolder"] == "true" ? EnableNukeFolder = true : EnableNukeFolder = false;
	//options["EnableNukeFile"] == "true" ? EnableNukeFile = true : EnableNukeFile = false;
	//options["EnableCleanSource"] == "true" ? EnableCleanSource = true : EnableCleanSource = false;
}
