package com.floristhub.network.dto

import kotlinx.serialization.Serializable

@Serializable
data class TenantDto(val id: String, val name: String, val slug: String, val description: String? = null)

@Serializable
data class NotificationPreferencesDto(
    val newOrder: Boolean = true,
    val orderPaid: Boolean = true,
    val orderStatus: Boolean = true,
    val lowStock: Boolean = true,
    val recurringSubscriptions: Boolean = true,
)

@Serializable
data class AuthResponse(
    val token: String,
    val expiresAt: String,
    val userId: String,
    val email: String,
    val role: String,
    val tenantId: String? = null,
    val tenantSlug: String? = null,
)

@Serializable
data class PortfolioPhotoDto(val url: String, val sortOrder: Int)

@Serializable
data class PortfolioDto(
    val id: String,
    val title: String,
    val description: String? = null,
    val price: Double? = null,
    val tags: String? = null,
    val status: Int = 0,
    val photos: List<PortfolioPhotoDto> = emptyList(),
    val items: List<PortfolioItemDto> = emptyList(),
)

@Serializable
data class PortfolioItemDto(
    val name: String,
    val quantity: Int,
    val isMaterial: Boolean,
    val flowerId: String? = null,
)

@Serializable
data class PublicCampaignFlowerDto(
    val flowerId: String,
    val name: String,
    val catalogPrice: Double = 0.0,
    val specialPrice: Double? = null,
)

@Serializable
data class PublicCampaignDto(
    val name: String,
    val slug: String,
    val description: String? = null,
    val startsOn: String,
    val endsOn: String,
    val flowers: List<PublicCampaignFlowerDto> = emptyList(),
)

@Serializable
data class PublicSeasonalFlowerDto(
    val id: String,
    val name: String,
    val unitPrice: Double = 0.0,
    val specialPrice: Double? = null,
)

@Serializable
data class PublicStorefrontDto(
    val name: String,
    val description: String? = null,
    val campaign: PublicCampaignDto? = null,
    val seasonalFlowers: List<PublicSeasonalFlowerDto> = emptyList(),
    val averageRating: Double = 0.0,
    val reviewCount: Int = 0,
)

@Serializable
data class FlowerDto(
    val id: String,
    val name: String,
    val color: String? = null,
    val unitPrice: Double,
    val isApproximatelyAvailable: Boolean = true,
    val imageUrl: String? = null,
    val stockQuantity: Int? = null,
    val isOutOfStock: Boolean = false,
)

@Serializable
data class OrderItemDto(
    val id: String,
    val flowerId: String? = null,
    val name: String,
    val quantity: Int,
    val unitPrice: Double,
)

@Serializable
data class CreateOrderItemDto(
    val flowerId: String? = null,
    val name: String,
    val quantity: Int,
    val unitPrice: Double,
)

@Serializable
data class OrderChangeProposalDto(
    val id: String,
    val status: Int,
    val message: String,
    val proposedItemChangesJson: String? = null,
    val revisedTotal: Double? = null,
    val createdAt: String,
)

@Serializable
data class OrderStatusHistoryDto(
    val id: String,
    val fromStatus: Int,
    val toStatus: Int,
    val createdAt: String,
    val notes: String? = null,
)

@Serializable
data class OrderPaymentDto(
    val id: String,
    val status: Int,
    val amount: Double,
    val refundedAmount: Double = 0.0,
    val currency: String = "eur",
)

@Serializable
data class CheckoutSessionDto(
    val paymentsEnabled: Boolean = false,
    val checkoutUrl: String? = null,
    val paymentId: String? = null,
    val status: Int? = null,
    val message: String? = null,
)

@Serializable
data class OrderDto(
    val id: String,
    val status: Int,
    val baseType: Int = 0,
    val deliveryType: Int = 0,
    val deliveryAddress: String? = null,
    val requestedDeliveryAt: String? = null,
    val customerNotes: String? = null,
    val inspiredByPortfolioArrangementId: String? = null,
    val estimatedTotal: Double,
    val deliveryFee: Double = 0.0,
    val deliveryZoneId: String? = null,
    val deliveryZoneName: String? = null,
    val paymentCaptured: Boolean = false,
    val payment: OrderPaymentDto? = null,
    val items: List<OrderItemDto> = emptyList(),
    val changeProposals: List<OrderChangeProposalDto> = emptyList(),
    val statusHistory: List<OrderStatusHistoryDto> = emptyList(),
    val discountAmount: Double = 0.0,
    val couponCode: String? = null,
)

@Serializable
data class PhotoUploadResponse(val url: String, val storagePath: String)

@Serializable
data class OpeningHourDto(
    val dayOfWeek: Int,
    val openTime: String? = null,
    val closeTime: String? = null,
    val isClosed: Boolean = false,
)

@Serializable
data class DeliveryZoneDto(
    val id: String,
    val name: String,
    val fee: Double,
    val isActive: Boolean = true,
    val sortOrder: Int = 0,
)

@Serializable
data class DeliveryZoneInputDto(
    val id: String? = null,
    val name: String,
    val fee: Double,
    val isActive: Boolean = true,
    val sortOrder: Int = 0,
)

@Serializable
data class PublicStoreThemeDto(
    val name: String,
    val slug: String,
    val themePrimaryColor: String,
    val themeAccentColor: String,
    val themeFontFamily: String? = null,
)

