#include "I18N.inl"

int Precomp4C_I18N_SetCurrentLocale(void* TableHandle, const wchar_t* LocaleName)
{
    PPRECOMP4C_I18N_TABLE Table = TableHandle;
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

const wchar_t* Precomp4C_I18N_GetString(void* TableHandle, int Index)
{
    PPRECOMP4C_I18N_TABLE Table = TableHandle;
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
