package com.floristhub.network

import com.floristhub.network.dto.AuthResponse
import com.floristhub.network.dto.CreateOrderItemDto
import com.floristhub.network.dto.FlowerDto
import com.floristhub.network.dto.CheckoutSessionDto
import com.floristhub.network.dto.NotificationPreferencesDto
import com.floristhub.network.dto.OrderDto
import com.floristhub.network.dto.PhotoUploadResponse
import com.floristhub.network.dto.FloristDashboardDto
import com.floristhub.network.dto.FloristDashboardMetricsDto
import com.floristhub.network.dto.FloristUpcomingRecurringDto
import com.floristhub.network.dto.RecurringSubscriptionDto
import com.floristhub.network.dto.PortfolioDto
import com.floristhub.network.dto.PublicCapacityDayDto
import com.floristhub.network.dto.PublicStoreCheckoutDto
import com.floristhub.network.dto.PublicLegalDocumentDto
import com.floristhub.network.dto.LegalVersionsDto
import com.floristhub.network.dto.PublicStoreThemeDto
import com.floristhub.network.dto.FloristOnboardingStatusDto
import com.floristhub.network.dto.PublicStorefrontDto
import com.floristhub.network.dto.StoreSettingsDto
import com.floristhub.network.dto.UpdateStoreSettingsDto
import com.floristhub.network.dto.TenantDto
import io.ktor.client.HttpClient
import io.ktor.client.call.body
import io.ktor.client.request.forms.formData
import io.ktor.client.request.forms.submitFormWithBinaryData
import io.ktor.client.request.HttpRequestBuilder
import io.ktor.client.request.get
import io.ktor.client.request.header
import io.ktor.client.request.patch
import io.ktor.client.request.post
import io.ktor.client.request.put
import io.ktor.client.request.setBody
import io.ktor.http.ContentType
import io.ktor.http.Headers
import io.ktor.http.HttpHeaders
import io.ktor.http.contentType
import io.ktor.http.encodeURLPathPart
import com.floristhub.config.normalizeTenantSlug
import kotlinx.serialization.Serializable
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put

open class ApiClient(private val http: HttpClient, private val baseUrl: String) {
    var token: String? = null
    var tenantId: String? = null
    var tenantSlug: String? = null
    var tenantName: String? = null
    var role: String? = null
    var acceptLanguage: String = "pt-PT"

    private fun HttpRequestBuilder.applyApiHeaders(includeAuth: Boolean = true) {
        header("Accept-Language", acceptLanguage)
        if (includeAuth) {
            token?.let { header("Authorization", "Bearer $it") }
            tenantId?.let { header("X-Tenant-Id", it) }
            tenantSlug?.let { header("X-Tenant-Slug", it) }
        }
    }

    fun mediaUrl(path: String): String =
        if (path.startsWith("http")) path else "$baseUrl${if (path.startsWith("/")) path else "/$path"}"

    open suspend fun resolveTenant(slug: String): TenantDto {
        val normalized = normalizeTenantSlug(slug)
        return http.get("$baseUrl/api/tenants/resolve/${normalized.encodeURLPathPart()}") {
            applyApiHeaders(includeAuth = false)
        }.body()
    }

    suspend fun bindResolvedTenant(tenant: TenantDto) {
        tenantId = tenant.id
        tenantSlug = tenant.slug
        tenantName = tenant.name
    }

    suspend fun ensureConfiguredTenantResolved(configuredSlug: String) {
        val tenant = resolveTenant(configuredSlug)
        bindResolvedTenant(tenant)
    }

    suspend fun login(email: String, password: String): AuthResponse =
        http.post("$baseUrl/api/auth/login") {
            contentType(ContentType.Application.Json)
            applyApiHeaders(includeAuth = false)
            setBody(
                LoginBody(
                    email = email,
                    password = password,
                    tenantId = tenantId?.takeIf { it.isNotBlank() },
                    tenantSlug = tenantSlug?.takeIf { it.isNotBlank() },
                ),
            )
        }.body<AuthResponse>().also { auth ->
            role = auth.role
            auth.tenantId?.takeIf { it.isNotBlank() }?.let { tenantId = it }
            auth.tenantSlug?.takeIf { it.isNotBlank() }?.let { tenantSlug = it }
        }

