#pragma once

#ifndef _KNSOFT_PRECOMP4C_
#define _KNSOFT_PRECOMP4C_
#endif

#ifndef EXTERN_C
#ifdef __cplusplus
#define EXTERN_C extern "C"
#else
#define EXTERN_C extern
#endif
#endif

#if !defined(__cplusplus) || !defined(_NATIVE_WCHAR_T_DEFINED)
#ifndef _WCHAR_T_DEFINED
#define _WCHAR_T_DEFINED
typedef unsigned short wchar_t;
#endif
#endif
