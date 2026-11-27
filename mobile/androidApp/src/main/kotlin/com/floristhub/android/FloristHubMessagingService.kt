package com.floristhub.android

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Intent
import android.net.Uri
import android.os.Build
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage

class FloristHubMessagingService : FirebaseMessagingService() {
    override fun onMessageReceived(message: RemoteMessage) {
        val orderId = message.data["orderId"]
        val title = message.notification?.title ?: message.data["title"] ?: "Florist Hub"
        val body = message.notification?.body ?: message.data["body"] ?: ""
        show(title, body, orderId)
    }

    override fun onNewToken(token: String) {
        LatestPushToken.value = token
    }

    private fun show(title: String, body: String, orderId: String?) {
        val manager = getSystemService(NotificationManager::class.java)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            manager.createNotificationChannel(
                NotificationChannel(CHANNEL_ID, "Encomendas", NotificationManager.IMPORTANCE_DEFAULT),
            )
        }
        val intent = Intent(this, MainActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP
            if (!orderId.isNullOrBlank()) {
                data = Uri.parse("floristhub://encomenda/$orderId")
                putExtra("orderId", orderId)
            }
        }
        val pending = PendingIntent.getActivity(
            this,
            orderId?.hashCode() ?: 0,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE,
        )
        val notification = android.app.Notification.Builder(this, CHANNEL_ID)
            .setSmallIcon(android.R.drawable.ic_dialog_info)
            .setContentTitle(title)
            .setContentText(body)
            .setAutoCancel(true)
            .setContentIntent(pending)
            .build()
        manager.notify(orderId?.hashCode() ?: title.hashCode(), notification)
    }

    companion object {
        const val CHANNEL_ID = "encomendas"
    }
}

object LatestPushToken {
    @Volatile
    var value: String? = null
}
