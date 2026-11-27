package com.floristhub.preview

import com.floristhub.network.ApiClient
import com.floristhub.network.createHttpClient
import com.floristhub.network.dto.FloristDashboardDto
import com.floristhub.network.dto.FlowerDto
import com.floristhub.network.dto.OrderDto
import com.floristhub.network.dto.OrderItemDto
import com.floristhub.network.dto.PortfolioDto
import com.floristhub.network.dto.PortfolioPhotoDto
import com.floristhub.network.dto.PublicCapacityDayDto
import com.floristhub.network.dto.PublicStoreCheckoutDto
import com.floristhub.network.dto.PublicStoreThemeDto
import com.floristhub.network.dto.PublicStorefrontDto
import com.floristhub.network.dto.PublicCampaignDto
import com.floristhub.network.dto.PublicSeasonalFlowerDto
import com.floristhub.network.dto.StoreSettingsDto
import com.floristhub.network.dto.TenantDto

/**
 * Fixture API for iOS simulator screenshots in CI (no backend).
 */
class PreviewApiClient : ApiClient(createHttpClient(), "http://127.0.0.1:5183") {
    init {
        token = "preview-token"
        tenantId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
        tenantSlug = "flores-lina"
        tenantName = "Flores da Lina (demo)"
        role = "Cliente"
    }

    override suspend fun resolveTenant(slug: String): TenantDto =
        TenantDto(id = tenantId!!, name = "Flores da Lina (demo)", slug = slug)

    override suspend fun portfolio(): List<PortfolioDto> = listOf(samplePortfolio)

    override suspend fun flowers(): List<FlowerDto> = sampleFlowers

    override suspend fun storeCheckout(): PublicStoreCheckoutDto = sampleCheckout

    override suspend fun publicCapacity(from: String, to: String): List<PublicCapacityDayDto> = emptyList()

    override suspend fun floristCapacity(from: String, to: String): List<PublicCapacityDayDto> = emptyList()

    override suspend fun storefront(): PublicStorefrontDto = PublicStorefrontDto(
        name = "Flores da Lina",
        description = "Florista de bairro.",
        campaign = PublicCampaignDto(
            name = "Dia da Mãe",
            slug = "dia-da-mae",
            description = "Bouquets para o Dia da Mãe.",
            startsOn = "2026-05-01",
            endsOn = "2026-05-03",
        ),
        seasonalFlowers = listOf(PublicSeasonalFlowerDto(id = "f1", name = "Rosa vermelha", unitPrice = 2.5, specialPrice = 2.0)),
        averageRating = 4.8,
        reviewCount = 12,
    )

    override suspend fun storeTheme(): PublicStoreThemeDto = PublicStoreThemeDto(
        name = "Flores da Lina",
        slug = "flores-lina",
        themePrimaryColor = "#2d6a4f",
        themeAccentColor = "#95d5b2",
        themeFontFamily = null,
    )

    override suspend fun myOrders(): List<OrderDto> = listOf(sampleOrder)

    override suspend fun floristDashboard(): FloristDashboardDto = FloristDashboardDto(
        ordersToday = 3,
        ordersTomorrow = 2,
        monthlyRevenue = 428.5,
        ordersByStatus = mapOf("Pendente" to 1, "Em preparação" to 2),
    )

    override suspend fun storeSettings(): StoreSettingsDto = StoreSettingsDto(
        tenantId = tenantId!!,
        name = "Flores da Lina",
        description = "Florista de bairro com entregas no mesmo dia.",
        contactEmail = "ola@floresdalina.demo",
        city = "Lisboa",
    )

    private val samplePortfolio = PortfolioDto(
        id = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        title = "Ramalhete Primaveril",
        description = "Rosa, lírio e folhagem — ideal para oferecer.",
        price = 35.0,
        photos = listOf(PortfolioPhotoDto(url = "/api/files/demo/bouquet-primaveril.jpg", sortOrder = 0)),
    )

    private val sampleFlowers = listOf(
        FlowerDto(id = "f1", name = "Rosa vermelha", color = "Vermelho", unitPrice = 2.5),
        FlowerDto(id = "f2", name = "Lírio branco", color = "Branco", unitPrice = 4.0),
        FlowerDto(id = "f3", name = "Folhagem", color = "Verde", unitPrice = 0.8),
    )

    private val sampleCheckout = PublicStoreCheckoutDto(
        name = "Flores da Lina",
        pickupEnabled = true,
        homeDeliveryEnabled = true,
        cemeteryDeliveryEnabled = false,
        minimumOrderAdvanceMinutes = 60,
    )

    private val sampleOrder = OrderDto(
        id = "55555555-5555-5555-5555-555555555501",
        status = 1,
        estimatedTotal = 42.0,
        items = listOf(
            OrderItemDto(id = "i1", name = "Rosa vermelha", quantity = 6, unitPrice = 2.5),
        ),
    )
}
