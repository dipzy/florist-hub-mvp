package com.floristhub.app.screenshots

import com.floristhub.network.dto.DailyCountDto
import com.floristhub.network.dto.FloristDashboardDto
import com.floristhub.network.dto.FloristDashboardMetricsDto
import com.floristhub.network.dto.FlowerDto
import com.floristhub.network.dto.OrderChangeProposalDto
import com.floristhub.network.dto.OrderDto
import com.floristhub.network.dto.OrderItemDto
import com.floristhub.network.dto.OrderStatusHistoryDto
import com.floristhub.network.dto.OpeningHourDto
import com.floristhub.network.dto.PortfolioDto
import com.floristhub.network.dto.PortfolioPhotoDto
import com.floristhub.network.dto.PublicStoreCheckoutDto

object ScreenshotFixtures {
    val storePrimary = "#2d6a4f"
    val storeAccent = "#95d5b2"

    val portfolio = listOf(
        PortfolioDto(
            id = "44444444-4444-4444-4444-444444444401",
            title = "Buquê primaveril",
            description = "Combinação leve de rosas e túlipas.",
            price = 35.0,
            tags = "casamento,primavera",
            photos = listOf(PortfolioPhotoDto(url = "/api/files/demo/bouquet-primaveril.jpg", sortOrder = 0)),
        ),
    )

    val flowers = listOf(
        FlowerDto(
            id = "33333333-3333-3333-3333-333333333301",
            name = "Rosa vermelha",
            color = "Vermelho",
            unitPrice = 2.5,
            imageUrl = "/api/files/demo/flower-rosa-vermelha.jpg",
        ),
        FlowerDto(
            id = "33333333-3333-3333-3333-333333333302",
            name = "Túlipa amarela",
            color = "Amarelo",
            unitPrice = 1.8,
            imageUrl = "/api/files/demo/flower-tulipa-amarela.jpg",
        ),
    )

    val checkout = PublicStoreCheckoutDto(
        name = "Flores da Lina",
        ordersAcceptingEnabled = true,
        minimumOrderAdvanceMinutes = 180,
        pickupEnabled = true,
        homeDeliveryEnabled = true,
        cemeteryDeliveryEnabled = true,
        openingHours = (1..5).map { day ->
            OpeningHourDto(dayOfWeek = day, isClosed = false, openTime = "09:00", closeTime = "19:00")
        },
    )

    val orderDetail = OrderDto(
        id = "55555555-5555-5555-5555-555555555502",
        status = 0,
        baseType = 1,
        deliveryType = 1,
        deliveryAddress = "Rua das Flores, 12, Lisboa",
        estimatedTotal = 42.0,
        deliveryFee = 4.5,
        deliveryZoneName = "Lisboa centro",
        paymentCaptured = false,
        items = listOf(
            OrderItemDto(id = "item-1", flowerId = "33333333-3333-3333-3333-333333333302", name = "Túlipa amarela", quantity = 8, unitPrice = 1.8),
            OrderItemDto(id = "item-2", flowerId = "33333333-3333-3333-3333-333333333301", name = "Rosa vermelha", quantity = 6, unitPrice = 2.5),
        ),
        statusHistory = listOf(
            OrderStatusHistoryDto(id = "h1", fromStatus = 0, toStatus = 0, createdAt = "2026-11-01T10:00:00Z", notes = "Encomenda criada pelo cliente."),
            OrderStatusHistoryDto(id = "h2", fromStatus = 0, toStatus = 0, createdAt = "2026-11-01T11:00:00Z", notes = "Proposta de alteração enviada pela florista."),
        ),
        changeProposals = listOf(
            OrderChangeProposalDto(
                id = "prop-1",
                status = 0,
                message = "Túlipas indisponíveis; sugerimos rosas vermelhas no mesmo tom.",
                revisedTotal = 45.0,
                createdAt = "2026-11-01T11:00:00Z",
            ),
        ),
    )

    val dashboard = FloristDashboardDto(
        ordersToday = 2,
        ordersTomorrow = 1,
        monthlyRevenue = 186.5,
        ordersByStatus = mapOf("0" to 2, "1" to 3, "4" to 5),
    )

    val metrics = FloristDashboardMetricsDto(
        days = 30,
        pendingOrders = 2,
        periodRevenue = 142.0,
        ordersByDay = (20 downTo 5).map { offset ->
            DailyCountDto(date = "2026-10-${offset.toString().padStart(2, '0')}", count = (offset % 5) + 1)
        },
    )
}
