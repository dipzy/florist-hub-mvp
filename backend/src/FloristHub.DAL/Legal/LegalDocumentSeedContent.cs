namespace FloristHub.DAL.Legal;

/// <summary>Textos modelo (requerem revisão jurídica).</summary>
public static class LegalDocumentSeedContent
{
    public const string SeedVersion = "2026.1";

    public const string Disclaimer =
        "> **Aviso:** Este texto é um modelo gerado para a plataforma FloristHub e **deve ser revisto por advogado** antes de utilização em produção.";

    public static string TermsBody() =>
        $"""
        {Disclaimer}

        # Termos e Condições

        ## 1. Âmbito
        A plataforma FloristHub permite que floristas (lojas) ofereçam serviços de venda e entrega de arranjos florais a clientes. Cada loja opera como estabelecimento independente; os presentes termos regulam o uso da plataforma e a relação com a loja selecionada.

        ## 2. Conta e registo
        O utilizador compromete-se a fornecer dados verdadeiros e a manter a confidencialidade das credenciais. O registo implica aceitação integral destes termos e da Política de Privacidade na versão indicada no momento do registo.

        ## 3. Encomendas e pagamentos
        As encomendas estão sujeitas a confirmação pela florista. Os preços apresentados são estimativas até confirmação. Os pagamentos processados através de Stripe Connect podem incluir comissão da plataforma conforme divulgado à florista.

        ## 4. Cancelamentos
        As regras de cancelamento seguem o estado da encomenda e a legislação aplicável aos bens perecíveis.

        ## 5. Responsável pelo tratamento (loja)
        Os dados de identificação do responsável pelo tratamento associado à loja onde efetua o registo ou encomenda constam na secção **Responsável pelo tratamento** abaixo.

        ## 6. Lei aplicável
        Aplica-se a lei portuguesa. Foro competente: Portugal.
        """;

    public static string PrivacyBody() =>
        $"""
        {Disclaimer}

        # Política de Privacidade

        ## 1. Papéis
        - **FloristHub (plataforma):** responsável pelo tratamento dos dados necessários ao funcionamento técnico e multi-loja.
        - **Florista (loja):** responsável pelo tratamento dos dados dos clientes da respetiva loja no âmbito das encomendas.

        ## 2. Dados tratados
        Identificação e contacto (nome, email), dados de encomenda e entrega, registos de pagamento (referências Stripe), consentimentos e registo de auditoria.

        ## 3. Finalidades
        Execução de contratos de venda, cumprimento de obrigações legais (faturação), segurança, estatísticas agregadas e, com consentimento, marketing.

        ## 4. Conservação
        Dados de conta enquanto ativa; encomendas conservadas de forma anonimizada após pedido de eliminação, pelo período fiscal legal.

        ## 5. Direitos
        Pode solicitar acesso, retificação, portabilidade, oposição e eliminação/anonimização através das funcionalidades da conta ou contactando a loja.

        ## 6. Subcontratantes
        Alojamento, Stripe (pagamentos), email transacional e, com consentimento de cookies, ferramentas de monitorização de erros (ex. Sentry).
        """;

    public static string CookiesBody() =>
        $"""
        {Disclaimer}

        # Política de Cookies

        ## 1. Essenciais
        Cookies e armazenamento local necessários à sessão, autenticação e escolha de loja (tenant).

        ## 2. Opcionais (com consentimento)
        Monitorização de erros e desempenho (ex. Sentry) para melhorar a estabilidade da aplicação.

        ## 3. Gestão
        Pode aceitar ou recusar cookies não essenciais no banner apresentado na primeira visita. A escolha é guardada no seu dispositivo.

        ## 4. Mais informação
        Consulte a Política de Privacidade para detalhe sobre finalidades e direitos.
        """;
}
