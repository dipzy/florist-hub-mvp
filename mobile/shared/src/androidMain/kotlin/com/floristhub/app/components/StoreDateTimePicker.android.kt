package com.floristhub.app.components

import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.SelectableDates
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.rememberDatePickerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableLongStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import com.floristhub.network.dto.OpeningHourDto
import com.floristhub.network.dto.PublicCapacityDayDto
import com.floristhub.orders.capacityDay
import com.floristhub.orders.isDeliveryDateSelectable
import com.floristhub.orders.isDeliveryYearSelectable
import com.floristhub.orders.localDateFromPickerUtcMillis
import com.floristhub.orders.timeSlotsForDate
import kotlinx.datetime.Instant
import kotlinx.datetime.LocalDate
import kotlinx.datetime.TimeZone
import kotlinx.datetime.toLocalDateTime

actual fun hasNativeMaterialPickers(): Boolean = true

@OptIn(ExperimentalMaterial3Api::class)
@Composable
actual fun NativeDateTimePickerDialog(
    selectedEpochMillis: Long?,
    openingHours: List<OpeningHourDto>,
    minimumAdvanceMinutes: Int,
    capacityDays: List<PublicCapacityDayDto>,
    onDismiss: () -> Unit,
    onConfirm: (Long?) -> Unit,
) {
    val timeZone = TimeZone.currentSystemDefault()
    var step by remember { mutableStateOf(PickerStep.Date) }
    val initialDateMillis = selectedEpochMillis ?: System.currentTimeMillis()

    val selectableDates = remember(openingHours, minimumAdvanceMinutes, capacityDays) {
        object : SelectableDates {
            override fun isSelectableDate(utcTimeMillis: Long): Boolean {
                val date = localDateFromPickerUtcMillis(utcTimeMillis, timeZone)
                return isDeliveryDateSelectable(
                    date,
                    openingHours,
                    minimumAdvanceMinutes,
                    timeZone,
                    capacity = capacityDay(capacityDays, date),
                )
            }

            override fun isSelectableYear(year: Int): Boolean =
                isDeliveryYearSelectable(year, openingHours, minimumAdvanceMinutes, timeZone)
        }
    }

    val dateState = rememberDatePickerState(
        initialSelectedDateMillis = initialDateMillis,
        selectableDates = selectableDates,
    )
    var pendingDate by remember { mutableStateOf<LocalDate?>(null) }
    var selectedSlot by remember { mutableLongStateOf(selectedEpochMillis ?: 0L) }

    when (step) {
        PickerStep.Date -> {
            DatePickerDialog(
                onDismissRequest = onDismiss,
                confirmButton = {
                    TextButton(
                        onClick = {
                            val millis = dateState.selectedDateMillis ?: return@TextButton
                            val localDate = Instant.fromEpochMilliseconds(millis)
                                .toLocalDateTime(timeZone)
                                .date
                            if (!isDeliveryDateSelectable(localDate, openingHours, minimumAdvanceMinutes, timeZone, capacity = capacityDay(capacityDays, localDate))) {
                                return@TextButton
                            }
                            pendingDate = localDate
                            val slots = timeSlotsForDate(localDate, openingHours, minimumAdvanceMinutes)
                            selectedSlot = selectedEpochMillis?.takeIf { epoch ->
                                slots.any { it.epochMillis == epoch }
                            } ?: slots.firstOrNull()?.epochMillis ?: 0L
                            step = PickerStep.Time
                        },
                    ) { Text("Seguinte") }
                },
                dismissButton = {
                    TextButton(onClick = onDismiss) { Text("Cancelar") }
                },
            ) {
                DatePicker(state = dateState)
            }
        }

        PickerStep.Time -> {
            val date = pendingDate ?: run {
                onDismiss()
                return
            }
            val slots = remember(date, openingHours, minimumAdvanceMinutes, capacityDays) {
                timeSlotsForDate(date, openingHours, minimumAdvanceMinutes, capacity = capacityDay(capacityDays, date))
            }
            DatePickerDialog(
                onDismissRequest = { step = PickerStep.Date },
                confirmButton = {
                    TextButton(
                        onClick = {
                            if (selectedSlot > 0L) onConfirm(selectedSlot)
                        },
                        enabled = selectedSlot > 0L,
                    ) { Text("Confirmar") }
                },
                dismissButton = {
                    TextButton(onClick = { step = PickerStep.Date }) { Text("Voltar") }
                },
            ) {
                DeliveryTimeSlotPicker(
                    slots = slots,
                    selectedSlotEpochMillis = selectedSlot.takeIf { it > 0L },
                    onSlotSelected = { selectedSlot = it },
                )
            }
        }
    }
}

private enum class PickerStep { Date, Time }
