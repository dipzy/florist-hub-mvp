package com.floristhub.app.screenshots

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.app.components.FloristScaffold
import com.floristhub.app.components.FloristScreenBox
import com.floristhub.app.components.MiniTrendChart
import com.floristhub.app.components.OrderStatusChip
import com.floristhub.app.format.formatEuroAmount
import com.floristhub.app.parseThemeColor
import com.floristhub.app.theme.FloristHubTheme
import com.floristhub.network.dto.FlowerDto
import com.floristhub.network.dto.OrderDto

@Composable
fun ScreenshotRoot(content: @Composable () -> Unit) {
    FloristHubTheme(
        storePrimary = parseThemeColor(ScreenshotFixtures.storePrimary),
        storeAccent = parseThemeColor(ScreenshotFixtures.storeAccent),
    ) {
        Surface(modifier = Modifier.fillMaxSize(), color = MaterialTheme.colorScheme.background) {
            content()
        }
    }
}

@Composable
fun ScreenshotPortfolioLoja() {
    ScreenshotRoot {
        FloristScaffold(title = "Portfolio") { padding ->
            FloristScreenBox(padding) {
                LazyColumn(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    items(ScreenshotFixtures.portfolio) { p ->
                        Card(elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)) {
                            Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                                Text(p.title, style = MaterialTheme.typography.titleLarge)
                                p.price?.let {
                                    Text("$it €", style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.primary)
                                }
                                p.description?.let { Text(it, color = MaterialTheme.colorScheme.onSurfaceVariant) }
                                Button(onClick = {}) { Text("Usar como base") }
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun ScreenshotEncomendaDetalhe() {
    ScreenshotRoot {
        FloristScaffold(title = "Encomendas") { padding ->
            FloristScreenBox(padding) {
                OrderDetailCard(ScreenshotFixtures.orderDetail)
            }
        }
    }
}

@Composable
private fun OrderDetailCard(o: OrderDto) {
    val statusLabels = listOf(
        "Pendente de confirmação", "Confirmada", "Em preparação", "Pronta", "Entregue", "Cancelada", "Recusada",
    )
    Card {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Text("#${o.id.take(8)}", style = MaterialTheme.typography.titleLarge)
            OrderStatusChip(status = o.status, label = statusLabels.getOrElse(o.status) { "?" })
            Text("Total: ${o.estimatedTotal} €", style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.primary)
            o.deliveryAddress?.let { Text("Morada: $it", style = MaterialTheme.typography.bodySmall) }
            o.items.forEach { i -> Text("${i.name} ×${i.quantity}", style = MaterialTheme.typography.bodyMedium) }
            if (o.statusHistory.isNotEmpty()) {
                Text("Linha do tempo", style = MaterialTheme.typography.labelLarge)
                o.statusHistory.forEach { h ->
                    Text(
                        "• ${statusLabels.getOrElse(h.fromStatus) { "?" }} → ${statusLabels.getOrElse(h.toStatus) { "?" }}: ${h.notes ?: "—"}",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
            o.changeProposals.filter { it.status == 0 }.forEach { proposal ->
                Text("Proposta: ${proposal.message}")
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    Button(onClick = {}) { Text("Aceitar") }
                    OutlinedButton(onClick = {}) { Text("Recusar") }
                }
            }
        }
    }
}

@Composable
fun ScreenshotMontarCheckout() {
    ScreenshotRoot {
        FloristScaffold(title = "Montar arranjo") { padding ->
            FloristScreenBox(padding) {
                LazyColumn(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    item { Text("Flores disponíveis", style = MaterialTheme.typography.titleMedium) }
                    items(ScreenshotFixtures.flowers) { flower -> FlowerRow(flower) }
                    item {
                        Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            Text("Cesto", style = MaterialTheme.typography.titleMedium)
                            Text("Rosa vermelha ×6 · Túlipa amarela ×4")
                            Text("Total estimado: 18,60 €", color = MaterialTheme.colorScheme.primary)
                            OutlinedTextField(value = "Para oferecer no sábado.", onValueChange = {}, label = { Text("Notas") })
                            Button(onClick = {}, modifier = Modifier.fillMaxWidth()) { Text("Submeter encomenda") }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun FlowerRow(flower: FlowerDto) {
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
        Column {
            Text(flower.name, style = MaterialTheme.typography.titleSmall)
            flower.color?.let { Text(it, style = MaterialTheme.typography.bodySmall) }
        }
        Text("${flower.unitPrice} €", color = MaterialTheme.colorScheme.primary)
    }
}

@Composable
fun ScreenshotFloristDashboard() {
    val d = ScreenshotFixtures.dashboard
    val m = ScreenshotFixtures.metrics
    val colors = MaterialTheme.colorScheme
    val trend = m.ordersByDay.map { it.count }.takeLast(14)
    ScreenshotRoot {
        Column(Modifier.fillMaxSize().padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Text("Dashboard da florista", style = MaterialTheme.typography.headlineSmall)
            Text("2 encomendas hoje · 1 encomenda amanhã")
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                listOf(7, 30, 90).forEach { days ->
                    FilterChip(
                        selected = m.days == days,
                        onClick = {},
                        label = { Text("${days}d") },
                        colors = FilterChipDefaults.filterChipColors(selectedContainerColor = colors.primary.copy(alpha = 0.2f)),
                    )
                }
            }
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                MetricTile("Pendentes", m.pendingOrders.toString(), Modifier.weight(1f))
                MetricTile("Receita (30d)", "${formatEuroAmount(m.periodRevenue)} €", Modifier.weight(1f))
            }
            Card(colors = CardDefaults.cardColors(containerColor = colors.surfaceVariant.copy(alpha = 0.45f))) {
                Column(Modifier.padding(12.dp)) {
                    Text("Tendência de encomendas", style = MaterialTheme.typography.titleSmall)
                    MiniTrendChart(values = trend, lineColor = colors.primary, fillColor = colors.tertiary.copy(alpha = 0.25f))
                }
            }
            Text("Receita do mês: ${d.monthlyRevenue} €")
        }
    }
}

@Composable
private fun MetricTile(label: String, value: String, modifier: Modifier = Modifier) {
    Card(modifier = modifier) {
        Column(Modifier.padding(12.dp)) {
            Text(label, style = MaterialTheme.typography.labelMedium)
            Text(value, style = MaterialTheme.typography.titleLarge)
        }
    }
}
