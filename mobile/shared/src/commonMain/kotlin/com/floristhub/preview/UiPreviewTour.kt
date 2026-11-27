package com.floristhub.preview

import androidx.navigation.NavHostController
import kotlinx.coroutines.delay

/**
 * Navigates main routes slowly so CI can capture simulator screenshots between steps.
 */
suspend fun runUiPreviewTour(nav: NavHostController) {
  suspend fun pause() {
      delay(4_500)
  }

  pause()
  nav.navigate("home") {
      popUpTo("login") { inclusive = true }
  }
  pause()
  nav.navigate("portfolio")
  pause()
  nav.navigate("build")
  pause()
  nav.navigate("orders")
  pause()
  nav.navigate("home") {
      popUpTo("home") { inclusive = true }
  }
  pause()
  nav.navigate("florist")
  pause()
  nav.navigate("florist-store")
}
