#include "dll.h"
#include <fstream>
#include <ShlObj.h>
#include <string>
#include "myLog.h"

Settings::Settings()
{
	Load();
}

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
				cfgfilew << "#CustomName1=SomeName here\n";
				cfgfilew << "#CustomName2=SomeName here\n";
				cfgfilew << "#CustomName3=SomeName here\n";
				cfgfilew << "#CustomName4=SomeName here\n";
				cfgfilew << "#CustomName5=SomeName here\n";
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

		if (id == "EnableCompareNames") {
			if (val == "true") { EnableCompareNames = true; }
		}
		else if (id == "EnableHardlink") {
			if (val == "true") { EnableHardlink = true; }
		}
		else if (id == "EnableNukeFolder") {
			if (val == "true") { EnableNukeFolder = true; }
		}
		else if (id == "EnableNukeFile") {
			if (val == "true") { EnableNukeFile = true; }
		}
		else if (id == "EnableCleanSource") {
			if (val == "true") { EnableCleanSource = true; }
		}
		else if (id == "CustomName1") {
			if (val != "") { CustomName1 = s2ws(val); Custom1Enabled = true; }
		}
		else if (id == "CustomName2") {
			if (val != "") { CustomName2 = s2ws(val); Custom2Enabled = true; }
		}
		else if (id == "CustomName3") {
			if (val != "") { CustomName3 = s2ws(val); Custom3Enabled = true; }
		}
		else if (id == "CustomName4") {
			if (val != "") { CustomName4 = s2ws(val); Custom4Enabled = true; }
		}
		else if (id == "CustomName5") {
			if (val != "") { CustomName5 = s2ws(val); Custom5Enabled = true; }
		}
	}
	Loaded = true;
}
