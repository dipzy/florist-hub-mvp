package com.floristhub.preferences

enum class AppThemePreference {
    Light,
    Dark,
    System,
    ;

    companion object {
        fun fromStored(value: String?): AppThemePreference = when (value) {
            "light" -> Light
            "dark" -> Dark
            "system", null, "" -> System
            else -> System
        }

        fun toStored(value: AppThemePreference): String = when (value) {
            Light -> "light"
            Dark -> "dark"
            System -> "system"
        }
    }
}
