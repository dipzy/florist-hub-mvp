package com.floristhub.orders

import com.floristhub.network.dto.OpeningHourDto
import com.floristhub.network.dto.PublicStoreCheckoutDto
import com.floristhub.time.nowInstant
import kotlinx.datetime.Instant
import kotlinx.datetime.LocalDateTime
import kotlinx.datetime.TimeZone
import kotlinx.datetime.toInstant
import kotlinx.datetime.toLocalDateTime
import kotlin.time.Duration.Companion.minutes

private val weekDaysPt = listOf(
    "domingo", "segunda-feira", "terça-feira", "quarta-feira",
    "quinta-feira", "sexta-feira", "sábado",
)

data class DeliveryOption(val value: Int, val label: String)

fun activeDeliveryOptions(checkout: PublicStoreCheckoutDto): List<DeliveryOption> {
    val labels = listOf("Levantamento na loja", "Entrega ao domicílio", "Cemitério / outro local")
    return buildList {
        if (checkout.pickupEnabled) add(DeliveryOption(0, labels[0]))
        if (checkout.homeDeliveryEnabled) add(DeliveryOption(1, labels[1]))
        if (checkout.cemeteryDeliveryEnabled) add(DeliveryOption(2, labels[2]))
    }
}

private fun parseMinutes(t: String?): Int? {
    if (t.isNullOrBlank()) return null
    val parts = t.split(":")
    if (parts.size < 2) return null
    val h = parts[0].toIntOrNull() ?: return null
    val m = parts[1].toIntOrNull() ?: return null
    return h * 60 + m
}

/** Parses "AAAA-MM-DDTHH:mm" (datetime-local style) to epoch millis in system TZ. */
fun parseDateTimeLocal(value: String): Long? {
    val m = Regex("""(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})""").matchEntire(value.trim()) ?: return null
    val (y, mo, d, h, mi) = m.destructured
    return runCatching {
        val ldt = LocalDateTime(y.toInt(), mo.toInt(), d.toInt(), h.toInt(), mi.toInt())
        ldt.toInstant(TimeZone.currentSystemDefault()).toEpochMilliseconds()
    }.getOrNull()
}

fun validateRequestedDeliveryAt(
    requestedAtEpochMs: Long,
    openingHours: List<OpeningHourDto>,
    minimumAdvanceMinutes: Int,
): String? {
    val instant = Instant.fromEpochMilliseconds(requestedAtEpochMs)
    val now = nowInstant()
    if (instant < now + minimumAdvanceMinutes.minutes) {
        return "É necessário antecedência mínima de $minimumAdvanceMinutes minutos para a data pretendida."
    }
    if (openingHours.isEmpty()) return null

    val local = instant.toLocalDateTime(TimeZone.currentSystemDefault())
    val apiDay = when (local.dayOfWeek) {
        kotlinx.datetime.DayOfWeek.SUNDAY -> 0
        kotlinx.datetime.DayOfWeek.MONDAY -> 1
        kotlinx.datetime.DayOfWeek.TUESDAY -> 2
        kotlinx.datetime.DayOfWeek.WEDNESDAY -> 3
        kotlinx.datetime.DayOfWeek.THURSDAY -> 4
        kotlinx.datetime.DayOfWeek.FRIDAY -> 5
        kotlinx.datetime.DayOfWeek.SATURDAY -> 6
        else -> 0
    }
    val config = openingHours.find { it.dayOfWeek == apiDay } ?: return null
    if (config.isClosed) return "A loja está encerrada à ${weekDaysPt[apiDay]}."
    val open = parseMinutes(config.openTime) ?: return null
    val close = parseMinutes(config.closeTime) ?: return null
    val minutes = local.hour * 60 + local.minute
    if (minutes < open || minutes > close) {
        return "A data pretendida tem de ser dentro do horário (${config.openTime}–${config.closeTime})."
    }
    return null
}
