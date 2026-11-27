package com.floristhub.app.i18n

import androidx.compose.runtime.Composable

@Composable
fun orderStatusShortLabel(status: Int): String {
    val labels = listOf(
        AppStrings.orderStatusShort0,
        AppStrings.orderStatusShort1,
        AppStrings.orderStatusShort2,
        AppStrings.orderStatusShort3,
        AppStrings.orderStatusShort4,
        AppStrings.orderStatusShort5,
        AppStrings.orderStatusShort6,
    )
    return if (status in labels.indices) l10nString(labels[status]) else l10nString(AppStrings.orderStatusUnknown, status)
}

@Composable
fun orderStatusFullLabel(status: Int): String {
    val labels = listOf(
        AppStrings.orderStatusFull0,
        AppStrings.orderStatusFull1,
        AppStrings.orderStatusFull2,
        AppStrings.orderStatusFull3,
        AppStrings.orderStatusFull4,
        AppStrings.orderStatusFull5,
        AppStrings.orderStatusFull6,
    )
    return if (status in labels.indices) l10nString(labels[status]) else l10nString(AppStrings.orderStatusUnknown, status)
}

@Composable
fun paymentStatusLabel(status: Int): String = when (status) {
    0 -> l10nString(AppStrings.paymentPending)
    1 -> l10nString(AppStrings.paymentPaid)
    2 -> l10nString(AppStrings.paymentFailed)
    3 -> l10nString(AppStrings.paymentRefundedPartial)
    4 -> l10nString(AppStrings.paymentRefunded)
    5 -> l10nString(AppStrings.paymentCancelled)
    else -> l10nString(AppStrings.paymentGeneric)
}

@Composable
fun deliveryTypeLabel(value: Int): String = when (value) {
    0 -> l10nString(AppStrings.storePickup)
    1 -> l10nString(AppStrings.storeHomeDelivery)
    2 -> l10nString(AppStrings.storeCemeteryDelivery)
    else -> ""
}

@Composable
fun weekdayLabel(dayOfWeek: Int): String {
    val labels = listOf(
        AppStrings.weekdaySun,
        AppStrings.weekdayMon,
        AppStrings.weekdayTue,
        AppStrings.weekdayWed,
        AppStrings.weekdayThu,
        AppStrings.weekdayFri,
        AppStrings.weekdaySat,
    )
    return if (dayOfWeek in labels.indices) l10nString(labels[dayOfWeek]) else "?"
}

@Composable
fun recurringStatusLabel(status: Int): String {
    val labels = listOf(
        AppStrings.recurringStatus0,
        AppStrings.recurringStatus1,
        AppStrings.recurringStatus2,
        AppStrings.recurringStatus3,
        AppStrings.recurringStatus4,
    )
    return if (status in labels.indices) l10nString(labels[status]) else "—"
}

@Composable
fun recurringFrequencyLabel(frequency: Int): String {
    val labels = listOf(
        AppStrings.recurringFreq0,
        AppStrings.recurringFreq1,
        AppStrings.recurringFreq2,
    )
    return if (frequency in labels.indices) l10nString(labels[frequency]) else l10nString(AppStrings.recurringFreqUnknown)
}
