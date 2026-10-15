//#include "dll.h"
//#include "myLog.h"
//#include <fstream>
//#include <ShlObj.h>
//
//void myLog(const std::string& text)
//{
//	PWSTR path = NULL;
//	if (SUCCEEDED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, NULL, &path))) {
//		std::wstring filepath = path;
//
//		if (!filepath.empty() && filepath.back() != L'\\' && filepath.back() != L'/')
//		{
//			filepath += L'\\';
//		}
//		filepath += L"CopyExtension\\log.log";
//		std::ofstream logFile(filepath, std::ios::app);
//		if (logFile)
//		{
//			logFile << "[ExplorerCommandVerb] " << text << '\n';
//		}
//	}
//}
//
//void myLog(const std::wstring& message)
//{
//	if (message.empty())
//	{
//		myLog("");
//		return;
//	}
//	myLog(ws2s(message));
//}
