#pragma once

#include "../Precomp4C.h"

#include <stdbool.h>
#include <stddef.h>
#include <string.h>

typedef struct _PRECOMP4C_RES2C_ENTRY
{
    const wchar_t* Type;
    const wchar_t* Name;
    unsigned short LangId;
    void* Data;
    unsigned int Length;
} PRECOMP4C_RES2C_ENTRY, *PPRECOMP4C_RES2C_ENTRY;

__forceinline
bool
Precomp4C_Res2C_AccessResource(
    PRECOMP4C_RES2C_ENTRY Entries[],
    unsigned int Count,
    const wchar_t* Type,
    const wchar_t* Name,
    unsigned short LanguageId,
    void** Resource,
    unsigned int* Length)
{
    unsigned int i;

    for (i = 0; i < Count; i++)
    {
        if (((size_t)Type <= 0xFFFF ? Entries[i].Type == Type : (wcscmp(Entries[i].Type, Type) == 0)) &&
            ((size_t)Name <= 0xFFFF ? Entries[i].Name == Name : (wcscmp(Entries[i].Name, Name) == 0)) &&
            Entries[i].LangId == LanguageId)
        {
            *Resource = Entries[i].Data;
            *Length = Entries[i].Length;
            return true;
        }
    }

    return false;
}