@Serializable
data class PublicCapacityWindowDto(
    val label: String,
    val start: String,
    val end: String,
    val limit: Int,
    val booked: Int,
    val remaining: Int,
)

@Serializable
data class PublicCapacityDayDto(
    val date: String,
    val closed: Boolean = false,
    val dailyLimit: Int? = null,
    val booked: Int = 0,
    val remaining: Int? = null,
    val note: String? = null,
    val windows: List<PublicCapacityWindowDto> = emptyList(),
)

@Serializable
data class PublicStoreCheckoutDto(
    val name: String,
    val logoUrl: String? = null,
    val ordersAcceptingEnabled: Boolean = true,
    val minimumOrderAdvanceMinutes: Int = 0,
    val pickupEnabled: Boolean = true,
    val homeDeliveryEnabled: Boolean = true,
    val cemeteryDeliveryEnabled: Boolean = true,
    val openingHours: List<OpeningHourDto> = emptyList(),
    val deliveryZones: List<DeliveryZoneDto> = emptyList(),
)

@Serializable
data class LegalVersionsDto(
    val termsVersion: String,
    val privacyVersion: String,
    val cookiesVersion: String,
)

@Serializable
data class PublicLegalDocumentDto(
    val documentType: Int,
    val version: String,
    val title: String,
    val contentMarkdown: String,
    val responsibleSectionMarkdown: String? = null,
)

data class StoreSettingsDto(
    val tenantId: String,
    val name: String,
    val description: String? = null,
    val logoUrl: String? = null,
    val contactEmail: String? = null,
    val contactPhone: String? = null,
    val addressLine: String? = null,
    val city: String? = null,
    val postalCode: String? = null,
    val customDomain: String? = null,
    val themePrimaryColor: String = "#2d6a4f",
    val themeAccentColor: String = "#95d5b2",
    val themeFontFamily: String? = null,
    val ordersAcceptingEnabled: Boolean = true,
    val minimumOrderAdvanceMinutes: Int = 0,
    val pickupEnabled: Boolean = true,
    val homeDeliveryEnabled: Boolean = true,
    val cemeteryDeliveryEnabled: Boolean = true,
    val openingHours: List<OpeningHourDto> = emptyList(),
    val deliveryZones: List<DeliveryZoneDto> = emptyList(),
)

@Serializable
data class UpdateStoreSettingsDto(
    val name: String,
    val description: String? = null,
    val logoUrl: String? = null,
    val contactEmail: String? = null,
    val contactPhone: String? = null,
    val addressLine: String? = null,
    val city: String? = null,
    val postalCode: String? = null,
    val themePrimaryColor: String = "#2d6a4f",
    val themeAccentColor: String = "#95d5b2",
    val themeFontFamily: String? = null,
    val ordersAcceptingEnabled: Boolean = true,
    val minimumOrderAdvanceMinutes: Int = 120,
    val pickupEnabled: Boolean = true,
    val homeDeliveryEnabled: Boolean = true,
    val cemeteryDeliveryEnabled: Boolean = true,
    val openingHours: List<OpeningHourDto> = emptyList(),
    val deliveryZones: List<DeliveryZoneInputDto> = emptyList(),
)

@Serializable
data class OnboardingChecklistItemDto(
    val key: String,
    val title: String,
    val description: String,
    val isRequired: Boolean,
    val isComplete: Boolean,
    val adminPath: String? = null,
    val wizardStep: String? = null,
)

@Serializable
data class FloristOnboardingStatusDto(
    val isPublished: Boolean,
    val canPublish: Boolean,
    val missingRequired: List<String> = emptyList(),
    val checklist: List<OnboardingChecklistItemDto> = emptyList(),
    val currentWizardStep: String,
    val wizardProgressPercent: Int,
)

@Serializable
data class TopCatalogItemDto(
    val name: String,
    val orderLineCount: Int,
    val totalValue: Double,
)

@Serializable
data class FloristDashboardDto(
    val ordersToday: Int,
    val ordersTomorrow: Int,
    val monthlyRevenue: Double,
    val ordersByStatus: Map<String, Int> = emptyMap(),
    val topItems: List<TopCatalogItemDto> = emptyList(),
)

@Serializable
data class DailyCountDto(
    val date: String,
    val count: Int,
)

@Serializable
data class DailyRevenueDto(
    val date: String,
    val revenue: Double,
)

@Serializable
data class FloristDashboardMetricsDto(
    val days: Int,
    val pendingOrders: Int,
    val periodRevenue: Double,
    val ordersByDay: List<DailyCountDto> = emptyList(),
    val revenueByDay: List<DailyRevenueDto> = emptyList(),
    val topFlowers: List<TopCatalogItemDto> = emptyList(),
)

@Serializable
data class RecurringSubscriptionDto(
    val id: String,
    val status: Int,
    val frequency: Int,
    val anchorDeliveryAt: String,
    val nextDeliveryAt: String? = null,
    val isFloristChoice: Boolean = false,
    val floristChoiceBudget: Double? = null,
    val estimatedItemsTotal: Double = 0.0,
    val deliveryFee: Double = 0.0,
)

@Serializable
data class FloristUpcomingRecurringDto(
    val subscriptionId: String,
    val customerName: String? = null,
    val scheduledDeliveryAt: String,
    val deliveryAddress: String? = null,
    val isFloristChoice: Boolean = false,
)
