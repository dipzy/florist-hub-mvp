package com.floristhub.app.components

import androidx.compose.material3.AssistChip
import androidx.compose.material3.AssistChipDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier

@Composable
fun OrderStatusChip(status: Int, label: String, modifier: Modifier = Modifier) {
    val containerColor = when (status) {
        0 -> MaterialTheme.colorScheme.tertiaryContainer
        1, 4 -> MaterialTheme.colorScheme.primaryContainer
        2, 3 -> MaterialTheme.colorScheme.secondaryContainer
        5, 6 -> MaterialTheme.colorScheme.errorContainer
        else -> MaterialTheme.colorScheme.surfaceVariant
    }
    AssistChip(
        onClick = {},
        enabled = false,
        modifier = modifier,
        label = { Text(label) },
        colors = AssistChipDefaults.assistChipColors(
            disabledContainerColor = containerColor,
            disabledLabelColor = MaterialTheme.colorScheme.onSurface,
        ),
    )
}
