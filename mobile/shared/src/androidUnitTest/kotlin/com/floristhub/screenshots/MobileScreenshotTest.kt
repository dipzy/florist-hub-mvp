package com.floristhub.screenshots

import androidx.compose.ui.test.junit4.createComposeRule
import androidx.compose.ui.test.onRoot
import com.floristhub.app.screenshots.ScreenshotEncomendaDetalhe
import com.floristhub.app.screenshots.ScreenshotFloristDashboard
import com.floristhub.app.screenshots.ScreenshotMontarCheckout
import com.floristhub.app.screenshots.ScreenshotPortfolioLoja
import com.github.takahirom.roborazzi.captureRoboImage
import org.junit.Rule
import org.junit.Test
import org.junit.runner.RunWith
import org.robolectric.RobolectricTestRunner
import org.robolectric.annotation.Config
import org.robolectric.annotation.GraphicsMode

@RunWith(RobolectricTestRunner::class)
@GraphicsMode(GraphicsMode.Mode.NATIVE)
@Config(sdk = [33], qualifiers = "w411dp-h891dp")
class MobileScreenshotTest {

    @get:Rule
    val composeRule = createComposeRule()

    @Test
    fun capture_01_portfolio_loja() {
        composeRule.setContent { ScreenshotPortfolioLoja() }
        composeRule.onRoot().captureRoboImage("01-portfolio-loja.png")
    }

    @Test
    fun capture_02_encomenda_detalhe() {
        composeRule.setContent { ScreenshotEncomendaDetalhe() }
        composeRule.onRoot().captureRoboImage("02-encomenda-detalhe.png")
    }

    @Test
    fun capture_03_montar_checkout() {
        composeRule.setContent { ScreenshotMontarCheckout() }
        composeRule.onRoot().captureRoboImage("03-montar-checkout.png")
    }

    @Test
    fun capture_04_florist_dashboard() {
        composeRule.setContent { ScreenshotFloristDashboard() }
        composeRule.onRoot().captureRoboImage("04-florist-dashboard.png")
    }
}
