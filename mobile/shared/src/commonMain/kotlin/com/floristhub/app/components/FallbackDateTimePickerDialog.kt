package com.floristhub.app.components

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableLongStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.network.dto.OpeningHourDto
import com.floristhub.network.dto.PublicCapacityDayDto
import com.floristhub.orders.capacityDay
import com.floristhub.orders.formatRequestedDeliveryPt
import com.floristhub.orders.selectableDeliveryDates
import com.floristhub.orders.timeSlotsForDate
import kotlinx.datetime.Instant
import kotlinx.datetime.LocalDate
import kotlinx.datetime.TimeZone
import kotlinx.datetime.atTime
import kotlinx.datetime.toInstant
import kotlinx.datetime.toLocalDateTime

@Composable
fun FallbackDateTimePickerDialog(
    selectedEpochMillis: Long?,
    openingHours: List<OpeningHourDto>,
    minimumAdvanceMinutes: Int,
    capacityDays: List<PublicCapacityDayDto> = emptyList(),
    onDismiss: () -> Unit,
    onConfirm: (Long?) -> Unit,
) {
    val timeZone = TimeZone.currentSystemDefault()
    val dates = remember(openingHours, minimumAdvanceMinutes, capacityDays) {
        selectableDeliveryDates(openingHours, minimumAdvanceMinutes = minimumAdvanceMinutes, capacityDays = capacityDays)
    }
    var selectedDate by remember {
        mutableStateOf(
            selectedEpochMillis?.let {
                Instant.fromEpochMilliseconds(it).toLocalDateTime(timeZone).date
            } ?: dates.firstOrNull(),
        )
    }
    var selectedSlot by remember { mutableLongStateOf(selectedEpochMillis ?: 0L) }

    val slots = remember(selectedDate, openingHours, minimumAdvanceMinutes, capacityDays) {
        selectedDate?.let { timeSlotsForDate(it, openingHours, minimumAdvanceMinutes, capacity = capacityDay(capacityDays, it)) } ?: emptyList()
    }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Data e hora de entrega") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                Text("Data")
                Column(Modifier.fillMaxWidth().heightIn(max = 200.dp)) {
                    dates.forEach { date ->
                        val label = formatRequestedDeliveryPt(
                            date.atTime(12, 0).toInstant(timeZone).toEpochMilliseconds(),
                            timeZone,
                        ).substringBefore(" ")
                        Text(
                            text = if (selectedDate == date) "✓ $label" else label,
                            modifier = Modifier
                                .fillMaxWidth()
                                .clickable {
                                    selectedDate = date
                                    selectedSlot = 0L
                                }
                                .padding(vertical = 6.dp),
                        )
                    }
                }
                DeliveryTimeSlotPicker(
                    slots = slots,
                    selectedSlotEpochMillis = selectedSlot.takeIf { it > 0L },
                    onSlotSelected = { selectedSlot = it },
                )
            }
        },
        confirmButton = {
            TextButton(
                onClick = {
                    if (selectedSlot > 0L) onConfirm(selectedSlot) else onConfirm(null)
                },
                enabled = selectedSlot > 0L,
            ) { Text("Confirmar") }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("Cancelar") }
        },
    )
}
