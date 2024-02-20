#pragma once

#ifdef __cplusplus
#define EXTERN_C extern "C"
#else
#define EXTERN_C extern
#endif

#if !defined(__cplusplus) || !defined(_NATIVE_WCHAR_T_DEFINED)
#ifndef _WCHAR_T_DEFINED
#define _WCHAR_T_DEFINED
typedef unsigned short wchar_t;
#endif
#endif

EXTERN_C int Precomp4C_I18N_SetCurrentLocale(void* TableHandle, const wchar_t* LocaleName);
#define I18N_SETLOCALE(table, locale) Precomp4C_I18N_SetCurrentLocale(I18N_Table_Handle_##table, locale)

EXTERN_C const wchar_t* Precomp4C_I18N_GetString(void* TableHandle, int Index);
#define I18N_STR(table, string) Precomp4C_I18N_GetString(I18N_Table_Handle_##table, I18N_##table##_##string)
