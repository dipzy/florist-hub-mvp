package com.floristhub.app.util

fun pluralPt(count: Int, singular: String, plural: String): String =
    if (count == 1) singular else plural

fun countLabel(count: Int, singular: String, plural: String): String =
    "$count ${pluralPt(count, singular, plural)}"
