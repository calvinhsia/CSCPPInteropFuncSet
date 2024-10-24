// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <windows.h>
#include <string>
#include <atlbase.h>

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

extern "C" __declspec(dllexport) int Add(int num1, int num2) {
	return num1+num2;
}




extern "C" __declspec(dllexport) BSTR AddStrings(WCHAR *  str1, WCHAR*  str2) {
    CComBSTR bstr1(str1);
    auto hr = bstr1.Append(str2);
	BSTR result = SysAllocString(bstr1);
	return result;
    //return SysAllocString(L"result.m_str");
};
