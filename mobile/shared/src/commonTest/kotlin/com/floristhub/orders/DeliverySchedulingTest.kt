package com.floristhub.orders

import com.floristhub.network.dto.OpeningHourDto
import com.floristhub.network.dto.PublicCapacityDayDto
import kotlinx.datetime.Instant
import kotlinx.datetime.LocalDate
import kotlinx.datetime.LocalDateTime
import kotlinx.datetime.TimeZone
import kotlinx.datetime.toInstant
import kotlin.test.Test
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class DeliverySchedulingTest {
    private val tz = TimeZone.UTC

    private fun instantAt(date: LocalDate, hour: Int, minute: Int): Instant {
        val epochMillis = LocalDateTime(date.year, date.monthNumber, date.dayOfMonth, hour, minute)
            .toInstant(tz)
            .toEpochMilliseconds()
        return Instant.fromEpochMilliseconds(epochMillis)
    }

    private val weekdayHours = listOf(
        OpeningHourDto(dayOfWeek = 1, isClosed = false, openTime = "09:00", closeTime = "18:00"),
        OpeningHourDto(dayOfWeek = 0, isClosed = true, openTime = null, closeTime = null),
    )

    @Test
    fun closed_day_is_not_selectable() {
        val now = instantAt(LocalDate(2026, 3, 8), 10, 0) // Sunday
        assertFalse(
            isDeliveryDateSelectable(
                LocalDate(2026, 3, 8),
                weekdayHours,
                minimumAdvanceMinutes = 0,
                timeZone = tz,
                referenceNow = now,
            ),
        )
    }

    @Test
    fun past_day_is_not_selectable() {
        val now = instantAt(LocalDate(2026, 3, 9), 10, 0) // Monday
        assertFalse(
            isDeliveryDateSelectable(
                LocalDate(2026, 3, 8),
                weekdayHours,
                minimumAdvanceMinutes = 0,
                timeZone = tz,
                referenceNow = now,
            ),
        )
    }

    @Test
    fun minimum_advance_eliminates_today_when_no_slots_remain() {
        val now = instantAt(LocalDate(2026, 3, 9), 17, 50)
        assertFalse(
            isDeliveryDateSelectable(
                LocalDate(2026, 3, 9),
                weekdayHours,
                minimumAdvanceMinutes = 30,
                timeZone = tz,
                referenceNow = now,
            ),
        )
    }

    @Test
    fun open_day_with_slots_is_selectable() {
        val now = instantAt(LocalDate(2026, 3, 9), 10, 0)
        assertTrue(
            isDeliveryDateSelectable(
                LocalDate(2026, 3, 9),
                weekdayHours,
                minimumAdvanceMinutes = 60,
                timeZone = tz,
                referenceNow = now,
            ),
        )
    }

    @Test
    fun time_slots_respect_opening_hours_and_advance() {
        val now = instantAt(LocalDate(2026, 3, 9), 10, 0)
        val slots = timeSlotsForDate(
            LocalDate(2026, 3, 9),
            weekdayHours,
            minimumAdvanceMinutes = 60,
            timeZone = tz,
            referenceNow = now,
        )
        assertTrue(slots.all { it.label >= "11:00" })
        assertTrue(slots.any { it.label == "11:00" })
        assertTrue(slots.last().label <= "18:00")
    }

    @Test
    fun year_selectable_when_a_day_in_range_is_open() {
        val now = instantAt(LocalDate(2026, 3, 9), 10, 0)
        assertTrue(
            isDeliveryYearSelectable(
                2026,
                weekdayHours,
                minimumAdvanceMinutes = 0,
                timeZone = tz,
                referenceNow = now,
            ),
        )
        assertFalse(
            isDeliveryYearSelectable(
                2025,
                weekdayHours,
                minimumAdvanceMinutes = 0,
                timeZone = tz,
                referenceNow = now,
            ),
        )
    }

    @Test
    fun full_capacity_day_is_not_selectable() {
        val now = instantAt(LocalDate(2026, 3, 9), 8, 0)
        val full = PublicCapacityDayDto(date = "2026-03-09", closed = false, dailyLimit = 1, booked = 1, remaining = 0)
        assertFalse(
            isDeliveryDateSelectable(
                LocalDate(2026, 3, 9),
                weekdayHours,
                minimumAdvanceMinutes = 0,
                timeZone = tz,
                referenceNow = now,
                capacity = full,
            ),
        )
        assertTrue(slotHasCapacity("15:00", null))
    }
}
