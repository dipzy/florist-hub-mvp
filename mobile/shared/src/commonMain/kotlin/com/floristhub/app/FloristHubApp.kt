package com.floristhub.app

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.graphics.Color
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import androidx.savedstate.read
import com.floristhub.app.i18n.LocalUiPreferences
import com.floristhub.app.i18n.ProvideAppLocale
import com.floristhub.app.i18n.UiPreferencesController
import com.floristhub.app.screens.AppPreferencesScreen
import com.floristhub.app.screens.ConfiguratorScreen
import com.floristhub.app.screens.BuildArrangementScreen
import com.floristhub.app.screens.FloristDashboardScreen
import com.floristhub.app.screens.FloristOnboardingScreen
import com.floristhub.app.screens.FloristStoreSettingsScreen
import com.floristhub.app.screens.HomeScreen
import com.floristhub.app.screens.LegalDocumentScreen
import com.floristhub.app.screens.LoginScreen
import com.floristhub.app.screens.NotificationPreferencesScreen
import com.floristhub.app.screens.OrderDetailScreen
import com.floristhub.app.screens.OrdersScreen
import com.floristhub.app.screens.PortfolioScreen
import com.floristhub.app.screens.RecurringSubscriptionsScreen
import com.floristhub.push.orderIdFromFloristHubUri
import com.floristhub.app.theme.FloristHubTheme
import com.floristhub.auth.createAuthSessionStore
import com.floristhub.config.DEFAULT_APP_TENANT_SLUG
import com.floristhub.config.normalizeTenantSlug
import com.floristhub.network.ApiClient
import com.floristhub.network.createHttpClient
import com.floristhub.platform.isUiPreviewEnabled
import com.floristhub.preferences.apiAcceptLanguage
import com.floristhub.preferences.createAppPreferencesStore
import com.floristhub.preferences.resolveDarkTheme
import com.floristhub.preview.PreviewApiClient
import com.floristhub.preview.runUiPreviewTour
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

typealias PhotoUploadTrigger = ((onPicked: (ByteArray, String, String) -> Unit) -> Unit)

