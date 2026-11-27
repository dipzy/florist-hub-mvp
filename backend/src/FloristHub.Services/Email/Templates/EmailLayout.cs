using System.Net;
using System.Text;

namespace FloristHub.Services.Email.Templates;

internal static class EmailLayout
{
    public static string Render(string tenantName, string? logoUrl, string title, string bodyHtml, string? footerNote = null)
    {
        var logoBlock = string.IsNullOrWhiteSpace(logoUrl)
            ? ""
            : $"""
               <tr>
                 <td align="center" style="padding:24px 24px 8px;">
                   <img src="{WebUtility.HtmlEncode(logoUrl)}" alt="" width="72" height="72" style="border-radius:12px;object-fit:cover;" />
                 </td>
               </tr>
               """;

        var footer = string.IsNullOrWhiteSpace(footerNote)
            ? $"<p style=\"margin:16px 0 0;font-size:13px;color:#6b7280;\">{WebUtility.HtmlEncode(tenantName)}</p>"
            : $"<p style=\"margin:16px 0 0;font-size:13px;color:#6b7280;\">{WebUtility.HtmlEncode(footerNote)}</p>";

        return $"""
            <!DOCTYPE html>
            <html lang="pt-PT">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>{WebUtility.HtmlEncode(title)}</title>
            </head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f3f4f6;padding:24px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#ffffff;border-radius:16px;box-shadow:0 1px 3px rgba(0,0,0,.08);">
                      {logoBlock}
                      <tr>
                        <td style="padding:24px 28px 8px;">
                          <p style="margin:0 0 4px;font-size:12px;letter-spacing:.06em;text-transform:uppercase;color:#9ca3af;">{WebUtility.HtmlEncode(tenantName)}</p>
                          <h1 style="margin:0;font-size:22px;line-height:1.3;color:#111827;">{WebUtility.HtmlEncode(title)}</h1>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:8px 28px 28px;font-size:16px;line-height:1.55;color:#374151;">
                          {bodyHtml}
                          {footer}
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    public static string Button(string href, string label) =>
        $"""
         <p style="margin:24px 0 0;text-align:center;">
           <a href="{WebUtility.HtmlEncode(href)}" style="display:inline-block;padding:12px 22px;background:#059669;color:#ffffff;text-decoration:none;border-radius:8px;font-weight:600;">
             {WebUtility.HtmlEncode(label)}
           </a>
         </p>
         """;

    public static string Paragraph(string text) =>
        $"<p style=\"margin:0 0 12px;\">{WebUtility.HtmlEncode(text)}</p>";

    public static string TrackingBlock(OrderEmailView order)
    {
        if (string.IsNullOrWhiteSpace(order.TrackingUrl))
            return "";
        return Paragraph("Pode acompanhar o estado da encomenda a qualquer momento:")
            + Button(order.TrackingUrl, "Acompanhar encomenda");
    }

    public static string OrderRef(Guid orderId) => orderId.ToString()[..8].ToUpperInvariant();

    public static string FormatMoney(decimal value) => value.ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("pt-PT")) + " €";

    public static string BuildOrderSummary(OrderEmailView order)
    {
        var sb = new StringBuilder();
        sb.Append("""
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:16px 0 0;border-collapse:collapse;font-size:14px;">
                  """);
        foreach (var item in order.Items)
        {
            sb.Append($"""
                       <tr>
                         <td style="padding:6px 0;border-bottom:1px solid #e5e7eb;">{WebUtility.HtmlEncode(item.Name)} × {item.Quantity}</td>
                         <td align="right" style="padding:6px 0;border-bottom:1px solid #e5e7eb;">{FormatMoney(item.LineTotal)}</td>
                       </tr>
                       """);
        }

        sb.Append($"""
                   <tr>
                     <td style="padding:10px 0 0;font-weight:600;">Total estimado</td>
                     <td align="right" style="padding:10px 0 0;font-weight:600;">{FormatMoney(order.EstimatedTotal)}</td>
                   </tr>
                   </table>
                   """);
        return sb.ToString();
    }
}

internal sealed record OrderEmailView(
    Guid Id,
    string? CustomerName,
    string? CustomerEmail,
    decimal EstimatedTotal,
    IReadOnlyList<OrderItemEmailView> Items,
    string DeliveryLabel,
    string? DeliveryAddress,
    DateTimeOffset? RequestedDeliveryAt,
    string? TrackingUrl = null);

internal sealed record OrderItemEmailView(string Name, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
