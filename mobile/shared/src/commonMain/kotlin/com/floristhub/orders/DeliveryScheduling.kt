package com.floristhub.orders

import com.floristhub.network.dto.OpeningHourDto
import com.floristhub.network.dto.PublicCapacityDayDto
import com.floristhub.time.nowInstant
import kotlinx.datetime.DateTimeUnit
import kotlinx.datetime.DayOfWeek
import kotlinx.datetime.Instant
import kotlinx.datetime.LocalDate
import kotlinx.datetime.LocalDateTime
import kotlinx.datetime.TimeZone
import kotlinx.datetime.atTime
import kotlinx.datetime.plus
import kotlinx.datetime.toInstant
import kotlinx.datetime.toLocalDateTime
const val DELIVERY_TIME_SLOT_MINUTES = 15

data class DeliveryTimeSlot(
    val label: String,
    val epochMillis: Long,
)

fun deliveryTypesRequiringZone(hasActiveZones: Boolean): Boolean = hasActiveZones

fun deliveryTypesNeedAddress(deliveryType: Int): Boolean =
    deliveryType == 1 || deliveryType == 2

fun needsDeliveryZone(deliveryType: Int, zoneCount: Int): Boolean =
    zoneCount > 0 && (deliveryType == 1 || deliveryType == 2)

fun formatRequestedDeliveryPt(epochMillis: Long, timeZone: TimeZone = TimeZone.currentSystemDefault()): String {
    val local = Instant.fromEpochMilliseconds(epochMillis).toLocalDateTime(timeZone)
    val dd = local.dayOfMonth.toString().padStart(2, '0')
    val mm = local.monthNumber.toString().padStart(2, '0')
    val hh = local.hour.toString().padStart(2, '0')
    val mi = local.minute.toString().padStart(2, '0')
    return "$dd/$mm/${local.year} $hh:$mi"
}

fun requestedDeliveryToIso(epochMillis: Long): String =
    Instant.fromEpochMilliseconds(epochMillis).toString()

private fun kotlinx.datetime.DayOfWeek.toApiDay(): Int = when (this) {
    DayOfWeek.SUNDAY -> 0
    DayOfWeek.MONDAY -> 1
    DayOfWeek.TUESDAY -> 2
    DayOfWeek.WEDNESDAY -> 3
    DayOfWeek.THURSDAY -> 4
    DayOfWeek.FRIDAY -> 5
    DayOfWeek.SATURDAY -> 6
    else -> 0
}

private fun parseMinutes(t: String?): Int? {
    if (t.isNullOrBlank()) return null
    val parts = t.split(":")
    if (parts.size < 2) return null
    val h = parts[0].toIntOrNull() ?: return null
    val m = parts[1].toIntOrNull() ?: return null
    return h * 60 + m
}

fun openingHourForDate(date: LocalDate, openingHours: List<OpeningHourDto>): OpeningHourDto? {
    val apiDay = date.dayOfWeek.toApiDay()
    return openingHours.find { it.dayOfWeek == apiDay }
}

fun isStoreClosedOnDate(date: LocalDate, openingHours: List<OpeningHourDto>): Boolean {
    if (openingHours.isEmpty()) return false
    val config = openingHourForDate(date, openingHours) ?: return false
    return config.isClosed
}

fun localDateFromPickerUtcMillis(utcTimeMillis: Long, timeZone: TimeZone): LocalDate =
    Instant.fromEpochMilliseconds(utcTimeMillis).toLocalDateTime(timeZone).date

fun capacityDay(days: List<PublicCapacityDayDto>, date: LocalDate): PublicCapacityDayDto? =
    days.find { it.date == date.toString() }

fun isCapacityOpen(day: PublicCapacityDayDto?): Boolean {
    if (day == null) return true
    if (day.closed) return false
    if (day.remaining == 0) return false
    if (day.windows.isNotEmpty() && day.windows.all { it.remaining <= 0 }) return false
    return true
}

fun slotHasCapacity(label: String, day: PublicCapacityDayDto?): Boolean {
    if (day == null) return true
    if (day.closed || day.remaining == 0) return false
    if (day.windows.isEmpty()) return true
    val minute = parseMinutes(label) ?: return false
    val window = day.windows.firstOrNull { candidate ->
        val start = parseMinutes(candidate.start) ?: return@firstOrNull false
        val end = parseMinutes(candidate.end) ?: return@firstOrNull false
        minute in start..end
    }
    return window != null && window.remaining > 0
}

