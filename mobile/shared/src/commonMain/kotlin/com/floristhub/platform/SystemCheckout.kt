package com.floristhub.platform

expect fun openSystemCheckout(url: String)

object CheckoutReturn {
    var onReturned: (() -> Unit)? = null

    fun notifyReturned() {
        onReturned?.invoke()
    }
}
