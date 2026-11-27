package com.floristhub.app.screens

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.getValue
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.l10nString
import com.floristhub.network.ApiClient
import com.floristhub.network.dto.FloristOnboardingStatusDto
import kotlinx.coroutines.launch

@Composable
fun FloristOnboardingScreen(api: ApiClient, onOpenSettings: () -> Unit) {
    var status by remember { mutableStateOf<FloristOnboardingStatusDto?>(null) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    val loadError = l10nString(AppStrings.onboardingLoadError)
    val publishError = l10nString(AppStrings.onboardingPublishError)
    val yes = l10nString(AppStrings.commonYes)
    val no = l10nString(AppStrings.commonNo)
    val required = l10nString(AppStrings.commonRequiredParen)

    fun refresh() {
        scope.launch {
            runCatching { status = api.floristOnboarding() }
                .onFailure { error = loadError }
        }
    }

    LaunchedEffect(Unit) { refresh() }

    Column(Modifier.fillMaxSize().padding(16.dp)) {
        Text(l10nString(AppStrings.onboardingTitle), style = MaterialTheme.typography.headlineSmall)
        error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        status?.let { s ->
            Text(
                l10nString(
                    AppStrings.onboardingProgress,
                    if (s.isPublished) yes else no,
                    s.wizardProgressPercent,
                ),
                modifier = Modifier.padding(vertical = 8.dp),
            )
            LazyColumn(Modifier.weight(1f)) {
                items(s.checklist) { item ->
                    Text(
                        l10nString(
                            AppStrings.onboardingChecklistItem,
                            if (item.isComplete) "✓" else "○",
                            item.title,
                            if (item.isRequired && !item.isComplete) required else "",
                        ),
                        modifier = Modifier.padding(vertical = 4.dp),
                    )
                }
            }
            if (!s.isPublished) {
                Button(
                    onClick = {
                        scope.launch {
                            runCatching {
                                status = api.publishFloristStore()
                            }.onFailure { error = publishError }
                        }
                    },
                    enabled = s.canPublish,
                ) {
                    Text(l10nString(AppStrings.onboardingPublishStore))
                }
            }
        }
        Button(onClick = onOpenSettings) { Text(l10nString(AppStrings.onboardingOpenSettings)) }
    }
}
