package com.floristhub.app.format

import kotlin.math.abs
import kotlin.math.round

/** Formata montante em euros (commonMain — sem `String.format` JVM). */
fun formatEuroAmount(amount: Double): String {
    val scaled = round(amount * 100).toLong()
    val whole = scaled / 100
    val cents = abs(scaled % 100).toInt()
    return "$whole.${cents.toString().padStart(2, '0')}"
}