    open suspend fun flowers(): List<FlowerDto> = authorizedGet("$baseUrl/api/flowers")

    open suspend fun portfolio(): List<PortfolioDto> = authorizedGet("$baseUrl/api/portfolio")

    suspend fun portfolioItem(id: String): PortfolioDto = authorizedGet("$baseUrl/api/portfolio/$id")

    suspend fun portfolioManage(): List<PortfolioDto> = authorizedGet("$baseUrl/api/portfolio/manage")

    open suspend fun myOrders(): List<OrderDto> = authorizedGet("$baseUrl/api/orders/mine")

    suspend fun getOrder(id: String): OrderDto = authorizedGet("$baseUrl/api/orders/$id")

    suspend fun pendingOrders(): List<OrderDto> = authorizedGet("$baseUrl/api/orders/pending")

    suspend fun floristOrders(status: Int? = null): List<OrderDto> {
        val url = if (status != null) "$baseUrl/api/orders/manage?status=$status" else "$baseUrl/api/orders/manage"
        return authorizedGet(url)
    }

    open suspend fun floristDashboard(): FloristDashboardDto = authorizedGet("$baseUrl/api/florist/dashboard")

    suspend fun floristDashboardMetrics(days: Int = 30): FloristDashboardMetricsDto =
        authorizedGet("$baseUrl/api/florist/dashboard/metrics?days=$days")

    open suspend fun storeCheckout(): PublicStoreCheckoutDto = authorizedGet("$baseUrl/api/store/checkout")

    open suspend fun publicCapacity(from: String, to: String): List<PublicCapacityDayDto> =
        authorizedGet("$baseUrl/api/store/capacity?from=$from&to=$to")

    open suspend fun floristCapacity(from: String, to: String): List<PublicCapacityDayDto> =
        authorizedGet("$baseUrl/api/florist/capacity?from=$from&to=$to")

    open suspend fun storeTheme(): PublicStoreThemeDto = authorizedGet("$baseUrl/api/store/theme")

    open suspend fun storefront(): PublicStorefrontDto = authorizedGet("$baseUrl/api/store/home")

    suspend fun applyCoupon(orderId: String, code: String): OrderDto =
        authorizedJsonPost("$baseUrl/api/orders/$orderId/coupon", buildJsonObject { put("code", code) }.toString())

    suspend fun createReview(orderId: String, stars: Int, comment: String) {
        authorizedJsonPost<ReviewAck>(
            "$baseUrl/api/orders/$orderId/review",
            buildJsonObject {
                put("stars", stars)
                put("comment", comment)
            }.toString(),
        )
    }

    suspend fun legalVersions(): LegalVersionsDto = authorizedGet("$baseUrl/api/legal/versions")

    suspend fun legalTerms(): PublicLegalDocumentDto = authorizedGet("$baseUrl/api/legal/terms")

    suspend fun legalPrivacy(): PublicLegalDocumentDto = authorizedGet("$baseUrl/api/legal/privacy")

    suspend fun legalCookies(): PublicLegalDocumentDto = authorizedGet("$baseUrl/api/legal/cookies")

    open suspend fun storeSettings(): StoreSettingsDto = authorizedGet("$baseUrl/api/florist/store")

    suspend fun floristOnboarding(): FloristOnboardingStatusDto = authorizedGet("$baseUrl/api/florist/onboarding")

    suspend fun publishFloristStore(): FloristOnboardingStatusDto =
        http.post("$baseUrl/api/florist/onboarding/publish") {
            contentType(ContentType.Application.Json)
            applyApiHeaders()
            setBody("{}")
        }.body()

