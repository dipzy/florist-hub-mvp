package com.floristhub.app.screens

import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import com.floristhub.app.components.FloristScaffold
import com.floristhub.app.components.FloristScreenBox
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.l10nString
import com.floristhub.network.ApiClient
import com.floristhub.network.dto.PublicLegalDocumentDto

@Composable
fun LegalDocumentScreen(api: ApiClient, kind: String) {
    var doc by remember { mutableStateOf<PublicLegalDocumentDto?>(null) }
    var error by remember { mutableStateOf<String?>(null) }

    LaunchedEffect(kind) {
        runCatching {
            doc = when (kind) {
                "privacidade" -> api.legalPrivacy()
                "cookies" -> api.legalCookies()
                else -> api.legalTerms()
            }
        }.onFailure { error = it.message }
    }

    val title = when (kind) {
        "privacidade" -> l10nString(AppStrings.legalTitlePrivacy)
        "cookies" -> l10nString(AppStrings.legalTitleCookies)
        else -> l10nString(AppStrings.legalTitleTerms)
    }

    FloristScaffold(title = title) { padding ->
        FloristScreenBox(padding) {
            when {
                error != null -> Text(error!!, color = MaterialTheme.colorScheme.error)
                doc == null -> Text(l10nString(AppStrings.commonLoading))
                else -> {
                    val body = buildString {
                        append(doc!!.contentMarkdown)
                        doc!!.responsibleSectionMarkdown?.let {
                            append("\n\n")
                            append(it)
                        }
                    }
                    Text(
                        l10nString(AppStrings.legalVersionLine, doc!!.version) + "\n\n$body",
                        modifier = Modifier.fillMaxSize().verticalScroll(rememberScrollState()),
                        style = MaterialTheme.typography.bodyMedium,
                    )
                }
            }
        }
    }
}
