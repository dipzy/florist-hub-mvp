package com.floristhub.push

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class OrderDeepLinkTest {
    @Test
    fun extracts_order_id_from_notification_uri() {
        val id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
        assertEquals(id, orderIdFromFloristHubUri("floristhub://encomenda/$id"))
        assertEquals(id, orderIdFromFloristHubUri("floristhub://encomenda/$id?origem=push"))
    }

    @Test
    fun ignores_payment_return_and_short_ids() {
        assertNull(orderIdFromFloristHubUri("floristhub://pagamento"))
        assertNull(orderIdFromFloristHubUri("floristhub://encomenda/123"))
        assertNull(orderIdFromFloristHubUri(null))
    }
}