    suspend fun updateStoreSettings(body: UpdateStoreSettingsDto): StoreSettingsDto =
        http.put("$baseUrl/api/florist/store") {
            contentType(ContentType.Application.Json)
            applyApiHeaders()
            setBody(body)
        }.body()

    suspend fun createOrder(
        baseType: Int,
        deliveryType: Int,
        deliveryAddress: String?,
        deliveryZoneId: String?,
        requestedDeliveryAt: String?,
        customerNotes: String?,
        inspiredByPortfolioArrangementId: String?,
        items: List<CreateOrderItemDto>,
        acceptTermsAndPrivacy: Boolean = false,
        termsVersion: String? = null,
        privacyVersion: String? = null,
        couponCode: String? = null,
    ): OrderDto {
        val body = CreateOrderBody(
            baseType,
            deliveryType,
            deliveryAddress,
            deliveryZoneId,
            requestedDeliveryAt,
            customerNotes,
            inspiredByPortfolioArrangementId,
            items,
            acceptTermsAndPrivacy,
            termsVersion,
            privacyVersion,
            couponCode,
        )
        return authorizedJsonPost("$baseUrl/api/orders", Json.encodeToString(body))
    }

    suspend fun acceptProposal(orderId: String, proposalId: String, notes: String? = null): OrderDto =
        authorizedPost("$baseUrl/api/orders/$orderId/proposals/$proposalId/accept", notes)

    suspend fun rejectProposal(orderId: String, proposalId: String, notes: String? = null): OrderDto =
        authorizedPost("$baseUrl/api/orders/$orderId/proposals/$proposalId/reject", notes)

    suspend fun proposeChanges(
        orderId: String,
        message: String,
        proposedItemChangesJson: String? = null,
        revisedTotal: Double? = null,
    ): OrderDto = authorizedJsonPost(
        "$baseUrl/api/orders/$orderId/proposals",
        buildJsonObject {
            put("message", message)
            proposedItemChangesJson?.let { put("proposedItemChangesJson", it) }
            revisedTotal?.let { put("revisedTotal", it) }
        }.toString(),
    )

    suspend fun confirmOrder(orderId: String, notes: String? = null): OrderDto =
        authorizedPost("$baseUrl/api/orders/$orderId/confirm", notes)

    suspend fun rejectOrder(orderId: String, notes: String): OrderDto =
        authorizedPost("$baseUrl/api/orders/$orderId/reject", notes)

    suspend fun cancelOrder(orderId: String, notes: String? = null): OrderDto =
        authorizedPost("$baseUrl/api/orders/$orderId/cancel", notes)

    suspend fun updateOrderStatus(orderId: String, newStatus: Int, notes: String? = null): OrderDto =
        http.patch("$baseUrl/api/orders/$orderId/status") {
            contentType(ContentType.Application.Json)
            applyApiHeaders()
            setBody(StatusBody(newStatus, notes))
        }.body()

    suspend fun startCheckout(
        orderId: String,
        successUrl: String,
        cancelUrl: String,
        acceptTermsAndPrivacy: Boolean,
        termsVersion: String,
        privacyVersion: String,
    ): CheckoutSessionDto =
        authorizedJsonPost(
            "$baseUrl/api/orders/$orderId/payment",
            Json.encodeToString(
                CheckoutBody(successUrl, cancelUrl, acceptTermsAndPrivacy, termsVersion, privacyVersion),
            ),
        )

    suspend fun notificationPreferences(): NotificationPreferencesDto =
        authorizedGet("$baseUrl/api/notifications/preferences")

    suspend fun updateNotificationPreferences(body: NotificationPreferencesDto): NotificationPreferencesDto =
        http.put("$baseUrl/api/notifications/preferences") {
            contentType(ContentType.Application.Json)
            applyApiHeaders()
            setBody(body)
        }.body()

