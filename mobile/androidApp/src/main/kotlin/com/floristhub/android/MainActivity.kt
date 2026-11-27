package com.floristhub.android

import android.Manifest
import android.content.Intent
import android.os.Build
import android.os.Bundle
import android.util.Log
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.platform.LocalContext
import com.floristhub.android.BuildConfig
import com.floristhub.app.FloristHubApp
import com.floristhub.auth.AuthTokenStorageHolder
import com.floristhub.platform.CheckoutReturn
import com.floristhub.push.orderIdFromFloristHubUri
import com.google.android.gms.tasks.Tasks
import com.google.firebase.FirebaseApp
import com.google.firebase.messaging.FirebaseMessaging
import java.io.ByteArrayOutputStream
import java.util.concurrent.TimeUnit
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class MainActivity : ComponentActivity() {
    private var openOrderUri by mutableStateOf<String?>(null)

    private val notificationPermission = registerForActivityResult(ActivityResultContracts.RequestPermission()) { }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        AuthTokenStorageHolder.applicationContext = applicationContext
        enableEdgeToEdge()
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            notificationPermission.launch(Manifest.permission.POST_NOTIFICATIONS)
        }
        openOrderUri = orderUri(intent)
        setContent {
            val context = LocalContext.current
            var pendingPick by remember { mutableStateOf<((ByteArray, String, String) -> Unit)?>(null) }
            val picker = rememberLauncherForActivityResult(ActivityResultContracts.GetContent()) { uri ->
                val callback = pendingPick
                pendingPick = null
                if (uri == null || callback == null) return@rememberLauncherForActivityResult
                val resolver = context.contentResolver
                val mime = resolver.getType(uri) ?: "image/jpeg"
                val name = uri.lastPathSegment?.substringAfterLast('/') ?: "foto.jpg"
                val bytes = resolver.openInputStream(uri)?.use { input ->
                    ByteArrayOutputStream().apply { input.copyTo(this) }.toByteArray()
                } ?: return@rememberLauncherForActivityResult
                callback(bytes, name, mime)
            }

            FloristHubApp(
                apiBaseUrl = BuildConfig.API_BASE_URL,
                webBaseUrl = BuildConfig.WEB_BASE_URL,
                configuredTenantSlug = BuildConfig.TENANT_SLUG,
                openOrderUri = openOrderUri,
                pushPlatform = "Android",
                pushToken = { firebaseTokenOrNull() },
                onUploadPhoto = { onPicked ->
                    pendingPick = onPicked
                    picker.launch("image/*")
                },
            )
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        orderUri(intent)?.let { openOrderUri = it }
        if (intent.data?.scheme == "floristhub" && intent.data?.host == "pagamento") {
            CheckoutReturn.notifyReturned()
        }
    }

    override fun onResume() {
        super.onResume()
        CheckoutReturn.notifyReturned()
    }

    private fun orderUri(intent: Intent?): String? {
        val data = intent?.data?.toString()
        if (orderIdFromFloristHubUri(data) != null) return data
        val extra = intent?.getStringExtra("orderId")?.takeIf { it.length == 36 } ?: return null
        return "floristhub://encomenda/$extra"
    }

    private suspend fun firebaseTokenOrNull(): String? = withContext(Dispatchers.IO) {
        LatestPushToken.value?.let { return@withContext it }
        try {
            if (FirebaseApp.getApps(applicationContext).isEmpty()) return@withContext null
            Tasks.await(FirebaseMessaging.getInstance().token, 5, TimeUnit.SECONDS)
        } catch (ex: Exception) {
            Log.i(TAG, "Push indisponível sem configuração Firebase.")
            null
        }
    }

    companion object {
        private const val TAG = "FloristHub"
    }
}
