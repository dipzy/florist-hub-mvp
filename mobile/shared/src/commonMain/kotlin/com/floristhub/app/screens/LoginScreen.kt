package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.app.components.FloristScaffold
import com.floristhub.app.components.FloristScreenBox
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.l10nString
import com.floristhub.auth.createAuthSessionStore
import com.floristhub.network.ApiClient
import kotlinx.coroutines.launch

@Composable
fun LoginScreen(api: ApiClient, onLoggedIn: () -> Unit) {
    var storeName by remember { mutableStateOf<String?>(null) }
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var error by remember { mutableStateOf<String?>(null) }
    var loading by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()
    val sessionStore = remember { createAuthSessionStore() }

    LaunchedEffect(api.tenantId, api.tenantName) {
        storeName = api.tenantName
        if (storeName.isNullOrBlank() && !api.tenantId.isNullOrBlank()) {
            storeName = runCatching { api.storeCheckout().name }.getOrNull()
        }
    }

    val loginTitle = l10nString(AppStrings.loginTitle)
    val loginErrorMessage = l10nString(AppStrings.loginError)

    FloristScaffold(title = loginTitle) { padding ->
        FloristScreenBox(padding) {
            Column(verticalArrangement = Arrangement.spacedBy(16.dp)) {
                Text(
                    storeName?.let { "$loginTitle — $it" } ?: loginTitle,
                    style = MaterialTheme.typography.headlineMedium,
                )
                Text(
                    l10nString(AppStrings.loginSubtitle),
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Card(modifier = Modifier.fillMaxWidth()) {
                    Column(
                        modifier = Modifier.fillMaxWidth().padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp),
                    ) {
                        OutlinedTextField(
                            value = email,
                            onValueChange = { email = it },
                            modifier = Modifier.fillMaxWidth(),
                            label = { Text(l10nString(AppStrings.loginEmail)) },
                            singleLine = true,
                        )
                        OutlinedTextField(
                            value = password,
                            onValueChange = { password = it },
                            modifier = Modifier.fillMaxWidth(),
                            label = { Text(l10nString(AppStrings.loginPassword)) },
                            singleLine = true,
                        )
                        error?.let {
                            Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
                        }
                        Button(
                            onClick = {
                                scope.launch {
                                    loading = true
                                    error = null
                                    runCatching {
                                        val auth = api.login(email, password)
                                        api.token = auth.token
                                        auth.tenantId?.let { sessionStore.setTenantId(it) }
                                        api.role = auth.role
                                        sessionStore.setToken(auth.token)
                                        onLoggedIn()
                                    }.onFailure {
                                        error = loginErrorMessage
                                    }
                                    loading = false
                                }
                            },
                            enabled = !loading && email.isNotBlank() && password.isNotBlank(),
                            modifier = Modifier.fillMaxWidth(),
                        ) {
                            Text(l10nString(AppStrings.loginSubmit))
                        }
                    }
                }
            }
        }
    }
}
