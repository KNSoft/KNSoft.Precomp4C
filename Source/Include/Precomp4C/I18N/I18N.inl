#pragma once

#include "I18N.h"

typedef struct _PRECOMP4C_I18N_LOCALE
{
    unsigned short FallbackIndex;
    const wchar_t* Name;
    const wchar_t* Strings[];
} PRECOMP4C_I18N_LOCALE, * PPRECOMP4C_I18N_LOCALE;

typedef struct _PRECOMP4C_I18N_TABLE
{
    PPRECOMP4C_I18N_LOCALE CurrentLocale;
    unsigned short FallbackIndex;
    unsigned short LocaleCount;
    PPRECOMP4C_I18N_LOCALE Locales[];
} PRECOMP4C_I18N_TABLE, * PPRECOMP4C_I18N_TABLE;
