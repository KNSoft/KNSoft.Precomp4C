#pragma once

#ifndef _KNSOFT_PRECOMP4C_I18N_
#define _KNSOFT_PRECOMP4C_I18N_
#endif

#include "../Precomp4C.h"

typedef struct _PRECOMP4C_I18N_LOCALE
{
    unsigned short FallbackIndex;
    const wchar_t* Name;
    const wchar_t* Strings[];
} PRECOMP4C_I18N_LOCALE, *PPRECOMP4C_I18N_LOCALE;

typedef struct _PRECOMP4C_I18N_TABLE
{
    PRECOMP4C_I18N_LOCALE* CurrentLocale;
    unsigned short FallbackIndex;
    unsigned short LocaleCount;
    unsigned int StringCount;
    PRECOMP4C_I18N_LOCALE* Locales[];
} PRECOMP4C_I18N_TABLE, *PPRECOMP4C_I18N_TABLE;

__forceinline
int
Precomp4C_I18N_SetCurrentLocale(
    PRECOMP4C_I18N_TABLE* Table,
    const wchar_t* LocaleName)
{
    unsigned short i;
    const wchar_t* Name;
    const wchar_t* NameToFind;

    if (LocaleName == (void*)0)
    {
        Table->CurrentLocale = Table->Locales[Table->FallbackIndex];
        return Table->FallbackIndex;
    }

    for (i = 0; i < Table->LocaleCount; i++)
    {
        Name = Table->Locales[i]->Name;
        NameToFind = LocaleName;
        while (*Name != L'\0' && (*Name == *NameToFind))
        {
            Name++;
            NameToFind++;
        }
        if (*Name == L'\0')
        {
            Table->CurrentLocale = Table->Locales[i];
            return i;
        }
    }

    return -1;
}

__forceinline
const wchar_t*
Precomp4C_I18N_GetString(
    PRECOMP4C_I18N_TABLE* Table,
    int Index)
{
    PPRECOMP4C_I18N_LOCALE Locale = Table->CurrentLocale == (void*)0 ?
        Table->Locales[Table->FallbackIndex] :
        Table->CurrentLocale;

    while (Locale->Strings[Index] == (void*)0)
    {
        if (Locale->FallbackIndex < Table->LocaleCount)
        {
            Locale = Table->Locales[Locale->FallbackIndex];
            break;
        }

        Locale = Table->Locales[Locale->FallbackIndex];
    }

    return Locale->Strings[Index];
}
