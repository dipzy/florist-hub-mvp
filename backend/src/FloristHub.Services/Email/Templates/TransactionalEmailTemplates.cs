using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;
using FloristHub.Services.Interfaces.Email;

namespace FloristHub.Services.Email.Templates;

internal static class TransactionalEmailTemplates
{
    public static (string Subject, string Html) Welcome(Tenant tenant, ApplicationUser user, string role)
    {
        var title = "Bem-vindo(a)";
        var roleLabel = role switch
        {
            FloristHub.DAL.Constants.Roles.Florist => "florista",
            FloristHub.DAL.Constants.Roles.Customer => "cliente",
            _ => "utilizador"
        };
        var body = EmailLayout.Paragraph($"Olá {user.FullName ?? user.Email},")
            + EmailLayout.Paragraph($"A sua conta de {roleLabel} em {tenant.Name} foi criada com sucesso.")
            + EmailLayout.Paragraph("Pode iniciar sessão na loja quando quiser para gerir arranjos e encomendas.");
        return ($"Bem-vindo(a) — {tenant.Name}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) FloristStoreInvite(
        Tenant tenant,
        ApplicationUser florist,
        string setPasswordUrl,
        int validHours)
    {
        var title = "Convite para gerir a sua loja";
        var body = EmailLayout.Paragraph($"Olá {florist.FullName ?? florist.Email},")
            + EmailLayout.Paragraph(
                $"Foi convidado(a) a gerir a loja «{tenant.Name}» no Florist Hub como florista responsável.")
            + EmailLayout.Paragraph(
                $"Defina a sua palavra-passe através do link abaixo. O convite é válido durante {validHours} horas.")
            + EmailLayout.Button(setPasswordUrl, "Definir palavra-passe")
            + EmailLayout.Paragraph(
                "Se não esperava este email, pode ignorá-lo. O link deixa de funcionar após o prazo indicado.");
        return ($"Convite — {tenant.Name}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) OrderReceivedCustomer(Tenant tenant, OrderEmailView order)
    {
        var title = "Encomenda recebida";
        var body = EmailLayout.Paragraph($"Olá{GreetName(order.CustomerName)},")
            + EmailLayout.Paragraph($"Recebemos a sua encomenda #{EmailLayout.OrderRef(order.Id)}. A florista irá analisá-la em breve.")
            + EmailLayout.Paragraph($"Entrega: {order.DeliveryLabel}.")
            + EmailLayout.BuildOrderSummary(order)
            + EmailLayout.Paragraph("Receberá um email quando a encomenda for confirmada ou se houver uma proposta de alteração.")
            + EmailLayout.TrackingBlock(order);
        return ($"Encomenda recebida — #{EmailLayout.OrderRef(order.Id)}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) NewOrderFlorist(Tenant tenant, OrderEmailView order)
    {
        var title = "Nova encomenda";
        var body = EmailLayout.Paragraph($"Chegou uma nova encomenda #{EmailLayout.OrderRef(order.Id)}.")
            + EmailLayout.Paragraph($"Cliente: {order.CustomerName ?? order.CustomerEmail ?? "—"}.")
            + EmailLayout.Paragraph($"Entrega: {order.DeliveryLabel}.")
            + EmailLayout.BuildOrderSummary(order)
            + EmailLayout.Paragraph("Confirme, proponha alterações ou recuse no painel da loja.");
        return ($"Nova encomenda #{EmailLayout.OrderRef(order.Id)}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) OrderConfirmed(Tenant tenant, OrderEmailView order, string? paymentUrl)
    {
        var title = "Encomenda confirmada";
        var body = EmailLayout.Paragraph($"Olá{GreetName(order.CustomerName)},")
            + EmailLayout.Paragraph($"A sua encomenda #{EmailLayout.OrderRef(order.Id)} foi confirmada pela florista.")
            + EmailLayout.BuildOrderSummary(order);

        if (!string.IsNullOrWhiteSpace(paymentUrl))
            body += EmailLayout.Paragraph("Para concluir, efetue o pagamento através do link abaixo.")
                + EmailLayout.Button(paymentUrl, "Pagar encomenda");
        else
            body += EmailLayout.Paragraph("Inicie sessão na loja para efetuar o pagamento quando estiver pronto.");

        body += EmailLayout.TrackingBlock(order);
        return ($"Encomenda confirmada — #{EmailLayout.OrderRef(order.Id)}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) ChangeProposal(Tenant tenant, OrderEmailView order, string message, decimal? revisedTotal)
    {
        var title = "Proposta de alteração";
        var extra = revisedTotal.HasValue
            ? EmailLayout.Paragraph($"Novo total proposto: {EmailLayout.FormatMoney(revisedTotal.Value)}.")
            : "";
        var body = EmailLayout.Paragraph($"Olá{GreetName(order.CustomerName)},")
            + EmailLayout.Paragraph($"A florista enviou uma proposta de alteração à encomenda #{EmailLayout.OrderRef(order.Id)}:")
            + EmailLayout.Paragraph(message)
            + extra
            + EmailLayout.Paragraph("Responda na área de encomendas da loja para aceitar ou recusar.");
        return ($"Proposta de alteração — #{EmailLayout.OrderRef(order.Id)}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) OrderRejected(Tenant tenant, OrderEmailView order, string reason)
    {
        var title = "Encomenda recusada";
        var body = EmailLayout.Paragraph($"Olá{GreetName(order.CustomerName)},")
            + EmailLayout.Paragraph($"Lamentamos, mas a encomenda #{EmailLayout.OrderRef(order.Id)} foi recusada.")
            + EmailLayout.Paragraph($"Motivo: {reason}");
        return ($"Encomenda recusada — #{EmailLayout.OrderRef(order.Id)}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) PaymentReceived(Tenant tenant, OrderEmailView order)
    {
        var title = "Pagamento recebido";
        var body = EmailLayout.Paragraph($"Olá{GreetName(order.CustomerName)},")
            + EmailLayout.Paragraph($"Registámos o pagamento da encomenda #{EmailLayout.OrderRef(order.Id)} ({EmailLayout.FormatMoney(order.EstimatedTotal)}).")
            + EmailLayout.Paragraph("A florista irá preparar o arranjo em breve.")
            + EmailLayout.TrackingBlock(order);
        return ($"Pagamento recebido — #{EmailLayout.OrderRef(order.Id)}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) PaymentReceivedFlorist(Tenant tenant, OrderEmailView order)
    {
        var title = "Pagamento recebido";
        var body = EmailLayout.Paragraph($"A encomenda #{EmailLayout.OrderRef(order.Id)} foi paga ({EmailLayout.FormatMoney(order.EstimatedTotal)}).")
            + EmailLayout.Paragraph("O valor, menos a comissão da plataforma, segue para a conta Stripe da loja.");
        return ($"Pagamento recebido — #{EmailLayout.OrderRef(order.Id)}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) OrderInPreparation(Tenant tenant, OrderEmailView order)
    {
        var title = "Encomenda em preparação";
        var body = EmailLayout.Paragraph($"Olá{GreetName(order.CustomerName)},")
            + EmailLayout.Paragraph($"A encomenda #{EmailLayout.OrderRef(order.Id)} está em preparação.")
            + EmailLayout.Paragraph("Avisaremos quando estiver pronta para entrega ou levantamento.")
            + EmailLayout.TrackingBlock(order);
        return ($"Em preparação — #{EmailLayout.OrderRef(order.Id)}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) OrderReady(Tenant tenant, OrderEmailView order)
    {
        var title = order.DeliveryLabel.Contains("levantamento", StringComparison.OrdinalIgnoreCase)
            ? "Pronta para levantamento"
            : "Pronta para entrega";
        var body = EmailLayout.Paragraph($"Olá{GreetName(order.CustomerName)},")
            + EmailLayout.Paragraph($"A encomenda #{EmailLayout.OrderRef(order.Id)} está pronta.")
            + EmailLayout.Paragraph($"Modalidade: {order.DeliveryLabel}.")
            + (string.IsNullOrWhiteSpace(order.DeliveryAddress)
                ? ""
                : EmailLayout.Paragraph($"Morada: {order.DeliveryAddress}."))
            + EmailLayout.TrackingBlock(order);
        return ($"Encomenda pronta — #{EmailLayout.OrderRef(order.Id)}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) OrderDelivered(Tenant tenant, OrderEmailView order)
    {
        var title = "Encomenda entregue";
        var body = EmailLayout.Paragraph($"Olá{GreetName(order.CustomerName)},")
            + EmailLayout.Paragraph($"A encomenda #{EmailLayout.OrderRef(order.Id)} foi marcada como entregue. Obrigado pela confiança!")
            + EmailLayout.TrackingBlock(order);
        return ($"Encomenda entregue — #{EmailLayout.OrderRef(order.Id)}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) OrderCancelled(Tenant tenant, OrderEmailView order, string? reason)
    {
        var title = "Encomenda cancelada";
        var reasonBlock = string.IsNullOrWhiteSpace(reason)
            ? ""
            : EmailLayout.Paragraph($"Nota: {reason}");
        var body = EmailLayout.Paragraph($"Olá{GreetName(order.CustomerName)},")
            + EmailLayout.Paragraph($"A encomenda #{EmailLayout.OrderRef(order.Id)} foi cancelada.")
            + reasonBlock;
        return ($"Encomenda cancelada — #{EmailLayout.OrderRef(order.Id)}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static (string Subject, string Html) LowStock(Tenant tenant, string flowerName, int quantity, int minimum)
    {
        var title = "Stock abaixo do mínimo";
        var body = EmailLayout.Paragraph($"A flor «{flowerName}» ficou com {quantity} unidades.")
            + EmailLayout.Paragraph($"O mínimo definido é {minimum}. Reponha o stock no catálogo da loja.");
        return ($"Stock baixo — {flowerName}", EmailLayout.Render(tenant.Name, tenant.LogoUrl, title, body));
    }

    public static OrderEmailView ToView(Order order, string? trackingUrl)
    {
        var delivery = DescribeDelivery(order);
        return new OrderEmailView(
            order.Id,
            order.Customer?.FullName,
            order.Customer?.Email,
            order.EstimatedTotal,
            order.Items.Select(i => new OrderItemEmailView(i.Name, i.Quantity, i.UnitPrice)).ToList(),
            delivery,
            order.DeliveryAddress,
            order.RequestedDeliveryAt,
            trackingUrl);
    }

    private static string GreetName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "" : $" {name}";

    private static string DescribeDelivery(Order order) => order.DeliveryType switch
    {
        DeliveryType.StorePickup => "Levantamento na loja",
        DeliveryType.HomeDelivery => "Entrega ao domicílio",
        DeliveryType.CemeteryOrOther => "Entrega em cemitério / outro local",
        _ => order.DeliveryType.ToString()
    };
}