    suspend fun registerPushDevice(deviceToken: String, platform: String) {
        val response = http.post("$baseUrl/api/notifications/devices") {
            contentType(ContentType.Application.Json)
            applyApiHeaders()
            setBody(PushDeviceBody(deviceToken, platform))
        }
        if (response.status.value !in 200..299) {
            error("Não foi possível registar o dispositivo.")
        }
    }

    suspend fun uploadPortfolioPhoto(bytes: ByteArray, fileName: String, contentType: String): PhotoUploadResponse =
        http.submitFormWithBinaryData(
            url = "$baseUrl/api/files/upload",
            formData = formData {
                append(
                    "file",
                    bytes,
                    Headers.build {
                        append(HttpHeaders.ContentType, contentType)
                        append(HttpHeaders.ContentDisposition, "filename=\"$fileName\"")
                    },
                )
            },
        ) {
            applyApiHeaders()
        }.body()

    suspend fun createPortfolio(title: String, description: String?, photoUrls: List<String>): PortfolioDto {
        val body = CreatePortfolioBody(
            title = title,
            description = description,
            photos = photoUrls.mapIndexed { index, url -> com.floristhub.network.dto.PortfolioPhotoDto(url, index) },
        )
        return authorizedJsonPost("$baseUrl/api/portfolio", Json.encodeToString(body))
    }

    private suspend inline fun <reified T> authorizedGet(url: String): T =
        http.get(url) { applyApiHeaders() }.body()

    private suspend inline fun <reified T> authorizedJsonPost(url: String, jsonBody: String): T =
        http.post(url) {
            contentType(ContentType.Application.Json)
            applyApiHeaders()
            setBody(jsonBody)
        }.body()

    private suspend inline fun <reified T> authorizedPost(url: String, notes: String?): T =
        http.post(url) {
            contentType(ContentType.Application.Json)
            applyApiHeaders()
            setBody(ProposalNotesBody(notes))
        }.body()

    open suspend fun myRecurringSubscriptions(): List<RecurringSubscriptionDto> =
        authorizedGet("$baseUrl/api/recurring-subscriptions/mine")

    suspend fun floristRecurringUpcoming(days: Int = 14): List<FloristUpcomingRecurringDto> =
        authorizedGet("$baseUrl/api/recurring-subscriptions/upcoming?days=$days")

    suspend fun pauseRecurring(id: String): RecurringSubscriptionDto =
        authorizedJsonPost("$baseUrl/api/recurring-subscriptions/$id/pause", "{}")

    @Serializable
    private data class PushDeviceBody(val token: String, val platform: String)

    @Serializable
    private data class LoginBody(
        val email: String,
        val password: String,
        val tenantId: String? = null,
        val tenantSlug: String? = null,
    )

    @Serializable
    private data class ProposalNotesBody(val notes: String?)

    @Serializable
    private data class CheckoutBody(
        val successUrl: String,
        val cancelUrl: String,
        val acceptTermsAndPrivacy: Boolean = false,
        val termsVersion: String? = null,
        val privacyVersion: String? = null,
    )

    @Serializable
    private data class StatusBody(val newStatus: Int, val notes: String?)

    @Serializable
    private data class CreateOrderBody(
        val baseType: Int,
        val deliveryType: Int,
        val deliveryAddress: String? = null,
        val deliveryZoneId: String? = null,
        val requestedDeliveryAt: String? = null,
        val customerNotes: String? = null,
        val inspiredByPortfolioArrangementId: String? = null,
        val items: List<CreateOrderItemDto>,
        val acceptTermsAndPrivacy: Boolean = false,
        val termsVersion: String? = null,
        val privacyVersion: String? = null,
        val couponCode: String? = null,
    )

    @Serializable
    private data class ReviewAck(val id: String = "")

    @Serializable
    private data class CreatePortfolioBody(
        val title: String,
        val description: String? = null,
        val photos: List<com.floristhub.network.dto.PortfolioPhotoDto>,
        val items: List<com.floristhub.network.dto.PortfolioItemDto> = emptyList(),
    )
}
