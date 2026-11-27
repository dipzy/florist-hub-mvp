package com.floristhub.app.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.orders.DELIVERY_TIME_SLOT_MINUTES
import com.floristhub.orders.DeliveryTimeSlot

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun DeliveryTimeSlotPicker(
    slots: List<DeliveryTimeSlot>,
    selectedSlotEpochMillis: Long?,
    onSlotSelected: (Long) -> Unit,
    modifier: Modifier = Modifier,
) {
    Column(modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp)) {
        if (slots.isEmpty()) {
            Text("Sem horários disponíveis neste dia.")
        } else {
            Text("Hora (intervalos de $DELIVERY_TIME_SLOT_MINUTES min)")
            FlowRow(
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                modifier = Modifier.padding(top = 8.dp),
            ) {
                slots.forEach { slot ->
                    FilterChip(
                        selected = selectedSlotEpochMillis == slot.epochMillis,
                        onClick = { onSlotSelected(slot.epochMillis) },
                        label = { Text(slot.label) },
                    )
                }
            }
        }
    }
}
