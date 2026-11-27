package com.floristhub.app.components

import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.floristhub.network.dto.OpeningHourDto
import com.floristhub.network.dto.PublicCapacityDayDto
import com.floristhub.orders.formatRequestedDeliveryPt
import kotlinx.datetime.TimeZone

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StoreDateTimePicker(
    selectedEpochMillis: Long?,
    onSelected: (Long?) -> Unit,
    openingHours: List<OpeningHourDto>,
    minimumAdvanceMinutes: Int,
    enabled: Boolean,
    modifier: Modifier = Modifier,
    capacityDays: List<PublicCapacityDayDto> = emptyList(),
) {
    val timeZone = TimeZone.currentSystemDefault()
    var showDialog by remember { mutableStateOf(false) }

    val display = selectedEpochMillis?.let { formatRequestedDeliveryPt(it, timeZone) } ?: "Escolher data e hora"

    OutlinedButton(onClick = { showDialog = true }, enabled = enabled, modifier = modifier.fillMaxWidth()) {
        Text(display)
    }

    if (!showDialog) return

    if (hasNativeMaterialPickers()) {
        NativeDateTimePickerDialog(
            selectedEpochMillis = selectedEpochMillis,
            openingHours = openingHours,
            minimumAdvanceMinutes = minimumAdvanceMinutes,
            capacityDays = capacityDays,
            onDismiss = { showDialog = false },
            onConfirm = { epoch ->
                onSelected(epoch)
                showDialog = false
            },
        )
    } else {
        FallbackDateTimePickerDialog(
            selectedEpochMillis = selectedEpochMillis,
            openingHours = openingHours,
            minimumAdvanceMinutes = minimumAdvanceMinutes,
            capacityDays = capacityDays,
            onDismiss = { showDialog = false },
            onConfirm = { epoch ->
                onSelected(epoch)
                showDialog = false
            },
        )
    }
}

/** Material3 DatePicker/TimePicker on Android; slot picker on iOS. */
expect fun hasNativeMaterialPickers(): Boolean

@Composable
expect fun NativeDateTimePickerDialog(
    selectedEpochMillis: Long?,
    openingHours: List<OpeningHourDto>,
    minimumAdvanceMinutes: Int,
    capacityDays: List<PublicCapacityDayDto>,
    onDismiss: () -> Unit,
    onConfirm: (Long?) -> Unit,
)
