# Notificações push

A florista recebe aviso de encomenda nova e de encomenda paga. O cliente recebe aviso quando a encomenda é confirmada, entra em preparação, fica pronta ou é entregue. O toque abre o ecrã dessa encomenda (`floristhub://encomenda/{id}`).

Não há estado «a caminho». O aviso correspondente é o de encomenda **pronta** (`Ready`): pronta para entrega ou para levantamento, como no resto da aplicação.

Web push no painel da florista fica para outro PR. A web não tem service worker, e o consentimento do browser cruza o trabalho de RGPD.

## O que o Diogo tem de criar

1. Um projeto em [Firebase console](https://console.firebase.google.com/) (plano Spark chega). Adicionar uma app Android com o package `com.floristhub.android`.
2. Descarregar `google-services.json` e colocá-lo em `mobile/androidApp/google-services.json`. Este ficheiro está no `.gitignore`. Não o commits.
3. No `mobile/androidApp/build.gradle.kts`, quando o ficheiro existir, aplicar o plugin (além das dependências que já estão no projecto):

```kotlin
plugins {
    id("com.google.gms.google-services") version "4.4.3"
}
```

Sem este ficheiro e sem o plugin, a app Android arranca na mesma. O registo do token é ignorado e o resto da aplicação funciona. O `FirebaseInitProvider` está removido de propósito, para a app não rebentar à procura de `google_app_id`.

4. Conta de serviço do Firebase com permissão para enviar mensagens (Firebase Cloud Messaging API, HTTP v1). Em IAM, o papel `Firebase Cloud Messaging API Admin` ou o papel de administrador do Firebase na conta de serviço. Guardar o JSON **fora do repositório**.

5. iOS, só quando existir conta Apple Developer paga e um Mac ou o workflow de simulador puder ser alargado:
   - Criar a app iOS no mesmo projeto Firebase (`pt.floristhub` / o bundle id do `iosApp`).
   - Em Apple Developer, criar uma chave APNs (Apple Push Notifications service) e carregá-la no Firebase (Cloud Messaging → Apple app configuration). Sem esta chave, o FCM não entrega no iOS.
   - Descarregar `GoogleService-Info.plist` para o `iosApp`. Também está no `.gitignore`.
   - No código Swift, depois de obter o token APNs, chamar `submitIosPushToken` do módulo partilhado. Esse gancho já existe. Sem chamada, o iOS não regista dispositivo.

## Onde ficam os segredos

Nunca no git. Em desenvolvimento e em produção (Render ou equivalente), variáveis de ambiente:

| Variável | Uso |
|----------|-----|
| `Push__Provider` | `Log` (defeito) ou `Fcm` |
| `Push__FcmProjectId` | Id do projeto Firebase |
| `Push__FcmServiceAccountJson` | JSON da conta de serviço, numa linha |
| `Push__FcmServiceAccountPath` | Caminho para o JSON, se for mais cómodo do que a variável |

Se `Push__Provider` é `Fcm` mas falta o projeto ou a conta de serviço, a API regista um aviso e usa o emissor de log. As encomendas e os pagamentos não falham por causa do push.

O `google-services.json` e o `GoogleService-Info.plist` ficam só na máquina ou no secret store do CI, nunca no commit.

## Comportamento

- `POST /api/notifications/devices` e `DELETE /api/notifications/devices` com `{ "token", "platform" }`. Plataformas: `Android` ou `Ios`. Florista ou Cliente, no tenant da sessão.
- O mesmo token só existe uma vez. Se o aparelho muda de conta ou de loja, o registo passa para a sessão nova.
- `GET` e `PUT /api/notifications/preferences` com `newOrder`, `orderPaid` e `orderStatus`. Sem linha gravada, os três ficam ligados.
- O envio é uma fila em memória, no mesmo processo da API. Falhas transitórias repetem-se. Um token `UNREGISTERED` ou inválido é apagado. Uma falha de push não desfaz a encomenda nem o pagamento.
- A florista avisada é quem tem a role Florista **nessa** loja e um dispositivo registado. O cliente avisado é o da encomenda, na mesma loja.

## Limitações no iOS sem conta Apple

O dono não tem Mac nem conta Apple Developer paga. O simulador do GitHub Actions não recebe push reais: a Apple não entrega APNs a um simulador sem a configuração de push, e sem a chave APNs o FCM também não tem por onde enviar. Este PR não altera o workflow manual de iOS e não o dispara.

O que fica pronto no iOS é o contrato: a API aceita plataforma `Ios`, a mensagem FCM inclui o bloco APNs, e `submitIosPushToken` guarda o token para o registo no próximo arranque com sessão. Até a chave APNs existir, a app iOS não mostra notificações.

## Testar sem Firebase

Deixar `Push__Provider=Log`. Criar uma encomenda ou mudar o estado. A API escreve uma linha `PUSH (dev)` com o tipo, a encomenda e o prefixo do token. Os testes automatizados usam um emissor falso e não chamam o Google.