fun isDeliveryDateSelectable(
    date: LocalDate,
    openingHours: List<OpeningHourDto>,
    minimumAdvanceMinutes: Int,
    timeZone: TimeZone = TimeZone.currentSystemDefault(),
    referenceNow: Instant = nowInstant(),
    daysAhead: Int = 60,
    capacity: PublicCapacityDayDto? = null,
): Boolean {
    val today = referenceNow.toLocalDateTime(timeZone).date
    if (date < today) return false
    val lastDay = today.plus(daysAhead - 1, DateTimeUnit.DAY)
    if (date > lastDay) return false
    if (isStoreClosedOnDate(date, openingHours)) return false
    if (!isCapacityOpen(capacity)) return false
    return timeSlotsForDate(
        date,
        openingHours,
        minimumAdvanceMinutes,
        timeZone = timeZone,
        referenceNow = referenceNow,
        capacity = capacity,
    ).isNotEmpty()
}

fun isDeliveryYearSelectable(
    year: Int,
    openingHours: List<OpeningHourDto>,
    minimumAdvanceMinutes: Int,
    timeZone: TimeZone = TimeZone.currentSystemDefault(),
    referenceNow: Instant = nowInstant(),
    daysAhead: Int = 60,
): Boolean {
    val today = referenceNow.toLocalDateTime(timeZone).date
    val lastDay = today.plus(daysAhead - 1, DateTimeUnit.DAY)
    if (year < today.year || year > lastDay.year) return false
    val start = if (today.year == year) today else LocalDate(year, 1, 1)
    val end = if (lastDay.year == year) lastDay else LocalDate(year, 12, 31)
    var cursor = start
    while (cursor <= end) {
        if (isDeliveryDateSelectable(cursor, openingHours, minimumAdvanceMinutes, timeZone, referenceNow, daysAhead)) {
            return true
        }
        cursor = cursor.plus(1, DateTimeUnit.DAY)
    }
    return false
}

fun selectableDeliveryDates(
    openingHours: List<OpeningHourDto>,
    daysAhead: Int = 60,
    timeZone: TimeZone = TimeZone.currentSystemDefault(),
    minimumAdvanceMinutes: Int = 0,
    referenceNow: Instant = nowInstant(),
    capacityDays: List<PublicCapacityDayDto> = emptyList(),
): List<LocalDate> {
    val today = referenceNow.toLocalDateTime(timeZone).date
    return (0 until daysAhead).mapNotNull { offset ->
        val date = today.plus(offset, DateTimeUnit.DAY)
        if (isDeliveryDateSelectable(date, openingHours, minimumAdvanceMinutes, timeZone, referenceNow, daysAhead, capacityDay(capacityDays, date))) {
            date
        } else {
            null
        }
    }
}

fun timeSlotsForDate(
    date: LocalDate,
    openingHours: List<OpeningHourDto>,
    minimumAdvanceMinutes: Int,
    slotMinutes: Int = DELIVERY_TIME_SLOT_MINUTES,
    timeZone: TimeZone = TimeZone.currentSystemDefault(),
    referenceNow: Instant = nowInstant(),
    capacity: PublicCapacityDayDto? = null,
): List<DeliveryTimeSlot> {
    val config = openingHourForDate(date, openingHours)
    if (config?.isClosed == true) return emptyList()
    val open = parseMinutes(config?.openTime) ?: (9 * 60)
    val close = parseMinutes(config?.closeTime) ?: (18 * 60)

    val minEpochMillis = referenceNow.toEpochMilliseconds() + minimumAdvanceMinutes * 60_000L
    val slots = mutableListOf<DeliveryTimeSlot>()
    var minuteOfDay = open
    while (minuteOfDay <= close) {
        val hour = minuteOfDay / 60
        val minute = minuteOfDay % 60
        val ldt = date.atTime(hour, minute)
        val instant = ldt.toInstant(timeZone)
        if (instant.toEpochMilliseconds() >= minEpochMillis) {
            val label = "${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}"
            if (slotHasCapacity(label, capacity)) {
                slots.add(DeliveryTimeSlot(label, instant.toEpochMilliseconds()))
            }
        }
        minuteOfDay += slotMinutes
    }
    return slots
}

fun localDateTimeFromSelection(date: LocalDate, slot: DeliveryTimeSlot): LocalDateTime {
    val instant = Instant.fromEpochMilliseconds(slot.epochMillis)
    return instant.toLocalDateTime(TimeZone.currentSystemDefault())
}
