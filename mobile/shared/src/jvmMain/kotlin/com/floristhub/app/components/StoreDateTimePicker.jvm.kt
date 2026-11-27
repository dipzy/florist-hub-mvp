package com.floristhub.app.components

import androidx.compose.runtime.Composable
import com.floristhub.network.dto.OpeningHourDto
import com.floristhub.network.dto.PublicCapacityDayDto

actual fun hasNativeMaterialPickers(): Boolean = false

@Composable
actual fun NativeDateTimePickerDialog(
    selectedEpochMillis: Long?,
    openingHours: List<OpeningHourDto>,
    minimumAdvanceMinutes: Int,
    capacityDays: List<PublicCapacityDayDto>,
    onDismiss: () -> Unit,
    onConfirm: (Long?) -> Unit,
) = Unit
