#pragma once

//bool DoesClipboardHaveData();
//LPTSTR GetClipboardText();
//bool DoesClipboardHaveValidPaths();
bool GetClipboardInfo(bool& hasData, bool& isCopy, bool& isMove);