@Composable
fun FloristHubApp(
    apiBaseUrl: String = "http://10.0.2.2:5183",
    webBaseUrl: String = "http://10.0.2.2:5173",
    configuredTenantSlug: String = DEFAULT_APP_TENANT_SLUG,
    onUploadPhoto: PhotoUploadTrigger? = null,
    openOrderUri: String? = null,
    pushToken: suspend () -> String? = { null },
    pushPlatform: String = "Android",
) {
    val preview = remember { isUiPreviewEnabled() }
    val appTenantSlug = remember(configuredTenantSlug) { normalizeTenantSlug(configuredTenantSlug) }
    val sessionStore = remember { createAuthSessionStore() }
    val preferencesStore = remember { createAppPreferencesStore() }
    val scope = rememberCoroutineScope()
    var themePreference by remember { mutableStateOf(preferencesStore.getThemePreference()) }
    var appLocale by remember { mutableStateOf(preferencesStore.getAppLocale()) }
    val systemDark = isSystemInDarkTheme()
    val darkTheme = resolveDarkTheme(themePreference, systemDark)
    val api = remember(apiBaseUrl, preview) {
        if (preview) {
            PreviewApiClient()
        } else {
            ApiClient(createHttpClient(), baseUrl = apiBaseUrl)
        }
    }
    LaunchedEffect(appLocale, api) {
        api.acceptLanguage = apiAcceptLanguage(appLocale)
    }
    fun syncPush() {
        if (preview || api.token.isNullOrBlank()) return
        scope.launch {
            repeat(3) {
                val device = runCatching { pushToken() }.getOrNull()
                if (!device.isNullOrBlank()) {
                    runCatching { api.registerPushDevice(device, pushPlatform) }
                    return@launch
                }
                delay(1500)
            }
        }
    }
    LaunchedEffect(sessionStore, appTenantSlug, preview) {
        if (!preview) {
            runCatching { api.ensureConfiguredTenantResolved(appTenantSlug) }
        }
    }
    LaunchedEffect(sessionStore) {
        sessionStore.getToken()?.let { api.token = it }
        sessionStore.getTenantId()?.let { api.tenantId = it }
        syncPush()
    }
    val nav = rememberNavController()
    fun openPendingOrder() {
        val id = orderIdFromFloristHubUri(openOrderUri) ?: return
        if (!preview && api.token.isNullOrBlank()) return
        nav.navigate("order/$id") { launchSingleTop = true }
    }
    LaunchedEffect(openOrderUri) {
        openPendingOrder()
    }
    val isCustomer = !preview && api.role == "Cliente"
    val isFlorist = preview || api.role == "Florista"

    var storePrimary by remember { mutableStateOf<Color?>(null) }
    var storeAccent by remember { mutableStateOf<Color?>(null) }
    LaunchedEffect(api.tenantId) {
        if (api.tenantId.isNullOrBlank()) return@LaunchedEffect
        runCatching { api.storeTheme() }
            .onSuccess {
                storePrimary = parseThemeColor(it.themePrimaryColor)
                storeAccent = parseThemeColor(it.themeAccentColor)
            }
    }

    if (preview) {
        LaunchedEffect(nav) {
            runUiPreviewTour(nav)
        }
    }

    val uiPreferences = UiPreferencesController(
        theme = themePreference,
        locale = appLocale,
        onThemeChange = { next ->
            themePreference = next
            preferencesStore.setThemePreference(next)
        },
        onLocaleChange = { next ->
            appLocale = next
            preferencesStore.setAppLocale(next)
        },
    )

    ProvideAppLocale(appLocale) {
        CompositionLocalProvider(LocalUiPreferences provides uiPreferences) {
            FloristHubTheme(
                darkTheme = darkTheme,
                storePrimary = storePrimary,
                storeAccent = storeAccent,
            ) {
                Surface(color = androidx.compose.material3.MaterialTheme.colorScheme.background) {
                    NavHost(navController = nav, startDestination = "login") {
                composable("login") {
                    LoginScreen(
                        api = api,
                        onLoggedIn = {
                            nav.navigate("home") { popUpTo("login") { inclusive = true } }
                            syncPush()
                            openPendingOrder()
                        },
                    )
                }
                composable("home") {
                    HomeScreen(
                        onPortfolio = { nav.navigate("portfolio") },
                        onOrders = { nav.navigate("orders") },
                        onBuild = if (isCustomer) {
                            { nav.navigate("build") }
                        } else {
                            null
                        },
                        onFloristDashboard = if (isFlorist) {
                            { nav.navigate("florist") }
                        } else {
                            null
                        },
                        onFloristStore = if (isFlorist) {
                            { nav.navigate("florist-store") }
                        } else {
                            null
                        },
                        onConfigurator = { nav.navigate("configurador") },
                        onNotifications = if (preview) null else {
                            { nav.navigate("notifications") }
                        },
                        onRecurring = if (isCustomer) {
                            { nav.navigate("recurring") }
                        } else {
                            null
                        },
                        onLegalTerms = { nav.navigate("legal/termos") },
                        onLegalPrivacy = { nav.navigate("legal/privacidade") },
                        onLegalCookies = { nav.navigate("legal/cookies") },
                        onAppPreferences = { nav.navigate("app-preferences") },
                        api = api,
                    )
                }
                composable("app-preferences") {
                    AppPreferencesScreen(onBack = { nav.popBackStack() })
                }
                composable(
                    route = "legal/{kind}",
                    arguments = listOf(navArgument("kind") { type = NavType.StringType }),
                ) { entry ->
                    val kind = entry.arguments?.read { getStringOrNull("kind") } ?: "termos"
                    LegalDocumentScreen(api, kind)
                }
                composable("portfolio") {
                    PortfolioScreen(
                        api = api,
                        isCustomer = isCustomer,
                        onUseAsBase = { id -> nav.navigate("build/$id") },
                    )
                }
                composable("build") {
                    BuildArrangementScreen(api, null)
                }
                composable(
                    route = "build/{inspire}",
                    arguments = listOf(navArgument("inspire") { type = NavType.StringType }),
                ) { entry ->
                    val inspire = entry.destination.route
                        ?.removePrefix("build/")
                        ?.takeIf { it.isNotEmpty() && it != "build" }
                    BuildArrangementScreen(api, inspire)
                }
                composable("orders") { OrdersScreen(api) }
                composable("recurring") {
                    RecurringSubscriptionsScreen(api, onBack = { nav.popBackStack() })
                }
                composable(
                    route = "order/{orderId}",
                    arguments = listOf(navArgument("orderId") { type = NavType.StringType }),
                ) { entry ->
                    val orderId = entry.arguments?.read { getStringOrNull("orderId") }.orEmpty()
                    OrderDetailScreen(
                        api = api,
                        orderId = orderId,
                        onBack = { nav.popBackStack() },
                    )
                }
                composable("notifications") {
                    NotificationPreferencesScreen(api, onBack = { nav.popBackStack() })
                }
                composable("configurador") {
                    ConfiguratorScreen(
                        api = api,
                        webBaseUrl = webBaseUrl,
                        preview = preview,
                        onBack = { nav.popBackStack() },
                    )
                }
                composable("florist") {
                    FloristDashboardScreen(
                        api = api,
                        onUploadPhoto = onUploadPhoto,
                        onOnboarding = { nav.navigate("florist-onboarding") },
                    )
                }
                composable("florist-onboarding") {
                    FloristOnboardingScreen(api = api, onOpenSettings = { nav.navigate("florist-store") })
                }
                composable("florist-store") {
                    FloristStoreSettingsScreen(
                        api = api,
                        onUploadLogo = onUploadPhoto,
                        onOpenAppPreferences = { nav.navigate("app-preferences") },
                    )
                }
                    }
                }
            }
        }
    }
}
