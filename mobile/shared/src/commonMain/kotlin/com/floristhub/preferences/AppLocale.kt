package com.floristhub.preferences

enum class AppLocale(val apiTag: String, val composeLanguageTag: String) {
    Pt("pt-PT", "pt"),
    En("en", "en"),
    ;

    companion object {
        fun fromStored(value: String?): AppLocale = when {
            value == null || value.isBlank() -> Pt
            value == "en" || value.startsWith("en") -> En
            else -> Pt
        }

        fun toStored(value: AppLocale): String = when (value) {
            Pt -> "pt-PT"
            En -> "en"
        }
    }
}
